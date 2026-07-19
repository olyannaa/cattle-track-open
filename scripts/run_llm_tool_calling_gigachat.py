#!/usr/bin/env python3
"""Run CattleTrack LLM tool-calling benchmark through GigaChat Chat Completions."""

from __future__ import annotations

import argparse
import json
import os
import ssl
import time
import uuid
import urllib.parse
import urllib.error
import urllib.request
from pathlib import Path

from run_llm_tool_calling_openai_compatible import (
    DEFAULT_SYSTEM_PROMPT,
    RESOLVER_AWARE_PROMPT,
    extract_tool_calls,
    make_resolver_aware_tools,
    read_jsonl_paths,
)


def load_gigachat_functions(schema_dir: Path) -> list[dict]:
    functions: list[dict] = []
    common_defs = json.loads((schema_dir.parent / "common" / "defs.schema.json").read_text(encoding="utf-8"))

    for path in sorted(schema_dir.glob("*.args.schema.json")):
        name = path.name.replace(".args.schema.json", "")
        schema = json.loads(path.read_text(encoding="utf-8"))
        parameters = simplify_schema(schema, schema, common_defs)
        functions.append(
            {
                "name": name,
                "description": schema.get("description") or schema.get("title") or name,
                "parameters": parameters,
            }
        )
    return functions


def make_resolver_aware_functions(functions: list[dict]) -> list[dict]:
    tools = [{"type": "function", "function": function} for function in functions]
    return [tool["function"] for tool in make_resolver_aware_tools(tools)]


def simplify_schema(value: object, root: dict, common_defs: dict) -> object:
    if isinstance(value, list):
        return [simplify_schema(item, root, common_defs) for item in value]
    if not isinstance(value, dict):
        return value

    ref = value.get("$ref")
    if isinstance(ref, str):
        target = resolve_ref(ref, root, common_defs)
        merged = {**target, **{key: val for key, val in value.items() if key != "$ref"}}
        return simplify_schema(merged, root, common_defs)

    result: dict = {}
    for key, item in value.items():
        if key.startswith("x-") or key in {"$schema", "$id", "$defs", "examples", "x-invalid-examples"}:
            continue
        if key == "format" and item not in {"date", "date-time", "time"}:
            continue
        if key == "const":
            result["enum"] = [item]
            continue
        result[key] = simplify_schema(item, root, common_defs)
    return result


def resolve_ref(ref: str, root: dict, common_defs: dict) -> dict:
    if ref.startswith("#/"):
        return walk_ref(root, ref[2:])
    if ref.startswith("../common/defs.schema.json#/"):
        return walk_ref(common_defs, ref.split("#/", 1)[1])
    raise ValueError(f"unsupported schema ref: {ref}")


def walk_ref(document: dict, path: str) -> dict:
    current: object = document
    for part in path.split("/"):
        if not isinstance(current, dict):
            raise ValueError(f"schema ref points through non-object: {path}")
        current = current[part]
    if not isinstance(current, dict):
        raise ValueError(f"schema ref does not point to object: {path}")
    return current


def request_access_token(auth_url: str, auth_key: str, scope: str, timeout: int, verify_ssl: bool) -> str:
    context = None if verify_ssl else ssl._create_unverified_context()
    body = urllib.parse.urlencode({"scope": scope}).encode("utf-8")
    request = urllib.request.Request(
        auth_url,
        data=body,
        headers={
            "Content-Type": "application/x-www-form-urlencoded",
            "RqUID": str(uuid.uuid4()),
            "Authorization": f"Bearer {auth_key}",
        },
        method="POST",
    )
    try:
        with urllib.request.urlopen(request, timeout=timeout, context=context) as response:
            payload = json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        error_body = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"OAuth HTTP {exc.code}: {error_body}") from exc
    token = payload.get("access_token")
    if not token:
        raise RuntimeError(f"OAuth response without access_token: {payload}")
    return token


def post_json(url: str, payload: dict, token: str, timeout: int, verify_ssl: bool) -> dict:
    context = None if verify_ssl else ssl._create_unverified_context()
    request = urllib.request.Request(
        url,
        data=json.dumps(payload, ensure_ascii=False).encode("utf-8"),
        headers={"Content-Type": "application/json", "Authorization": f"Bearer {token}"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(request, timeout=timeout, context=context) as response:
            return json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        error_body = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"HTTP {exc.code}: {error_body}") from exc


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-url", default=os.getenv("GIGACHAT_BASE_URL", "https://gigachat.devices.sberbank.ru/api/v1"))
    parser.add_argument("--access-token", default=os.getenv("GIGACHAT_ACCESS_TOKEN"))
    parser.add_argument("--auth-key", default=os.getenv("GIGACHAT_AUTH_KEY"))
    parser.add_argument("--auth-url", default=os.getenv("GIGACHAT_AUTH_URL", "https://ngw.devices.sberbank.ru:9443/api/v2/oauth"))
    parser.add_argument("--scope", default=os.getenv("GIGACHAT_SCOPE", "GIGACHAT_API_PERS"))
    parser.add_argument("--model", default=os.getenv("GIGACHAT_MODEL", "GigaChat"))
    parser.add_argument("--dataset", action="append", default=[])
    parser.add_argument("--schema-dir", type=Path, default=Path("ai-contracts/schemas/v1/tools"))
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--samples", type=int, default=3)
    parser.add_argument("--split", action="append", choices=["train", "dev", "test"])
    parser.add_argument("--resolver-aware-tools", action="store_true")
    parser.add_argument("--timeout", type=int, default=180)
    parser.add_argument("--limit", type=int)
    parser.add_argument("--no-verify-ssl", action="store_true")
    args = parser.parse_args()

    access_token = args.access_token
    if not access_token:
        if not args.auth_key:
            raise SystemExit("Set GIGACHAT_AUTH_KEY or GIGACHAT_ACCESS_TOKEN. Do not commit tokens.")
        access_token = request_access_token(args.auth_url, args.auth_key, args.scope, args.timeout, not args.no_verify_ssl)

    dataset_paths = args.dataset or ["datasets/tool_calling/*.jsonl", "datasets/fault_injection/*.jsonl"]
    rows = read_jsonl_paths(dataset_paths)
    if args.split:
        selected_splits = set(args.split)
        rows = [row for row in rows if row.get("split") in selected_splits]
    if args.limit:
        rows = rows[: args.limit]
    functions = load_gigachat_functions(args.schema_dir)
    if args.resolver_aware_tools:
        functions = make_resolver_aware_functions(functions)
    url = args.base_url.rstrip("/") + "/chat/completions"
    system_prompt = DEFAULT_SYSTEM_PROMPT + (RESOLVER_AWARE_PROMPT if args.resolver_aware_tools else "")

    args.out.parent.mkdir(parents=True, exist_ok=True)
    with args.out.open("w", encoding="utf-8") as output:
        for row in rows:
            for sample_index in range(args.samples):
                started = time.perf_counter()
                payload = {
                    "model": args.model,
                    "messages": [
                        {"role": "system", "content": system_prompt},
                        {"role": "user", "content": row["utterance"]},
                    ],
                    "functions": functions,
                    "function_call": "auto",
                    "temperature": 0,
                }
                try:
                    response = post_json(url, payload, access_token, args.timeout, not args.no_verify_ssl)
                    latency_ms = round((time.perf_counter() - started) * 1000, 2)
                    record = {
                        "id": row["id"],
                        "model": args.model,
                        "sample_index": sample_index,
                        "latency_ms": latency_ms,
                        "tool_calls": extract_tool_calls(response),
                        "raw_response": response,
                    }
                except Exception as exc:
                    record = {
                        "id": row["id"],
                        "model": args.model,
                        "sample_index": sample_index,
                        "error": str(exc),
                        "tool_calls": [],
                    }
                output.write(json.dumps(record, ensure_ascii=False) + "\n")
                output.flush()


if __name__ == "__main__":
    main()
