#!/usr/bin/env python3
"""Unit tests for model-selection additions to the OpenAI-compatible runner."""

from __future__ import annotations

import importlib.util
import unittest
from pathlib import Path


SCRIPT_PATH = Path(__file__).with_name("run_llm_tool_calling_openai_compatible.py")
SPEC = importlib.util.spec_from_file_location("llm_runner", SCRIPT_PATH)
assert SPEC and SPEC.loader
RUNNER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(RUNNER)


class ModelSelectionRunnerTests(unittest.TestCase):
    def test_backend_owned_fields_are_removed_recursively(self) -> None:
        schema = {
            "required": ["schema_version", "batch_idempotency_key", "items"],
            "properties": {
                "schema_version": {"type": "string"},
                "batch_idempotency_key": {"type": "string"},
                "items": {
                    "type": "array",
                    "items": {
                        "required": ["idempotency_key", "tag"],
                        "properties": {
                            "idempotency_key": {"type": "string"},
                            "tag": {"type": "string"},
                        },
                    },
                },
            },
        }

        RUNNER.remove_backend_owned_arguments(schema)

        self.assertEqual(schema["required"], ["items"])
        self.assertNotIn("schema_version", schema["properties"])
        item_schema = schema["properties"]["items"]["items"]
        self.assertEqual(item_schema["required"], ["tag"])
        self.assertNotIn("idempotency_key", item_schema["properties"])

    def test_multiturn_messages_are_preserved(self) -> None:
        row = {
            "messages": [
                {"role": "user", "content": "Покажи карточку 523"},
                {"role": "assistant", "content": "Карточка открыта."},
                {"role": "user", "content": "А теперь историю ее веса"},
            ]
        }

        payload = RUNNER.build_payload(row, "model", [], "native-tools", 0.0, True, 64, True, None, "2026-07-13")

        self.assertEqual(payload["messages"][1]["content"], "Покажи карточку 523")
        self.assertEqual(payload["messages"][-1]["content"], "А теперь историю ее веса\n/no_think")
        self.assertEqual(payload["reasoning_effort"], "none")
        self.assertEqual(row["messages"][-1]["content"], "А теперь историю ее веса")
        self.assertIn("2026-07-13", payload["messages"][0]["content"])

    def test_streamed_tool_call_fragments_are_merged(self) -> None:
        target: list[dict] = []
        RUNNER.merge_streamed_tool_calls(
            target,
            [{"index": 0, "id": "call-1", "function": {"name": "find_", "arguments": "{\"tag\":"}}],
        )
        RUNNER.merge_streamed_tool_calls(
            target,
            [{"index": 0, "function": {"name": "animal", "arguments": "\"523\"}"}}],
        )

        self.assertEqual(target[0]["id"], "call-1")
        self.assertEqual(target[0]["function"]["name"], "find_animal")
        self.assertEqual(target[0]["function"]["arguments"], '{"tag":"523"}')


if __name__ == "__main__":
    unittest.main()
