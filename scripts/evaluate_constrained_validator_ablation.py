#!/usr/bin/env python3
"""Evaluate constrained-output and validator ablation for CattleTrack AI writes."""

from __future__ import annotations

import argparse
import collections
import glob
import json
import math
from pathlib import Path
from typing import Any

from evaluate_llm_tool_calling import normalize_tool_calls, read_jsonl


WRITE_TOOLS = {"create_weight", "create_daily_action", "create_insemination"}
KNOWN_TOOLS = {
    "create_weight",
    "create_daily_action",
    "create_insemination",
    "find_animal",
    "get_animal_card",
    "get_animal_parents",
    "get_pregnancies_to_check",
    "get_weight_history",
    "list_groups",
}
WRITE_STRATA = {"single-write", "batch-write", "fault-injection", "adversarial-ambiguous"}
TODAY = "2026-07-09"
WEIGHT_METHODS = {"Автоматическая весовая станция", "Ручное взвешивание", "Расчетный метод"}
DAILY_TYPES = {
    "Осмотры",
    "Обработка",
    "Вакцинации и обработки",
    "Лечение",
    "Перевод",
    "Выбытие",
    "Исследования",
    "Присвоение номеров",
    "Изменение половозрастной группы",
}
INSEMINATION_TYPES = {"Искусственное", "Естественное", "Эмбрион"}


def load_dataset(patterns: list[str], selected_splits: set[str] | None) -> dict[str, dict[str, Any]]:
    rows: dict[str, dict[str, Any]] = {}
    for pattern in patterns:
        for path in sorted(glob.glob(pattern)):
            for row in read_jsonl(Path(path)):
                if selected_splits and row.get("split") not in selected_splits:
                    continue
                if row.get("stratum") not in WRITE_STRATA:
                    continue
                rows[row["id"]] = row
    return rows


def load_predictions(path: Path) -> dict[str, dict[int, dict[str, Any]]]:
    grouped: dict[str, dict[int, dict[str, Any]]] = collections.defaultdict(dict)
    for row in read_jsonl(path):
        grouped[row["id"]][int(row.get("sample_index", 0))] = row
    return grouped


def expected_invalid(row: dict[str, Any]) -> bool:
    if row.get("stratum") == "fault-injection":
        return True
    expected = row.get("golden", {}).get("expected_result", {})
    kind = expected.get("kind")
    return kind in {"validation_error", "clarification_or_validation_error"}


def is_write_prediction(calls: list[dict[str, Any]]) -> bool:
    return any(call.get("name") in WRITE_TOOLS for call in calls)


def schema_errors_for_call(call: dict[str, Any]) -> list[str]:
    name = call.get("name")
    args = call.get("arguments", {})
    if name not in KNOWN_TOOLS:
        return ["schema:unknown_tool"]
    if name not in WRITE_TOOLS:
        return []
    if not isinstance(args, dict):
        return ["schema:arguments_object"]
    if name == "create_weight":
        return validate_weight_schema(args)
    if name == "create_daily_action":
        return validate_daily_schema(args)
    if name == "create_insemination":
        return validate_insemination_schema(args)
    return []


def validate_weight_schema(args: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    for key in ["tag", "weight", "date", "method"]:
        if key not in args:
            errors.append(f"schema:required:{key}")
    if "weight" in args and not isinstance(args["weight"], (int, float)):
        errors.append("schema:type:weight")
    if "tag" in args and not isinstance(args["tag"], str):
        errors.append("schema:type:tag")
    if "date" in args and not isinstance(args["date"], str):
        errors.append("schema:type:date")
    return errors


def validate_daily_schema(args: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    items = args.get("items")
    if not isinstance(items, list) or not items:
        return ["schema:required:items"]
    for index, item in enumerate(items):
        if not isinstance(item, dict):
            errors.append(f"schema:type:items[{index}]")
            continue
        for key in ["tag", "type", "date"]:
            if key not in item:
                errors.append(f"schema:required:items[{index}].{key}")
    return errors


def validate_insemination_schema(args: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    items = args.get("items")
    if not isinstance(items, list) or not items:
        return ["schema:required:items"]
    for index, item in enumerate(items):
        if not isinstance(item, dict):
            errors.append(f"schema:type:items[{index}]")
            continue
        for key in ["cow_tags", "date", "insemination_type"]:
            if key not in item:
                errors.append(f"schema:required:items[{index}].{key}")
        if "cow_tags" in item and not isinstance(item["cow_tags"], list):
            errors.append(f"schema:type:items[{index}].cow_tags")
    return errors


def business_errors_for_call(call: dict[str, Any]) -> list[str]:
    name = call.get("name")
    args = call.get("arguments", {})
    if not isinstance(args, dict):
        return ["AI-VAL-REQUIRED"]
    if name == "create_weight":
        return validate_weight_business(args)
    if name == "create_daily_action":
        return validate_daily_business(args)
    if name == "create_insemination":
        return validate_insemination_business(args)
    return []


def validate_weight_business(args: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    weight = args.get("weight")
    if not isinstance(weight, (int, float)) or weight < 1 or weight > 3000:
        errors.append("AI-VAL-WEIGHT-RANGE")
    if args.get("date") and str(args["date"]) > TODAY:
        errors.append("AI-VAL-DATE-FUTURE")
    if args.get("method") not in WEIGHT_METHODS:
        errors.append("AI-VAL-ENUM-KNOWN")
    return errors


def validate_daily_business(args: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    for item in args.get("items", []) if isinstance(args.get("items"), list) else []:
        if not isinstance(item, dict):
            errors.append("AI-VAL-REQUIRED")
            continue
        action_type = item.get("type")
        if action_type not in DAILY_TYPES:
            errors.append("AI-VAL-ENUM-KNOWN")
        if item.get("date") and str(item["date"]) > TODAY:
            errors.append("AI-VAL-DATE-FUTURE")
        if action_type == "Перевод" and not any(item.get(key) for key in ["new_group_id", "new_group_name"]):
            errors.append("AI-VAL-DAILY-CASCADE")
        if action_type == "Выбытие" and not item.get("subtype"):
            errors.append("AI-VAL-DAILY-CASCADE")
        if action_type == "Исследования" and not item.get("research_name"):
            errors.append("AI-VAL-DAILY-CASCADE")
        if action_type == "Присвоение номеров" and (not item.get("subtype") or not item.get("identification_value")):
            errors.append("AI-VAL-DAILY-CASCADE")
        if action_type == "Изменение половозрастной группы" and (not item.get("old_type") or not item.get("new_type")):
            errors.append("AI-VAL-DAILY-CASCADE")
    return errors


def validate_insemination_business(args: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    for item in args.get("items", []) if isinstance(args.get("items"), list) else []:
        if not isinstance(item, dict):
            errors.append("AI-VAL-REQUIRED")
            continue
        ins_type = item.get("insemination_type")
        if ins_type not in INSEMINATION_TYPES:
            errors.append("AI-VAL-ENUM-KNOWN")
        if item.get("date") and str(item["date"]) > TODAY:
            errors.append("AI-VAL-DATE-FUTURE")
        if not item.get("cow_tags"):
            errors.append("AI-VAL-REQUIRED")
        if ins_type == "Естественное" and not item.get("bull_tags"):
            errors.append("AI-VAL-DAILY-CASCADE")
        if ins_type == "Эмбрион" and not item.get("embryo_id"):
            errors.append("AI-VAL-DAILY-CASCADE")
    return errors


def wilson(successes: int, total: int, z: float = 1.96) -> dict[str, Any]:
    if total == 0:
        return {"successes": successes, "total": total, "mean": None, "ci95_pct": [None, None]}
    phat = successes / total
    denom = 1 + z * z / total
    centre = phat + z * z / (2 * total)
    spread = z * math.sqrt((phat * (1 - phat) + z * z / (4 * total)) / total)
    low = (centre - spread) / denom
    high = (centre + spread) / denom
    return {
        "successes": successes,
        "total": total,
        "mean": phat,
        "mean_pct": round(phat * 100, 2),
        "ci95_pct": [round(low * 100, 2), round(high * 100, 2)],
    }


def evaluate_arm(name: str, predictions_path: Path, dataset: dict[str, dict[str, Any]]) -> dict[str, Any]:
    predictions = load_predictions(predictions_path)
    confusion = {
        "schema": collections.Counter(),
        "validator": collections.Counter(),
        "schema_plus_validator": collections.Counter(),
    }
    rule_counts: collections.Counter[str] = collections.Counter()
    details: list[dict[str, Any]] = []

    for row_id, row in sorted(dataset.items()):
        samples = predictions.get(row_id, {})
        sample = samples.get(0, {})
        calls = normalize_tool_calls(sample.get("tool_calls"))
        invalid_expected = expected_invalid(row)
        write_predicted = is_write_prediction(calls)
        schema_errors = [err for call in calls for err in schema_errors_for_call(call)]
        business_errors = [err for call in calls for err in business_errors_for_call(call)]
        rule_counts.update(business_errors)

        # If a case is expected to be invalid and model safely emits no write tool, count it as caught.
        safe_no_write = invalid_expected and not write_predicted
        schema_caught = bool(schema_errors)
        validator_caught = bool(business_errors) or safe_no_write
        combined_caught = schema_caught or validator_caught

        add_confusion(confusion["schema"], invalid_expected, schema_caught)
        add_confusion(confusion["validator"], invalid_expected, validator_caught)
        add_confusion(confusion["schema_plus_validator"], invalid_expected, combined_caught)

        details.append(
            {
                "id": row_id,
                "split": row.get("split"),
                "stratum": row.get("stratum"),
                "expected_invalid": invalid_expected,
                "write_predicted": write_predicted,
                "schema_errors": sorted(set(schema_errors)),
                "business_errors": sorted(set(business_errors)),
                "safe_no_write": safe_no_write,
            }
        )

    return {
        "name": name,
        "predictions": str(predictions_path),
        "rows": len(dataset),
        "confusion": {key: dict(value) for key, value in confusion.items()},
        "metrics": {key: metrics_from_confusion(value) for key, value in confusion.items()},
        "business_rule_counts": dict(rule_counts),
        "details": details,
    }


def add_confusion(counter: collections.Counter[str], expected_invalid_value: bool, caught: bool) -> None:
    if expected_invalid_value and caught:
        counter["tp"] += 1
    elif expected_invalid_value and not caught:
        counter["fn"] += 1
    elif not expected_invalid_value and caught:
        counter["fp"] += 1
    else:
        counter["tn"] += 1


def metrics_from_confusion(counter: collections.Counter[str]) -> dict[str, Any]:
    tp = counter["tp"]
    fp = counter["fp"]
    tn = counter["tn"]
    fn = counter["fn"]
    return {
        "catch_rate": wilson(tp, tp + fn),
        "false_positive_rate": wilson(fp, fp + tn),
    }


def write_markdown(report: dict[str, Any], path: Path) -> None:
    lines = [
        "# Constrained Output And Validator Ablation",
        "",
        f"Dataset rows: {report['dataset_rows']}",
        f"Splits: {', '.join(report['splits']) if report['splits'] else 'all'}",
        "",
        "| Arm | Layer | TP | FP | TN | FN | Catch rate | FPR |",
        "|---|---|---:|---:|---:|---:|---:|---:|",
    ]
    for arm in report["arms"]:
        for layer, confusion in arm["confusion"].items():
            metrics = arm["metrics"][layer]
            lines.append(
                "| {arm} | {layer} | {tp} | {fp} | {tn} | {fn} | {catch_rate} | {fpr} |".format(
                    arm=arm["name"],
                    layer=layer,
                    tp=confusion.get("tp", 0),
                    fp=confusion.get("fp", 0),
                    tn=confusion.get("tn", 0),
                    fn=confusion.get("fn", 0),
                    catch_rate=fmt_metric(metrics["catch_rate"]),
                    fpr=fmt_metric(metrics["false_positive_rate"]),
                )
            )
    lines.extend(["", "## Business Rule Counts", ""])
    for arm in report["arms"]:
        lines.extend([f"### {arm['name']}", "", "| Rule | Count |", "|---|---:|"])
        for rule, count in sorted(arm["business_rule_counts"].items()):
            lines.append(f"| `{rule}` | {count} |")
        if not arm["business_rule_counts"]:
            lines.append("| n/a | 0 |")
        lines.append("")
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def fmt_metric(metric: dict[str, Any]) -> str:
    if metric["mean"] is None:
        return "n/a"
    low, high = metric["ci95_pct"]
    return f"{metric['mean_pct']:.2f}% [{low:.2f}, {high:.2f}]"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dataset", action="append", default=[])
    parser.add_argument("--split", action="append", choices=["train", "dev", "test"])
    parser.add_argument("--arm", action="append", required=True, help="NAME=predictions.jsonl")
    parser.add_argument("--out-json", required=True, type=Path)
    parser.add_argument("--out-md", required=True, type=Path)
    args = parser.parse_args()

    dataset_paths = args.dataset or ["datasets/tool_calling/*.jsonl", "datasets/fault_injection/*.jsonl"]
    selected_splits = set(args.split) if args.split else None
    dataset = load_dataset(dataset_paths, selected_splits)
    arms = []
    for raw_arm in args.arm:
        if "=" not in raw_arm:
            raise SystemExit(f"--arm must be NAME=path, got {raw_arm!r}")
        name, path = raw_arm.split("=", 1)
        arms.append(evaluate_arm(name, Path(path), dataset))

    report = {
        "dataset_rows": len(dataset),
        "splits": args.split or [],
        "arms": arms,
    }
    args.out_json.parent.mkdir(parents=True, exist_ok=True)
    args.out_md.parent.mkdir(parents=True, exist_ok=True)
    args.out_json.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    write_markdown(report, args.out_md)


if __name__ == "__main__":
    main()
