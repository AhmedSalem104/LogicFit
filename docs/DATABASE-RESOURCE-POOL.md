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
server-side assignment. The database name is never derived from `TenantId`, accepted from a
frontend, or returned with an encrypted connection string. Platform DTOs may expose only the
safe `HasProtectedConnection` Boolean; the protected value itself remains server-side.

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
