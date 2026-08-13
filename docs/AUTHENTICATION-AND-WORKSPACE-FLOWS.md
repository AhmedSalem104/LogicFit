# Authentication and workspace flows

## Platform gym credentials and lifecycle deletion (Issue #214)

The Platform tenant screen exposes only the owner's login email and non-secret account state via
`GET /api/platform/tenants/{id}/credentials`. Passwords and password hashes are never returned,
stored in the dashboard, or written to audit records. `POST
/api/platform/tenants/{id}/credentials/reset` issues the existing single-use password-reset email
flow and records the administrative request.

Platform-admin gym creation now provisions/reuses the owner's `IdentityAccount` and creates a
pending owner `WorkspaceMembership`; approval promotes that membership to `Active`. A reused
Global Identity keeps its existing password. A newly provisioned admin account is marked as
operator-verified for this explicit onboarding path, while subsequent password changes remain on
the normal single-use reset-link flow.

`POST /api/platform/tenants/{id}/soft-delete` marks the workspace deleted, revokes active identity
selection sessions/refresh tokens, increments local permission versions, and keeps the tenant data
and database mapping for recovery. `POST /api/platform/tenants/{id}/restore` reverses that state
only while the active database mapping still exists.

Permanent deletion requires an exact gym-name confirmation and `PlatformOwner` authorization. The
server first creates a completed tenant BACPAC artifact, then invokes the configured database-purge
provider, removes workspace memberships/invites/join codes and active requests, marks the mapping
inactive, and releases the resource back to `Available`. The owner `IdentityAccount` is never
deleted; only its workspace membership association is removed. Deleting a Global Identity is not
part of this endpoint and would require a separate explicit administrative workflow after checking
for active workspaces and active requests. Monster Free remains `ManualOnly` for purge, so the API
fails closed until a reviewed operator capability is enabled.

## Current contract (Issue #161)

The active authentication system is Identity-first and Email + Password only. Phone Login, OTP,
Passkey, WebAuthn, and unlinked legacy sessions are not active flows. Email verification and
password recovery use short-lived, single-use links whose hashes are stored server-side.

## Identity login

```text
POST /api/identity/login { email, password }
  -> normalize email and validate active, email-verified IdentityAccount
  -> apply lockout and audit rules without logging credentials
  -> return identity context: activeWorkspaces, pendingApplications, invitations, nextAction
```

The identity context does not contain tenant permissions or a tenant JWT. A user with one active
workspace can continue automatically; a user with multiple workspaces chooses one. Pending
applications remain visible and do not block access to another active workspace.

For an already-Active Gym whose owner membership was left in
`PendingPlatformApproval` by an older release, the issuer performs a narrow, idempotent repair
during identity login: only that Gym owner's membership is promoted to `Active`. Pending client
or other workspace memberships are never promoted by this repair. This guarantees the single
active Gym owner path reaches `/api/identity/select-workspace` automatically instead of showing
an empty context screen.

## Platform login

```text
POST /api/platform/auth/login { email, password }
  -> normalize email and locate an active PlatformOwner/PlatformAdmin linked to that identity
  -> require active identity and verified email; apply lockout and audit rules
  -> load/repair the effective platform RBAC assignment and PermissionsVersion
  -> issue Platform JWT and Platform refresh cookie
```

There is no second OTP/passkey request. The Platform token has a separate audience and no TenantId.
`POST /api/platform/auth/refresh` rotates the Platform HttpOnly cookie; logout-all revokes the
server-side token family and clears the cookie.

## Registration and email links

```text
POST /api/identity/register { fullName, email, password, phoneNumber? }
  -> normalize and uniquely index NormalizedEmail
  -> create an unverified IdentityAccount (phone is contact data only)
  -> issue a hashed, single-use email verification link
POST /api/identity/verify-email { token }
  -> atomically consume the token and allow Email + Password login
POST /api/identity/password-reset { email }
  -> generic accepted response; send a reset link when eligible
POST /api/identity/password-reset/confirm { token, newPassword }
  -> atomically consume the link, change the password, and revoke old sessions
```

Raw links, passwords, access tokens, refresh tokens, and private uploads never appear in logs.

## Workspace selection

```text
POST /api/identity/select-workspace { workspaceSelectionToken, workspaceId }
  -> resolve the selection server-side
  -> require IdentityAccount.Active, Membership.Active, local User.Active,
     Workspace.Active, subscription access, and effective RBAC permissions
  <- tenant JWT + roles[] + permissions[] + TenantId + PermissionsVersion
```

The browser cannot submit a database name, connection string, or arbitrary TenantId. Tenant data
is resolved server-side through the active TenantDatabaseMapping.

## Creating a Gym or Freelance workspace

The public flow is intentionally short: the visitor chooses `Gym` or `FreelanceCoach`, selects an
active plan, enters the minimum owner/workspace fields, uploads payment proof, and submits once.
The server creates or reuses the global identity and keeps identity, Tenant, subscription, payment,
and provisioning steps behind the lifecycle gate. A password is collected only as the initial owner
credential; the form does not ask the visitor to create a Tenant, database, mapping, or membership.

The unified multipart endpoint is `POST /api/workspace-applications`. It creates an application,
payment request, plan snapshot, and pending subscription, then returns an opaque tracking session.
The tracking response exposes only the user journey (`Submitted`, `UnderReview`, `MoreInformation`,
`Preparing`, `Ready`, or a safe failure state). Requested fields can be updated with
`PATCH /api/workspace-applications/tracking/fields` and resubmitted without restarting.

Platform Admin uses explicit `ManageTenants` and `ManagePaymentRequests` permissions to review,
request information, approve/reject payment, approve/reject the application, and retry provisioning.

Application transitions are concurrency-safe and audited: Draft -> Submitted -> UnderReview ->
NeedsMoreInformation -> Submitted, or UnderReview -> Approved/Rejected. Repeated submissions use
revision history and idempotency keys.

### Platform-admin unified creation (Issues #244 and #245)

`POST /api/platform/workspace-applications` is the admin entry point for both `Gym` and
`FreelanceCoach`. It creates the same central application, plan snapshot, pending subscription,
and pending payment records; only the type-specific payload fields differ. A FreelanceCoach is a
standalone tenant with `WorkspaceType=FreelanceCoach`, its own database resource and subscription,
and an `FreelanceOwner` membership. It is never created as a gym employee.

The review queue uses `POST /api/platform/workspace-applications/{id}/approve-workspace` for both
workspace types. `approve-membership` remains reserved for Coach/Assistant/Client membership
applications. The response exposes separate payment, workspace, subscription, database, and
provisioning states plus `canAccessDashboard`, `requiredAction`, `nextStep`, and a safe user
message. A newly created identity may receive a one-time temporary password in the explicit create
response only; the password is hashed immediately, the resulting local owner is marked
`MustChangePassword`, and the value is never returned by list/detail endpoints or written to logs.

## Provisioning and activation

```text
Application Approved + Payment Approved
  -> Subscription PendingActivation
  -> reserve an Available DatabaseResource atomically
  -> apply Tenant migrations and seed
  -> create the local owner and RBAC role
  -> health-check the tenant database and record the encrypted mapping
  -> Workspace Active + Membership Active
  -> Subscription Active/Trial with StartDate and EndDate set now
```

Capacity shortages remain `AwaitingDatabaseCapacity`; provider failures remain
`ProvisioningFailed`. Neither starts a subscription term or issues a tenant session. The saga is
retryable and idempotent; Platform DB Outbox records coordinate work across databases.

The review list can filter the same application by `applicationType`, application `status`,
`paymentStatus`, `workspaceStatus`, `subscriptionStatus`, and `provisioningStatus`. Operators must
read the next action from the lifecycle response rather than interpreting `Active` as proof that
payment, database readiness, membership, and access are all complete.

The legacy `POST /api/platform/tenants` contract remains available for compatibility and accepts an
`Idempotency-Key` header, but the Platform dashboard no longer uses its old direct-creation form.
New Gym and FreelanceCoach workspaces must be started from `/workspace-applications`, because that
flow creates the plan snapshot, pending subscription, payment request, identity link, and
provisioning record required by the access gate. The `/tenants` screen is an operational list and
routes its creation action to that unified flow; this prevents a partial tenant from being created
without payment or database prerequisites.

The public tracking response `GET /api/workspace-applications/tracking` now carries the same safe
lifecycle facts for the applicant: `workspaceType`, payment/workspace/subscription/database and
provisioning states, `canAccessDashboard`, `requiredAction`, `nextStep`, `userMessage`, and the
last update time. It never returns connection material or a tenant token, and it keeps the dashboard
blocked until the server-side access gate is satisfied.

For a Gym, Platform approval/activation is also the authorization hand-off for the owner:
`Tenant.Active` activates any non-deleted owner `WorkspaceMembership` still in
`PendingPlatformApproval`, records `ApprovedAt`/`ApprovedBy`, and makes the workspace appear in
the next identity context. This is idempotent. The identity issuer also repairs an already-Active
Gym at the next owner login, so an operator does not need a second manual activation action after
an older release left the membership pending. Client memberships in
`PendingWorkspaceApproval` remain subject to the gym's own approval flow.

## Issue #248 — unified lifecycle and E2E acceptance

Both workspace types use `WorkspaceType` as the only type discriminator and the same provisioning
saga. Access is allowed only when application approval, payment approval, an active Tenant and
subscription, an `Assigned` database resource, a completed provisioning job, an active encrypted
mapping, and an active Owner membership all exist. Database connection material is accepted and
decrypted only inside the server.

Capacity shortage is represented as `AwaitingDatabaseCapacity`; connection or migration failure is
represented as `ProvisioningFailed` with a retryable job. Retry reuses the original application,
Tenant, subscription, job, and membership identities. The complete local E2E evidence and the
human-readable user journey are in `C:/Users/B-SMART/Desktop/LogicFit-Subscription-Flow-E2E-Guide.md`.

## Owner-managed workspace members (Issues #246 and #65)

`/api/workspace-members` is the unified owner flow for Gym team access. `POST` creates or reuses a
global `IdentityAccount`, creates the tenant-local `User` and `WorkspaceMembership`, replaces the
workspace role assignments, and writes one security audit event before saving. A duplicate active
membership in the same workspace is rejected; the same identity may receive a membership in a
different workspace.

The create response contains a one-time temporary password only when a new identity was created.
The password is BCrypt-hashed immediately, `MustChangePassword` is set on the local user, and the
value is not persisted, logged, or returned by list endpoints. `POST /{membershipId}/reset-password`
generates a new one-time password and clears lockout counters. Suspend, activate, and remove keep
the global identity intact while changing only workspace access. The stable access states are
`PendingSetup`, `PasswordChangeRequired`, `Active`, `Suspended`, `Locked`, and `Removed`.

## Invitations and clients

Workspace invitations are single-use email-bound links. The recipient must sign in with the
invited, verified email and accept the displayed workspace/role; the server creates the membership
and local user. No OTP challenge is required or exposed. Clients join through the configured invite,
join-code, or QR flow and receive no administrative permissions; AutoApproveClients controls their
membership state.

## Access gate and session boundaries

```text
Identity Active
  -> Membership Active
  -> User Active
  -> Workspace Allowed (not Suspended/Archived/ProvisioningFailed)
  -> Subscription Allowed
  -> RBAC Permission Allowed
```

Identity, Platform, and Tenant refresh cookies are separate. Changing a password, role, or
permission version revokes old refresh sessions. Frontend route guards only improve navigation; the
Backend remains the security boundary.

## Platform screen query resilience (Issues #290 and #88)

The platform tenant list and the Dashboard tenant widget intentionally do not calculate member
counts inside a correlated tenant projection. They page the tenant rows first, then run an
explicit cross-tenant member-count query with tenant filters disabled and merge the bounded result
in memory. This keeps the platform screens readable across production EF/SQL combinations where
the older correlated shape could translate to a 500.

The workspace-application list also groups payment snapshots by application and selects the latest
updated row. This makes historical duplicate payment records harmless to screen loading while
preserving the newest lifecycle status for review actions. No connection material is involved in
either response.

## Canonical references

- [API endpoint catalog](API-ENDPOINT-CATALOG.md) — generated from controllers.
- [Feature catalog](FEATURE-CATALOG.md) — implementation source and roles.
- [Users and permissions](USERS-AND-PERMISSIONS.md) — RBAC and isolation.
- [SaaS domain and data](SAAS-DOMAIN-AND-DATA.md) — states, snapshots, and migrations.
- [Operations and deployment](OPERATIONS-AND-DEPLOYMENT.md) — secrets, migrations, health checks,
  and rollback.
