# Monster Capability Assessment

Status: assessment only. This document records the current Monster Free limits used by
the Database-per-Workspace design. It does not authorize a production migration, reset,
database creation, restore, or deployment.

## Observed SQL capability

The read-only probe against the configured Monster database succeeded and reported:

| Capability | Observed result | Design consequence |
|---|---|---|
| SQL Server edition | Express 64-bit, 17.0.4060.2 | Do not assume Enterprise features or Always-On. |
| Current database | Online, SIMPLE recovery, about 144 MB | It is a Platform database today, not a tenant pool member. |
| Database connection | Works with the current server-side secret | Secrets remain server-only. |
| `BACKUP DATABASE` | Granted for the current database | Native backup remains an administrative/provider capability. |
| Database creation | Not granted (`IsDbCreator=0`) | `ManualMonsterProvisioningProvider` can only use pre-created databases. |
| `ALTER ANY DATABASE` | Not granted | No programmatic database lifecycle on the current account. |
| `RESTORE VERIFYONLY` | Rejected by `CREATE DATABASE` permission in `master` | Restore is `ManualOnly`/`Disabled` until a privileged operator is supplied. |
| Always-On | `IsHadrEnabled=0` | Do not rely on Always-On for jobs or failover. |
| API access to native `.bak` | Not proven | Tenant native-backup download stays disabled until file transfer is tested. |

## Current provider capabilities

The current temporary environment is represented by configuration/provider capabilities,
not by silently changing the target architecture:

```json
{
  "MaxDatabases": 1,
  "AvailableTenantDatabaseCapacity": 0,
  "SupportsProgrammaticDatabaseCreate": false,
  "SupportsAlwaysRunning": false,
  "SupportsScheduledTasks": false,
  "SupportsAutomaticBackupFtp": false,
  "SupportsNativeRestore": false,
  "SupportsBacpacExport": true,
  "DatabaseStorageLimit": "1GB",
  "WebsiteStorageLimit": "5GB",
  "ApplicationMemoryLimit": "256MB"
}
```

The values are capability guards for the current free hosting plan. They do not create a
shared-database fallback. If no database is `Available`, onboarding remains:

```text
Workspace = AwaitingDatabaseCapacity
Subscription = PendingActivation
```

## Provider boundary

`LocalSqlProvisioningProvider` is the integration-test/development implementation. It may
create and delete test databases only, apply the Tenant migration assembly, run seed and
health checks, and exercise retry, mapping-switch and BACPAC import behavior.

`ManualMonsterProvisioningProvider` is the hosting implementation. It reads pre-registered
database resources, reserves them atomically, builds the server-side connection, applies
Tenant migrations and seed, runs health checks, creates the encrypted mapping and assigns
the database. It never creates or deletes a Monster database.

Native Monster backup and restore are separate from application-readable BACPAC export:

- `ManualMonsterNativeBackupProvider`: administrative/native `.bak`, no tenant download.
- `BacpacTenantExportProvider`: application-side private export, SHA-256, retention and
  single-use signed download grant; disabled by capability guard until storage/memory/file
  access are proven.

## Background execution

The current application uses in-process `BackgroundService` implementations. They are not a
durable scheduler and cannot be assumed to survive free-hosting sleep/recycle behavior.
Provisioning and backup jobs therefore remain persistent, resumable and idempotent in the
Platform database, and expose an explicit manual-run path for Platform operators. A future
Monster upgrade may attach a scheduler without changing provider contracts.

## Required operator proof before enabling restricted features

On an upgraded/prepared Monster environment, an operator must provide a test database and
prove, without production data:

1. SQL connection from the API process to at least two databases.
2. Tenant migrations and seed on an empty prepared database.
3. BACPAC export, API read, and controlled transfer/download.
4. Native backup output visibility to the service that owns the download.
5. `RESTORE VERIFYONLY` and `RESTORE DATABASE` under the dedicated restore account.
6. Database size/free-space discovery.
7. A reliable scheduler/always-running mechanism.

Until these checks pass, native restore, native tenant download and final activation are
blocked by capability guards.

