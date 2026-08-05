# Backup admin screen review — 2026-08-05

Related issues: [#239](https://github.com/AhmedSalem104/LogicFit/issues/239) and
[#230](https://github.com/AhmedSalem104/LogicFit/issues/230).

## Review status

This is a read-only source audit and planning record. It is not a release record and does not
prove Production behavior. During this review no source code, database, `TenantDatabaseMapping`,
`DatabaseResource`, tenant database, or Production deployment was changed. No build or test run
was required because the review did not change executable files.

The review used the canonical workspaces required by `AGENTS.md`:

- Backend: `C:\Users\B-SMART\Desktop\Projects\LogicFit Project\LogicFit`.
- Platform Dashboard: `C:\Users\B-SMART\Desktop\Projects\LogicFit Project\LogiFit_Platform_Admin_Dashboard`.

The Dashboard worktree already contained unrelated local changes in configuration and a local log
file. Those changes were preserved and were not part of this review.

## Existing screen contract

The existing Dashboard route is `/backups` and is protected by `ManagePlatformBackups`. Its
`BackupsService` currently calls only:

| UI operation | API contract currently used |
|---|---|
| Load file list | `GET /api/platform/backups` |
| Load readiness | `GET /api/platform/backups/status` |
| Create from the button | `POST /api/platform/backups` |
| Download a file | `GET /api/platform/backups/{fileName}/download` |

The screen displays readiness, BACPAC format, retention, UTC schedule, file count, file name,
creation time, size, status, and create/download actions. It does not ask the operator to select a
backup scope or tenant.

The `POST /api/platform/backups` call is a compatibility shortcut for a `Platform` batch. It is
not a request for one backup per tenant and it is not a `FullSystem` backup.

## Server capability found during the audit

The Backend already contains a newer platform-owned orchestration contract:

- `BackupScope`: `Platform`, `SelectedTenants`, `AllGyms`, `AllFreelance`, `AllTenants`, and
  `FullSystem`.
- `POST /api/platform/backups/batch` for scope-based creation.
- `GET /api/platform/backups/batches` for batch history.
- `POST /api/platform/backups/batches/{batchId}/retry` for failed or partial batches.
- One private `DatabaseBackup` artifact per resolved target.
- Server-side resolution from active tenant mappings and `Assigned` resources; the client does not
  send database names or connection strings.
- Idempotency keys, an in-process guard, and a SQL application lock to prevent duplicate or
  overlapping batches.
- SHA-256 generation and a safe JSON manifest for the batch operation.
- Private storage below `App_Data` and retention cleanup.

The API also has restore contracts, but the current Monster provider reports `ManualOnly` and
disabled. `LocalSql` restore is explicitly Development/CI-only. The Dashboard currently has no
restore route, restore capability view, batch history view, or restore rehearsal flow.

## Requirement gap matrix

| Requirement | Current evidence | Assessment |
|---|---|---|
| Separate backup for every Tenant | Backend batch scope exists, but `/backups` calls the platform-only compatibility endpoint and has no tenant/scope selector or per-target result view. | **Gap in the existing screen** |
| Full Platform backup | `FullSystem` exists in Backend, but the screen does not request it or show its manifest/artifacts. | **Gap in the existing screen** |
| Success and checksum verification | The service computes and stores SHA-256 and writes it to the manifest. The current list DTO and screen do not expose the checksum, and no screen verification action was found. | **Partially implemented server-side; UI/verification gap** |
| Restore and rollback | Restore jobs and capability-gated providers exist. Monster is `ManualOnly`; the UI has no restore route and no restore rehearsal action. | **Intentionally disabled in Production; UI gap for safe status visibility** |
| Audit trail | Restore and tenant-owner export flows contain explicit audit calls. The reviewed Platform backup UI has no audit view, and the reviewed platform backup service does not emit a matching append-only backup start/success/failure/verification audit event. | **Gap to resolve under #239** |
| No connection material exposure | The screen does not display connection material. Batch requests resolve targets server-side and the API policy is `ManagePlatformBackups`. | **Pass for the reviewed boundary** |
| Retention and private storage | Readiness shows retention; the service restricts storage to `App_Data` and prunes expired files. | **Implemented, but not evidence of a verified restore** |
| Failure/partial/retry visibility | Backend has `Partial`, `Failed`, artifact statuses, safe error codes, and retry. The screen only consumes the legacy file list and has no batch/retry view. | **Gap in the existing screen** |
| Backup gate before risky work | Deployment and restore documentation requires a verified backup. The screen does not display or enforce a change-specific backup gate for mapping repair/deployment. | **Operational gap; must remain server/operator controlled** |

## Decision from this review

The existing screen is useful and should be extended rather than replaced at this stage. A second
backup screen is not justified by the source audit. The first implementation slice should align
the existing `/backups` screen with the already-defined batch contract and make unsupported
Production restore capability explicit without enabling a restore mutation on Monster.

This decision is still a plan. It is not authorization to modify mappings, resources, databases, or
Production.

## Proposed implementation start after approval

1. Confirm #239 as the implementation issue and start a task branch from the repository's required
   current integration baseline. Preserve the existing #230 Production incident branch and all
   unrelated Dashboard working-tree changes.
2. Review and, if necessary, harden the Backend response contract first. It must safely cover
   scope, batch status, per-target status, artifact identity, size, timestamps, checksum/manifest
   reference, safe error code, retry state, and restore capability. It must never return connection
   strings, passwords, protected values, absolute server paths, or raw provider exceptions.
3. Add Backend contract tests for authorization, each scope, idempotency, partial/failed batches,
   checksum/manifest metadata, retry rules, audit events, and the `ManualOnly` restore boundary.
4. Extend the existing Dashboard service and component to use batch creation/history/retry and
   capability endpoints. Display only safe metadata, clearly distinguish Platform from FullSystem
   and per-Tenant results, and keep immutable records read-only.
5. Add UI tests/build verification for loading, empty, running, partial, failed, retryable,
   unavailable, unauthorized, and no-connection-material states. Do not add a restore mutation
   while the provider reports `ManualOnly`.
6. Update the generated API catalog if a controller contract changes, then synchronize the Backend
   and Dashboard screen, operations, architecture, permissions, and deployment documentation.
7. Run the required local/CI verification and review the diff. No Production action is part of this
   first implementation slice.
8. Treat any deployment or mapping repair as a separate protected operation with a verified backup,
   reviewed migration state, CI success, health checks, rollback plan, and explicit operator gate.

## Acceptance and stop conditions

The work cannot be called complete until the approved acceptance criteria in #239 are evidenced:

- Every active mapping has an independently verifiable private backup before a repair or release
  operation.
- The full Platform backup has a safe manifest and integrity evidence.
- A restore rehearsal succeeds only against an isolated target; it never replaces a live tenant as
  a test.
- Audit events cover backup start, completion/failure, verification, and restore rehearsal using
  safe identifiers only.
- The Dashboard never exposes connection material and does not silently switch mappings.
- Missing or failed backup evidence blocks destructive, mapping-changing, or deployment work.

Stop immediately if a task requests a live mapping switch, resource recreation, deletion, raw
connection material, Production restore on `ManualOnly`, or deployment without the protected backup,
CI, health, and rollback gates.
