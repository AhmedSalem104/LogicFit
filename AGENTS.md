# LogicFit Agent Execution Rules

This file is the persistent execution record for automated work on the LogicFit repository. It is intentionally separate from `docs/LOGICFIT-PROJECT-STATUS.md`: `AGENTS.md` describes how work must be performed, while `docs` describes what the product currently does.

## Task lifecycle

For every non-trivial task:

1. Create a GitHub Issue in `AhmedSalem104/LogicFit` before implementation.
2. Record the issue number, scope, acceptance criteria, API/database impact, tests, and deployment impact.
3. Use a branch named with the issue number and a short purpose.
4. Inspect the current working tree and preserve unrelated user changes.
5. Implement the smallest safe change.
6. Update `docs/LOGICFIT-PROJECT-STATUS.md` and every affected canonical user-flow document for API, database, security, behavior, deployment, architectural, role, status, or screen changes.
7. Add or update regression tests.
8. Run restore/build/test and migration validation.
9. Commit with the issue number and push the branch.
10. Open or update a Pull Request and report verification results.

## Canonical workspace and worktree handoff

The user-facing canonical Backend workspace is
`C:\Users\B-SMART\Desktop\Projects\LogicFit Project\LogicFit`. Temporary Git worktrees are
implementation-isolation folders for preserving unrelated changes; they are not separate projects
and must never become the only local location where completed files are visible.

- Announce the exact path and branch whenever a temporary worktree is created, and state why the
  canonical workspace cannot safely be used for that task.
- Before creating a branch, compare its start commit with `origin/develop`. A failed fast-forward is
  a blocker to that branch setup; never continue a task branch from the stale local branch.
- Before a publish or final handoff, verify the production artifact is tree-equivalent to
  `origin/master`, not merely built from a similarly named local branch.
- Verify every required migration exists both in `origin/master` and in the canonical workspace,
  and separately verify its ID/schema effect in production. A generated SQL file or temporary
  worktree does not satisfy the canonical-workspace check.
- After merge/release, inspect the canonical workspace. If it is clean, synchronize it safely to the
  branch the user expects and confirm the commit plus required files. Preserve any divergent local
  branch tip under a clearly named local backup branch before realigning it; do not use
  `git reset --hard`.
- If the canonical workspace is dirty, do not switch, overwrite, or merge it silently. Report the
  exact branch/commit gap and leave the user's changes untouched until they choose the integration
  method.
- Remove obsolete temporary worktrees only after confirming they are clean and their commits are
  reachable from Git or a preserved branch. Report which temporary path was removed.

## Documentation currency gate

Documentation is part of the definition of done for every project change. The current source code and domain rules are the authority; a planned, branch-only, or unavailable behavior must be labelled as such and must never be documented as released.

The documentation record is synchronized across all three repositories: `LogicFit` (Backend),
`LogiFit_Angular` (Tenant UI), and `LogiFit_Platform_Admin_Dashboard` (Platform UI). A
cross-repository feature is not complete until every affected repository updates its own
canonical flow, screen, architecture, and API references in the same task. If a repository is
not affected, the Pull Request must state `No documentation impact` for that repository and why.
Every affected document must distinguish local work, an open PR, merge to `develop`, release,
deployment, and production verification; none of those states may be inferred from another one.

For every change, before opening the Pull Request:

1. Update `docs/FEATURE-CATALOG.md` when a feature is added, removed, renamed, materially changed, or moved between projects. Keep its implementation source, roles, and affected flow accurate.
2. Update the canonical flow that users or operators follow. Use `docs/AUTHENTICATION-AND-WORKSPACE-FLOWS.md` for identity, login, workspace selection, applications, memberships, and access gates; use `docs/PRODUCT-AND-FLOWS.md` for other product journeys. Update the relevant frontend repository screen/flow documentation whenever a screen, route, step, or UI behavior changes.
3. Update `docs/USERS-AND-PERMISSIONS.md`, `docs/SAAS-DOMAIN-AND-DATA.md`, and `docs/OPERATIONS-AND-DEPLOYMENT.md` whenever the change affects authorization, domain data/state/migration, or operations/deployment respectively.
4. For every API route, policy, request, or response-contract change, run `Scripts/Export-ApiEndpointCatalog.ps1` and commit the generated `docs/API-ENDPOINT-CATALOG.md`; do not hand-maintain endpoint rows.
5. In the Pull Request description, name the updated catalog/flow documents and state whether any frontend repository must be merged or deployed before the documented behavior is available.

Do not record secrets, passwords, refresh tokens, connection strings, publish profiles, or private customer/health data in documentation.

## Production rules

- Manual Billing remains the active payment model unless a new issue explicitly changes it.
- Never deploy directly without a protected production environment, backup, migration review, health check, and rollback plan.
- Production deployment must run only after CI passes.
- Required production secrets must stay in GitHub Environment secrets or the server secret store; never commit them.
- Database migrations must be idempotent and reviewed before release. The unified API applies any
  reviewed pending migrations at startup before seeding; the protected deployment pre-apply step
  remains preferred and a verified backup plus rollback plan are still mandatory.
- A failed health check must stop the rollout and trigger rollback or operator review.
- Monster ASP deployment details must be recorded before enabling automatic deployment: host, user, app directory, service/container command, backup command, migration command, health URL, and rollback command.
- The supplied `logicfit-platform.runasp.net-WebDeploy.publishSettings` is a Platform API MSDeploy profile only. Its password must be stored as a protected GitHub Environment secret and must never be committed or printed.
- Tenant API deployment requires a separate WebDeploy profile or equivalent target before production CD can deploy the complete application.
- The protected CD workflow requires `RUNASP_UNIFIED_PUBLISH_SETTINGS_B64`, `RUNASP_UNIFIED_HEALTHCHECK_URL`, and `LOGICFIT_PRODUCTION_DB_CONNECTION` in the GitHub `production` Environment. The profile is decoded and the database connection is injected only into the ephemeral Windows runner; neither value may be printed.

## GitHub branching and review policy

- `develop` is the protected daily integration branch; `main` (or `master`, if that is the repository release branch) is protected production/release history.
- Never push directly to `develop`, `main`, or `master`, and never force-push or delete them.
- Start every task from the latest `origin/develop` and use `feature/<issue>-<slug>`, `fix/<issue>-<slug>`, or `chore/<issue>-<slug>`.
- Open a Pull Request from the task branch into `develop`. CI must pass and at least one reviewer must approve before merge.
- Release changes move from `develop` to `main`/`master` through a reviewed Pull Request.

```powershell
git fetch origin
git switch develop
git pull --ff-only
git switch -c feature/<issue>-<slug>
git push -u origin feature/<issue>-<slug>
```

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
- CI runs on every branch and PR, validates tests/migrations, and builds the unified API Docker image.

### 2026-07-27

- Platform administration is a module inside `LogicFit.API/Features/Platform`, not a separate host or project.
- `LogicFit.API/appsettings.json` and its environment variables are the single configuration source for both Tenant and Platform API routes.
- Production deployment publishes one unified API artifact and uses one protected WebDeploy profile and health-check URL.
- Production CD is manual and protected until Monster ASP deployment details and secrets are configured.
- Current verification baseline: 53 passing tests; three pre-existing nullable warnings remain.
- Establish `develop` as the protected integration branch; require task-branch Pull Requests and passing CI for all merges.

### 2026-07-28 — production migration incident prevention

- A Git migration file is not a database deployment. Every production migration must have an explicit apply step, a recorded target database, and a post-apply health check.
- Before applying production migrations, inspect `__EFMigrationsHistory` and the actual production schema. Production may contain columns added manually or may be missing columns that exist in the EF model.
- Migrations targeting shared/legacy Monster databases must be idempotent against both states: use guarded `COL_LENGTH`/`OBJECT_ID` SQL for add/drop/constraint operations when the target schema may differ. Never assume `AddColumn` or `DropColumn` is safe merely because the migration is new.
- Never make a migration change that drops an existing column without verifying the column exists in every supported production schema. Preserve data and constraints; use a reviewed backup and rollback plan.
- Do not treat EF model warnings about query filters as startup failures. The release blocker is the first `SqlException`/`Unhandled exception` in stdout, not the preceding warnings.
- If IIS reports `ISAPI reported an unhealthy condition`, retrieve `stdout_*.log` before changing code. This message is only a symptom of a startup crash/recycle.
- For Monster ASP, deploy with stdout logging enabled during diagnosis, ensure the logs directory is writable, capture the first root exception, then disable verbose stdout after recovery.
- Startup migration is the final safety net after Issue #147: the unified API serializes migration
  execution across workers, applies compiled pending migrations before seeding, and verifies that
  none remain. If the database account lacks DDL permission, stop and use the approved database
  operator procedure.
- After each migration fix, run `dotnet build`, `dotnet test`, generate an idempotent migration script, deploy to a schema that represents the production drift, and verify `/health` before merging/releasing.
- QR/media changes must not introduce storage-provider startup failures: local storage remains the default; R2 is selected only when all required R2 settings are present. Keep sensitive files private and test the authenticated media endpoint separately.
- Do not declare a deployment healthy from a successful WebDeploy sync alone. Require application startup, database migration completion, `/health` 200, and a smoke test for the affected endpoint.

### 2026-08-01 — protected migration-aware publishing

- The manual production workflow pre-applies reviewed pending migrations before WebDeploy. The IIS
  startup migrator repeats the pending check and normally performs no work.
- A production migration run requires a verified BACPAC reference, the reviewed idempotent SQL artifact, explicit approval for destructive statements, and the protected database connection secret.
- A migration failure or remaining pending migration stops the rollout before WebDeploy; a failed health check stops completion and requires operator review.

### 2026-07-25

- Platform administration collection APIs use the one-based `{ items, totalCount, page, pageSize, totalPages }` pagination contract; page size is bounded to 100.
- Preserve immutable financial and operational history: do not add generic edit/delete APIs to invoices, audit logs, backup records, Outbox records, jobs, or payment requests.
- Use lifecycle commands for tenants, subscriptions, administrators, plans in use, and feature overrides; destructive deletion is limited to safe configuration records such as a feature dependency.
- The Angular dashboard must use the shared server paginator for each collection screen rather than local pagination of an unbounded API response.
- The documentation index at `docs/README.md` is the required written hand-off for product flows, permissions, Platform screens, API contracts, domain data, tenant application, and operations; update the affected document in every future change.
- `docs/API-ENDPOINT-CATALOG.md` is generated from every Tenant and Platform controller by `Scripts/Export-ApiEndpointCatalog.ps1`. Any endpoint, policy, request, or response-contract change must regenerate this catalog in the same task; do not manually maintain endpoint rows.
- The Platform dashboard assistant is a local, permission-filtered operational guide. It must not invoke a mutation directly, expose secrets, or claim an external LLM integration unless a server-side, reviewed integration is actually added.

### 2026-07-30 — authentication documentation contract

- Any authentication, identity, OTP, session, invite, join, workspace-selection, or access-gate
  change must update `docs/AUTHENTICATION-AND-WORKSPACE-FLOWS.md`, the generated API catalog,
  the relevant domain/security/operations documents, and both affected frontend flow/screen
  references in the same task. Documentation must explicitly distinguish local/unreleased,
  merged, deployed, and production-verified behavior.

### 2026-08-01 — canonical workspace handoff

- Temporary worktrees are isolation mechanisms only. Completed changes must be verified in the
  canonical project directory before publish handoff.
- A local branch name is not proof that it matches its remote. Compare commits and tree content,
  preserve divergent local tips, and never continue from a failed fast-forward.
- Migration delivery requires three separate checks: the file in `origin/master`, the file in the
  canonical workspace, and the migration/schema state in the target database.

### 2026-08-01 — Platform Owner recovery

- Never seed a fixed Platform Owner password or log bootstrap credentials. Initial creation and
  legacy-identity repair must be an explicit, temporary server-secret operation.
- Disable and remove all `PlatformBootstrap__*` settings immediately after the verified recovery
  run; routine password changes and account recovery must use the normal authenticated flows.

### 2026-08-02 — startup migration safety net

- Issue #147 changes the unified API to apply compiled pending EF migrations before `DataSeeder`
  and before serving requests. SQL Server `sp_getapplock` serializes the operation across IIS
  workers, and startup verifies that no migration remains pending.
- This runtime safety net does not generate migration source files, replace migration review, or
  remove the backup/rollback requirement. The protected pre-publish migration step remains the
  preferred production path.
- `Database__StartupMigrations__Enabled=false` is an emergency operator switch only. The default is
  enabled; lock and command timeouts are bounded configuration values.
