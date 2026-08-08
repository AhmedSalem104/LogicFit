# Platform Dashboard Contract

Issue #162 adds a permission-aware operational contract for the Platform dashboard.

## Endpoints

- `GET /api/platform/dashboard` (`ManagePlatformReports`) keeps the existing KPI response and
  adds `operations` summaries for workspace applications, payment review, the database resource
  pool, provisioning jobs, central backups, and conditional restore capabilities.
- `GET /api/platform/database-resources` (`ManagePlatformBackups`) returns a bounded, server-paged
  resource-pool view. It includes status, workspace, health, size and schema metadata, plus the
  safe Boolean `HasProtectedConnection` indicating whether the operator-managed resource has a
  protected connection value. It never returns `DatabaseName`, connection strings, encrypted
  connection material or storage paths.
- `GET /api/platform/operations/provisioning` (`ManagePlatformReports`) returns a bounded list of
  retryable provisioning jobs with status, attempts, scheduling, safe error codes and references.
- `GET /api/platform/diagnostics/version` (`ManagePlatformReports`) returns the API contract
  version, a short build SHA, assembly version, runtime and environment for release compatibility
  diagnostics. Secrets and tokens are never included.

## Frontend rules

The dashboard may use `operations` to render review queues, capacity warnings, provisioning
failures, backup/restore state and the ManualMonster capability banner. It must keep actions
permission-filtered and treat the API as the authorization authority. A zero available resource
must be shown as capacity information; it is not permission to provision or to bypass the
`AwaitingDatabaseCapacity` lifecycle state.

All collection responses use the one-based bounded pagination contract from
`docs/API-ENDPOINT-CATALOG.md`.

## Security boundary

The dashboard is an aggregate/read-only view. Database resolution, mapping switches, exports,
restores and lifecycle mutations remain behind their existing server-side policies and
reauthentication/concurrency gates.
