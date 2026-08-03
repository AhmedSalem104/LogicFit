# Central backup orchestration

Issue #173 adds a platform-owned orchestration layer without removing the existing BACPAC
backup screen or endpoint.  Native `.bak` and restore operations remain outside this issue;
Monster Free does not provide the server capabilities needed for them.

## Targets and isolation

`POST /api/platform/backups/batch` accepts only a `scope`, optional tenant IDs for
`SelectedTenants`, and an optional idempotency key.  The API never accepts a database name or
connection string.  The service resolves the platform connection from server configuration and
tenant connections from active `TenantDatabaseMappings` joined to an `Assigned`
`DatabaseResource` in the platform database.

Supported scopes are `Platform`, `SelectedTenants`, `AllGyms`, `AllFreelance`, `AllTenants`, and
`FullSystem`.  Every target produces a separate private BACPAC file; a full-system operation
also produces a JSON manifest containing safe metadata and SHA-256 checksums.  Connection strings,
passwords, absolute paths, and file contents are never returned by the API or written to logs.

## Reliability

`BackupBatches` and `DatabaseBackups` are platform-owned records.  A unique idempotency key makes
retries return the existing batch rather than create duplicate exports.  A SQL Server application
lock plus an in-process guard prevents overlapping batches across API instances.  Per-target
exports are bounded by `Backup:MaxConcurrent` (clamped to 1-4), and each partial file is removed
on failure.  Batch status is `Completed`, `Partial`, or `Failed` and each artifact records its own
status, size, checksum, timestamps, and safe error code.

The existing `POST /api/platform/backups` remains a compatibility shortcut for a platform-only
batch.  `GET /api/platform/backups/batches` lists recent batch metadata and
`POST /api/platform/backups/batches/{batchId}/retry` creates a new idempotent attempt for a
failed or partial batch. The existing download endpoint streams only a validated filename from
private `App_Data` storage.

## Scheduling and Monster

The daily hosted job now requests an idempotent `FullSystem` batch using the configured UTC time.
The job remains disabled unless `Backup:Enabled=true`; it is not assumed that Monster Free keeps a
background process alive.  Operators can run the same endpoint manually.  Retention is applied to
private BACPAC and manifest files using `Backup:RetentionDays`.

The current provider is BACPAC export through DacFx because the application can write the file
itself.  Native Monster backup, restore, capacity alerts, and tenant-owner download grants are
Native Monster backup/restore and capacity alerts remain separate capabilities. Tenant-owner
BACPAC export/download is implemented in [TENANT-BACKUP-EXPORT.md](TENANT-BACKUP-EXPORT.md) and
uses this service only after server-side tenant authorization and password reauthentication.

## Database changes

The additive migrations are:

- `LogicFit.Infrastructure`: `20260802155234_AddCentralBackupOrchestration`
- `LogicFit.Platform.Migrations`: `20260802155354_AddCentralBackupOrchestration`
- `LogicFit.Infrastructure`: `20260802162826_AddTenantBackupExportSecurity`
- `LogicFit.Platform.Migrations`: `20260802162856_AddTenantBackupExportSecurity`

They add only `BackupBatches` and `DatabaseBackups` to platform-compatible stores.  No tenant
migration is changed and no production database is modified by this PR.
