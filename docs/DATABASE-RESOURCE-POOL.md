# Database Resource Pool

Issue: #174

The Platform database now records pre-created customer databases independently from Workspace
ids. `DatabaseResource` is the operator-managed pool row and `TenantDatabaseMapping` is the
server-side assignment. The database name is never derived from `TenantId`, accepted from a
frontend, or returned with an encrypted connection string.

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

## Registering pre-created databases

The canonical registration path is the authenticated Platform Admin screen at
`/database-resources`. It calls `POST /api/platform/database-resources/test-connection`, then
`POST /api/platform/database-resources`; the API tests the target database, protects the
connection string with `IConnectionStringProtector`, and stores it in `DatabaseResources`. The
connection string is never returned by a response and never belongs in source control.

For a first deployment where the admin UI is not available yet, the API also supports a one-time
server-only bootstrap from `DatabaseResourcePool:Resources`. Each entry needs `Provider`,
`DatabaseName`, and `ConnectionString`:

```json
{
  "DatabaseResourcePool": {
    "ProvisioningProvider": "ManualMonster",
    "SeedConfiguredResources": true,
    "Resources": [
      {
        "Provider": "ManualMonster",
        "DatabaseName": "<database-name>",
        "ConnectionString": "<server-secret>"
      }
    ]
  }
}
```

For a hosting environment that manages settings as environment variables, use the equivalent
keys `DatabaseResourcePool__Resources__0__DatabaseName` and
`DatabaseResourcePool__Resources__0__ConnectionString` (and indexes `1`, `2`, `3` for the other
resources). The seeder validates that the database name in each connection string matches the
configured name, encrypts it with `IConnectionStringProtector`, and inserts an idempotent
`Available` row. It does not reset a `Reserved`, `Provisioning`, or `Assigned` resource.

The production appsettings file is intentionally excluded from the publish output; configure
these values only in the hosting provider's protected application settings, run the app once so
the encrypted rows are created, then remove the bootstrap values. Never commit the connection
strings or place them in a migration. After that, `DatabaseResources` is the source of truth.

Platform-created gyms now create an approved internal provisioning request and use the same
allocation path as freelance workspaces. The existing final status `Assigned` is the database
pool's equivalent of `Allocated`.

The Monster Free example remains fail-closed (`AvailableTenantDatabaseCapacity: 0` and
`BlockActivationWhenNoCapacity: true`). The additive compatibility migration and isolated Platform
migration are checked in but are not applied to Production by #174.
