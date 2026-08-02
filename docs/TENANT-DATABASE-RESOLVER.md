# Tenant database resolver (Issue #175)

The Platform database is the only source of truth for a Workspace database assignment. The
application accepts a `TenantId` obtained from the authenticated server-side context and resolves
the active mapping; it never accepts a database name, server name, or connection string from an
HTTP request.

## Resolution rules

`TenantDatabaseResolver` reads an active `TenantDatabaseMapping` together with its
`DatabaseResource`. Resolution succeeds only when:

1. The mapping is active.
2. The resource is `Assigned`.
3. The resource is reserved for the same `TenantId`.
4. Protected connection material can be decrypted by the configured server-side data-protection
   key.

Missing, stale, cross-tenant, or undecryptable mappings fail closed and return no connection. The
resolver logs only the mapping/tenant identifiers and never logs protected or decrypted material.

`TenantDatabaseResolution` is an infrastructure/application boundary type. It is not an API DTO,
is not serialized, and must not be copied into Platform or Tenant frontend contracts.

## Components

| Component | Responsibility |
|---|---|
| `ITenantDatabaseResolver` | Server-only resolution contract accepting only a tenant id. |
| `PlatformTenantDatabaseMappingReader` | Reads the mapping/resource projection from Platform DB. |
| `DataProtectionConnectionStringProtector` | Protects connection material at rest and unprotects it in memory. |
| `TenantDbContext` | Receives the resolved connection and an explicit tenant scope; it is never built from client input. |

This is the resolver boundary for the later provisioning saga (#166) and tenant request pipeline.
The existing `ApplicationDbContext` remains the compatibility context until that cutover is
completed; this issue does not migrate or modify any Production database.
