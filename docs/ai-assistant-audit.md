# AI assistant audit

AI-события пишутся через существующий `UserActionQueue` в таблицу пользовательских действий с `table = "ai_assistant"`.

## Action types

| Action type | Когда пишется |
| --- | --- |
| `ai_llm_turn` | Каждый ответ LLM внутри bounded agent loop. |
| `ai_tool_call` | Каждый выполненный backend tool call и его результат. |
| `ai_draft_created` | После создания Redis draft. |
| `ai_clarification` | Когда draft требует уточнения или не имеет строк, готовых к commit. |
| `ai_commit` | Confirm/cancel/commit, включая expired/cannot_commit/partial failures. |
| `ai_loop_guard` | Anti-loop guard, invalid constrained output или iteration limit. |

## AdditionalInfo

`UserActionService` сохраняет `AdditionalInfo` как JSON-объект внутри `OriginalInfo`, если входная строка является валидным JSON.

```json
{
  "originalInfo": {
    "schemaVersion": "v1",
    "organizationId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "event": {
      "actionType": "ai_tool_call",
      "status": "success",
      "createdAtUtc": "2026-07-11T00:00:00+00:00"
    },
    "details": {
      "sessionId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      "iteration": 1,
      "toolName": "find_animal",
      "success": true
    },
    "error": null
  },
  "caller": {
    "method": "Log"
  }
}
```

## Privacy

Audit layer redacts fields whose names look sensitive: password, token, secret, api key, authorization, bearer, connection string. Long free-text fields are truncated to 500 characters. Raw draft payloads are not written by `AiAssistantService`; commit audit stores item-level report, counters and ids.
