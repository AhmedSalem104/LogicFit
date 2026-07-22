# LogicFit Project Status

Last reviewed: 2026-07-23

## Current product

LogicFit is a .NET 8 multi-tenant gym-management SaaS backend. It contains two APIs that share the Application, Domain, Infrastructure, and database layers:

- `LogicFit.API`: tenant/gym operations, audience `LogicFitUsers`.
- `LogicFit.Platform.API`: SaaS administration and manual billing, audience `LogicFitPlatform`.

The current billing model is manual payment approval. No payment gateway or webhook integration is enabled.

## Current architecture

The solution uses Clean Architecture-style boundaries, MediatR CQRS, EF Core/SQL Server, JWT authentication, database-backed RBAC, FluentValidation, Serilog, Docker, and xUnit.

Tenant requests resolve a tenant before authorization. Tenant query filters, tenant access gates, permission policies, and MediatR behaviors are used together. Platform users operate without a tenant claim.

## Recent correctness and security changes

- Password reset tokens are cryptographically generated, hashed at rest, short-lived, and only exposed in Development when explicitly enabled.
- Password change and reset use the registration password policy.
- Tenant ownership checks restrict client access to their own appointments, subscriptions, workout/diet plans, measurements, class enrollments, and bookings.
- Manual and QR gate access validate active client accounts, active subscriptions, and subscription freezes.
- Permission authorization validates the JWT `perm_ver` against the current database `PermissionsVersion`.
- Duplicate subscription refunds are rejected.
- Audit logs redact password and token properties.
- Upload deletion is constrained to the uploads root; upload subfolders and MIME types are validated.
- Global API rate limiting is enabled with configurable defaults.
- Wallet and stock entities use SQL Server rowversion concurrency tokens.
- Coupon uses use a rowversion concurrency token.
- Manual wallet transactions validate balance and update the user wallet balance.
- POS validates positive quantities, non-negative discounts, and duplicate products.
- Sale and invoice numbers no longer use `Count + 1`; they use collision-resistant timestamp/UUID values.
- EF concurrency conflicts return HTTP 409.

## Database migrations added by the hardening work

- `AddWalletAndStockConcurrency`
- `AddCouponConcurrency`

Migrations must be applied explicitly during deployment after a tested backup. The API does not silently migrate production at startup.

## Verification status

- `dotnet test LogicFit.sln -c Release --no-restore`: 53 passing tests.
- `dotnet build LogicFit.sln -c Release --no-restore`: successful.
- Three existing nullable warnings remain in coach-client and client-subscription query projections.

## CI/CD policy

- Every pull request must restore, build, test, validate the migration script, and build both Docker images.
- Production deployment must be a protected GitHub Environment operation and must run a health check after deployment.
- Production deployment must have a rollback procedure and must not expose secrets in repository files or logs.
- A future task that changes API, database, security, deployment, or behavior must update this document and add a GitHub Issue describing scope, acceptance criteria, tests, and deployment impact.

## Known remaining work

- Replace in-process rate limiting and memory cache with gateway/Redis-backed distributed controls for multi-instance production.
- Use atomic SQL updates/transactions for wallet and stock hot paths, and add concurrency integration tests.
- Add coupon usage idempotency and payment request idempotency keys.
- Move private uploads to object storage with signed URLs and malware scanning.
- Add distributed locks/idempotency for background lifecycle jobs.
- Add integration, end-to-end, load, concurrency, and tenant-isolation tests.
- Define the Monster ASP deployment target, application directory, service manager, backup command, and health URL before enabling automatic production deployment.
- A local WebDeploy settings file was provided for `logicfit-platform.runasp.net` (`MSDeploy`, site `site78301`). It configures the Platform API only; its password is intentionally not recorded. A separate Tenant API publish profile and the production backup/migration/rollback procedure are still required.

## Change log

### 2026-07-23

- Hardened tenant ownership and class enrollment flows.
- Hardened manual wallet transactions, POS validation, and concurrency handling.
- Added concurrency migrations for wallet/stock and coupons.
- Added file path/MIME validation and API rate limiting.
- Added initial CI/CD and project-status documentation.
