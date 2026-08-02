# Application, plan and payment activation (Issue #168)

Workspace creation now carries the selected billing data through Platform DB before review.
The selection is captured as an immutable JSON snapshot; changing a Plan later cannot change the
amount or limits shown to the reviewer for an existing request.

## Submission contract

The freelance application submission accepts a plan, billing cycle, exact payment amount,
idempotency key, and private proof metadata. Proof metadata accepts only JPEG, PNG, or PDF and a
maximum of 10 MiB. The request contains an opaque private-storage key, SHA-256, content type, file
name, and size; public URLs are rejected. The file itself is not placed in the database or a JWT.

Submission creates, atomically in Platform DB:

* a central Workspace placeholder in `PendingApproval`;
* an `ApplicationRequest` in `Submitted` with `PlanSnapshotJson`;
* a `TenantSubscription` in `PendingPayment` without start/end dates;
* a `PaymentRequest` in `PendingReview` linked to the application and identity;
* version 1 of an immutable `PaymentProof` row;
* the tracking session used by the onboarding UI.

Retries with the same identity and idempotency key do not create another payment request; a fresh
short-lived tracking session is returned for the existing application.

## Review and activation boundary

Payment approval for an application is deliberately not workspace activation. It moves the
subscription to `PendingActivation` and the placeholder to `PendingSubscription`, leaving
`StartDate` and `EndDate` empty. Database reservation, migrations, seed, owner creation and health
checks are performed by the provisioning saga (#166); only that saga can activate the subscription
and membership.

Existing tenant renewal/payment requests retain their legacy approval behavior for compatibility.
All payment and application transitions continue to use the existing concurrency columns and
review permissions.

The migration is additive and review-only. It has not been applied to Production.
