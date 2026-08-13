# Workspace capability contract

Status: task-branch implementation for Issue #296. This document describes the code in this
branch; it is not a production deployment confirmation.

## Purpose

`WorkspaceType` is the source of truth for the product surface inside the shared API. A JWT role
still controls who may perform an action, but it no longer determines whether a feature exists in
the selected workspace. The API evaluates both dimensions:

```text
authenticated identity
  + active tenant membership
  + RBAC permission
  + WorkspaceType capability
  + subscription/plan policy
  = allowed operation
```

The capability check reads the selected tenant from the server-side tenant record. It does not
trust a browser header, a route value, or a client-supplied workspace type.

## Capability sets

| Workspace | Available product surface | Deliberately unavailable surface |
|---|---|---|
| `Gym` | Gym experience, branches/facilities, attendance, staff, inventory, POS, gate access, membership cards, gym membership plans, shared coaching, reports, billing, settings, backups | Freelance-only team surface |
| `FreelanceCoach` | Coaching experience, clients, training, nutrition, progress, appointments, finance, reports, assistant team, billing, settings, backups | Branches, rooms/equipment operations, staff/payroll, inventory, POS, gate access, membership cards, group classes, gym membership plans |

The canonical constants are in `LogicFit.Domain/Authorization/WorkspaceCapabilities.cs`. The
frontend mirrors these names only for navigation and route protection; the backend remains
authoritative.

## API enforcement

Gym-only controllers carry a `WorkspaceCapabilities` policy in addition to their existing RBAC
policy. This includes facilities, attendance, employees/staff, inventory, POS, gate access,
membership cards, gym settings, group classes, commissions, and gym membership plans. Gym-only
report views are also protected. Coaching finance and coaching reports remain available to both
workspace types where the existing permission and plan allow them.

When a valid tenant member calls a feature that is not part of the selected workspace, the API
returns HTTP `403` with:

```json
{
  "statusCode": 403,
  "code": "WORKSPACE_CAPABILITY_NOT_AVAILABLE",
  "capability": "GymInventory",
  "message": "This feature is not available for the selected workspace type.",
  "errors": null
}
```

The response is intentionally distinct from `403` for a missing RBAC permission so clients can
render a useful blocked state without leaking tenant or database details.

## Identity and workspace selection

`select-workspace` and `refresh-token` return `workspaceType` and the calculated `capabilities`
for the selected tenant. Tenant-scoped RBAC authorization is loaded for the selected tenant;
assignments from another workspace are not included in the authorization snapshot. Existing
platform/global authorization remains separate.

`FreelanceOwner` is seeded with a curated owner permission set. It does not inherit
`TenantPermissions.ToArray()`, and stale gym-only role grants are removed during idempotent role
seeding. Removing a stale grant increments the permission version so old refresh sessions are
invalidated.

## Isolation and retry behavior

The capability requirement resolves the tenant from the authenticated `TenantId` claim and loads
the tenant type with query filters bypassed only for this server-side authorization lookup. All
feature handlers retain their normal tenant filters and ownership checks. A capability does not
grant cross-tenant access and does not replace subscription, membership, or plan checks.

## Verification

- `dotnet build LogicFit.sln -c Release --no-restore` — passed; five pre-existing nullable warnings remain.
- `dotnet test LogicFit.sln -c Release --no-build --verbosity minimal` — 207 passed.
- `Scripts/Export-ApiEndpointCatalog.ps1` generated 396 endpoint entries.
- Production deployment and merge are not claimed by this document. The protected `/health`
  check must be repeated after the branch is merged and deployed.
