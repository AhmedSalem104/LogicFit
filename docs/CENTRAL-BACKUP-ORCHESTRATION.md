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
retries return the existing batch rather than create duplicate exports.  The shared
`IDistributedLockProvider` uses a SQL Server session-owned application lock, while an in-process
guard prevents overlapping batches in the same worker.  Per-target exports are bounded by
`Backup:MaxConcurrent` (clamped to 1-4), and each partial file is removed on failure.  Batch status
is `Completed`, `Partial`, or `Failed` and each artifact records its own status, size, checksum,
timestamps, and safe error code.

The service verifies that the private `App_Data` storage can be created and written before starting
database work. Manifest writes are atomic. Mapping decryption failures fail closed instead of
silently reducing coverage. SQL, storage, metadata, and cryptographic infrastructure failures are
logged with safe exception types and returned as `503` with a stable backup error code; raw
connection material and exception details never cross the API boundary. A manifest failure records
the terminal batch state so the operator can retry it without leaving a permanent `Running` batch.

## Download and retry correctness

Generated artifact keys include the artifact GUID so two targets exported in the same second
cannot overwrite one another. Manifest keys use the same allowlisted GUID form. The protected
download policy accepts both these current keys and the legacy timestamp keys, while rejecting
paths, unsupported extensions, and incomplete names; this keeps every key returned by a batch
downloadable without exposing the storage directory.

Retry is target-scoped. A retry request carries the tenant IDs of only failed artifacts. An
explicit empty tenant set is meaningful: it represents a `FullSystem` batch whose platform
artifact failed, so the retry includes the platform target and does not expand back to every
active tenant. The retry creates a new idempotent batch and never duplicates completed artifacts
from the original batch.

Backup target resolution is fail-closed: if an active tenant mapping cannot be decrypted, the
batch does not silently omit that tenant and report complete coverage. The API returns a safe
service-unavailable result instructing the operator to repair the mapping before retrying.

## Protected pre-deployment gate

When production has no verified backup yet, use the manual GitHub Environment workflow
`.github/workflows/protected-backup.yml` with `confirm=CREATE-PROTECTED-BACKUP` and the released
`master` SHA. The workflow runs the same `IBackupService` FullSystem orchestration in a protected
operator process, verifies every artifact's size and SHA-256, and uploads only
`App_Data/PrivateBackups` through the protected WebDeploy profile. BACPAC files are never stored in
the repository, workflow logs, or a GitHub artifact. Use the reported
`protected-webdeploy:<run-id>:<batch-id>` reference for the protected production deployment.

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
