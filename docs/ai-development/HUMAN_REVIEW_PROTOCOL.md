# Human Review Protocol for AI-Assisted Development

AI tools may draft code, documentation, datasets, prompts, and test cases. They do not replace review.

## Required Review

- Dataset rows require `review.status=approved` before use in final evaluation.
- Schema changes must be checked against backend validators and frontend/runtime adapters.
- Write tools must be reviewed for organization scope, validation, preview/confirm behavior, and audit logging.
- Security-sensitive code requires explicit review: auth, secrets, DB writes, file uploads, external HTTP clients.

## Required Automated Checks

```bash
python3 scripts/validate_ai_contract_schemas.py
python3 scripts/validate_ai_dataset.py
python3 scripts/validate_asr_dataset.py
dotnet test backend/CAT.Tests/CAT.Tests.csproj --no-restore
cd frontend && npm run lint && npm run build
```

## Rejection Criteria

Reject or rework AI-generated changes if they:

- introduce secrets or real credentials;
- bypass backend validation or human confirmation for writes;
- weaken organization isolation;
- change test data without updating reports;
- add undocumented model/runtime assumptions;
- pass only by disabling meaningful validation without documenting the trade-off.
