# Agent Task Log

| Date | Agent/tool | Task | Output artifact | Verification |
|---|---|---|---|---|
| 2026-07-09 | AI-assisted workflow | Tool-calling dataset design and schema structure | `ai-contracts/schemas/v1` | schema validation, human review |
| 2026-07-10 | AI-assisted workflow | LLM tool-calling benchmark design | `scripts/run_llm_tool_calling_openai_compatible.py` | evaluator dry runs |
| 2026-07-11 | AI-assisted workflow | Constrained validator ablation tooling | `scripts/evaluate_constrained_validator_ablation.py` | local reports, human review |
| 2026-07-13 | AI-assisted workflow | ASR/LLM model selection methodology | `docs/ai/02-research-plan.md` | latency/quality gates |
| 2026-07-14 | AI-assisted workflow | AI assistant MVP use-case specification | `06-ai-assistant-mvp-use-cases.md` | design review |
| 2026-07-14 | AI-assisted workflow | Backend AI integration and audit trail | `backend/CAT/Services/Ai`, `docs/ai-assistant-audit.md` | `dotnet test` |
| 2026-07-15 | Codex | Schema gate, resolver and write safety hardening | backend AI services and tests | backend AI tests |
| 2026-07-15 | Codex | Frontend assistant UX hardening | `frontend/src/app/ai-event-input` | lint/build and manual checks |
| 2026-07-19 | Codex | Repository packaging and verification | repository structure and CI | build/test checks |

Datasets, runtime deployment details, and environment-specific settings are managed outside this repository.
