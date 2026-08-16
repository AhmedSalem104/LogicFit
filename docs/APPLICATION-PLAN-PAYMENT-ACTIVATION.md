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

## Payment-proof retention and platform review

The platform review queue exposes the proof as a protected server stream; it never returns a
connection string, storage key, or public upload URL. `POST
/api/platform/payment-requests/{id}/proof` is the operator replacement/attachment endpoint for a
pending workspace payment. It accepts only JPEG, PNG, or PDF up to 10 MiB, calculates SHA-256 on
the server, and writes an immutable `PaymentProof` version plus the `PaymentRequest.ProofFileUrl`.

Replacing a proof marks the previous row `IsCurrent=false` but does not delete its file or metadata.
The complete retained metadata is available through `GET
/api/platform/payment-requests/{id}/proofs`; a current or historical file is streamed through
`GET /api/platform/payment-requests/{id}/proof` with an optional `?version=N`. Only the current
version can satisfy the payment approval gate. A workspace payment cannot be approved without a
current proof, and payment approval remains separate from workspace approval/provisioning.

The migration is additive and review-only. It has not been applied to Production.
