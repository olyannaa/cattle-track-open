#!/usr/bin/env python3
"""Compare free and constrained tool-call JSON for structural reliability."""

from __future__ import annotations

import argparse
import json
import statistics
from pathlib import Path
from typing import Any

from evaluate_constrained_validator_ablation import schema_errors_for_call, wilson
from evaluate_llm_tool_calling import normalize_tool_calls, read_jsonl


def percentile(values: list[float], fraction: float) -> float | None:
    if not values:
        return None
    ordered = sorted(values)
    return ordered[min(len(ordered) - 1, max(0, int(len(ordered) * fraction + 0.999999) - 1))]


def evaluate_arm(name: str, path: Path, dataset: dict[str, dict[str, Any]]) -> dict[str, Any]:
    predictions = {row["id"]: row for row in read_jsonl(path) if int(row.get("sample_index", 0)) == 0}
    parse_success = 0
    structural_success = 0
    latencies = []
    details = []
    for row_id, source in dataset.items():
        prediction = predictions.get(row_id, {})
        content = (
            prediction.get("raw_response", {})
            .get("choices", [{}])[0]
            .get("message", {})
            .get("content", "")
        )
        parsed = False
        try:
            value = json.loads(content)
            parsed = isinstance(value, dict) and isinstance(value.get("tool_calls"), list)
        except (json.JSONDecodeError, TypeError):
            pass
        parse_success += int(parsed)
        calls = normalize_tool_calls(prediction.get("tool_calls"))
        schema_errors = sorted({error for call in calls for error in schema_errors_for_call(call)})
        expects_calls = bool(source.get("golden", {}).get("tool_calls"))
        structurally_valid = parsed and not schema_errors and (bool(calls) == expects_calls)
        structural_success += int(structurally_valid)
        if isinstance(prediction.get("latency_ms"), (int, float)):
            latencies.append(float(prediction["latency_ms"]))
        details.append({
            "id": row_id,
            "parsed": parsed,
            "expects_calls": expects_calls,
            "predicted_calls": len(calls),
            "schema_errors": schema_errors,
            "structural_success": structurally_valid,
            "finish_reason": prediction.get("raw_response", {}).get("choices", [{}])[0].get("finish_reason"),
            "error": prediction.get("error"),
        })
    total = len(dataset)
    return {
        "name": name,
        "predictions": str(path),
        "rows": total,
        "parse_success": wilson(parse_success, total),
        "structural_success": wilson(structural_success, total),
        "latency_ms": {
            "mean": statistics.mean(latencies) if latencies else None,
            "median": statistics.median(latencies) if latencies else None,
            "p95": percentile(latencies, 0.95),
            "max": max(latencies) if latencies else None,
        },
        "details": details,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dataset", action="append", required=True, type=Path)
    parser.add_argument("--arm", action="append", required=True, help="NAME=predictions.jsonl")
    parser.add_argument("--out-json", required=True, type=Path)
    parser.add_argument("--out-md", required=True, type=Path)
    args = parser.parse_args()

    dataset = {row["id"]: row for path in args.dataset for row in read_jsonl(path)}
    arms = []
    for raw in args.arm:
        name, path = raw.split("=", 1)
        arms.append(evaluate_arm(name, Path(path), dataset))
    report = {"dataset_rows": len(dataset), "arms": arms}
    args.out_json.parent.mkdir(parents=True, exist_ok=True)
    args.out_json.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    lines = [
        "# Structured Output Evaluation",
        "",
        f"Dataset rows: {len(dataset)}",
        "",
        "| Arm | JSON parse | Structural success | Mean latency | p95 latency |",
        "|---|---:|---:|---:|---:|",
    ]
    for arm in arms:
        parse = arm["parse_success"]
        structural = arm["structural_success"]
        latency = arm["latency_ms"]
        lines.append(
            f"| {arm['name']} | {parse['successes']}/{parse['total']} ({parse['mean_pct']:.2f}%) | "
            f"{structural['successes']}/{structural['total']} ({structural['mean_pct']:.2f}%) | "
            f"{latency['mean']:.2f} ms | {latency['p95']:.2f} ms |"
        )
    args.out_md.write_text("\n".join(lines) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
