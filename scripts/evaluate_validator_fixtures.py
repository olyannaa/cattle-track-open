#!/usr/bin/env python3
"""Evaluate schema and business validation on approved valid and corrupted fixtures."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

import evaluate_constrained_validator_ablation as ablation
from evaluate_llm_tool_calling import read_jsonl


def load_calls(row: dict[str, Any]) -> list[dict[str, Any]]:
    if "prediction_fixture" in row:
        return row["prediction_fixture"].get("tool_calls", [])
    return row.get("golden", {}).get("tool_calls", [])


def is_valid_write_fixture(row: dict[str, Any]) -> bool:
    calls = load_calls(row)
    if not any(call.get("name") in ablation.WRITE_TOOLS for call in calls):
        return False
    kind = row.get("golden", {}).get("expected_result", {}).get("kind")
    return kind not in {"validation_error", "clarification_or_validation_error"}


def transport_schema_errors(call: dict[str, Any]) -> list[str]:
    args = call.get("arguments", {})
    if not isinstance(args, dict) or call.get("name") not in ablation.WRITE_TOOLS:
        return []
    errors = []
    if "schema_version" not in args:
        errors.append("schema:required:schema_version")
    if call.get("name") == "create_weight":
        if "idempotency_key" not in args:
            errors.append("schema:required:idempotency_key")
    else:
        if "batch_idempotency_key" not in args:
            errors.append("schema:required:batch_idempotency_key")
        for index, item in enumerate(args.get("items", []) if isinstance(args.get("items"), list) else []):
            if isinstance(item, dict) and "idempotency_key" not in item:
                errors.append(f"schema:required:items[{index}].idempotency_key")
    return errors


def metric(tp: int, fp: int, tn: int, fn: int) -> dict[str, Any]:
    return {
        "confusion": {"tp": tp, "fp": fp, "tn": tn, "fn": fn},
        "catch_rate": ablation.wilson(tp, tp + fn),
        "false_positive_rate": ablation.wilson(fp, fp + tn),
    }


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--valid", action="append", required=True, type=Path)
    parser.add_argument("--invalid", action="append", required=True, type=Path)
    parser.add_argument("--regression", type=Path)
    parser.add_argument("--reference-date", default="2026-07-09")
    parser.add_argument("--out-json", required=True, type=Path)
    parser.add_argument("--out-md", required=True, type=Path)
    args = parser.parse_args()

    ablation.TODAY = args.reference_date
    fixtures: list[tuple[bool, dict[str, Any], str]] = []
    for path in args.valid:
        fixtures.extend((False, row, str(path)) for row in read_jsonl(path) if is_valid_write_fixture(row))
    for path in args.invalid:
        fixtures.extend((True, row, str(path)) for row in read_jsonl(path))
    if args.regression:
        fixtures.extend((bool(row.get("expected_invalid", True)), row, str(args.regression)) for row in read_jsonl(args.regression))

    counters = {"schema": [0, 0, 0, 0], "business_validator": [0, 0, 0, 0], "combined": [0, 0, 0, 0]}
    details = []
    for expected_invalid, row, source in fixtures:
        calls = load_calls(row)
        schema_errors = sorted({
            error
            for call in calls
            for error in [*ablation.schema_errors_for_call(call), *transport_schema_errors(call)]
        })
        business_errors = sorted({error for call in calls for error in ablation.business_errors_for_call(call)})
        decisions = {
            "schema": bool(schema_errors),
            "business_validator": bool(business_errors),
            "combined": bool(schema_errors or business_errors),
        }
        for layer, caught in decisions.items():
            index = 0 if expected_invalid and caught else 1 if not expected_invalid and caught else 2 if not expected_invalid else 3
            counters[layer][index] += 1
        details.append({
            "id": row["id"],
            "source": source,
            "expected_invalid": expected_invalid,
            "schema_errors": schema_errors,
            "business_errors": business_errors,
        })

    metrics = {name: metric(*values) for name, values in counters.items()}
    report = {
        "reference_date": args.reference_date,
        "fixtures": len(fixtures),
        "invalid_fixtures": sum(1 for expected, _, _ in fixtures if expected),
        "valid_fixtures": sum(1 for expected, _, _ in fixtures if not expected),
        "metrics": metrics,
        "details": details,
    }
    args.out_json.parent.mkdir(parents=True, exist_ok=True)
    args.out_json.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    lines = [
        "# Validator Fixture Evaluation",
        "",
        f"Reference date: `{args.reference_date}`",
        f"Fixtures: {report['fixtures']} ({report['invalid_fixtures']} invalid, {report['valid_fixtures']} valid)",
        "",
        "| Layer | TP | FP | TN | FN | Catch rate | FPR |",
        "|---|---:|---:|---:|---:|---:|---:|",
    ]
    for name, values in metrics.items():
        confusion = values["confusion"]
        lines.append(
            f"| {name} | {confusion['tp']} | {confusion['fp']} | {confusion['tn']} | {confusion['fn']} | "
            f"{ablation.fmt_metric(values['catch_rate'])} | {ablation.fmt_metric(values['false_positive_rate'])} |"
        )
    args.out_md.write_text("\n".join(lines) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
