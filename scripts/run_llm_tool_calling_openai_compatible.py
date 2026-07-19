#!/usr/bin/env python3
"""Run CattleTrack LLM tool-calling benchmark through an OpenAI-compatible API."""

from __future__ import annotations

import argparse
import glob
import json
import os
import subprocess
import threading
import time
import urllib.error
import urllib.request
from pathlib import Path
from typing import Any


DEFAULT_SYSTEM_PROMPT = """Ты AI-слой CattleTrack. Нужно выбрать только MVP-тулы из списка.
Верни только tool call. Не придумывай функций, заметок или действий вне схем.
Если подходящего MVP-тула нет, не вызывай инструменты.
Бирка ищется точным совпадением внутри организации. Если в запросе не хватает обязательных полей для write, выбери ближайший write-тул с теми аргументами, которые можно извлечь; backend-валидатор покажет уточнение.
"""

RESOLVER_AWARE_PROMPT = """\nResolver-aware режим: не угадывай внутренние UUID.
Если пользователь назвал животное, возвращай бирку/tag или массив cow_tags/bull_tags.
Если пользователь назвал группу, возвращай название группы в *_group_name.
Backend resolver потом отдельно превратит AnimalRef/GroupRef/MedicineRef в resolved/ambiguous/not_found.
Не генерируй schema_version, idempotency_key и batch_idempotency_key: backend добавляет их после валидации.
"""

BACKEND_OWNED_ARGUMENT_KEYS = {"schema_version", "idempotency_key", "batch_idempotency_key"}


def strip_reasoning(value: Any) -> Any:
    if isinstance(value, list):
        return [strip_reasoning(item) for item in value]
    if isinstance(value, dict):
        return {key: strip_reasoning(item) for key, item in value.items() if key != "reasoning"}
    return value


def read_jsonl_paths(patterns: list[str]) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    for pattern in patterns:
        for path in sorted(glob.glob(pattern)):
            with open(path, encoding="utf-8") as handle:
                for line in handle:
                    if line.strip():
                        rows.append(json.loads(line))
    return rows


def load_tools(schema_dir: Path) -> list[dict[str, Any]]:
    tools: list[dict[str, Any]] = []
    for path in sorted(schema_dir.glob("*.args.schema.json")):
        name = path.name.replace(".args.schema.json", "")
        schema = json.loads(path.read_text(encoding="utf-8"))
        tools.append(
            {
                "type": "function",
                "function": {
                    "name": name,
                    "description": schema.get("description") or schema.get("title") or name,
                    "parameters": schema,
                },
            }
        )
    return tools


def load_tools_simplified(schema_dir: Path) -> list[dict[str, Any]]:
    common_defs = json.loads((schema_dir.parent / "common" / "defs.schema.json").read_text(encoding="utf-8"))
    tools: list[dict[str, Any]] = []
    for path in sorted(schema_dir.glob("*.args.schema.json")):
        name = path.name.replace(".args.schema.json", "")
        schema = json.loads(path.read_text(encoding="utf-8"))
        parameters = simplify_schema(schema, schema, common_defs)
        tools.append(
            {
                "type": "function",
                "function": {
                    "name": name,
                    "description": schema.get("description") or schema.get("title") or name,
                    "parameters": parameters,
                },
            }
        )
    return tools


def make_resolver_aware_tools(tools: list[dict[str, Any]]) -> list[dict[str, Any]]:
    rewritten = json.loads(json.dumps(tools, ensure_ascii=False))
    for tool in rewritten:
        function = tool.get("function", {})
        name = function.get("name")
        parameters = function.get("parameters", {})
        if name in {"get_animal_card", "get_weight_history"}:
            properties = parameters.setdefault("properties", {})
            properties.pop("animal_id", None)
            properties["tag"] = {
                "type": "string",
                "minLength": 1,
                "maxLength": 64,
                "description": "Animal tag exactly as spoken by the user. Backend resolver resolves it inside OrganizationId.",
            }
            required = parameters.get("required", [])
            parameters["required"] = ["tag" if item == "animal_id" else item for item in required]
            function["description"] = f"{function.get('description', name)} Resolver-aware: provide tag, not animal_id."
        elif name == "create_daily_action":
            rewrite_group_ids_to_names(parameters)
            function["description"] = f"{function.get('description', name)} Resolver-aware: provide group names, not group ids."
        remove_backend_owned_arguments(parameters)
    return rewritten


def remove_backend_owned_arguments(value: object) -> object:
    if isinstance(value, list):
        for index, item in enumerate(value):
            value[index] = remove_backend_owned_arguments(item)
        return value
    if not isinstance(value, dict):
        return value

    properties = value.get("properties")
    if isinstance(properties, dict):
        for key in BACKEND_OWNED_ARGUMENT_KEYS:
            properties.pop(key, None)

    required = value.get("required")
    if isinstance(required, list):
        value["required"] = [key for key in required if key not in BACKEND_OWNED_ARGUMENT_KEYS]

    for key, item in list(value.items()):
        value[key] = remove_backend_owned_arguments(item)
    return value


def build_tool_calls_output_schema(tools: list[dict[str, Any]]) -> dict[str, Any]:
    variants: list[dict[str, Any]] = []
    for tool in tools:
        function = tool.get("function", {})
        name = function.get("name")
        parameters = function.get("parameters", {})
        if not name:
            continue
        variants.append(
            {
                "type": "object",
                "additionalProperties": False,
                "required": ["name", "arguments"],
                "properties": {
                    "name": {"const": name},
                    "arguments": parameters,
                },
            }
        )
    return {
        "type": "object",
        "additionalProperties": False,
        "required": ["tool_calls"],
        "properties": {
            "tool_calls": {
                "type": "array",
                "items": {"oneOf": variants},
            }
        },
    }


def rewrite_group_ids_to_names(value: object) -> object:
    if isinstance(value, list):
        for index, item in enumerate(value):
            value[index] = rewrite_group_ids_to_names(item)
        return value
    if not isinstance(value, dict):
        return value

    properties = value.get("properties")
    if isinstance(properties, dict):
        if "new_group_id" in properties:
            properties.pop("new_group_id")
            properties["new_group_name"] = {
                "type": "string",
                "minLength": 1,
                "maxLength": 255,
                "description": "Group name exactly as spoken by the user. Backend resolver resolves it inside OrganizationId.",
            }
        if "old_group_id" in properties:
            properties.pop("old_group_id")
            properties["old_group_name"] = {
                "type": "string",
                "minLength": 1,
                "maxLength": 255,
                "description": "Previous group name exactly as spoken by the user.",
            }

    required = value.get("required")
    if isinstance(required, list):
        value["required"] = [
            "new_group_name" if item == "new_group_id" else "old_group_name" if item == "old_group_id" else item
            for item in required
        ]

    for key, item in list(value.items()):
        value[key] = rewrite_group_ids_to_names(item)
    return value


def simplify_schema(value: object, root: dict[str, Any], common_defs: dict[str, Any]) -> object:
    if isinstance(value, list):
        return [simplify_schema(item, root, common_defs) for item in value]
    if not isinstance(value, dict):
        return value

    ref = value.get("$ref")
    if isinstance(ref, str):
        target = resolve_ref(ref, root, common_defs)
        merged = {**target, **{key: val for key, val in value.items() if key != "$ref"}}
        return simplify_schema(merged, root, common_defs)

    result: dict[str, Any] = {}
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


def resolve_ref(ref: str, root: dict[str, Any], common_defs: dict[str, Any]) -> dict[str, Any]:
    if ref.startswith("#/"):
        return walk_ref(root, ref[2:])
    if ref.startswith("../common/defs.schema.json#/"):
        return walk_ref(common_defs, ref.split("#/", 1)[1])
    raise ValueError(f"unsupported schema ref: {ref}")


def walk_ref(document: dict[str, Any], path: str) -> dict[str, Any]:
    current: object = document
    for part in path.split("/"):
        if not isinstance(current, dict):
            raise ValueError(f"schema ref points through non-object: {path}")
        current = current[part]
    if not isinstance(current, dict):
        raise ValueError(f"schema ref does not point to object: {path}")
    return current


def post_json(url: str, payload: dict[str, Any], headers: dict[str, str], timeout: int) -> dict[str, Any]:
    body = json.dumps(payload, ensure_ascii=False).encode("utf-8")
    request = urllib.request.Request(url, data=body, headers=headers, method="POST")
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            return json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        error_body = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"HTTP {exc.code}: {error_body}") from exc


def merge_streamed_tool_calls(target: list[dict[str, Any]], deltas: list[dict[str, Any]]) -> None:
    for fallback_index, delta in enumerate(deltas):
        index = int(delta.get("index", fallback_index))
        while len(target) <= index:
            target.append({"id": "", "type": "function", "function": {"name": "", "arguments": ""}})
        current = target[index]
        if delta.get("id"):
            current["id"] = delta["id"]
        function_delta = delta.get("function") or {}
        function = current["function"]
        function["name"] += function_delta.get("name") or ""
        function["arguments"] += function_delta.get("arguments") or ""


def post_json_stream(
    url: str,
    payload: dict[str, Any],
    headers: dict[str, str],
    timeout: int,
) -> tuple[dict[str, Any], float | None]:
    body = json.dumps({**payload, "stream": True}, ensure_ascii=False).encode("utf-8")
    request = urllib.request.Request(url, data=body, headers=headers, method="POST")
    started = time.perf_counter()
    first_token_at: float | None = None
    content_parts: list[str] = []
    tool_calls: list[dict[str, Any]] = []
    usage: dict[str, Any] | None = None
    finish_reason: str | None = None
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            for raw_line in response:
                line = raw_line.decode("utf-8", errors="replace").strip()
                if not line.startswith("data:"):
                    continue
                data = line[5:].strip()
                if not data or data == "[DONE]":
                    continue
                chunk = json.loads(data)
                if isinstance(chunk.get("usage"), dict):
                    usage = chunk["usage"]
                choices = chunk.get("choices") or []
                if not choices:
                    continue
                choice = choices[0]
                delta = choice.get("delta") or {}
                content_delta = delta.get("content") or ""
                tool_delta = delta.get("tool_calls") or []
                if first_token_at is None and (content_delta or tool_delta):
                    first_token_at = time.perf_counter()
                if content_delta:
                    content_parts.append(content_delta)
                if tool_delta:
                    merge_streamed_tool_calls(tool_calls, tool_delta)
                finish_reason = choice.get("finish_reason") or finish_reason
    except urllib.error.HTTPError as exc:
        error_body = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"HTTP {exc.code}: {error_body}") from exc

    response = {
        "choices": [
            {
                "finish_reason": finish_reason,
                "message": {
                    "role": "assistant",
                    "content": "".join(content_parts),
                    "tool_calls": tool_calls,
                },
            }
        ]
    }
    if usage is not None:
        response["usage"] = usage
    ttft_ms = round((first_token_at - started) * 1000, 2) if first_token_at is not None else None
    return response, ttft_ms


class GpuMemorySampler:
    def __init__(self, interval_sec: float = 0.05) -> None:
        self.interval_sec = interval_sec
        self.samples: list[int] = []
        self._stop = threading.Event()
        self._thread: threading.Thread | None = None

    @staticmethod
    def read_used_mb() -> int | None:
        try:
            result = subprocess.run(
                ["nvidia-smi", "--query-gpu=memory.used", "--format=csv,noheader,nounits"],
                check=True,
                capture_output=True,
                text=True,
                timeout=2,
            )
            first_gpu = result.stdout.strip().splitlines()[0]
            return int(first_gpu.strip())
        except (FileNotFoundError, IndexError, subprocess.SubprocessError, ValueError):
            return None

    def start(self) -> int | None:
        before = self.read_used_mb()
        if before is None:
            return None
        self.samples.append(before)
        self._thread = threading.Thread(target=self._poll, daemon=True)
        self._thread.start()
        return before

    def _poll(self) -> None:
        while not self._stop.wait(self.interval_sec):
            value = self.read_used_mb()
            if value is not None:
                self.samples.append(value)

    def stop(self) -> tuple[int | None, int | None]:
        self._stop.set()
        if self._thread is not None:
            self._thread.join(timeout=2)
        after = self.read_used_mb()
        if after is not None:
            self.samples.append(after)
        return (max(self.samples) if self.samples else None, after)


def extract_tool_calls(response: dict[str, Any]) -> list[dict[str, Any]]:
    choices = response.get("choices") or []
    if choices:
        message = choices[0].get("message", {})
        tool_calls = message.get("tool_calls")
        if tool_calls is not None:
            return tool_calls
        function_call = message.get("function_call")
        if isinstance(function_call, dict):
            return [{"function": function_call}]
        functions_state = message.get("functions_state_id")
        if functions_state:
            # GigaChat can return function_call in different envelope versions.
            # Keep the raw marker in predictions instead of silently inventing args.
            return []
        content = message.get("content")
        if isinstance(content, str):
            try:
                parsed = json.loads(content)
                if isinstance(parsed, dict):
                    return parsed.get("tool_calls", [])
                if isinstance(parsed, list):
                    return parsed
            except json.JSONDecodeError:
                return []
    return []


def extract_free_json_tool_calls(response: dict[str, Any]) -> list[dict[str, Any]]:
    choices = response.get("choices") or []
    if not choices:
        return []
    content = choices[0].get("message", {}).get("content")
    if not isinstance(content, str):
        return []
    text = content.strip()
    if text.startswith("```"):
        text = text.strip("`")
        if "\n" in text:
            text = text.split("\n", 1)[1]
    start = text.find("{")
    end = text.rfind("}")
    if start >= 0 and end > start:
        text = text[start : end + 1]
    try:
        parsed = json.loads(text)
    except json.JSONDecodeError:
        return []
    if isinstance(parsed, dict):
        calls = parsed.get("tool_calls", [])
    elif isinstance(parsed, list):
        calls = parsed
    else:
        return []
    if not isinstance(calls, list):
        return []
    normalized: list[dict[str, Any]] = []
    for call in calls:
        if not isinstance(call, dict):
            continue
        if "function" in call:
            normalized.append(call)
            continue
        name = call.get("name")
        arguments = call.get("arguments", {})
        normalized.append({"function": {"name": name, "arguments": json.dumps(arguments, ensure_ascii=False)}})
    return normalized


def build_payload(
    row: dict[str, Any],
    model: str,
    tools: list[dict[str, Any]],
    mode: str,
    temperature: float,
    no_think: bool,
    max_tokens: int | None,
    resolver_aware: bool,
    structured_output_backend: str | None,
    reference_date: str | None,
) -> dict[str, Any]:
    system_content = DEFAULT_SYSTEM_PROMPT + (RESOLVER_AWARE_PROMPT if resolver_aware else "")
    if reference_date:
        system_content += f"\nДата, относительно которой нужно понимать сегодня/вчера/завтра: {reference_date}."
    source_messages = row.get("messages")
    if source_messages:
        messages = [{"role": "system", "content": system_content}, *json.loads(json.dumps(source_messages))]
        if no_think:
            for message in reversed(messages):
                if message.get("role") == "user":
                    message["content"] += "\n/no_think"
                    break
    else:
        user_content = row["utterance"] + ("\n/no_think" if no_think else "")
        messages = [
            {"role": "system", "content": system_content},
            {"role": "user", "content": user_content},
        ]
    payload: dict[str, Any] = {
        "model": model,
        "messages": messages,
        "temperature": temperature,
    }
    if no_think:
        payload["reasoning_effort"] = "none"
    if max_tokens is not None:
        payload["max_tokens"] = max_tokens
    if mode == "native-tools":
        payload["tools"] = tools
        payload["tool_choice"] = "auto"
    elif mode == "json-prompt":
        payload["response_format"] = {"type": "json_object"}
        payload["messages"][0]["content"] += "\nВерни JSON строго вида: {\"tool_calls\":[{\"name\":\"...\",\"arguments\":{...}}]}."
        payload["messages"][0]["content"] += "\nДоступные схемы:\n" + json.dumps(tools, ensure_ascii=False)
    else:
        payload["messages"][0]["content"] += "\nВерни только JSON вида: {\"tool_calls\":[{\"name\":\"...\",\"arguments\":{...}}]}. Без markdown и пояснений."
        payload["messages"][0]["content"] += "\nЕсли tool не нужен, верни {\"tool_calls\":[]}."
        payload["messages"][0]["content"] += "\nДоступные схемы:\n" + json.dumps(tools, ensure_ascii=False)
    if structured_output_backend:
        output_schema = build_tool_calls_output_schema(tools)
        if structured_output_backend == "guided-json":
            payload["guided_json"] = output_schema
        elif structured_output_backend == "structured-outputs-json":
            payload["structured_outputs"] = {"json": output_schema}
        elif structured_output_backend == "openai-json-schema":
            payload["response_format"] = {
                "type": "json_schema",
                "json_schema": {
                    "name": "cattletrack_tool_calls",
                    "strict": True,
                    "schema": output_schema,
                },
            }
        else:
            raise ValueError(f"unknown structured output backend: {structured_output_backend}")
    return payload


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-url", default=os.getenv("OPENAI_COMPAT_BASE_URL", "http://localhost:8000/v1"))
    parser.add_argument("--api-key", default=os.getenv("OPENAI_COMPAT_API_KEY", "EMPTY"))
    parser.add_argument("--model", required=True)
    parser.add_argument("--dataset", action="append", default=[])
    parser.add_argument("--schema-dir", type=Path, default=Path("ai-contracts/schemas/v1/tools"))
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--samples", type=int, default=3)
    parser.add_argument("--split", action="append", choices=["train", "dev", "test"])
    parser.add_argument("--mode", choices=["native-tools", "json-prompt", "free-json"], default="native-tools")
    parser.add_argument("--simplify-schemas", action="store_true")
    parser.add_argument("--resolver-aware-tools", action="store_true")
    parser.add_argument(
        "--structured-output-backend",
        choices=["guided-json", "structured-outputs-json", "openai-json-schema"],
    )
    parser.add_argument("--no-think", action="store_true")
    parser.add_argument("--max-tokens", type=int)
    parser.add_argument("--temperature", type=float, default=0.0)
    parser.add_argument("--timeout", type=int, default=180)
    parser.add_argument("--limit", type=int)
    parser.add_argument("--stream", action="store_true", help="Measure TTFT using OpenAI-compatible SSE streaming.")
    parser.add_argument("--sample-gpu", action="store_true", help="Sample NVIDIA GPU memory during each request.")
    parser.add_argument("--reference-date", help="Fixed YYYY-MM-DD date for relative-date benchmark cases.")
    args = parser.parse_args()

    dataset_paths = args.dataset or ["datasets/tool_calling/*.jsonl", "datasets/fault_injection/*.jsonl"]
    rows = read_jsonl_paths(dataset_paths)
    if args.split:
        selected_splits = set(args.split)
        rows = [row for row in rows if row.get("split") in selected_splits]
    if args.limit:
        rows = rows[: args.limit]
    tools = load_tools_simplified(args.schema_dir) if args.simplify_schemas else load_tools(args.schema_dir)
    if args.resolver_aware_tools:
        tools = make_resolver_aware_tools(tools)
    url = args.base_url.rstrip("/") + "/chat/completions"
    headers = {"Content-Type": "application/json", "Authorization": f"Bearer {args.api_key}"}

    args.out.parent.mkdir(parents=True, exist_ok=True)
    with args.out.open("w", encoding="utf-8") as output:
        for row in rows:
            for sample_index in range(args.samples):
                started = time.perf_counter()
                gpu_sampler = GpuMemorySampler() if args.sample_gpu else None
                gpu_before_mb = gpu_sampler.start() if gpu_sampler else None
                try:
                    payload = build_payload(
                        row,
                        args.model,
                        tools,
                        args.mode,
                        args.temperature,
                        args.no_think,
                        args.max_tokens,
                        args.resolver_aware_tools,
                        args.structured_output_backend,
                        args.reference_date,
                    )
                    if args.stream:
                        response, ttft_ms = post_json_stream(url, payload, headers, args.timeout)
                    else:
                        response = post_json(url, payload, headers, args.timeout)
                        ttft_ms = None
                    latency_ms = round((time.perf_counter() - started) * 1000, 2)
                    gpu_peak_mb, gpu_after_mb = gpu_sampler.stop() if gpu_sampler else (None, None)
                    record = {
                        "id": row["id"],
                        "model": args.model,
                        "sample_index": sample_index,
                        "latency_ms": latency_ms,
                        "ttft_ms": ttft_ms,
                        "gpu_memory_before_mb": gpu_before_mb,
                        "gpu_memory_peak_mb": gpu_peak_mb,
                        "gpu_memory_after_mb": gpu_after_mb,
                        "tool_calls": extract_free_json_tool_calls(response) if args.mode == "free-json" else extract_tool_calls(response),
                        "raw_response": strip_reasoning(response),
                    }
                except Exception as exc:  # keep long benchmark runs resumable
                    gpu_peak_mb, gpu_after_mb = gpu_sampler.stop() if gpu_sampler else (None, None)
                    record = {
                        "id": row["id"],
                        "model": args.model,
                        "sample_index": sample_index,
                        "error": str(exc),
                        "gpu_memory_before_mb": gpu_before_mb,
                        "gpu_memory_peak_mb": gpu_peak_mb,
                        "gpu_memory_after_mb": gpu_after_mb,
                        "tool_calls": [],
                    }
                output.write(json.dumps(record, ensure_ascii=False) + "\n")
                output.flush()


if __name__ == "__main__":
    main()
