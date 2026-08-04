# Tenant database runtime cutover — Issue #208

Status: implementation on the task branch `codex/208-tenant-db-runtime-cutover`. This document
describes the source behavior in this branch; it is not a claim that Production has been migrated
or deployed.

## Final ownership

| Store | Owns | Runtime context |
|---|---|---|
| Platform DB | Identity accounts, ASP.NET Identity, workspaces, memberships, applications, plans, SaaS subscriptions, payments, provisioning jobs, `DatabaseResources`, `TenantDatabaseMappings`, backups, restores and platform RBAC | `PlatformDbContext` |
| Tenant DB | Domain users, profiles, clients, coaches, exercises, nutrition, workouts, attendance, appointments, chat, branches, finance, POS, inventory, HR/payroll and tenant-local RBAC projections | `TenantDbContext(tenantId)` |

Each Gym or Freelance Workspace has one active mapping and one operational database. Staff and
clients in that workspace use the same Tenant DB; a user never receives a database of their own.

## Request flow

```text
JWT / host / X-Tenant-Id
        │
        ▼
TenantMiddleware -> CurrentTenantId
        │
        ▼
TenantDatabaseRoutingMiddleware
  └─ Platform DB: active mapping + Assigned resource + matching reservation
  └─ decrypt connection string in memory
  └─ create request-scoped TenantDbContext with TenantId
        │
        ▼
Identity workspace gate -> subscription/permission gates -> controller/handler
        │
        ├─ platform-owned DbSet  -> PlatformDbContext
        └─ tenant-owned DbSet    -> TenantDbContext for the resolved workspace
```

If the tenant has no valid mapping, the request receives `503 TENANT_DATABASE_UNAVAILABLE`.
There is no shared-database fallback for a request that already has a TenantId. This is the
security boundary that prevents Top Gym rows from being read by another workspace.

The existing handlers still depend on `IApplicationDbContext` during the staged refactor. The
registered implementation is a routing proxy: it returns a DbSet from the correct real context,
so daily handlers for clients, exercises, attendance, inventory, finance and HR are already
executed against Tenant DB without changing hundreds of constructors at once. The old
`ApplicationDbContext` is retained only as a compatibility host for platform-only code that still
reads the legacy shared User projection and for legacy startup migrations. It is never selected
for a resolved tenant request.

## Provisioning flow

1. Gym creation and Freelance approval create/retain a central workspace placeholder.
2. `WorkspaceProvisioningSaga` reserves one `Available` resource in Platform DB using a
   serializable transaction.
3. `ManualMonsterProvisioningProvider` decrypts the protected resource value in memory, runs the
   Tenant migration assembly, checks connectivity, seeds the local reference catalog and tenant
   RBAC projection, then creates/repairs the local owner assignment before recording the active
   encrypted mapping.
4. The resource becomes `Assigned`; the workspace/membership/subscription is activated only
   after the mapping exists.
5. A retry is idempotent: an existing valid mapping is health-checked and reused.

Platform and tenant changes are not a distributed SQL transaction. Provisioning therefore remains
owned by the persistent saga/outbox workflow; a handler must not assume a write to both stores is
atomic.

## Existing workspace migration gate

Existing shared tenant rows must be copied to the assigned Tenant DB before routing is enabled for
that workspace. The safe sequence is:

1. Take and verify a backup of the shared source.
2. Apply the Tenant baseline to the prepared resource.
3. Copy the workspace-owned rows while preserving IDs and remapping local RBAC keys by stable
   role/permission codes.
4. Reconcile counts and foreign-key references for users, profiles, clients, fitness, attendance,
   finance, POS, inventory and HR.
5. Run the isolation smoke tests with two different mappings.
6. Enable routing for that workspace and keep the source rows read-only until rollback expiry.

This branch does not silently run that one-time transfer against Production. It must be an
explicit, backed-up operator job because identity IDs, local RBAC IDs and existing schema drift
must be inspected on the actual server first.

## Secrets and key persistence

`DatabaseResource.EncryptedConnectionString` and `TenantDatabaseMapping.EncryptedConnectionString`
are protected with ASP.NET Data Protection. They are not returned by API DTOs or written to logs.
The key ring is persisted under `DataProtection:KeyDirectory`; the example uses
`App_Data/DataProtection-Keys`. Production must point this setting at durable storage shared by
all IIS workers. Losing or rotating that key ring without a planned re-protection operation makes
existing mappings unreadable and intentionally stops tenant traffic.

## Verification

- `TenantDatabaseRuntimeRoutingTests` verifies tenant-owned sets use TenantDbContext, platform
  sets use PlatformDbContext, and a missing mapping cannot use the shared context.
- Full test suite: 172 passing tests on this task branch.
- No Production database, mapping, backup or connection secret was changed by this branch.
