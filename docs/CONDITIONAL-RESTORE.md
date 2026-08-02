# Conditional Restore and Mapping Switch (Issue #165)

Restore is provider-backed and fail-closed. Monster Free reports `ManualOnly` and the application
does not execute native restore there. `LocalSql` is available only for Development/CI when
`Restore:Provider=LocalSql` and `Restore:Enabled=true` are explicitly set.

## PlatformOwner flow

1. `GET /api/platform/restores/capabilities` shows whether the current provider is enabled.
2. PlatformOwner re-enters the current password at `POST /api/platform/restores/reauthenticate`;
   the server returns a five-minute, single-use SensitiveActionGrant scoped to
   `platform-database-restore`.
3. The owner submits the grant, tenant, completed source backup id, optional pre-created target
   pool resource id, exact Workspace name confirmation, and a mandatory reason to
   `POST /api/platform/restores`.
4. The service creates a persistent `RestoreJob`, creates a fresh tenant BACPAC before import,
   validates the source and target server-side, and asks the provider to import the BACPAC into a
   separate pool database.
5. LocalSql performs connectivity checks and switches `TenantDatabaseMapping` only after import
   and health check succeed. The old mapping is retained for rollback/operations review. All
   failures set a safe error code and never activate a partial mapping.

Only a PlatformOwner can call the mutation endpoints. PlatformAdmin permissions alone are not
sufficient. No database name, connection string, or storage path is accepted from the client.

## Persistence

`RestoreJobs` stores tenant, source/target resource ids, provider, confirmation/reason, status,
timestamps, previous mapping id and safe error code. Migration `AddConditionalRestoreJobs` is
additive in both the compatibility and Platform migration assemblies and is not applied to
Production by this change.

Monster Free example settings keep `Restore:Provider=ManualMonster` and `Restore:Enabled=false`.
