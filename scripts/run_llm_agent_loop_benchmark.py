#!/usr/bin/env python3
"""Run a bounded resolver-aware agent loop against an OpenAI-compatible LLM."""

from __future__ import annotations

import argparse
import json
import os
import time
from pathlib import Path
from typing import Any

from run_llm_tool_calling_openai_compatible import (
    build_payload,
    extract_free_json_tool_calls,
    extract_tool_calls,
    load_tools_simplified,
    make_resolver_aware_tools,
    post_json_stream,
    read_jsonl_paths,
    strip_reasoning,
)


def normalized_call(call: dict[str, Any]) -> dict[str, Any]:
    function = call.get("function", call)
    arguments = function.get("arguments", {})
    if isinstance(arguments, str):
        try:
            arguments = json.loads(arguments)
        except json.JSONDecodeError:
            arguments = {"__raw_arguments": arguments}
    return {
        "id": call.get("id"),
        "name": function.get("name"),
        "arguments": arguments if isinstance(arguments, dict) else {},
    }


def call_signature(call: dict[str, Any]) -> str:
    return json.dumps(
        {"name": call.get("name"), "arguments": call.get("arguments", {})},
        ensure_ascii=False,
        sort_keys=True,
    )


def mock_find_result(call: dict[str, Any]) -> dict[str, Any]:
    tag = str(call.get("arguments", {}).get("tag", "")).strip()
    return {
        "status": "resolved",
        "entity": {
            "kind": "animal",
            "id": f"benchmark-animal-{tag}",
            "tag": tag,
            "organization_scope": "benchmark",
        },
    }


def expected_terminal_names(row: dict[str, Any]) -> list[str]:
    calls = row.get("golden", {}).get("tool_calls", [])
    if not calls:
        return []
    if row.get("stratum") == "multi-hop-read":
        return [calls[-1].get("name")]
    return [call.get("name") for call in calls]


def run_loop(
    row: dict[str, Any],
    *,
    url: str,
    headers: dict[str, str],
    model: str,
    tools: list[dict[str, Any]],
    temperature: float,
    max_tokens: int,
    timeout: int,
    max_iterations: int,
    reference_date: str,
    output_mode: str,
) -> dict[str, Any]:
    constrained = output_mode == "constrained-json"
    payload = build_payload(
        row,
        model,
        tools,
        "free-json" if constrained else "native-tools",
        temperature,
        True,
        max_tokens,
        True,
        "openai-json-schema" if constrained else None,
        reference_date,
    )
    expected_names = expected_terminal_names(row)
    path: list[dict[str, Any]] = []
    terminal_calls: list[dict[str, Any]] = []
    seen: set[str] = set()
    first_ttft_ms: float | None = None
    final_content = ""
    stop_reason = "iteration_limit"
    started = time.perf_counter()

    for iteration in range(1, max_iterations + 1):
        response, ttft_ms = post_json_stream(url, payload, headers, timeout)
        if first_ttft_ms is None:
            first_ttft_ms = ttft_ms
        message = response.get("choices", [{}])[0].get("message", {})
        final_content = str(message.get("content") or "")
        extracted = extract_free_json_tool_calls(response) if constrained else extract_tool_calls(response)
        calls = [normalized_call(call) for call in extracted]
        for call in calls:
            path.append({"iteration": iteration, **call})

        if not calls:
            stop_reason = "final_answer"
            break

        signatures = [call_signature(call) for call in calls]
        if any(signature in seen for signature in signatures):
            stop_reason = "duplicate_call"
            break
        seen.update(signatures)

        terminal = [call for call in calls if call.get("name") != "find_animal"]
        if terminal or expected_names == ["find_animal"]:
            terminal_calls = terminal or calls
            stop_reason = "terminal_tool"
            break

        payload["messages"].append(message)
        for index, call in enumerate(calls):
            result = json.dumps(mock_find_result(call), ensure_ascii=False)
            if constrained:
                payload["messages"].append(
                    {
                        "role": "user",
                        "content": f"Результат find_animal: {result}\nПродолжи исходный запрос.",
                    }
                )
            else:
                call_id = call.get("id") or f"benchmark-call-{iteration}-{index}"
                payload["messages"].append(
                    {
                        "role": "tool",
                        "tool_call_id": call_id,
                        "content": result,
                    }
                )

    return {
        "latency_ms": round((time.perf_counter() - started) * 1000, 2),
        "ttft_ms": first_ttft_ms,
        "iterations": max((item["iteration"] for item in path), default=1),
        "stop_reason": stop_reason,
        "path_tool_calls": path,
        "terminal_tool_calls": terminal_calls,
        "final_content": final_content,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-url", default=os.getenv("OPENAI_COMPAT_BASE_URL", "http://localhost:8000/v1"))
    parser.add_argument("--api-key", default=os.getenv("OPENAI_COMPAT_API_KEY", "EMPTY"))
    parser.add_argument("--model", required=True)
    parser.add_argument("--dataset", action="append", default=[])
    parser.add_argument("--schema-dir", type=Path, default=Path("ai-contracts/schemas/v1/tools"))
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--samples", type=int, default=1)
    parser.add_argument("--split", action="append", choices=["train", "dev", "test"])
    parser.add_argument("--limit", type=int)
    parser.add_argument("--temperature", type=float, default=0.0)
    parser.add_argument("--max-tokens", type=int, default=256)
    parser.add_argument("--max-iterations", type=int, default=8)
    parser.add_argument("--timeout", type=int, default=180)
    parser.add_argument("--reference-date", required=True)
    parser.add_argument("--output-mode", choices=["native-tools", "constrained-json"], default="native-tools")
    args = parser.parse_args()

    rows = read_jsonl_paths(args.dataset or ["datasets/tool_calling/*.jsonl"])
    if args.split:
        selected = set(args.split)
        rows = [row for row in rows if row.get("split") in selected]
    if args.limit:
        rows = rows[: args.limit]
    tools = make_resolver_aware_tools(load_tools_simplified(args.schema_dir))
    url = args.base_url.rstrip("/") + "/chat/completions"
    headers = {"Content-Type": "application/json", "Authorization": f"Bearer {args.api_key}"}

    args.out.parent.mkdir(parents=True, exist_ok=True)
    with args.out.open("w", encoding="utf-8") as output:
        for row in rows:
            for sample_index in range(args.samples):
                try:
                    result = run_loop(
                        row,
                        url=url,
                        headers=headers,
                        model=args.model,
                        tools=tools,
                        temperature=args.temperature,
                        max_tokens=args.max_tokens,
                        timeout=args.timeout,
                        max_iterations=args.max_iterations,
                        reference_date=args.reference_date,
                        output_mode=args.output_mode,
                    )
                    record = {"id": row["id"], "model": args.model, "sample_index": sample_index, **result}
                except Exception as exc:
                    record = {
                        "id": row["id"],
                        "model": args.model,
                        "sample_index": sample_index,
                        "error": str(exc),
                        "path_tool_calls": [],
                        "terminal_tool_calls": [],
                    }
                output.write(json.dumps(strip_reasoning(record), ensure_ascii=False) + "\n")
                output.flush()


if __name__ == "__main__":
    main()
