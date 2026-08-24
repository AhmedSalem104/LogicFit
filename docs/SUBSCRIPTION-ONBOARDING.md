# Client subscription onboarding contract

## Purpose

The gym owner can create the first client and the client's subscription from the same screen, or create a subscription for an existing client. Both paths are tenant-scoped and reject overlapping active memberships.

## API paths

- `POST /api/Clients/onboard`: creates the client and, when `membership` is supplied, creates the subscription and optional membership card.
- `POST /api/Subscriptions`: creates a subscription for an existing client.

## Transaction boundary

`OnboardClientCommandHandler` owns the outer transaction because it coordinates identity/client, subscription, and membership-card work. The nested `CreateClientSubscriptionCommandHandler` validates and stages the subscription but joins that transaction through its internal `UseExistingTransaction` orchestration flag. Direct calls to `POST /api/Subscriptions` continue to open and own their own transaction.

This prevents the EF Core error caused by attempting to start a second transaction on the same `DbContext`, while preserving rollback of the entire new-client flow when any stage fails.

## Validation and isolation

The handler validates that the selected plan and client belong to the current tenant, that the plan is active, and that the client has no overlapping active or suspended subscription. A phone conflict or business conflict returns a clear conflict response without creating duplicate data.

The tenant is resolved from the authenticated context; client, plan, subscription, and card identifiers supplied by the browser are never treated as an authorization boundary.
Optional client email is normalized to a deterministic tenant-local fallback when it is blank, and duplicate phone/email values return a conflict instead of a database 500. The onboarding path also tolerates a missing zero-permission Client RBAC seed and ignores an invalid/stale seller claim for the non-critical commission link.
