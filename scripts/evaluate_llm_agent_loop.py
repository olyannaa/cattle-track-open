#!/usr/bin/env python3
"""Evaluate terminal behavior of resolver-aware agent-loop benchmark runs."""

from __future__ import annotations

import argparse
import collections
import json
import math
from pathlib import Path
from typing import Any

from evaluate_llm_tool_calling import (
    load_dataset,
    load_predictions,
    normalize_tool_calls,
    resolver_aware_calls,
    wilson,
)


BACKEND_DEFAULT_KEYS = {"include_empty", "limit"}


def drop_backend_defaults(value: Any) -> Any:
    if isinstance(value, dict):
        return {key: drop_backend_defaults(item) for key, item in sorted(value.items()) if key not in BACKEND_DEFAULT_KEYS}
    if isinstance(value, list):
        return [drop_backend_defaults(item) for item in value]
    return value


def expected_terminal_calls(row: dict[str, Any]) -> list[dict[str, Any]]:
    calls = normalize_tool_calls(row.get("golden", {}).get("tool_calls"))
    if row.get("stratum") == "multi-hop-read" and calls:
        return [calls[-1]]
    return calls


def predicted_terminal_calls(prediction: dict[str, Any]) -> list[dict[str, Any]]:
    return normalize_tool_calls(prediction.get("terminal_tool_calls"))


def write_shape_valid(calls: list[dict[str, Any]]) -> bool:
    for call in calls:
        name = call.get("name")
        args = call.get("arguments", {})
        if name == "create_weight":
            if not all(key in args for key in ("tag", "weight", "date", "method")):
                return False
            if not isinstance(args.get("weight"), (int, float)) or args["weight"] <= 0:
                return False
        elif name in {"create_daily_action", "create_insemination"}:
            items = args.get("items")
            if not isinstance(items, list) or not items:
                return False
            for item in items:
                if not isinstance(item, dict) or "date" not in item:
                    return False
                if name == "create_daily_action" and not all(key in item for key in ("tag", "type")):
                    return False
                if name == "create_insemination" and not all(
                    key in item for key in ("cow_tags", "insemination_type")
                ):
                    return False
    return True


def semantic_equal(gold: list[dict[str, Any]], pred: list[dict[str, Any]]) -> bool:
    if [item.get("name") for item in gold] != [item.get("name") for item in pred]:
        return False
    if not write_shape_valid(pred):
        return False
    gold_args = drop_backend_defaults(resolver_aware_calls(gold))
    pred_args = drop_backend_defaults(resolver_aware_calls(pred))
    return gold_args == pred_args


def percentile(values: list[float], quantile: float) -> float | None:
    if not values:
        return None
    values = sorted(values)
    rank = (len(values) - 1) * quantile
    low = math.floor(rank)
    high = math.ceil(rank)
    if low == high:
        return round(values[low], 2)
    fraction = rank - low
    return round(values[low] * (1 - fraction) + values[high] * fraction, 2)


def metric(successes: int, total: int) -> dict[str, Any]:
    result = wilson(successes, total)
    if result["mean"] is not None:
        result["mean_pct"] = round(float(result["mean"]) * 100, 2)
        result["ci95_pct"] = [round(float(result["low"]) * 100, 2), round(float(result["high"]) * 100, 2)]
    return result


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dataset", action="append", default=[])
    parser.add_argument("--predictions", required=True, type=Path)
    parser.add_argument("--out-json", required=True, type=Path)
    parser.add_argument("--out-md", required=True, type=Path)
    parser.add_argument("--split", action="append", choices=["train", "dev", "test"])
    parser.add_argument("--pass-k", type=int, default=1)
    args = parser.parse_args()

    dataset = load_dataset(args.dataset or ["datasets/tool_calling/*.jsonl"], set(args.split) if args.split else None)
    predictions = load_predictions(args.predictions)
    by_stratum: dict[str, collections.Counter[str]] = collections.defaultdict(collections.Counter)
    totals = collections.Counter()
    successes = collections.Counter()
    latency: list[float] = []
    ttft: list[float] = []
    iterations: list[float] = []
    details: list[dict[str, Any]] = []

    for row_id, row in sorted(dataset.items()):
        samples = predictions.get(row_id, {})
        sample_results: list[bool] = []
        for sample_index in range(args.pass_k):
            prediction = samples.get(sample_index, {})
            gold = expected_terminal_calls(row)
            pred = predicted_terminal_calls(prediction)
            sample_results.append(semantic_equal(gold, pred))
        first = samples.get(0, {})
        gold = expected_terminal_calls(row)
        pred = predicted_terminal_calls(first)
        pass1 = sample_results[0] if sample_results else False
        passk = any(sample_results)
        path_calls = first.get("path_tool_calls", [])
        unnecessary = bool(not gold and path_calls)
        runtime_ok = not first.get("error") and first.get("stop_reason") not in {"duplicate_call", "iteration_limit"}
        stratum = row.get("stratum", "unknown")

        for scope in ("overall", f"stratum:{stratum}"):
            totals[(scope, "semantic_pass@1")] += 1
            successes[(scope, "semantic_pass@1")] += int(pass1)
            totals[(scope, f"semantic_pass^{args.pass_k}")] += 1
            successes[(scope, f"semantic_pass^{args.pass_k}")] += int(passk)
            totals[(scope, "runtime_success")] += 1
            successes[(scope, "runtime_success")] += int(runtime_ok)
            totals[(scope, "no_unnecessary_tool")] += 1
            successes[(scope, "no_unnecessary_tool")] += int(not unnecessary)

        if first.get("latency_ms") is not None:
            latency.append(float(first["latency_ms"]))
        if first.get("ttft_ms") is not None:
            ttft.append(float(first["ttft_ms"]))
        if first.get("iterations") is not None:
            iterations.append(float(first["iterations"]))

        error = "ok" if pass1 else "semantic_mismatch"
        if first.get("error"):
            error = "runtime_error"
        elif first.get("stop_reason") in {"duplicate_call", "iteration_limit"}:
            error = str(first.get("stop_reason"))
        by_stratum[stratum][error] += 1
        details.append(
            {
                "id": row_id,
                "stratum": stratum,
                "utterance": row.get("utterance"),
                "pass1": pass1,
                f"pass{args.pass_k}": passk,
                "stop_reason": first.get("stop_reason"),
                "gold_terminal_calls": drop_backend_defaults(resolver_aware_calls(gold)),
                "pred_terminal_calls": drop_backend_defaults(resolver_aware_calls(pred)),
                "path_tool_names": [item.get("name") for item in path_calls],
            }
        )

    metrics: dict[str, dict[str, Any]] = collections.defaultdict(dict)
    for (scope, name), total in sorted(totals.items()):
        metrics[scope][name] = metric(successes[(scope, name)], total)
    report = {
        "dataset_size": len(dataset),
        "prediction_samples": sum(len(value) for value in predictions.values()),
        "pass_k": args.pass_k,
        "metrics": metrics,
        "latency_ms": {"p50": percentile(latency, 0.5), "p95": percentile(latency, 0.95)},
        "ttft_ms": {"p50": percentile(ttft, 0.5), "p95": percentile(ttft, 0.95)},
        "iterations": {"p50": percentile(iterations, 0.5), "p95": percentile(iterations, 0.95)},
        "errors_by_stratum": {key: dict(value) for key, value in sorted(by_stratum.items())},
        "details": details,
    }
    args.out_json.parent.mkdir(parents=True, exist_ok=True)
    args.out_json.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    overall = metrics["overall"]
    lines = [
        "# Resolver-aware agent-loop evaluation",
        "",
        f"Dataset examples: {len(dataset)}",
        f"Prediction samples: {report['prediction_samples']}",
        "",
        "| Metric | Success/Total | Mean | 95% Wilson CI |",
        "|---|---:|---:|---:|",
    ]
    for name, value in overall.items():
        mean = "n/a" if value["mean"] is None else f"{value['mean_pct']:.2f}%"
        ci = "n/a" if value["mean"] is None else f"{value['ci95_pct'][0]:.2f}% - {value['ci95_pct'][1]:.2f}%"
        lines.append(f"| {name} | {value['successes']}/{value['total']} | {mean} | {ci} |")
    lines.extend(
        [
            "",
            f"TTFT p50/p95: {report['ttft_ms']['p50']}/{report['ttft_ms']['p95']} ms.",
            f"Total latency p50/p95: {report['latency_ms']['p50']}/{report['latency_ms']['p95']} ms.",
            f"Iterations p50/p95: {report['iterations']['p50']}/{report['iterations']['p95']}.",
            "",
        ]
    )
    args.out_md.write_text("\n".join(lines), encoding="utf-8")


if __name__ == "__main__":
    main()
