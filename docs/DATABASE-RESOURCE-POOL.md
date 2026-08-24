# Database Resource Pool

## Release after permanent gym deletion (Issue #214)

An assigned resource is eligible for `ReleaseAsync` only after the permanent-delete service has
verified a completed tenant backup and the selected purge provider has succeeded. The mapping is
then marked inactive and its connection material is tombstoned; the pool resource itself retains
its operator-managed connection material for future provisioning and returns to `Available`.
Monster Free intentionally exposes `ManualOnly` purge capability, so the API cannot empty or drop a
production database directly.

Issue: #174

The Platform database now records pre-created customer databases independently from Workspace
ids. `DatabaseResource` is the operator-managed pool row and `TenantDatabaseMapping` is the
server-side assignment. The safe database name and optional server host/port are stored as
operator metadata; they are never derived from `TenantId`, and are returned without any credential
material. Platform DTOs expose diagnostics such as the last test time, duration, safe error code
and user-facing reason. The encrypted connection string itself remains server-side.

## Platform console operations

`GET /api/platform/database-resources` returns the database name, provider, safe server metadata,
workspace mapping, provisioning/backup summary, health timestamps and the last connection-test
diagnostics. It never returns `ConnectionString` or `EncryptedConnectionString`.

`POST /api/platform/database-resources/test-connection` tests a new value without persisting it;
`POST /api/platform/database-resources/{id}/test-connection` tests the already protected value on
the server. Registration and repair persist a sanitized result so a failed resource remains
visible as `Faulted` with an actionable code such as `DATABASE_AUTHENTICATION_FAILED`,
`DATABASE_NOT_FOUND`, `DATABASE_CONNECTION_TIMEOUT`, or `DATABASE_CONNECTION_REFUSED`.

`DELETE /api/platform/database-resources/{id}` is a permanent metadata/credential deletion, not a
drop of the external database. The server rejects it when the resource has an active tenant
mapping, an active provisioning or restore job, a reservation, or any historical backup artifact.
Every successful deletion is audited. The UI therefore displays a delete button only when the
server reports `canDelete=true` and explains the blocking reason otherwise.

## Lifecycle

`Available -> Reserved -> Provisioning -> Assigned`

Operational exception states are `Maintenance`, `RestorePending`, `Faulted`, and `Retired`.
Reservation is serialized in the Platform database at `Serializable` isolation and uses EF
row-version concurrency. A missing Available row returns `DATABASE_CAPACITY_UNAVAILABLE`; the
activation saga must keep the Workspace in `AwaitingDatabaseCapacity` and the subscription in
`PendingActivation`.

## Provider boundary

`ManualMonsterProvisioningProvider` only reserves an operator-registered row. It does not create
or delete Monster databases and does not claim that native `.bak` restore is available. Migration,
seed, health check, mapping assignment and retry are owned by the later provisioning saga (#166).
`IConnectionStringProtector` uses ASP.NET Data Protection; plaintext connection strings never
enter API DTOs or logs.

## Registration and connection-test contract

`POST /api/platform/database-resources/test-connection` and `POST
/api/platform/database-resources` accept a SQL Server connection string. The server parses and
normalizes it, clamps the connection timeout, requires a SQL Server data source and an initial
catalog, opens the connection, and verifies the active database with `SELECT DB_NAME()` before a
resource is accepted. When registering a new resource, `DatabaseName` may be omitted because the
server can safely use the connection string's `Initial Catalog`; when it is supplied, it must
match that catalog. A malformed or mismatched identity returns `400` with a safe validation code;
an otherwise valid string that cannot connect returns `422`. No connection material is returned.

The platform resource list also treats historical backup rows with a null resource reference as
unlinked history rather than dereferencing them. This keeps the operator screen readable while
preserving the fail-closed mapping and backup rules.

The Monster Free example remains fail-closed (`AvailableTenantDatabaseCapacity: 0` and
`BlockActivationWhenNoCapacity: true`). The additive compatibility migration and isolated Platform
migration are checked in but are not applied to Production by #174.
