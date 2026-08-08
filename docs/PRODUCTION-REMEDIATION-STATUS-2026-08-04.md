# Production remediation and release status — 2026-08-04

This document records the database-resource, Data Protection, provisioning, and admin-dashboard
work completed during the current incident. It is an operational snapshot, not a source of
secrets. Connection strings, passwords, JWT values, publish profiles, and raw encrypted values
must never be added here or anywhere in Git.

## Executive summary

The original registration failures had two related causes:

1. The platform database-resource pool had no `Available` resource while two rows were `Faulted`.
2. The normal resource edit endpoint intentionally rejects replacing the protected connection of
   an allocated resource. The correct operation is the protected repair action (`wrench` in the
   admin dashboard).

The code now supports repair for both allocated resources and unallocated failed resources. A
successful repair tests the operator-provided connection, protects it with ASP.NET Data
Protection, clears the failure state, and returns an unallocated resource to `Available`. The
current Production snapshot has available capacity, but `/health` is still `503 Unhealthy`; the
remaining readiness problem is tracked below and must be resolved before claiming an end-to-end
registration recovery.

No database was deleted, truncated, or directly edited as part of this documentation/release
verification.

## Repositories and released commits

| Area | Repository branch | Released commit / PR | Result |
|---|---|---|---|
| Backend | `master` | [`106a9b2`](https://github.com/AhmedSalem104/LogicFit/commit/106a9b260c98247a06b416acc2d48ea0050b465a), [PR #223](https://github.com/AhmedSalem104/LogicFit/pull/223) | Repair and release failed database resources |
| Backend | `master` | [`d98982a`](https://github.com/AhmedSalem104/LogicFit/commit/d98982a9b70198fd454eefa2c0c56129d0eddca0), [PR #222](https://github.com/AhmedSalem104/LogicFit/pull/222) | Durable Data Protection key storage and health validation |
| Admin dashboard | `main` | [`13fea6b`](https://github.com/AhmedSalem104/LogiFit_Platform_Admin_Dashboard/commit/13fea6b1f87430a7f6aecee1e1b25994c449702f), [PR #71](https://github.com/AhmedSalem104/LogiFit_Platform_Admin_Dashboard/pull/71) | Repair action visible for failed resources |
| Admin dashboard | `main` | [`67b9652`](https://github.com/AhmedSalem104/LogiFit_Platform_Admin_Dashboard/commit/67b965250beca75d806f57d163c29bf31c5ef39f), [PR #70](https://github.com/AhmedSalem104/LogiFit_Platform_Admin_Dashboard/pull/70) | Repair action visible for allocated resources |

The backend release CI for `106a9b2` completed successfully, including build, tests, migration
validation, and Docker image build. The dashboard build and Vercel preview checks also passed.

## Current Production snapshot

The following checks were read-only and were verified on 2026-08-04. Resource status names are
the API/admin names, not raw database integer values.

| Check | Current result | Interpretation |
|---|---|---|
| `GET /health` | `503 Unhealthy` | Production readiness is not green yet. |
| Database resources | 4 total | Pool rows exist. |
| `Available` | 2: `db62139`, `db62140` | New workspace capacity exists. |
| `Assigned` | 2: `db62141` (Smart Gym), `db62278` (Air Gym) | Active workspace mappings remain allocated. |
| `Faulted` | 0 | The two previously failed pool rows were repaired/released. |
| Data Protection migration | Applied | `DataProtectionKeys` exists in the Platform database. |

The `503` is therefore not explained by a lack of available capacity. The readiness check also
attempts to decrypt every non-empty protected resource value and active tenant mapping. One or
more older protected values still cannot be decrypted by the currently deployed key ring, or the
Production process is not yet running the released backend binary. The server log must identify
the exact protected value type/row; the connection material itself must never be logged.

## What is complete

### Backend

- Added durable Data Protection key persistence in the central Platform database.
- Kept the server-side `App_Data` key location as a mirrored recovery path and excluded it from
  Web Deploy overwrite/delete behavior.
- Added readiness validation for protected database-resource and active-mapping values.
- Added `POST /api/platform/database-resources/{id}/repair-connection` handling for:
  - an `Assigned` resource with an active workspace mapping; and
  - an unallocated `Available`, `Faulted`, or `Maintenance` resource.
- The repair endpoint tests the submitted connection, stores only the protected value, writes an
  audit event, and releases a repaired unallocated row to `Available`.
- Kept the normal update guard: changing the connection of an allocated/reserved/provisioning
  resource through `PUT` returns `DATABASE_RESOURCE_ALLOCATED`. This prevents accidental mapping
  replacement through the ordinary edit form.
- Preserved the owner Global Identity behavior: workspace deletion removes the workspace link,
  not the owner identity, unless the separate explicit identity-deletion rules are satisfied.

### Admin dashboard

- Added the repair action for allocated resources.
- Added the repair action for failed resources.
- The repair form never displays the current protected connection value.
- The failed-resource repair message explains that a successful repair returns the row to the
  available pool.

### Verification

- Local Release build passed.
- Targeted backend tests passed (12/12).
- Backend PR verify and Docker checks passed.
- Dashboard `npm run build` passed.
- Production resource counts and statuses were checked without mutating the database.

## What is still pending

1. Publish backend `master` commit `106a9b2` from the canonical Visual Studio source directory:
   `C:\Users\B-SMART\Desktop\Projects\LogicFit Project\LogicFit`.
2. Recycle the Production application and confirm the released binary is active.
3. Inspect the server log for the protected value that keeps `/health` unhealthy.
4. Use the dashboard wrench action to repair every affected active mapping, entering the actual
   Monster connection string only in the protected admin form. Do not use the normal pencil/edit
   action for an allocated row.
5. Confirm `GET /health` returns `200`.
6. Retest new gym registration end to end and verify all of the following:
   - a new tenant and owner request are created;
   - an `Available` resource moves through `Reserved` and `Provisioning` to `Assigned`;
   - tenant migrations and owner seeding complete;
   - the active mapping is written and protected;
   - the owner can log in and reach the expected workspace context;
   - plans, subscription, payment-request, and notification endpoints respond normally.
7. If registration still fails after health is green, capture the HTTP status, `errorCode`, and
   correlation/request identifier from the browser Network response and attach them to the
   provisioning issue below. Never attach credentials or connection strings.

## Operational contract to keep

```text
Normal edit (PUT) on an allocated resource
  -> rejected with DATABASE_RESOURCE_ALLOCATED

Repair (POST .../{id}/repair-connection, confirm=true)
  -> test connection
  -> protect connection with Data Protection
  -> update resource/mapping and audit log
  -> unallocated repaired row becomes Available
```

The pool is allocated by the server-side provisioning saga. A frontend does not choose a database
name or write a connection string directly into a tenant mapping. The Platform database remains
the source of truth for resource availability and assignment.

## Issue #239 readiness correction — 2026-08-08 (local task branch)

The protected read-only production probe found four pool resources: two `Assigned` resources with
active mappings and two `Faulted` resources with no active mappings. All four rows have protected
connection material; the Platform database has one Data Protection key, two active mappings, and
no backup batch or artifact records. Production `/health` remained HTTP 503 after the safe stdout
diagnostic and rollback.

The task branch corrects the readiness boundary so only `Reserved`, `Provisioning`, and `Assigned`
resources used by runtime routing are included in the protected-value health check. Faulted,
retired, maintenance, and other unallocated pool rows remain visible to the resource-operations
surface and must be repaired before allocation, but they no longer make unrelated active tenants
unavailable. Backup target resolution now fails closed with a safe `503` when an active mapping
cannot be decrypted, preventing a false `Completed` full-system result.

Local verification on the task branch: Release build passed, full test suite passed (`212/212`),
focused remediation/backup tests passed (`14/14`), and both idempotent migration scripts generated
successfully. The change is not deployed; Production health remains HTTP 503 until the released
binary is published and recycled. No Production database, mapping, connection, backup, or restore
was changed by this task branch.

The release gate now also contains a protected pre-deployment backup operator. It invokes the
central FullSystem backup service, verifies artifact checksums, and transfers only private backup
files to the server; no BACPAC is committed or uploaded as a GitHub artifact.

## Follow-up GitHub issues

The remaining work is intentionally tracked as separate issues so it can be closed with evidence:

- [#225 — Recover health from legacy protected workspace mappings](https://github.com/AhmedSalem104/LogicFit/issues/225): exact `/health` and server-log acceptance checks.
- [#226 — Make new gym registration failures actionable and retryable](https://github.com/AhmedSalem104/LogicFit/issues/226): end-to-end registration and retry contract.
- [#227 — Add protected post-deploy smoke checks](https://github.com/AhmedSalem104/LogicFit/issues/227): repeatable release verification covering health, repair, and registration.

These issues must not contain raw connection strings, passwords, JWTs, publish settings, or full
request bodies containing secrets.
