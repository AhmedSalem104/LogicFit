# Workspace Provisioning Saga

Issue #166 introduces the persistent, retryable activation workflow for a workspace application.
It is deliberately separate from the Platform review transaction because provisioning crosses the
Platform database and a tenant database without a distributed transaction.

## Activation contract

```text
Application Approved
  -> Payment Approved
  -> Subscription PendingActivation
  -> Workspace Provisioning
  -> Database Reserved
  -> Tenant migrations + seed
  -> Owner created in tenant database
  -> Tenant health check
  -> Database Assigned
  -> Workspace Active
  -> Membership Active
  -> Subscription Active
```

`StartDate`, `EndDate`, `Workspace.Active`, and an active owner membership are written only after
the provider reports a successful migration and connectivity check. A capacity shortage produces
`AwaitingDatabaseCapacity`; provider failures produce `ProvisioningFailed`. Neither state starts
the subscription term.

Platform-created gyms use the same job/provider path but do not have a payment or subscription
gate. After a successful allocation and migration, the resource is `Assigned` while the gym and
owner membership remain `PendingApproval` until the platform operator approves the gym. Freelance
workspace applications continue through payment and subscription activation.

## Persistent job and idempotency

`ProvisioningJobs` is a Platform DB table with a unique
`workspace-provisioning:{ApplicationRequestId}` idempotency key, attempt count, status,
resource id, safe error code, retry time, and row version. The saga reuses the same tenant,
resource mapping, local owner, membership, subscription, and outbox key on retries. It never
creates a second tenant database for the same application.

Platform operators can retry an approved application through:

```text
POST /api/platform/workspace-applications/{applicationId}/retry-provisioning
```

The endpoint requires `ManageTenants` through the existing controller policy and accepts no
database name or connection string.

## Providers

- `ManualMonsterProvisioningProvider` reads operator-registered Monster resources, reserves one
  atomically, applies the isolated `LogicFit.Tenant.Migrations` assembly, seeds the owner, checks
  connectivity, and records an encrypted server-side mapping. It never creates or deletes a
  Monster database.
- `LocalSqlProvisioningProvider` is selected with
  `DatabaseResourcePool:ProvisioningProvider=LocalSql` in Development/CI. It uses the same
  isolated tenant migration and health-check engine against multiple pre-created local SQL
  resources; test harnesses own creation and cleanup of those databases. It never falls back to
  the Platform database.

The current Monster Free capability guard remains authoritative: with no `Available` resource,
workspace activation is blocked and the job remains `AwaitingDatabaseCapacity`.

## Migration boundaries

The additive Platform migrations create `ProvisioningJobs` in the compatibility and isolated
Platform migration assemblies:

- `20260802144655_AddProvisioningSaga`
- `20260802144724_AddProvisioningSaga`

Tenant databases use the independent baseline and history table from `LogicFit.Tenant.Migrations`;
the legacy shared Platform migration chain is never applied to a tenant database.

## Failure handling

Provider exceptions are logged with application/tenant/resource identifiers only. Connection
strings, passwords, tokens, payment proofs, and raw payloads are not logged. A failed resource is
marked `Faulted`, the workspace is `ProvisioningFailed`, the payment remains approved, and the
subscription remains `PendingActivation` until an operator retry succeeds.

## Platform gym registration contract (Issue #226)

`POST /api/platform/tenants` accepts an optional `Idempotency-Key` header. The server stores only a
one-way request scope hash; it never stores the header value or owner password. Retrying the same
body with the same key resumes the existing application and provisioning job. A request without a
key still receives a deterministic body fingerprint so a client timeout cannot create a second
gym, owner, or mapping.

Provisioning failures use the shared exception response contract:

```json
{
  "code": "DATABASE_CAPACITY_UNAVAILABLE",
  "message": "safe actionable message",
  "requestId": "trace-id",
  "details": {
    "retryable": true,
    "tenantId": "...",
    "applicationRequestId": "...",
    "databaseResourceId": "...",
    "retryEndpoint": "/api/platform/workspace-applications/{id}/retry-provisioning"
  }
}
```

Capacity is reported as HTTP `409` with `DATABASE_CAPACITY_UNAVAILABLE`. Provider, migration,
mapping, and tenant-database health failures preserve their stable safe error code and use HTTP
`503` when retryable. The response never includes a provider exception, SQL message, connection
string, database name, or password. The Platform UI should display `code`/`message`, retain the
`requestId`, and retry the original idempotent request or call the returned retry endpoint only
after the operator has repaired capacity or the failed resource.
