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

## Runtime request cutover (Issue #208)

`TenantDatabaseRoutingMiddleware` runs immediately after `TenantMiddleware` and before identity
workspace access, tenant access, authorization, or handlers. It calls the resolver using only the
server-side `CurrentTenantId`, stores the result in a request-scoped boundary, and returns
`503 TENANT_DATABASE_UNAVAILABLE` when the mapping is missing or invalid.

`TenantDatabaseContextAccessor` creates one `TenantDbContext` for that request. The compatibility
`IApplicationDbContext` registration is now a routing proxy: Platform-owned sets use the real
`PlatformDbContext`; tenant-owned sets use that request's TenantDbContext. A resolved tenant can
never select `ApplicationDbContext`.

The old context is retained only for legacy startup migrations and compatibility-only platform
reports/provisioning rows while the explicit existing-workspace transfer is staged. No Production
database, mapping, or tenant data was changed by Issue #208's task branch.
