#!/usr/bin/env python3
"""Evaluate LLM tool-calling predictions against the approved CattleTrack dataset."""

from __future__ import annotations

import argparse
import collections
import glob
import json
import math
from pathlib import Path
from typing import Any


WRITE_STRATA = {"single-write", "batch-write"}
BATCH_STRATA = {"batch-write"}
IGNORED_RESOLVER_AWARE_KEYS = {"schema_version", "idempotency_key", "batch_idempotency_key"}

ANIMAL_ID_TO_TAG = {
    "11111111-1111-4111-8111-111111111432": "1432",
    "11111111-1111-4111-8111-111111110523": "523",
    "11111111-1111-4111-8111-111111110524": "524",
    "11111111-1111-4111-8111-111111110981": "981",
    "11111111-1111-4111-8111-111111110017": "A-17",
    "11111111-1111-4111-8111-111111110077": "77",
}

GROUP_ID_TO_NAME = {
    "22222222-2222-4222-8222-222222220001": "Основное стадо",
    "22222222-2222-4222-8222-222222220002": "Молодняк",
    "22222222-2222-4222-8222-222222220003": "Производители",
    "22222222-2222-4222-8222-222222220004": "Карантин",
}


def read_jsonl(path: Path) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    with path.open(encoding="utf-8") as handle:
        for line_no, line in enumerate(handle, 1):
            line = line.strip()
            if not line:
                continue
            try:
                rows.append(json.loads(line))
            except json.JSONDecodeError as exc:
                raise SystemExit(f"{path}:{line_no}: invalid JSON: {exc}") from exc
    return rows


def load_dataset(paths: list[str], selected_splits: set[str] | None = None) -> dict[str, dict[str, Any]]:
    rows: dict[str, dict[str, Any]] = {}
    expanded: list[str] = []
    for path in paths:
        matches = sorted(glob.glob(path))
        expanded.extend(matches or [path])
    for raw_path in expanded:
        path = Path(raw_path)
        for row in read_jsonl(path):
            if selected_splits and row.get("split") not in selected_splits:
                continue
            row_id = row["id"]
            if row_id in rows:
                raise SystemExit(f"duplicate dataset id: {row_id}")
            rows[row_id] = row
    return rows


def load_predictions(path: Path) -> dict[str, dict[int, dict[str, Any]]]:
    grouped: dict[str, dict[int, dict[str, Any]]] = collections.defaultdict(dict)
    for row in read_jsonl(path):
        row_id = row.get("id")
        if not row_id:
            raise SystemExit(f"{path}: prediction row without id")
        sample_index = int(row.get("sample_index", 0))
        if sample_index in grouped[row_id]:
            raise SystemExit(f"{path}: duplicate prediction for {row_id} sample {sample_index}")
        grouped[row_id][sample_index] = row
    return grouped


def canonical(value: Any) -> Any:
    if isinstance(value, dict):
        return {key: canonical(value[key]) for key in sorted(value)}
    if isinstance(value, list):
        return [canonical(item) for item in value]
    return value


def normalized_text(value: Any) -> str:
    return str(value).strip().casefold()


def animal_ref(tag: Any) -> dict[str, str]:
    return {"kind": "animal", "tag": str(tag).strip()}


def group_ref(name: Any) -> dict[str, str]:
    return {"kind": "group", "name": str(name).strip()}


def resolver_aware_value(value: Any, key_hint: str | None = None) -> Any:
    if isinstance(value, dict):
        return resolver_aware_arguments(value)
    if isinstance(value, list):
        return [resolver_aware_value(item, key_hint) for item in value]
    if key_hint in {"animal_id", "cow_id", "bull_id"}:
        return animal_ref(ANIMAL_ID_TO_TAG.get(str(value), value))
    if key_hint in {"tag"}:
        return animal_ref(value)
    if key_hint in {"new_group_id", "old_group_id", "group_id"}:
        return group_ref(GROUP_ID_TO_NAME.get(str(value), value))
    if key_hint in {"new_group_name", "old_group_name", "group_name"}:
        return group_ref(value)
    return value


def resolver_aware_arguments(args: dict[str, Any]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for key, value in args.items():
        if key in IGNORED_RESOLVER_AWARE_KEYS:
            continue
        if key == "animal_id":
            result["animal_ref"] = resolver_aware_value(value, key)
        elif key == "tag":
            result["animal_ref"] = resolver_aware_value(value, key)
        elif key == "cow_tags":
            result["cow_refs"] = [animal_ref(item) for item in value] if isinstance(value, list) else resolver_aware_value(value, key)
        elif key == "bull_tags":
            result["bull_refs"] = [animal_ref(item) for item in value] if isinstance(value, list) else resolver_aware_value(value, key)
        elif key in {"new_group_id", "new_group_name"}:
            result["new_group_ref"] = resolver_aware_value(value, key)
        elif key in {"old_group_id", "old_group_name"}:
            result["old_group_ref"] = resolver_aware_value(value, key)
        elif key in {"group_id", "group_name"}:
            result["group_ref"] = resolver_aware_value(value, key)
        elif key == "items" and isinstance(value, list):
            result[key] = [resolver_aware_arguments(item) if isinstance(item, dict) else item for item in value]
        else:
            result[key] = resolver_aware_value(value, key)
    return canonical(result)


def resolver_aware_calls(calls: list[dict[str, Any]]) -> list[dict[str, Any]]:
    return [
        {
            "name": call.get("name"),
            "arguments": resolver_aware_arguments(call.get("arguments", {})),
        }
        for call in calls
    ]


def normalize_tool_calls(value: Any) -> list[dict[str, Any]]:
    if value is None:
        return []
    if isinstance(value, dict) and "tool_calls" in value:
        value = value["tool_calls"]
    if not isinstance(value, list):
        return []

    calls: list[dict[str, Any]] = []
    for call in value:
        if not isinstance(call, dict):
            continue

        name = call.get("name")
        args = call.get("arguments", {})

        # OpenAI-compatible native tool call shape.
        if "function" in call and isinstance(call["function"], dict):
            function = call["function"]
            name = function.get("name", name)
            args = function.get("arguments", args)

        if isinstance(args, str):
            try:
                args = json.loads(args)
            except json.JSONDecodeError:
                args = {"__raw_arguments": args}

        calls.append({"name": name, "arguments": canonical(args if isinstance(args, dict) else {})})
    return calls


def names(calls: list[dict[str, Any]]) -> list[str | None]:
    return [call.get("name") for call in calls]


def exact_match(gold: list[dict[str, Any]], pred: list[dict[str, Any]]) -> bool:
    return canonical(gold) == canonical(pred)


def argument_exact(gold: list[dict[str, Any]], pred: list[dict[str, Any]]) -> bool:
    if names(gold) != names(pred) or len(gold) != len(pred):
        return False
    return all(canonical(g["arguments"]) == canonical(p["arguments"]) for g, p in zip(gold, pred))


def resolver_aware_argument_exact(gold: list[dict[str, Any]], pred: list[dict[str, Any]]) -> bool:
    if names(gold) != names(pred) or len(gold) != len(pred):
        return False
    return argument_exact(resolver_aware_calls(gold), resolver_aware_calls(pred))


def resolver_aware_exact_match(gold: list[dict[str, Any]], pred: list[dict[str, Any]]) -> bool:
    return exact_match(resolver_aware_calls(gold), resolver_aware_calls(pred))


def batch_item_counts(gold: list[dict[str, Any]], pred: list[dict[str, Any]]) -> tuple[int, int]:
    if len(gold) != 1 or len(pred) != 1:
        return 0, 0
    if gold[0].get("name") != pred[0].get("name"):
        return 0, len(gold[0].get("arguments", {}).get("items", []))

    gold_items = gold[0].get("arguments", {}).get("items", [])
    pred_items = pred[0].get("arguments", {}).get("items", [])
    if not isinstance(gold_items, list) or not isinstance(pred_items, list):
        return 0, len(gold_items) if isinstance(gold_items, list) else 0

    total = len(gold_items)
    matched = 0
    for index, gold_item in enumerate(gold_items):
        if index < len(pred_items) and canonical(gold_item) == canonical(pred_items[index]):
            matched += 1
    return matched, total


def resolver_aware_batch_item_counts(gold: list[dict[str, Any]], pred: list[dict[str, Any]]) -> tuple[int, int]:
    return batch_item_counts(resolver_aware_calls(gold), resolver_aware_calls(pred))


def wilson(successes: int, total: int, z: float = 1.96) -> dict[str, float | int | None]:
    if total == 0:
        return {"successes": successes, "total": total, "mean": None, "low": None, "high": None}
    phat = successes / total
    denom = 1 + z * z / total
    centre = phat + z * z / (2 * total)
    spread = z * math.sqrt((phat * (1 - phat) + z * z / (4 * total)) / total)
    return {
        "successes": successes,
        "total": total,
        "mean": phat,
        "low": (centre - spread) / denom,
        "high": (centre + spread) / denom,
    }


def metric_row(successes: int, total: int) -> dict[str, Any]:
    interval = wilson(successes, total)
    if interval["mean"] is None:
        return interval
    return {
        **interval,
        "mean_pct": round(float(interval["mean"]) * 100, 2),
        "ci95_pct": [
            round(float(interval["low"]) * 100, 2),
            round(float(interval["high"]) * 100, 2),
        ],
    }


def classify_error(gold: list[dict[str, Any]], pred: list[dict[str, Any]], stratum: str) -> str:
    if exact_match(gold, pred):
        return "ok"
    if not pred and gold:
        return "missing_tool"
    if pred and not gold:
        return "extra_tool_should_clarify_or_no_tool"
    if names(gold) != names(pred):
        return "wrong_tool_selection"
    if stratum in BATCH_STRATA:
        matched, total = batch_item_counts(gold, pred)
        if total and matched < total:
            return "batch_item_mismatch"
    return "argument_mismatch"


def evaluate(dataset: dict[str, dict[str, Any]], predictions: dict[str, dict[int, dict[str, Any]]], pass_k: int) -> dict[str, Any]:
    by_stratum: dict[str, list[str]] = collections.defaultdict(list)
    for row_id, row in dataset.items():
        by_stratum[row["stratum"]].append(row_id)

    details: list[dict[str, Any]] = []
    error_counts: dict[str, collections.Counter[str]] = collections.defaultdict(collections.Counter)

    totals = collections.Counter()
    successes = collections.Counter()
    batch_item_success = 0
    batch_item_total = 0

    for row_id, row in sorted(dataset.items()):
        gold = normalize_tool_calls(row["golden"]["tool_calls"])
        samples = predictions.get(row_id, {})
        first = samples.get(0, {})
        pred = normalize_tool_calls(first.get("tool_calls"))
        stratum = row["stratum"]

        tool_ok = names(gold) == names(pred)
        args_ok = argument_exact(gold, pred)
        exact_ok = exact_match(gold, pred)
        resolver_args_ok = resolver_aware_argument_exact(gold, pred)
        resolver_exact_ok = resolver_aware_exact_match(gold, pred)
        pass_k_ok = any(exact_match(gold, normalize_tool_calls(samples.get(i, {}).get("tool_calls"))) for i in range(pass_k))
        resolver_pass_k_ok = any(
            resolver_aware_exact_match(gold, normalize_tool_calls(samples.get(i, {}).get("tool_calls")))
            for i in range(pass_k)
        )

        error = classify_error(gold, pred, stratum)
        error_counts[stratum][error] += 1

        for scope in ("overall", f"stratum:{stratum}"):
            totals[(scope, "tool_selection_accuracy")] += 1
            successes[(scope, "tool_selection_accuracy")] += int(tool_ok)
            totals[(scope, "argument_exact_match")] += 1
            successes[(scope, "argument_exact_match")] += int(args_ok)
            totals[(scope, "pass@1")] += 1
            successes[(scope, "pass@1")] += int(exact_ok)
            totals[(scope, f"pass^{pass_k}")] += 1
            successes[(scope, f"pass^{pass_k}")] += int(pass_k_ok)
            totals[(scope, "resolver_aware_argument_match")] += 1
            successes[(scope, "resolver_aware_argument_match")] += int(resolver_args_ok)
            totals[(scope, "resolver_aware_pass@1")] += 1
            successes[(scope, "resolver_aware_pass@1")] += int(resolver_exact_ok)
            totals[(scope, f"resolver_aware_pass^{pass_k}")] += 1
            successes[(scope, f"resolver_aware_pass^{pass_k}")] += int(resolver_pass_k_ok)

            if stratum in WRITE_STRATA:
                totals[(scope, "state_based_success_proxy")] += 1
                successes[(scope, "state_based_success_proxy")] += int(exact_ok)
                totals[(scope, "resolver_aware_state_based_success_proxy")] += 1
                successes[(scope, "resolver_aware_state_based_success_proxy")] += int(resolver_exact_ok)
            if stratum in BATCH_STRATA:
                totals[(scope, "strict_batch_success")] += 1
                successes[(scope, "strict_batch_success")] += int(exact_ok)
                totals[(scope, "resolver_aware_strict_batch_success")] += 1
                successes[(scope, "resolver_aware_strict_batch_success")] += int(resolver_exact_ok)

        if stratum in BATCH_STRATA:
            matched, total = batch_item_counts(gold, pred)
            batch_item_success += matched
            batch_item_total += total
            resolver_matched, resolver_total = resolver_aware_batch_item_counts(gold, pred)
            batch_item_success += 0
            batch_item_total += 0
            totals[("overall", "resolver_aware_partial_batch_item_success")] += resolver_total
            successes[("overall", "resolver_aware_partial_batch_item_success")] += resolver_matched

        details.append(
            {
                "id": row_id,
                "split": row["split"],
                "stratum": stratum,
                "utterance": row["utterance"],
                "tool_selection_ok": tool_ok,
                "argument_exact_ok": args_ok,
                "resolver_aware_argument_ok": resolver_args_ok,
                "pass1_ok": exact_ok,
                "resolver_aware_pass1_ok": resolver_exact_ok,
                f"pass{pass_k}_ok": pass_k_ok,
                f"resolver_aware_pass{pass_k}_ok": resolver_pass_k_ok,
                "error": error,
                "gold_tool_names": names(gold),
                "pred_tool_names": names(pred),
                "gold_resolver_aware_calls": resolver_aware_calls(gold),
                "pred_resolver_aware_calls": resolver_aware_calls(pred),
            }
        )

    metrics: dict[str, dict[str, Any]] = collections.defaultdict(dict)
    for (scope, metric), total in sorted(totals.items()):
        metrics[scope][metric] = metric_row(successes[(scope, metric)], total)
    metrics["overall"]["partial_batch_item_success"] = metric_row(batch_item_success, batch_item_total)

    return {
        "dataset_size": len(dataset),
        "prediction_size": sum(len(samples) for samples in predictions.values()),
        "pass_k": pass_k,
        "metrics": metrics,
        "errors_by_stratum": {key: dict(counter) for key, counter in sorted(error_counts.items())},
        "details": details,
    }


def write_markdown(report: dict[str, Any], path: Path) -> None:
    lines = [
        "# LLM Tool-Calling Evaluation Report",
        "",
        f"Dataset examples: {report['dataset_size']}",
        f"Prediction samples: {report['prediction_size']}",
        f"pass^k: {report['pass_k']}",
        "",
        "## Overall",
        "",
        "| Metric | Success/Total | Mean | 95% Wilson CI |",
        "|---|---:|---:|---:|",
    ]

    def fmt(metric: dict[str, Any]) -> str:
        if metric["mean"] is None:
            return "n/a"
        return f"{metric['mean_pct']:.2f}%"

    def ci(metric: dict[str, Any]) -> str:
        if metric["mean"] is None:
            return "n/a"
        low, high = metric["ci95_pct"]
        return f"{low:.2f}% - {high:.2f}%"

    for metric_name, metric in report["metrics"]["overall"].items():
        lines.append(
            f"| {metric_name} | {metric['successes']}/{metric['total']} | {fmt(metric)} | {ci(metric)} |"
        )

    lines.extend(["", "## By Stratum", ""])
    for scope, metrics in sorted(report["metrics"].items()):
        if not scope.startswith("stratum:"):
            continue
        stratum = scope.split(":", 1)[1]
        lines.extend([f"### {stratum}", "", "| Metric | Success/Total | Mean | 95% Wilson CI |", "|---|---:|---:|---:|"])
        for metric_name, metric in metrics.items():
            lines.append(
                f"| {metric_name} | {metric['successes']}/{metric['total']} | {fmt(metric)} | {ci(metric)} |"
            )
        lines.append("")

    lines.extend(["## Error Analysis", "", "| Stratum | Error | Count |", "|---|---|---:|"])
    for stratum, errors in report["errors_by_stratum"].items():
        for error, count in sorted(errors.items()):
            lines.append(f"| {stratum} | {error} | {count} |")

    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dataset", action="append", default=[], help="Dataset JSONL path or glob. Can be repeated.")
    parser.add_argument("--predictions", required=True, type=Path, help="Predictions JSONL.")
    parser.add_argument("--out-json", required=True, type=Path)
    parser.add_argument("--out-md", required=True, type=Path)
    parser.add_argument("--pass-k", type=int, default=3)
    parser.add_argument("--split", action="append", choices=["train", "dev", "test"])
    args = parser.parse_args()

    dataset_paths = args.dataset or ["datasets/tool_calling/*.jsonl", "datasets/fault_injection/*.jsonl"]
    dataset = load_dataset(dataset_paths, set(args.split) if args.split else None)
    predictions = load_predictions(args.predictions)
    report = evaluate(dataset, predictions, args.pass_k)

    args.out_json.parent.mkdir(parents=True, exist_ok=True)
    args.out_md.parent.mkdir(parents=True, exist_ok=True)
    args.out_json.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    write_markdown(report, args.out_md)


if __name__ == "__main__":
    main()
