# LogicFit Agent Execution Rules

This file is the persistent execution record for automated work on the LogicFit repository. It is intentionally separate from `docs/LOGICFIT-PROJECT-STATUS.md`: `AGENTS.md` describes how work must be performed, while `docs` describes what the product currently does.

## Task lifecycle

For every non-trivial task:

1. Create a GitHub Issue in `AhmedSalem104/LogicFit` before implementation.
2. Record the issue number, scope, acceptance criteria, API/database impact, tests, and deployment impact.
3. Use a branch named with the issue number and a short purpose.
4. Inspect the current working tree and preserve unrelated user changes.
5. Implement the smallest safe change.
6. Update `docs/LOGICFIT-PROJECT-STATUS.md` for API, database, security, behavior, deployment, or architectural changes.
7. Add or update regression tests.
8. Run restore/build/test and migration validation.
9. Commit with the issue number and push the branch.
10. Open or update a Pull Request and report verification results.

## Production rules

- Manual Billing remains the active payment model unless a new issue explicitly changes it.
- Never deploy directly without a protected production environment, backup, migration review, health check, and rollback plan.
- Production deployment must run only after CI passes.
- Required production secrets must stay in GitHub Environment secrets or the server secret store; never commit them.
- Database migrations must be idempotent/reviewed and applied separately from application startup.
- A failed health check must stop the rollout and trigger rollback or operator review.
- Monster ASP deployment details must be recorded before enabling automatic deployment: host, user, app directory, service/container command, backup command, migration command, health URL, and rollback command.
- The supplied `logicfit-platform.runasp.net-WebDeploy.publishSettings` is a Platform API MSDeploy profile only. Its password must be stored as a protected GitHub Environment secret and must never be committed or printed.
- Tenant API deployment requires a separate WebDeploy profile or equivalent target before production CD can deploy the complete application.
- The protected CD workflow requires `RUNASP_PLATFORM_PUBLISH_SETTINGS_B64`, `RUNASP_TENANT_PUBLISH_SETTINGS_B64`, `RUNASP_PLATFORM_HEALTHCHECK_URL`, and `RUNASP_TENANT_HEALTHCHECK_URL` in the GitHub `production` Environment. Profiles are decoded only into the ephemeral Windows runner.

## Safety and correctness rules

- Treat TenantId and ownership checks as security boundaries.
- Use RowVersion/optimistic concurrency for shared mutable balances, stock, quotas, approvals, and counters.
- Use unique constraints for duplicate prevention and idempotency keys for retried commands.
- Do not use `Count + 1` for identifiers shared by concurrent requests.
- Do not log passwords, reset tokens, refresh tokens, payment proofs, or sensitive health data.
- Keep private uploads out of public static-file paths; use authorization and signed URLs when storage is migrated.
- Do not use destructive seed/reset operations in production without an explicit operator action and backup.

## Verification commands

```powershell
dotnet build LogicFit.sln -c Release --no-restore
dotnet test LogicFit.sln -c Release --no-build --verbosity minimal
dotnet ef migrations script --idempotent --project LogicFit.Infrastructure --startup-project LogicFit.API
```

## Decision log

### 2026-07-23

- Keep billing manual; harden its correctness instead of adding a payment gateway.
- Treat this `AGENTS.md` as the persistent execution memory for future repository tasks.
- Use `docs/LOGICFIT-PROJECT-STATUS.md` for product/API/database/deployment status.
- CI runs on every branch and PR, validates tests/migrations, and builds both Docker images.
- Production CD is manual and protected until Monster ASP deployment details and secrets are configured.
- Current verification baseline: 53 passing tests; three pre-existing nullable warnings remain.
