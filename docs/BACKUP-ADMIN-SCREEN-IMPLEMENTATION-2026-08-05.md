# Backup admin screen implementation — 2026-08-05

Related: [LogicFit #239](https://github.com/AhmedSalem104/LogicFit/issues/239).

## Problem solved

The existing Platform Admin `/backups` screen only exposed the legacy single platform backup.
Operators could not start or verify one independent artifact for every active tenant mapping plus
the platform database, and the UI omitted batch scope, per-target status, checksum, manifest,
retry, and restore-provider state.

## Implemented scope

- `BackupArtifactDto` now returns `Sha256` in addition to safe status, size, storage key, and
  error-code metadata. It never returns a connection string, password, absolute path, or raw
  exception.
- `DatabaseBackupService` records `PlatformBackupBatchStarted` and
  `PlatformBackupBatchFinished` in the Platform Audit Log. The event subject is the batch ID;
  request payloads remain server-resolved.
- The existing Dashboard screen uses `FullSystem` by default. The server resolves the platform
  database and active assigned `TenantDatabaseMapping` rows; `AllTenants`, `AllGyms`,
  `AllFreelance`, and `Platform` remain available as explicit scopes.
- Batch history displays scope, status, target completion, per-artifact status/size/checksum,
  manifest download, and safe retry for `Failed` or `Partial` batches.
- Restore capability is displayed from `/api/platform/restores/capabilities`. No restore mutation
  was added, and `ManualOnly` remains an operator handoff state.
- The API endpoint catalog and Platform Dashboard screen/operations/architecture documents were
  refreshed.

## Verification and release boundary

- Dashboard `npm run build`: passed.
- Backend Release build: passed with five pre-existing nullable warnings and zero errors.
- Backup/controller contract tests: 9 passed.
- Idempotent EF migration script generation: passed; no migration was added or applied.
- No Production deployment, database mutation, Tenant mapping change, resource change, restore,
  or rollback was performed. The feature is local to the task branch until CI, PR review, and
  protected release gates pass.

## Security boundary

The screen remains permission-protected by `ManagePlatformBackups`. Backup records are immutable;
there is no edit/delete action. The existing private App_Data storage and filename validation stay
in force. Missing or failed backup evidence must block destructive or mapping-changing operations.
