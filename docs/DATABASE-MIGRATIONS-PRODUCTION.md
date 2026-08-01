# Production database migrations

Creating and pushing an EF Core migration does not change the production database. The
schema changes must be applied against the same database used by the deployed API.

## Supported production operation

Production migrations never run from the IIS application-startup path. The API ignores
`Database__ApplyMigrationsOnStartup`; enabling it on the server is not a deployment procedure.
This keeps a failed or long-running DDL operation from putting the application into an IIS
recycle loop.

The supported release operation is the protected WebDeploy helper or the manual GitHub
`CD - Production` workflow. Both perform one ordered rollout:

1. Verify that the selected artifact is tree-equivalent to `origin/master` and that CI passes.
2. Record a current, verified BACPAC backup reference.
3. Generate and review the idempotent EF SQL from that exact release.
4. Apply pending migrations with the protected production database connection.
5. Stop before WebDeploy if backup, review, connection, or migration verification fails.
6. Publish the unified API and require `/health` to return a success response.
7. Run an authenticated smoke test for the affected flow.

Never update `dbo.__EFMigrationsHistory` manually to hide a migration mismatch.

## Mandatory pre-deploy checks

- Compare the migration IDs in `origin/master` and the canonical workspace with
  `dbo.__EFMigrationsHistory` on the target production database.
- Inspect the actual target schema as well as migration history. A previous manual repair can
  make those states differ.
- Create and verify a restorable BACPAC before applying a migration.
- Review the generated idempotent SQL. `DROP`, `DELETE`, and `TRUNCATE` require the explicit
  destructive-review approval even when they are guarded and intentional.
- Keep `LOGICFIT_PRODUCTION_DB_CONNECTION`, the WebDeploy profile, and the health URL only in
  the GitHub `production` Environment or the operator secret store.
- Stop the rollout on migration or health failure. Roll back the application binary only after
  operator review; do not reverse a data migration casually.

Useful inspection query:

```sql
SELECT MigrationId, ProductVersion
FROM dbo.__EFMigrationsHistory
ORDER BY MigrationId;
```

## Manual protected WebDeploy

Build and test the release first, then make the protected database connection available to the
current process without writing it to a file or command history. Generate the reviewed SQL and
publish with:

```powershell
dotnet ef migrations script --idempotent `
  --configuration Release `
  --project LogicFit.Infrastructure `
  --startup-project LogicFit.API `
  --output <reviewed-migration-script>

.\Scripts\deploy-webdeploy.ps1 `
  -PublishSettingsPath <protected-publish-settings-file> `
  -ContentPath <release-publish-directory> `
  -ApplyMigrations `
  -VerifiedBackupReference <verified-bacpac-name-or-reference> `
  -MigrationScriptPath <reviewed-migration-script> `
  -ApproveDestructiveMigrationReview `
  -HealthCheckUrl https://<production-host>/health
```

The helper copies the protected connection into `LOGICFIT_EF_CONNECTION_STRING` only for the
EF operator process. The design-time factory otherwise remains pinned to LocalDB, preventing a
normal developer command from reaching production accidentally.

`-ApproveDestructiveMigrationReview` is required only when the reviewed SQL contains a
destructive statement. It records operator intent; it does not make an unsafe statement safe.
The helper applies migrations before WebDeploy, verifies that EF reports no pending migration,
publishes without deleting server-only configuration, then verifies health.

## GitHub protected workflow

The manual production workflow requires:

- `RUNASP_UNIFIED_PUBLISH_SETTINGS_B64`
- `RUNASP_UNIFIED_HEALTHCHECK_URL`
- `LOGICFIT_PRODUCTION_DB_CONNECTION`
- a `backup_reference` input identifying the verified pre-deployment BACPAC
- `migration_review=MIGRATIONS-REVIEWED`
- `confirm=DEPLOY-PRODUCTION`

The generated SQL is retained as a short-lived workflow artifact for audit. GitHub secrets are
injected only into the protected Windows deployment job and are never printed.
