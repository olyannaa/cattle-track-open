#!/usr/bin/env python3
"""Summarize latency, GPU use and basic quality for model-selection smoke runs."""

from __future__ import annotations

import argparse
import glob
import json
import math
from pathlib import Path
from typing import Any


BACKEND_OWNED_KEYS = {"schema_version", "idempotency_key", "batch_idempotency_key"}


def read_jsonl(path: Path) -> list[dict[str, Any]]:
    with path.open(encoding="utf-8") as handle:
        return [json.loads(line) for line in handle if line.strip()]


def load_dataset(patterns: list[str]) -> dict[str, dict[str, Any]]:
    rows: dict[str, dict[str, Any]] = {}
    for pattern in patterns:
        for raw_path in sorted(glob.glob(pattern)):
            for row in read_jsonl(Path(raw_path)):
                rows[row["id"]] = row
    return rows


def canonical(value: Any) -> Any:
    if isinstance(value, dict):
        return {key: canonical(item) for key, item in sorted(value.items()) if key not in BACKEND_OWNED_KEYS}
    if isinstance(value, list):
        return [canonical(item) for item in value]
    return value


def normalize_calls(value: Any) -> list[dict[str, Any]]:
    if isinstance(value, dict):
        value = value.get("tool_calls", [])
    if not isinstance(value, list):
        return []
    result: list[dict[str, Any]] = []
    for call in value:
        if not isinstance(call, dict):
            continue
        function = call.get("function") if isinstance(call.get("function"), dict) else call
        name = function.get("name")
        arguments = function.get("arguments", {})
        if isinstance(arguments, str):
            try:
                arguments = json.loads(arguments)
            except json.JSONDecodeError:
                arguments = {"__raw_arguments": arguments}
        result.append({"name": name, "arguments": canonical(arguments if isinstance(arguments, dict) else {})})
    return result


def percentile(values: list[float], quantile: float) -> float | None:
    if not values:
        return None
    ordered = sorted(values)
    rank = (len(ordered) - 1) * quantile
    low = math.floor(rank)
    high = math.ceil(rank)
    if low == high:
        return round(ordered[low], 2)
    fraction = rank - low
    return round(ordered[low] * (1 - fraction) + ordered[high] * fraction, 2)


def summarize(model: str, path: Path, dataset: dict[str, dict[str, Any]]) -> dict[str, Any]:
    predictions = [row for row in read_jsonl(path) if int(row.get("sample_index", 0)) == 0]
    latency = [float(row["latency_ms"]) for row in predictions if row.get("latency_ms") is not None]
    ttft = [float(row["ttft_ms"]) for row in predictions if row.get("ttft_ms") is not None]
    gpu_peak = [int(row["gpu_memory_peak_mb"]) for row in predictions if row.get("gpu_memory_peak_mb") is not None]
    errors = sum(1 for row in predictions if row.get("error"))
    tool_selection = 0
    argument_exact = 0
    evaluated = 0
    for prediction in predictions:
        source = dataset.get(prediction.get("id"))
        if source is None:
            continue
        gold = normalize_calls(source["golden"]["tool_calls"])
        pred = normalize_calls(prediction.get("tool_calls"))
        evaluated += 1
        tool_selection += int([call["name"] for call in gold] == [call["name"] for call in pred])
        argument_exact += int(gold == pred)
    return {
        "model": model,
        "predictions": len(predictions),
        "errors": errors,
        "evaluated": evaluated,
        "tool_selection_accuracy": round(tool_selection / evaluated, 4) if evaluated else None,
        "argument_exact_match": round(argument_exact / evaluated, 4) if evaluated else None,
        "latency_ms": {"p50": percentile(latency, 0.5), "p95": percentile(latency, 0.95), "max": max(latency) if latency else None},
        "ttft_ms": {"p50": percentile(ttft, 0.5), "p95": percentile(ttft, 0.95), "max": max(ttft) if ttft else None},
        "gpu_memory_peak_mb": max(gpu_peak) if gpu_peak else None,
        "source": str(path),
    }


def render_markdown(report: dict[str, Any]) -> str:
    lines = [
        "# Model-selection smoke summary",
        "",
        "Предварительный отсев. Эти результаты не заменяют полный benchmark и ручную валидацию multi-turn набора.",
        "",
        "| Model | Cases | Errors | Tool selection | Argument exact | TTFT p50/p95 | Total p50/p95 | Peak VRAM |",
        "|---|---:|---:|---:|---:|---:|---:|---:|",
    ]
    for item in report["models"]:
        selection = item["tool_selection_accuracy"]
        exact = item["argument_exact_match"]
        selection_text = f"{selection * 100:.1f}%" if selection is not None else "n/a"
        exact_text = f"{exact * 100:.1f}%" if exact is not None else "n/a"
        ttft = item["ttft_ms"]
        latency = item["latency_ms"]
        ttft_text = f"{ttft['p50']}/{ttft['p95']} ms" if ttft["p50"] is not None else "n/a"
        latency_text = f"{latency['p50']}/{latency['p95']} ms" if latency["p50"] is not None else "n/a"
        gpu_text = f"{item['gpu_memory_peak_mb']} MiB" if item["gpu_memory_peak_mb"] is not None else "n/a"
        lines.append(
            f"| `{item['model']}` | {item['evaluated']} | {item['errors']} | {selection_text} | {exact_text} | {ttft_text} | {latency_text} | {gpu_text} |"
        )
    lines.append("")
    return "\n".join(lines)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dataset", action="append", required=True)
    parser.add_argument("--predictions", action="append", required=True, help="MODEL=PATH")
    parser.add_argument("--out-json", type=Path, required=True)
    parser.add_argument("--out-md", type=Path, required=True)
    args = parser.parse_args()

    dataset = load_dataset(args.dataset)
    models = []
    for value in args.predictions:
        model, separator, raw_path = value.partition("=")
        if not separator:
            raise SystemExit(f"--predictions must be MODEL=PATH, got: {value}")
        models.append(summarize(model, Path(raw_path), dataset))
    report = {"kind": "model_selection_smoke", "models": models}
    args.out_json.parent.mkdir(parents=True, exist_ok=True)
    args.out_json.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    args.out_md.write_text(render_markdown(report), encoding="utf-8")


if __name__ == "__main__":
    main()
