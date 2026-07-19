#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import random
import re
import statistics
import sys
from collections import Counter, defaultdict
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MANIFEST = ROOT / "datasets" / "asr" / "manifest.jsonl"
TOKEN_RE = re.compile(r"[A-Za-zА-Яа-яЁё]+(?:-[A-Za-zА-Яа-яЁё0-9]+)?|\d+(?:[.,]\d+)?")
ENTITY_CATEGORIES = {"number", "proper_noun", "other"}


def normalize(text: str) -> str:
    text = text.lower().replace("ё", "е").replace(",", ".")
    return " ".join(TOKEN_RE.findall(text))


def tokens(text: str) -> list[str]:
    normalized = normalize(text)
    return normalized.split() if normalized else []


def compact_alnum(text: str) -> str:
    return "".join(tokens(text))


def load_jsonl(path: Path) -> list[dict[str, Any]]:
    rows = []
    with path.open("r", encoding="utf-8") as f:
        for line_no, line in enumerate(f, start=1):
            try:
                rows.append(json.loads(line))
            except json.JSONDecodeError as exc:
                raise SystemExit(f"{path}:{line_no}: invalid JSON: {exc}") from exc
    return rows


def align(ref: list[str], hyp: list[str]) -> list[tuple[str, int | None, int | None]]:
    rows = len(ref) + 1
    cols = len(hyp) + 1
    dp = [[0] * cols for _ in range(rows)]
    back: list[list[tuple[str, int | None, int | None] | None]] = [[None] * cols for _ in range(rows)]

    for i in range(1, rows):
        dp[i][0] = i
        back[i][0] = ("delete", i - 1, None)
    for j in range(1, cols):
        dp[0][j] = j
        back[0][j] = ("insert", None, j - 1)

    for i in range(1, rows):
        for j in range(1, cols):
            cost = 0 if ref[i - 1] == hyp[j - 1] else 1
            candidates = [
                (dp[i - 1][j - 1] + cost, "equal" if cost == 0 else "substitute", i - 1, j - 1),
                (dp[i - 1][j] + 1, "delete", i - 1, None),
                (dp[i][j - 1] + 1, "insert", None, j - 1),
            ]
            score, op, ref_index, hyp_index = min(candidates, key=lambda item: item[0])
            dp[i][j] = score
            back[i][j] = (op, ref_index, hyp_index)

    i, j = len(ref), len(hyp)
    ops = []
    while i > 0 or j > 0:
        op, ref_index, hyp_index = back[i][j] or ("equal", None, None)
        ops.append((op, ref_index, hyp_index))
        if op in {"equal", "substitute"}:
            i -= 1
            j -= 1
        elif op == "delete":
            i -= 1
        else:
            j -= 1
    return list(reversed(ops))


def token_categories(row: dict[str, Any]) -> list[str]:
    categories = []
    for token in row.get("tokens", []):
        category = token.get("category", "other")
        token_parts = tokens(token.get("text", ""))
        categories.extend([category if category in ENTITY_CATEGORIES else "other"] * len(token_parts))
    return categories


def animal_tag_values(row: dict[str, Any]) -> list[list[str]]:
    values = []
    golden_compact = compact_alnum(row.get("golden_transcript", ""))
    seen = set()
    for entity in row.get("entities", []):
        if entity.get("kind") == "animal_tag":
            entity_tokens = tokens(entity.get("text", ""))
            entity_compact = "".join(entity_tokens)
            if entity_tokens and entity_compact in golden_compact and entity_compact not in seen:
                seen.add(entity_compact)
                values.append(entity_tokens)
    return values


def percentile(values: list[float], p: float) -> float | None:
    if not values:
        return None
    ordered = sorted(values)
    index = min(len(ordered) - 1, max(0, round((len(ordered) - 1) * p)))
    return ordered[index]


def ratio(num: float, den: float) -> float | None:
    return num / den if den else None


def evaluate_items(
    manifest: dict[str, dict[str, Any]],
    predictions: list[dict[str, Any]],
    prediction_field: str,
) -> tuple[list[dict[str, Any]], list[str]]:
    items = []
    missing = []
    for pred in predictions:
        row = manifest.get(pred.get("id"))
        if not row:
            missing.append(str(pred.get("id")))
            continue
        ref = tokens(row["golden_transcript"])
        prediction = pred.get(prediction_field, "")
        hyp = tokens(prediction)
        ops = align(ref, hyp)
        categories = token_categories(row)

        category_ref = Counter()
        category_errors = Counter()
        for op, ref_index, _hyp_index in ops:
            if ref_index is None:
                continue
            category = categories[ref_index] if ref_index < len(categories) else "other"
            category_ref[category] += 1
            if op != "equal":
                category_errors[category] += 1

        tag_groups = animal_tag_values(row)
        hyp_compact = compact_alnum(prediction)
        tag_exact = None
        if tag_groups:
            tag_exact = all("".join(group) in hyp_compact for group in tag_groups)

        wer_errors = sum(1 for op, _ref_index, _hyp_index in ops if op != "equal")
        items.append({
            "id": row["id"],
            "source_stratum": row.get("source_stratum"),
            "noise_profile": (row.get("noise_profile") or {}).get("id"),
            "voice_profile": (row.get("voice_profile") or {}).get("id"),
            "ref_tokens": len(ref),
            "wer_errors": wer_errors,
            "category_ref": dict(category_ref),
            "category_errors": dict(category_errors),
            "animal_tag_exact": tag_exact,
            "utterance_exact": normalize(row["golden_transcript"]) == normalize(prediction),
            "latency_sec": pred.get("latency_sec"),
            "prediction": prediction,
            "golden_transcript": row["golden_transcript"],
            "error": pred.get("error"),
        })
    return items, missing


def aggregate(items: list[dict[str, Any]]) -> dict[str, Any]:
    ref_tokens = sum(item["ref_tokens"] for item in items)
    wer_errors = sum(item["wer_errors"] for item in items)
    category_ref = Counter()
    category_errors = Counter()
    for item in items:
        category_ref.update(item["category_ref"])
        category_errors.update(item["category_errors"])

    tag_items = [item for item in items if item["animal_tag_exact"] is not None]
    latencies = [float(item["latency_sec"]) for item in items if isinstance(item.get("latency_sec"), (int, float))]
    return {
        "items": len(items),
        "wer": ratio(wer_errors, ref_tokens),
        "wer_errors": wer_errors,
        "ref_tokens": ref_tokens,
        "entity_wer_by_category": {
            category: ratio(category_errors[category], category_ref[category])
            for category in sorted(ENTITY_CATEGORIES)
        },
        "entity_errors_by_category": dict(sorted(category_errors.items())),
        "entity_ref_by_category": dict(sorted(category_ref.items())),
        "animal_tag_exact_match": ratio(sum(1 for item in tag_items if item["animal_tag_exact"]), len(tag_items)),
        "animal_tag_items": len(tag_items),
        "utterance_exact_match": ratio(sum(1 for item in items if item["utterance_exact"]), len(items)),
        "latency_sec": {
            "mean": statistics.mean(latencies) if latencies else None,
            "median": statistics.median(latencies) if latencies else None,
            "p95": percentile(latencies, 0.95),
            "max": max(latencies) if latencies else None,
            "items": len(latencies),
        },
    }


def bootstrap_ci(items: list[dict[str, Any]], iterations: int, seed: int) -> dict[str, Any]:
    if not items or iterations <= 0:
        return {}
    rng = random.Random(seed)
    wer_values = []
    tag_values = []
    utterance_values = []
    for _ in range(iterations):
        sample = [items[rng.randrange(len(items))] for _item in items]
        summary = aggregate(sample)
        wer_values.append(summary["wer"])
        if summary["animal_tag_exact_match"] is not None:
            tag_values.append(summary["animal_tag_exact_match"])
        if summary["utterance_exact_match"] is not None:
            utterance_values.append(summary["utterance_exact_match"])
    return {
        "wer_95_ci": [percentile(wer_values, 0.025), percentile(wer_values, 0.975)],
        "animal_tag_exact_match_95_ci": [percentile(tag_values, 0.025), percentile(tag_values, 0.975)] if tag_values else None,
        "utterance_exact_match_95_ci": [percentile(utterance_values, 0.025), percentile(utterance_values, 0.975)] if utterance_values else None,
        "bootstrap_iterations": iterations,
        "bootstrap_seed": seed,
    }


def grouped(items: list[dict[str, Any]], field: str) -> dict[str, Any]:
    groups: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for item in items:
        groups[str(item.get(field) or "unknown")].append(item)
    return {key: aggregate(value) for key, value in sorted(groups.items())}


def write_markdown(path: Path, report: dict[str, Any], worst: list[dict[str, Any]]) -> None:
    lines = [
        "# ASR Benchmark Report",
        "",
        f"Model: `{report.get('model', 'unknown')}`",
        f"Predictions: `{report.get('predictions_file')}`",
        "",
        "## Summary",
        "",
        "| Metric | Value |",
        "| --- | ---: |",
        f"| Items | {report['overall']['items']} |",
        f"| WER | {report['overall']['wer']:.4f} |",
        f"| Animal tag exact-match | {report['overall']['animal_tag_exact_match']:.4f} |",
        f"| Utterance exact-match | {report['overall']['utterance_exact_match']:.4f} |",
        f"| Latency mean, sec | {report['overall']['latency_sec']['mean']:.4f} |",
        f"| Latency p95, sec | {report['overall']['latency_sec']['p95']:.4f} |",
        "",
        "## Entity WER",
        "",
        "| Category | WER | Errors | Ref tokens |",
        "| --- | ---: | ---: | ---: |",
    ]
    for category, value in report["overall"]["entity_wer_by_category"].items():
        errors = report["overall"]["entity_errors_by_category"].get(category, 0)
        ref = report["overall"]["entity_ref_by_category"].get(category, 0)
        lines.append(f"| {category} | {value:.4f} | {errors} | {ref} |")

    lines.extend(["", "## Worst Items", ""])
    for item in worst:
        lines.append(f"- `{item['id']}` WER errors {item['wer_errors']}/{item['ref_tokens']}: ref `{item['golden_transcript']}`; hyp `{item['prediction']}`")

    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Evaluate CattleTrack ASR benchmark predictions.")
    parser.add_argument("predictions", type=Path, help="JSONL with fields: id, prediction, latency_sec")
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--markdown-output", type=Path)
    parser.add_argument("--prediction-field", default="prediction")
    parser.add_argument("--bootstrap", type=int, default=1000)
    parser.add_argument("--seed", type=int, default=42)
    args = parser.parse_args()

    manifest_rows = load_jsonl(args.manifest)
    manifest = {row["id"]: row for row in manifest_rows}
    predictions = load_jsonl(args.predictions)
    items, missing = evaluate_items(manifest, predictions, args.prediction_field)
    model = next((row.get("model") for row in predictions if row.get("model")), None)
    report = {
        "model": model,
        "manifest_file": str(args.manifest),
        "predictions_file": str(args.predictions),
        "prediction_field": args.prediction_field,
        "missing_prediction_ids": missing,
        "overall": aggregate(items),
        "confidence_intervals": bootstrap_ci(items, args.bootstrap, args.seed),
        "by_noise_profile": grouped(items, "noise_profile"),
        "by_source_stratum": grouped(items, "source_stratum"),
        "by_voice_profile": grouped(items, "voice_profile"),
        "worst_items": sorted(items, key=lambda item: (ratio(item["wer_errors"], item["ref_tokens"]) or 0, item["wer_errors"]), reverse=True)[:12],
    }

    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    if args.markdown_output:
        args.markdown_output.parent.mkdir(parents=True, exist_ok=True)
        write_markdown(args.markdown_output, report, report["worst_items"])
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 1 if missing else 0


if __name__ == "__main__":
    sys.exit(main())
