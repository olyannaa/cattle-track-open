# AI Usage During Development

This document covers AI tools used while developing the project, not the AI features inside the product.

## Tools and Roles

| Tool / agent | Used for | Human verification |
|---|---|---|
| Codex / ChatGPT | Repository audit, architecture review, implementation planning, code edits, documentation drafts | Local tests, lint, schema validation, manual review |
| LLM-assisted dataset drafting | Initial Russian command variants and fault-injection ideas | `review.status=approved` after human review |
| LLM-assisted code review | Security, backend AI flow, frontend lint and docs issues | Findings converted to code changes or backlog |
| LLM-assisted research automation | Evaluator scripts, model-selection protocols, report structure | Reproducible scripts and saved reports |

## Development Workflow

1. Define a concrete task and constraints.
2. Let the AI agent inspect the existing code and propose/implement changes.
3. Run deterministic checks: tests, lint, dataset validation, schema validation.
4. Human reviews any domain-sensitive outputs: dataset rows, schema enums, validation rules, write behavior.
5. Commit only after checks pass and secrets are absent.

## Measured Impact

- AI-assisted audits identified missing CI gates, dataset/schema drift, frontend lint debt, and incomplete technical evidence.
- AI-assisted dataset work produced a 190-row stratified Russian tool-calling corpus with manual approval.
- AI-assisted research automation created repeatable ASR/LLM/validator evaluation scripts and reports.
- AI-assisted implementation added backend AI orchestration, schema validation, draft/confirm flow, audit trail, and frontend AI UI changes that are covered by local tests/build checks.

## Known AI Failure Modes

- AI can overstate production readiness when metrics are only MVP-level.
- AI can generate schemas and datasets that drift apart; `validate_ai_dataset.py` is the guard.
- AI can propose broad refactors that are too risky before defense; changes must stay scoped.
- AI-generated documentation must be checked against actual commands and files.
