#!/usr/bin/env python3
import json
import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATASETS = ROOT / "datasets"
sys.path.insert(0, str((ROOT / "scripts").resolve()))

from validate_ai_contract_schemas import ValidationError, load_json, validate  # noqa: E402

EXPECTED_STRATA = {
    "single-read",
    "no-tool",
    "multi-hop-read",
    "single-write",
    "batch-write",
    "adversarial-ambiguous",
    "fault-injection",
}

EXPECTED_SPLITS = {"train", "dev", "test"}
EXPECTED_REVIEW_STATUSES = {"pending", "approved", "needs_edit", "reject", "duplicate"}


def main() -> int:
    errors = []
    ids = set()
    split_counts = Counter()
    stratum_counts = Counter()
    total = 0
    schema_root = ROOT / "ai-contracts" / "schemas"
    schemas = {path.resolve(): load_json(path) for path in schema_root.rglob("*.schema.json")}
    tool_schemas = {
        path.name.replace(".args.schema.json", ""): path.resolve()
        for path in (schema_root / "v1" / "tools").glob("*.args.schema.json")
    }

    dataset_paths = (
        sorted((DATASETS / "tool_calling").glob("*.jsonl"))
        + sorted((DATASETS / "fault_injection").glob("*.jsonl"))
    )

    for path in dataset_paths:
        expected_split = path.stem
        if expected_split not in EXPECTED_SPLITS:
            errors.append(f"{path}: unexpected split filename")

        with path.open("r", encoding="utf-8") as f:
            for line_no, line in enumerate(f, start=1):
                total += 1
                try:
                    row = json.loads(line)
                except json.JSONDecodeError as exc:
                    errors.append(f"{path}:{line_no}: invalid json: {exc}")
                    continue

                row_id = row.get("id")
                if not row_id:
                    errors.append(f"{path}:{line_no}: missing id")
                elif row_id in ids:
                    errors.append(f"{path}:{line_no}: duplicate id {row_id}")
                else:
                    ids.add(row_id)

                if row.get("split") != expected_split:
                    errors.append(f"{path}:{line_no}: split does not match filename")
                if row.get("split") not in EXPECTED_SPLITS:
                    errors.append(f"{path}:{line_no}: unexpected split {row.get('split')!r}")
                if row.get("stratum") not in EXPECTED_STRATA:
                    errors.append(f"{path}:{line_no}: unexpected stratum {row.get('stratum')!r}")

                golden = row.get("golden")
                if not isinstance(golden, dict):
                    errors.append(f"{path}:{line_no}: missing golden object")
                else:
                    if "tool_calls" not in golden or not isinstance(golden["tool_calls"], list):
                        errors.append(f"{path}:{line_no}: golden.tool_calls must be a list")
                    if "expected_result" not in golden or not isinstance(golden["expected_result"], dict):
                        errors.append(f"{path}:{line_no}: golden.expected_result must be an object")
                    if path.parent.name == "tool_calling":
                        for call_index, tool_call in enumerate(golden.get("tool_calls", [])):
                            name = tool_call.get("name") if isinstance(tool_call, dict) else None
                            arguments = tool_call.get("arguments") if isinstance(tool_call, dict) else None
                            schema_path = tool_schemas.get(name)
                            if not schema_path:
                                errors.append(f"{path}:{line_no}: unknown tool name {name!r}")
                                continue
                            try:
                                validate(arguments, schemas[schema_path], schema_path, schemas, f"{row_id}.tool_calls[{call_index}]")
                            except ValidationError as exc:
                                errors.append(f"{path}:{line_no}: tool args do not match schema: {exc}")

                review = row.get("review")
                if not isinstance(review, dict) or review.get("status") not in EXPECTED_REVIEW_STATUSES:
                    errors.append(f"{path}:{line_no}: review.status must be one of {sorted(EXPECTED_REVIEW_STATUSES)!r}")

                split_counts[row.get("split")] += 1
                stratum_counts[row.get("stratum")] += 1

    if total < 170 or total > 220:
        errors.append(f"total examples must be 170-220, got {total}")

    if errors:
        print("AI dataset validation failed:")
        for error in errors:
            print(f"- {error}")
        return 1

    print(json.dumps({
        "total": total,
        "by_split": dict(sorted(split_counts.items())),
        "by_stratum": dict(sorted(stratum_counts.items())),
    }, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
