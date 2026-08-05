# Protected post-deploy smoke procedure (Issue #227)

`Scripts/post-deploy-smoke.ps1` is a protected, auditable release check for the Backend and
Platform Dashboard contract. It is intentionally not run by local development, CI, or this change.
Run it only after the release is deployed, the verified backup reference is recorded, and the
operator has an approved maintenance window.

## Preconditions

- The published commit is known and matches the expected release SHA/prefix.
- `/health` is expected to be available after the recycle.
- A verified backup reference and the exact operator approval string are available.
- The operator has selected two different resources: one currently allocated and one unallocated
  `Failed`/`Faulted` resource. The selected resources must not be guessed from a list.
- The three secret values are present only in the protected process environment:

| Environment variable | Use |
|---|---|
| `LOGICFIT_SMOKE_PLATFORM_PASSWORD` | Platform Owner login. |
| `LOGICFIT_SMOKE_ALLOCATED_CONNECTION` | Repaired connection for the selected allocated resource. |
| `LOGICFIT_SMOKE_FAILED_CONNECTION` | Repaired connection for the selected failed/unallocated resource. |

The script never accepts passwords or connection strings as command-line arguments and never writes
them to the result artifact or console.

## Protected command

Run from the released Backend workspace in the protected operator shell. The `-AllowMutations`
switch and exact approval value are deliberate gates because the procedure repairs two resources,
creates a disposable smoke gym, approves it, and authenticates its owner. It does not call any
delete, purge, permanent-delete, or direct SQL endpoint.

```powershell
.\Scripts\post-deploy-smoke.ps1 `
  -BaseUrl https://your-production-host `
  -PlatformEmail platform-owner@example.com `
  -ExpectedReleaseCommit $env:LOGICFIT_RELEASE_SHA `
  -VerifiedBackupReference $env:LOGICFIT_VERIFIED_BACKUP_REFERENCE `
  -OperatorApproval POST-DEPLOY-SMOKE-APPROVED `
  -AllocatedResourceId 00000000-0000-0000-0000-000000000001 `
  -FailedResourceId 00000000-0000-0000-0000-000000000002 `
  -AllowMutations
```

Replace the resource IDs and release/backup references from the protected change record. Do not
paste the three secret values into the command or a transcript. The default result is a JSON file
under `artifacts/`; pass `-ResultPath` when the release system needs a specific artifact location.

## Checks and evidence

The procedure records only safe statuses, identifiers, stable error codes, the release SHA, backup
reference, and request IDs when available:

1. Release version matches `/api/platform/diagnostics/version`; `/health` returns HTTP 200 and
   `Healthy`.
2. Platform login works and the database-resource list does not expose connection properties.
3. An invalid `Idempotency-Key` produces the expected non-mutating `400
   IDEMPOTENCY_KEY_INVALID` response.
4. An allocated resource is repaired in place, remains allocated, and has a matching repair audit
   event. A failed/unallocated resource is repaired to `Available` and remains unmapped.
5. A unique idempotent gym registration reaches `Completed`, has exactly one assigned resource,
   is approved, and the generated owner can complete identity login and workspace selection.
6. Tenant plans, subscription, payment requests, and unread notifications remain reachable; the
   corresponding Platform subscription, plan, payment-request, and notification endpoints also
   return their expected read-only success status.

If the script fails, preserve the JSON result and the printed safe request ID/code. Do not create a
second gym or retry a repair blindly. Review the resource/job/audit state, repair the underlying
condition through the approved operator path, and record the final pass/fail artifact with the
release change record. The smoke gym is disposable by policy, but its cleanup is a separate,
explicitly reviewed lifecycle action and is not performed by this non-destructive smoke script.
