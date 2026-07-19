#!/usr/bin/env python3
import json
import re
import sys
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
SCHEMA_ROOT = ROOT / "ai-contracts" / "schemas"
DRAFT_2020_12 = "https://json-schema.org/draft/2020-12/schema"


class ValidationError(Exception):
    pass


def load_json(path: Path) -> dict[str, Any]:
    with path.open("r", encoding="utf-8") as f:
        return json.load(f)


def resolve_ref(ref: str, current_schema: dict[str, Any], current_path: Path, schemas: dict[Path, dict[str, Any]]) -> Any:
    if "#" in ref:
        file_ref, pointer = ref.split("#", 1)
    else:
        file_ref, pointer = ref, ""

    target_path = current_path if not file_ref else (current_path.parent / file_ref).resolve()
    if target_path not in schemas:
        raise ValidationError(f"Unknown $ref target: {ref}")

    target: Any = schemas[target_path]
    if not pointer:
        return target

    if not pointer.startswith("/"):
        raise ValidationError(f"Only JSON pointer refs are supported: {ref}")

    for part in pointer.lstrip("/").split("/"):
        part = part.replace("~1", "/").replace("~0", "~")
        target = target[part]
    return target


def type_matches(expected: str, value: Any) -> bool:
    if expected == "object":
        return isinstance(value, dict)
    if expected == "array":
        return isinstance(value, list)
    if expected == "string":
        return isinstance(value, str)
    if expected == "integer":
        return isinstance(value, int) and not isinstance(value, bool)
    if expected == "number":
        return (isinstance(value, int) or isinstance(value, float)) and not isinstance(value, bool)
    if expected == "boolean":
        return isinstance(value, bool)
    if expected == "null":
        return value is None
    raise ValidationError(f"Unsupported schema type in test validator: {expected}")


def validate(instance: Any, schema: dict[str, Any], current_path: Path, schemas: dict[Path, dict[str, Any]], where: str) -> None:
    if "$ref" in schema:
        validate(instance, resolve_ref(schema["$ref"], schema, current_path, schemas), current_path, schemas, where)
        return

    if "allOf" in schema:
        for i, sub_schema in enumerate(schema["allOf"]):
            validate(instance, sub_schema, current_path, schemas, f"{where}.allOf[{i}]")

    if "anyOf" in schema:
        errors = []
        for sub_schema in schema["anyOf"]:
            try:
                validate(instance, sub_schema, current_path, schemas, where)
                break
            except ValidationError as exc:
                errors.append(str(exc))
        else:
            raise ValidationError(f"{where}: anyOf failed: {errors}")

    if "oneOf" in schema:
        matches = 0
        errors = []
        for sub_schema in schema["oneOf"]:
            try:
                validate(instance, sub_schema, current_path, schemas, where)
                matches += 1
            except ValidationError as exc:
                errors.append(str(exc))
        if matches != 1:
            raise ValidationError(f"{where}: oneOf expected 1 match, got {matches}: {errors}")

    if "if" in schema:
        try:
            validate(instance, schema["if"], current_path, schemas, f"{where}.if")
            condition_matches = True
        except ValidationError:
            condition_matches = False

        if condition_matches and "then" in schema:
            validate(instance, schema["then"], current_path, schemas, f"{where}.then")
        if not condition_matches and "else" in schema:
            validate(instance, schema["else"], current_path, schemas, f"{where}.else")

    if "const" in schema and instance != schema["const"]:
        raise ValidationError(f"{where}: expected const {schema['const']!r}, got {instance!r}")

    if "enum" in schema and instance not in schema["enum"]:
        raise ValidationError(f"{where}: expected one of {schema['enum']!r}, got {instance!r}")

    expected_type = schema.get("type")
    if expected_type:
        if isinstance(expected_type, list):
            if not any(type_matches(t, instance) for t in expected_type):
                raise ValidationError(f"{where}: expected type {expected_type!r}, got {type(instance).__name__}")
        elif not type_matches(expected_type, instance):
            raise ValidationError(f"{where}: expected type {expected_type!r}, got {type(instance).__name__}")

    if isinstance(instance, dict):
        required = schema.get("required", [])
        for key in required:
            if key not in instance:
                raise ValidationError(f"{where}: missing required property {key!r}")

        properties = schema.get("properties", {})
        if schema.get("additionalProperties") is False:
            extra = set(instance) - set(properties)
            if extra:
                raise ValidationError(f"{where}: additional properties are not allowed: {sorted(extra)!r}")

        for key, value in instance.items():
            if key in properties:
                validate(value, properties[key], current_path, schemas, f"{where}.{key}")

    if isinstance(instance, list):
        if "minItems" in schema and len(instance) < schema["minItems"]:
            raise ValidationError(f"{where}: expected at least {schema['minItems']} items")
        if "maxItems" in schema and len(instance) > schema["maxItems"]:
            raise ValidationError(f"{where}: expected at most {schema['maxItems']} items")
        if "items" in schema:
            for i, item in enumerate(instance):
                validate(item, schema["items"], current_path, schemas, f"{where}[{i}]")

    if isinstance(instance, str):
        if "minLength" in schema and len(instance) < schema["minLength"]:
            raise ValidationError(f"{where}: string shorter than {schema['minLength']}")
        if "maxLength" in schema and len(instance) > schema["maxLength"]:
            raise ValidationError(f"{where}: string longer than {schema['maxLength']}")
        if "pattern" in schema and not re.search(schema["pattern"], instance):
            raise ValidationError(f"{where}: string does not match pattern {schema['pattern']!r}")

    if isinstance(instance, (int, float)) and not isinstance(instance, bool):
        if "minimum" in schema and instance < schema["minimum"]:
            raise ValidationError(f"{where}: number below minimum {schema['minimum']}")
        if "maximum" in schema and instance > schema["maximum"]:
            raise ValidationError(f"{where}: number above maximum {schema['maximum']}")
        if "exclusiveMinimum" in schema and instance <= schema["exclusiveMinimum"]:
            raise ValidationError(f"{where}: number not above exclusive minimum {schema['exclusiveMinimum']}")


def collect_enums(schema: Any) -> list[list[Any]]:
    enums = []
    if isinstance(schema, dict):
        if "enum" in schema:
            enums.append(schema["enum"])
        for value in schema.values():
            enums.extend(collect_enums(value))
    elif isinstance(schema, list):
        for value in schema:
            enums.extend(collect_enums(value))
    return enums


def main() -> int:
    paths = sorted(SCHEMA_ROOT.rglob("*.schema.json"))
    schemas = {path.resolve(): load_json(path) for path in paths}
    errors: list[str] = []

    for path in paths:
        schema = schemas[path.resolve()]
        rel = path.relative_to(ROOT)

        if schema.get("$schema") != DRAFT_2020_12:
            errors.append(f"{rel}: missing or invalid $schema")
        if "$id" not in schema:
            errors.append(f"{rel}: missing $id")

        if "/schemas/v1/" in schema.get("$id", "") and schema.get("x-tool-version") not in (None, "v1"):
            errors.append(f"{rel}: x-tool-version must be v1")

        if path.name.endswith(".args.schema.json"):
            if schema.get("x-tool-version") != "v1":
                errors.append(f"{rel}: tool schema must have x-tool-version=v1")
            if schema.get("type") != "object":
                errors.append(f"{rel}: tool schema must be an object")
            if schema.get("additionalProperties") is not False:
                errors.append(f"{rel}: tool schema must set additionalProperties=false")
            if "schema_version" not in schema.get("required", []):
                errors.append(f"{rel}: tool schema must require schema_version")
            if "schema_version" not in schema.get("properties", {}):
                errors.append(f"{rel}: tool schema must define schema_version")
            if not schema.get("examples"):
                errors.append(f"{rel}: tool schema must contain at least one valid example")

        if path.name in {"create_weight.args.schema.json", "create_daily_action.args.schema.json", "create_insemination.args.schema.json"}:
            if not any("__unknown" in enum_values for enum_values in collect_enums(schema)):
                errors.append(f"{rel}: write enum schemas must contain __unknown fallback")

        for i, example in enumerate(schema.get("examples", [])):
            try:
                validate(example, schema, path.resolve(), schemas, f"{rel}.examples[{i}]")
            except Exception as exc:
                errors.append(f"{rel}: valid example {i} failed validation: {exc}")

        for i, example in enumerate(schema.get("x-invalid-examples", [])):
            try:
                validate(example, schema, path.resolve(), schemas, f"{rel}.x-invalid-examples[{i}]")
            except ValidationError:
                continue
            except Exception as exc:
                errors.append(f"{rel}: invalid example {i} crashed validator: {exc}")
            else:
                errors.append(f"{rel}: invalid example {i} unexpectedly passed validation")

    if errors:
        print("AI contract schema validation failed:")
        for error in errors:
            print(f"- {error}")
        return 1

    print(f"AI contract schema validation passed: {len(paths)} schema files")
    return 0


if __name__ == "__main__":
    sys.exit(main())
