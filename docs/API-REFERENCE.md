# LogicFit API reference

The generated [API endpoint catalog](API-ENDPOINT-CATALOG.md) is the authoritative list of
controllers, routes, request contracts, response contracts, and policies. Run
`Scripts/Export-ApiEndpointCatalog.ps1` after every API contract change.

## Authentication contract (Issue #161)

All active authentication surfaces use Email + Password:

| Method | Endpoint | Result |
|---|---|---|
| POST | `/api/identity/register` | Creates an unverified identity and sends a one-use email link. |
| POST | `/api/identity/verify-email` | Atomically consumes the email verification link. |
| POST | `/api/identity/login` | Returns identity context after verified Email + Password login. |
| POST | `/api/identity/password-reset` | Sends a one-use reset link without revealing account existence. |
| POST | `/api/identity/password-reset/confirm` | Changes the password and revokes old sessions. |
| POST | `/api/platform/auth/login` | Validates the linked active PlatformOwner/PlatformAdmin identity and issues a Platform session. |
| POST | `/api/platform/auth/refresh` | Rotates the Platform refresh cookie and returns a new access token. |
| POST | `/api/platform/auth/logout-all` | Revokes the current user's sessions and clears the Platform cookie. |
| POST | `/api/identity/select-workspace` | Exchanges the identity context for a tenant session for an active membership. |

There is no active Phone Login, OTP verification, Passkey, WebAuthn, or legacy login/register
controller route. Historical migrations may mention removed tables, but no OTP provider or runtime
service is registered. `20260803090742_RemoveLegacyOtpArtifacts` is the guarded cleanup migration.

## Session and error rules

- Access tokens are short-lived JWTs. Refresh tokens are surface-specific HttpOnly, Secure,
  SameSite cookies and are never returned to JavaScript or written to logs.
- Platform tokens do not contain a tenant id. Tenant tokens are issued only after membership,
  workspace, subscription, and RBAC permission gates pass.
- `401` means missing/expired/invalid authentication, `403` means missing permission, `404` means
  a missing resource, and `409` means an invalid state transition or stale concurrency version.
- The server is the authority for TenantId, roles, permissions, ownership, and subscription gates.

## Pagination

Platform collection endpoints use `{ items, totalCount, page, pageSize, totalPages,
hasPreviousPage, hasNextPage }`. `page` starts at 1 and `pageSize` is bounded to 100.
