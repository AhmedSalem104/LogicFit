# Platform/Tenant DbContext and Migration Split

Issue: #170

The repository now has two explicit EF Core context contracts:

- `PlatformDbContext` owns identity, workspace metadata, applications, plans, billing,
  platform RBAC and platform operations.
- `TenantDbContext` owns operational users, fitness, scheduling, finance, inventory, HR and
  tenant-local RBAC projections for exactly one `TenantId`.

The contexts are not a runtime shared-database fallback. The existing `ApplicationDbContext`
remains temporarily wired for the pre-cutover API so existing handlers and the current deployed
schema are not broken. Resolver/provisioning cutover is tracked by #174/#175/#166.

## Safety boundaries

- `TenantDbContext` requires a non-empty server-supplied `TenantId` and rejects writes for any
  entity whose `TenantId` does not match the context scope.
- Platform and tenant models explicitly ignore entities owned by the other store.
- Identity, workspace and billing ids in a tenant database are scalar references; no
  cross-database foreign keys are generated.
- The only model overlap is the declared local shared-contract projection (`Role`, `Permission`,
  `RolePermission`, `AuditLog`, `OutboxMessage`, `JobExecutionLog`).
- SQL Server options select different assemblies and history tables:
  `LogicFit.Platform.Migrations`/`__PlatformEFMigrationsHistory` and
  `LogicFit.Tenant.Migrations`/`__TenantEFMigrationsHistory`.

## Migration baselines

The first isolated baselines are checked into the two dedicated projects:

- `LogicFit.Platform.Migrations/Migrations/*PlatformBaseline*`
- `LogicFit.Tenant.Migrations/Migrations/*TenantBaseline*`

They are generated from the new context models and are not applied to Production by this task.
The 53 legacy shared-context migrations remain immutable. #170 records the inventory and creates
the new baseline; #174/#175/#166 will add resource reservation, resolver and idempotent
provisioning before any tenant database is cut over.

## Design-time and deployment note

The checked-in baseline files are the source of truth for this stage. Provisioning will call the
context-specific EF migrator after #174/#175 select a server-side mapping. The two migration
projects also contain design-time factories and wrappers so future EF scripts can be generated
without loading the legacy shared context. Do not apply either baseline to Production until the
resolver/provisioning cutover has a reviewed rollback plan.

Use `LOGICFIT_PLATFORM_EF_CONNECTION_STRING` or `LOGICFIT_TENANT_EF_CONNECTION_STRING` only in a
local operator shell. Never accept these values from a frontend request, commit them, or log them.
