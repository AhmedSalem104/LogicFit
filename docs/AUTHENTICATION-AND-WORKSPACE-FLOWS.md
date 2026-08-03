# Authentication and workspace flows

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

1. The authenticated identity chooses Gym or FreelanceCoach; it never submits an owner role.
2. The application is saved as Draft and collects the minimal business/contact fields.
3. The user selects Monthly, SemiAnnual, or Annual billing and receives an immutable Plan Snapshot.
4. A PaymentRequest is created and a JPG/JPEG/PNG/PDF proof is uploaded to private storage.
5. Submit changes Application to Submitted, PaymentRequest to PendingReview, Subscription to
   PendingPayment, Workspace to PendingApproval, and Membership to Pending.
6. Platform Admin uses explicit `ManageTenants` and `ManagePaymentRequests` permissions to review,
   request information, approve/reject payment, or reject the application with a reason.

Application transitions are concurrency-safe and audited: Draft -> Submitted -> UnderReview ->
NeedsMoreInformation -> Submitted, or UnderReview -> Approved/Rejected. Repeated submissions use
revision history and idempotency keys.

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

## Canonical references

- [API endpoint catalog](API-ENDPOINT-CATALOG.md) — generated from controllers.
- [Feature catalog](FEATURE-CATALOG.md) — implementation source and roles.
- [Users and permissions](USERS-AND-PERMISSIONS.md) — RBAC and isolation.
- [SaaS domain and data](SAAS-DOMAIN-AND-DATA.md) — states, snapshots, and migrations.
- [Operations and deployment](OPERATIONS-AND-DEPLOYMENT.md) — secrets, migrations, health checks,
  and rollback.
