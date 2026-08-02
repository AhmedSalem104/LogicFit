# Tenant Backup Export (Issue #167)

Tenant owners (or a tenant role explicitly granted `CreateAndDownloadTenantBackup`) can export
their current Workspace database as a private BACPAC. The server resolves the tenant database
from `TenantDatabaseMapping`; the API never accepts a database name, connection string, or storage
path from the browser.

## Flow

1. The authenticated user calls `POST /api/tenant/backups/reauthenticate` with the current
   password. The server verifies the password and returns a short-lived, single-use
   `SensitiveActionGrant` for `tenant-backup-export`.
2. The user calls `POST /api/tenant/backups/exports` with that grant and an optional idempotency
   key. The server consumes the grant, enforces the daily/concurrent limits, and asks the central
   BACPAC orchestration service to export only the current tenant.
3. Export status is available through `GET /api/tenant/backups/exports` and
   `GET /api/tenant/backups/exports/{exportId}`. Failed exports expose only the safe error code
   `TENANT_BACKUP_EXPORT_FAILED`.
4. Before downloading, the user repeats password reauthentication through
   `POST /api/tenant/backups/reauthenticate-download`, then exchanges that grant with
   `POST /api/tenant/backups/exports/{exportId}/download-grant`.
5. The returned download token is hashed in the Platform DB, expires quickly, is bound to the
   user/tenant/export, and is consumed atomically on the first successful stream. The file is
   streamed from private storage as an opaque `workspace-*.bacpac` filename.

## Security boundaries

- Permission: `CreateAndDownloadTenantBackup` (seeded for Owner and FreelanceOwner through the
  tenant permission set).
- TenantId comes only from the authenticated server-side tenant context.
- Passwords, opaque grants, connection strings, database names, and private storage keys are not
  written to logs or returned as database metadata.
- RowVersion/concurrency checks prevent a sensitive grant or download grant from being reused.
- Previous active grants for the same user/export are revoked when a new grant is issued.
- Export requests are idempotent per tenant and key, limited to one active export per Workspace,
  and subject to `Backup:TenantExportDailyLimit` (default 3).
- `Backup:SensitiveGrantMinutes` and `Backup:TenantDownloadGrantMinutes` default to five minutes
  and are bounded by the service.
- Restore remains a Platform-only, capability-gated workflow (#165); a tenant cannot restore or
  select another Workspace database.

## Persistence and migration

The Platform/compatibility migration `AddTenantBackupExportSecurity` adds:

- `SensitiveActionGrants`
- `TenantBackupExports`
- `TenantBackupDownloadGrants`

The migration is additive and must be reviewed/applied separately in each environment. It has not
been applied to Production by this change.

## Monster limitation

The export uses the existing central BACPAC provider. Monster Free must still have an assigned
tenant mapping and enough private storage; no Shared-Database fallback is introduced. If the
provider cannot access the assigned database or private artifact, the export is recorded as failed
and no download grant is issued.
