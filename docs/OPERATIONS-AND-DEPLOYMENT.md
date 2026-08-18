# التشغيل والنشر والاستعادة

## Gym deletion runbook (Issue #214)

Use the Platform `/tenants` lifecycle buttons rather than direct database edits. Credentials view
is metadata-only and reset is delivered through the configured identity email provider. Soft delete
revokes sessions and keeps the assigned resource. Permanent delete must show a completed BACPAC
artifact before purge; it then purges through the provider boundary, deactivates the mapping,
releases the resource to `Available`, and records audit events such as
`PlatformTenantPermanentDeleteBackupCompleted`, `PlatformTenantPermanentDeletePurgeCompleted`,
and `PlatformTenantPermanentDeleteCompleted`.

In Production/Monster Free, `TENANT_DATABASE_PURGE_MANUAL_ONLY` is an intentional safety response.
Do not enable a destructive provider by changing a runtime flag casually and do not execute ad-hoc
SQL. Use the separately reviewed operator workflow, verify the backup and health state, and retain
the deleted tenant tombstone so the backup remains linked. The owner Global Identity must remain
available for another workspace unless a separate explicit identity-deletion review proves there
are no active workspace memberships or active application requests.

## Issue #161 authentication deployment note

The active login contract is Email + Password for Identity and Platform surfaces. Deploy the
Backend and Platform Dashboard together because `/api/platform/auth/login` returns the session
directly and no OTP verification call is valid. Do not add OTP, Phone Login, Passkey, or WebAuthn
secrets to the server. No Production deployment or migration was performed by this change.

## Tenant approval and existing owner memberships (Issues #210 and #217)

When a Gym is approved or activated through `/api/platform/tenants/{id}/activate`, the Backend
also promotes its non-deleted owner membership from `PendingPlatformApproval` to `Active`. The
operation is idempotent and requires no schema migration. The identity login issuer also repairs
an already-`Active` Gym with a pending owner membership on the next successful owner login, so
the owner receives the workspace context and is routed to workspace selection automatically.
The repair is restricted to the Gym owner membership; do not update `WorkspaceMemberships`
directly in Production.

## Backup admin screen and batch evidence (Issue #239)

The existing Platform Admin `/backups` screen is the operator entry point for the server-owned
backup batches. `FullSystem` resolves the platform database and every active assigned tenant
mapping; `AllTenants`, `AllGyms`, `AllFreelance`, and `Platform` are explicit alternatives. The
server returns per-artifact status, size, safe storage key, SHA-256 and manifest reference. Batch
start/finish events are written to the Platform Audit Log.

If `POST /api/platform/backups/batch` returns `503 BACKUP_SERVICE_UNAVAILABLE`,
`BACKUP_DATABASE_UNAVAILABLE`, or `BACKUP_STORAGE_UNAVAILABLE`, inspect the protected batch history
and `/api/platform/backups/status`, repair the reported database/storage dependency, and use the
retry action for the recorded `Failed` or `Partial` batch. A raw `500` from this route is a release
regression: collect the safe server log category and exception type, verify `/health`, and do not
repeat the operation with a new idempotency key until the cause is fixed.

Creation and retry require confirmation. Retry is limited to `Failed` or `Partial` batches. The
screen never renders connection material, credentials, raw exceptions, or absolute storage paths.
Restore capability is informational; `ManualOnly` must remain a manual operator handoff and does
not authorize a mapping switch. A failed or missing batch must stop destructive or
mapping-changing work until a verified backup and rollback plan exist.

This implementation has no schema migration and no Production deployment. Before release, run CI,
review the generated API catalog, verify the protected backup/migration/health/rollback gates, and
perform a restore rehearsal only in an isolated target approved for that purpose.

## بيئات ومكونات النشر

- `LogicFit.API` هو المضيف الموحد؛ يحتوي Platform وTenant modules ويستخدم إعدادات
  قاعدة بيانات وJWT وPassword Reset والنسخ الاحتياطي من مصدر واحد.
- لوحة الإدارة Angular تنشر كواجهة static وتتصل بمسارات `/api/platform/...` في المضيف الموحد.
- لا تضع Connection String أو أسرار JWT أو Publish Settings في Git أو docs أو logs.

## الإعدادات المطلوبة في الخادم

### Identity email delivery (Issue #113, unreleased)

Before enabling email registration or identity password reset, configure these server-only settings (environment-variable form shown; values are never committed): `Email__Provider=smtp`, `Email__Smtp__Host`, `Email__Smtp__Port`, `Email__Smtp__UseSsl`, `Email__Smtp__UserName`, `Email__Smtp__Password`, `Email__Smtp__FromEmail`, `Email__Smtp__FromName`, and `IdentityEmailLinks__FrontendBaseUrl` (HTTPS only). The API returns `503 IDENTITY_EMAIL_NOT_CONFIGURED` until both delivery and HTTPS frontend-link settings are present. Do not log the generated link or raw token. Apply `20260730143000_AddIdentityEmailSecurity` from a reviewed idempotent script after backup, publish, then verify `/health` and a non-production email flow.

### Email/password authentication (Issue #161)

All active authentication surfaces use Email + Password. Email verification and password reset
are single-use, short-lived email links backed by hashed tokens. There is no Phone Login, OTP,
Passkey, WebAuthn, Meta WhatsApp, or OTP secret configuration. The compatibility migration
`20260803090742_RemoveLegacyOtpArtifacts` is guarded with `OBJECT_ID` and only removes the old
`OtpChallenges` table when it exists; review and apply it separately after a verified backup.
No Production migration or deployment is implied by a source merge.

### Platform Owner recovery bootstrap (Issue #140)

The API no longer creates a Platform Owner with a hardcoded password. If the existing owner
predates `IdentityAccount` or has no verified E.164 phone, configure the following values in the
server secret store for one controlled restart only: `PlatformBootstrap__Enabled=true`,
`PlatformBootstrap__Email`, `PlatformBootstrap__Password`, `PlatformBootstrap__PhoneNumber`,
`PlatformBootstrap__FullName`, and `PlatformBootstrap__ResetPassword=true`. The password must be
at least 12 characters and contain uppercase, lowercase, digit, and symbol; the phone must already
be an operator-controlled E.164 number. The bootstrap creates or repairs one active owner identity,
marks the operator-asserted email and phone as verified, clears lockout, and revokes existing refresh
sessions when resetting the password. It never logs the configured values.

After one successful recycle, verify that `/api/platform/auth/login` accepts the configured email
and password and returns a Platform session, then immediately set `PlatformBootstrap__Enabled=false`,
remove every other `PlatformBootstrap__*` value from the server, and recycle again. Do not commit
these values, keep the bootstrap enabled, or use it as a routine password-reset path.

تُخزن في إعدادات الموقع/Secret Store الخاصة بالخادم، لا في source control:

| المفتاح | الغرض |
|---|---|
| `ConnectionStrings__DefaultConnection` | اتصال قاعدة البيانات الفعلي. |
| `JwtSettings__Secret` | سر توقيع JWT طويل وعشوائي. |
| `PasswordReset__Secret` | سر مستقل لإعادة الضبط. |
| `ASPNETCORE_ENVIRONMENT=Production` | بيئة التطبيق. |
| `Backup__Enabled` | تمكين خدمة النسخ إن كانت سياسة الخادم تسمح. |

تغيير الإعدادات يتبعه Save ثم Restart/Recycle للتطبيق. وجود قيمة في `appsettings.json`
المحلي لا يضمن وصولها لعملية Production.

### ملف إعدادات Production على الخادم

يُنشأ `appsettings.Production.json` داخل موقع Monster ASP فقط ولا يُرفع إلى Git. يضع
المشغّل فيه `ConnectionStrings:DefaultConnection` و`JwtSettings:Secret` وأي إعدادات
خاصة بالإنتاج. سكربت WebDeploy يحتفظ بالملفات الإضافية على الخادم عبر
`LogicFit.API.csproj` excludes it from publish output and `deploy-webdeploy.ps1` skips it
explicitly at the destination. `DoNotDeleteRule` alone prevents deletion but still allows an
existing file to be overwritten. لا تُسجّل محتويات الملف
أو كلمات المرور في التذاكر أو السجلات.

## فحص ما قبل النشر

1. راجع `git status` وتأكد أن النسخة المنشورة هي commit/branch المقصود؛ لا تخلط مجلد
   Visual Studio قديم مع GitHub.

### Issue #321 - tenant boundary release gate

The API now fails closed before authorization when an authenticated non-platform request has no
valid tenant context. Tenant routes require the `LogicFitUsers` audience and signed `TenantId`,
and an optional `X-Tenant-Id` header must match it. Platform routes are tenantless and require the
`LogicFitPlatform` audience. After deployment, verify one authenticated tenant smoke request, one
platform request, and negative checks for a platform token on a tenant route and a missing tenant
claim. A build/test pass without these checks is not a production approval.
2. شغّل build/tests ومراجعة migrations:

```powershell
dotnet build LogicFit.sln -c Release --no-restore
dotnet test LogicFit.sln -c Release --no-build --verbosity minimal
dotnet ef migrations script --idempotent --project LogicFit.Infrastructure --startup-project LogicFit.API --context LogicFit.Infrastructure.Persistence.ApplicationDbContext
```

### Migrations and health verification

Issue #147 makes startup migration the default safety net for manual Visual Studio/WebDeploy
publishes. Before `DataSeeder` or HTTP traffic, the API obtains a SQL Server application lock,
checks `__EFMigrationsHistory`, applies pending compiled EF migrations, and verifies that none
remain. It does not generate migrations at runtime. Concurrent IIS workers wait for the same lock
instead of applying the same migration twice.

This does not remove release controls: create and verify a BACPAC, generate and review the
idempotent script from the released `origin/master` tree, and preferably let the protected
WebDeploy helper pre-apply the migration. The helper stops before WebDeploy on a missing backup
reference, missing protected database connection, unapproved destructive SQL, migration failure,
or remaining pending migration. It verifies health after publishing without printing database or
publish credentials:

```powershell
.\Scripts\deploy-webdeploy.ps1 `
  -PublishSettingsPath <publish-settings-file> `
  -ContentPath <publish-output-directory> `
  -ApplyMigrations `
  -VerifiedBackupReference <verified-bacpac-name-or-reference> `
  -MigrationScriptPath <reviewed-idempotent-sql-file> `
  -ApproveDestructiveMigrationReview `
  -HealthCheckUrl https://your-host/health
```

### Wallet and stock concurrency rollout (Issue #195, unreleased)

Wallet debits/credits and their ledger rows commit in one database transaction. Stock
adjustments, transfers, and POS checkout use guarded SQL quantity updates; stock creation
paths use Serializable transactions. A failed balance/quantity guard rolls back the related
ledger, movement, sale, invoice, payment, and commission changes.

Issue #195 introduces no EF schema migration; it relies on the existing SQL Server row-version
columns and tenant/product/branch uniqueness. Validate the Release build and the concurrency
integration tests before release. Redis is not used as the source of truth for wallet or stock,
and no Redis credential belongs in repository configuration.
### Background jobs across multiple API instances (Issue #193, unreleased)

The subscription lifecycle and Outbox workers coordinate through SQL Server session-owned
application locks. The lock resources are
`LogicFit:Background:TenantSubscriptionLifecycle`,
`LogicFit:Background:PlatformSubscriptionLifecycle`, and
`LogicFit:Background:OutboxProcessor`. A busy instance skips that pass; a lock or database
failure must remain visible in logs/`JobExecutionLog` and must not be recorded as completed.

Before applying the Issue #193 migrations, review duplicate Outbox keys in every affected
database and resolve them through the approved operator procedure:

```sql
SELECT [IdempotencyKey], COUNT_BIG(*) AS [DuplicateCount]
FROM [OutboxMessages]
GROUP BY [IdempotencyKey]
HAVING COUNT_BIG(*) > 1;
```

The migration intentionally throws before creating the unique index when this query returns
rows. Do not delete Outbox history to force a migration through. The change is currently on
the task branch and is not a Production deployment claim.

### Protected IIS 500.30 startup recovery

Issue #137 recovered `site81605` after stdout identified a missing required secret. The same
procedure is available through the manual protected CD workflow with
`confirm=RECOVER-PRODUCTION-STARTUP`. It selects an existing GitHub Environment publish profile,
requires the exact Monster site id, captures the existing production configuration and web.config
for rollback, injects protected JWT/password-reset secrets, forces one recycle, restores the
original web.config, and requires health after both recycles. It changes neither the database nor
the application binary.

Never leave stdout enabled after diagnosis. Never upload captured stdout as an artifact when it can
contain authentication payloads. Rotate exposed application credentials and remove or redact the
affected server logs through an explicitly approved operator action.

Before retrying a backup activation after a failed recovery, use the protected CD dispatch value
`DIAGNOSE-PRODUCTION-HEALTH`. This read-only job runs `SELECT 1` through the protected production
database connection and then requires the configured HTTPS `/health` endpoint to return `Healthy`.
It does not run WebDeploy, migrations, configuration writes, or backup exports. Do not dispatch
`RECOVER-PRODUCTION-STARTUP` with `enable_backups=true` until both diagnostic checks pass.

When the exact Monster site still returns `503 Unhealthy` while the protected database and IIS
metadata probes pass, use `DIAGNOSE-MONSTER-LOGS` on the verified site. This operation temporarily
enables ASP.NET Core stdout logging in the existing `web.config`, recycles the site through the
normal WebDeploy sync, reads only safe root-cause categories from the resulting files, and restores
the original `web.config` in a `finally` block. It never uploads raw stdout, prints log contents,
changes application configuration or database data, or enables backups. The operation is still
subject to the post-rollback `/health` gate; a remaining `503` is recorded as an incident blocker.
The protected job also runs an EF pending-migration probe in read-only mode; it reports only the
count/ids or the exception type, never applies a migration. A database permission failure must be
handled through the approved database operator procedure and a verified backup/migration review.
The same diagnostic reports only whether IIS `web.config` contains connection-string or Redis
environment-variable overrides; it never prints their values.

The connection is read from `LOGICFIT_PRODUCTION_DB_CONNECTION` in the current protected process and is passed to the EF design-time factory through the short-lived `LOGICFIT_EF_CONNECTION_STRING` operator variable. Without that explicit override, EF remains pinned to LocalDB and cannot reach production accidentally. The GitHub `production` Environment must store the production secret together with `RUNASP_UNIFIED_PUBLISH_SETTINGS_B64` and `RUNASP_UNIFIED_HEALTHCHECK_URL`. The manual workflow also requires `backup_reference`, `migration_review=MIGRATIONS-REVIEWED`, and `confirm=DEPLOY-PRODUCTION`. `-ApproveDestructiveMigrationReview` is used only after reviewing a plan containing intentional `DROP`, `DELETE`, or `TRUNCATE` statements.

The protected WebDeploy secret may contain either the Base64-encoded publish-settings file or the
publish-settings XML itself for compatibility with the existing Environment configuration. The
workflow validates direct XML before writing the short-lived runner file and never prints it.

Startup settings are optional because safe defaults are compiled in: enabled, 120-second lock
wait, and 300-second command timeout. Override them only in the server secret/configuration store
with `Database__StartupMigrations__Enabled`,
`Database__StartupMigrations__LockTimeoutSeconds`, and
`Database__StartupMigrations__CommandTimeoutSeconds`. If the database login lacks DDL permission,
startup fails clearly; grant only the reviewed permission window or pre-apply through the protected
operator flow. Never disable startup migration merely to bypass a pending schema.

3. خذ Backup وراجع Migration Dry Run وتقرير المخالفات لأي تغيير بيانات كبير.
4. طبّق migrations في خطوة مراجعة منفصلة، ثم انشر الـAPI الصحيح، ثم نفّذ health check.
5. انشر Dashboard المبني من البيئة التي تشير إلى API الصحيح.
6. اختبر الدخول، لوحة المتابعة، خطط المنصة، تنبيهات، Jobs، ونسخة احتياطية من حساب
   Platform Owner محدود للاختبار.

### Workspace onboarding release gate (Issues #244/#245)

تغيير عقد `workspace-applications` يحتاج نشر الـBackend والـDashboard كإصدار متوافق. قبل
التفعيل التشغيلي راجع أن قائمة الطلبات تستخدم `approve-workspace` لمساحات Gym/FreelanceCoach
ولا تستخدم `approve-membership`، ثم نفّذ smoke checks للحالات التالية: pending payment، under
review، more information، provisioning، provisioning failed/retry، active access، suspended،
expired، وقاعدة بيانات غير متاحة. يجب أن تكون `/health` HTTP 200 و`Healthy` بعد كل تعديل/نشر؛
لا تُختبر هذه الرحلة بإنشاء Tenant أو Mapping في Production دون backup ونافذة تشغيل معتمدة.

تتضمن مراجعة الدفع في الإصدار المتوافق فحص `GET /api/platform/payment-requests/{id}/proof` ثم
سجل `.../{id}/proofs` للتأكد من حفظ الإصدار والـSHA-256، وتجربة `?version=N` عند وجود أكثر من
إصدار. لا يُسمح بـ`approve` لمساحة عمل بلا إثبات حالي، بينما يبقى `approve-workspace` قرارًا
مستقلًا يبدأ التجهيز بعد اعتماد الدفع. عند استبدال الملف يجب أن يبقى الإصدار السابق قابلًا
للاسترجاع وألا يظهر storage key أو connection material في الاستجابة أو السجلات.

### Redis cache and distributed request controls (Issue #197)

The tenant-access gate uses `IDistributedCache`. In non-production environments without Redis it
falls back to the in-memory provider. Production requires Redis by default and fails before the
host starts when the connection is missing. Configure either a complete secret-backed
`ConnectionStrings__Redis` value, or keep the endpoint separate from the credential:

```text
Redis__Endpoint=<redis-host-and-port-or-redis-url>
Redis__PasswordFile=<server-only-secret-file>
Redis__InstanceName=LogicFit
Redis__Required=true
```

`Redis__PasswordFile` is read only during startup and its contents are never written to logs,
Git, or documentation. The local credential file supplied for development can be used through
this setting once the Redis provider's endpoint is supplied separately; the credential alone is
not a Redis connection string. A direct `Redis__Password` is supported for a secret manager but
must never be committed.

When the application owns rate limiting, the global and named fixed-window policies use the same
Redis namespace and an atomic Lua counter, so all API instances share the limits. If an upstream
gateway owns those policies, set `RateLimiting__ManagedByGateway=true`; the API then skips its
local limiter instead of enforcing a second, instance-local limit. A production deployment must
choose Redis-backed application limiting or an explicitly configured gateway. Redis remains a
coordination/cache layer only: SQL Server is still the source of truth for wallet, stock, and
other business data.

## CI/CD

CI يعمل على الفروع وPull Requests ويتحقق من البناء والاختبارات ومراجعة migrations
وبناء الصور. إنتاجياً لا يحق للنشر أن يبدأ قبل CI أخضر وبيئة محمية وخطة Rollback.
يستخدم preflight الخاص بالنشر SQL Server مؤقتًا و`LOGICFIT_TEST_CONNECTION_STRING` مثل CI؛
لا يسمح باختبارات قواعد البيانات التي تسقط إلى LocalDB غير المدعوم على Linux.
الـCD يظل يدوياً ومحميًا. لا يبدأ إلا من شجرة مطابقة لـ`origin/master` وبعد CI ناجح،
ونسخة BACPAC متحقق منها، ومراجعة SQL، وخطة Rollback، وتخزين أسرار WebDeploy وقاعدة
البيانات وhealth URL في GitHub Environment `production` فقط.

## النسخ الاحتياطي والاستعادة

- النسخ اليومية وسياسة الاحتفاظ يضبطها الخادم؛ النسخة تحتوي Schema وData حسب الخدمة.
- أنشئ نسخة يدوية قبل Migration أو تغيير واسع. لا تحفظها في wwwroot أو مستودع Git.
- إذا ظهر `404` عند `/api/platform/backups/{file}/download` بينما القائمة تعمل، تحقق
  من أن DLL المنشور يحتوي Endpoint التنزيل؛ غالباً هذا إصدار خادم قديم وليس خطأ UI.
- إذا ظهرت رسالة أن النسخ غير مفعلة، تحقق من `Backup__Enabled` في إعدادات الموقع ثم
  أعد تشغيل التطبيق.
- اختبر الاستعادة على قاعدة منفصلة؛ لا تستبدل Production مباشرة. وثّق نتيجة الاختبار.

## الاستجابة للحوادث

| العرض | البداية الصحيحة للتشخيص |
|---|---|
| `401` من لوحة الإدارة | افحص انتهاء Access Token/Refresh Token وصلاحية المستخدم ثم Endpoint الحقيقي في Network. |
| `403` بعد دخول Platform جديد مع ظهور شاشة مسموحة في الواجهة | افحص تطابق `Users.Role` مع `UserRoles` والدور الموقع داخل JWT و`PermissionsVersion`. Issue #156 يصالح `PlatformOwner`/`PlatformAdmin` عند Startup وإصدار الجلسة؛ بعد نشره أعد الدخول، ولا تحذف Policy مثل `ManageTenants`. |
| `409` عند اعتماد مدرب حر | افحص حالة الطلب أولاً. إذا كانت `UnderReview` ورسالة اللوحة تشير إلى أدوار غير مهيأة، خذ Backup وطبّق `20260729133325_SeedFreelanceSystemRoles` عبر إجراء Migrations المعتمد، ثم حدّث الطابور وأعد المحاولة. |
| `500` متكرر | راجع Application Logs وConnection String وMigration وحالة الجداول، ولا تكشف exception للعميل. |
| `503` من النسخ | افحص تفعيل الخدمة وأداة/مسار النسخ وصلاحيات ملف التخزين ومساحة القرص. |
| تنزيل نسخة `404` | تحقق من اسم الملف ومسار التخزين، ثم إصدار LogicFit.API الموحد المنشور. |
| فشل Job/Outbox | راجع JobExecutionLog/Outbox/Alerts/Audit؛ لا تحذف record لمحاولة إخفاء الفشل. |

## Identity access migration rollout

- `Authentication__IdentityAccess__AllowUnlinkedLegacySessions=true` is the production-safe default while legacy tenant-local accounts are migrated. It permits only the existing compatibility path; it does not make an unlinked account an approved identity membership.
- Before switching the value to `false`, deploy the verified-email legacy-linking phase, measure remaining legacy-compatible sessions, verify account-recovery support, and test login, refresh, workspace selection, suspended membership, and inactive identity behavior in the production-like environment.
- Treat a spike in `IDENTITY_MIGRATION_REQUIRED`, `WORKSPACE_MEMBERSHIP_INACTIVE`, `IDENTITY_ACCOUNT_INACTIVE`, or `WORKSPACE_ACCOUNT_INACTIVE` as an operator-review signal. Do not work around it by re-enabling users or memberships without an audited decision.
- Cancellation access is evaluated against `EndDate` at request time. Test a cancelled workspace before and at the end date during rollout; it must be full before the end date and read-only at/after it.

## Rollback

Rollback قرار تشغيلي موثق: أوقف rollout عند health check فاشل، أعد binary السابق
المعتمد، لا تعكس Migration بيانات بشكل عشوائي، واستعد من Backup مختبر إذا لزم. استخدم
Feature Flag للتفعيل التدريجي عند إدخال SaaS policy أو Job جديد عالي التأثير.

## Coach plan release and verification gate (Issues #272/#69)

The coach-plan change requires the tenant migration
`20260810125711_CoachPlanExecutionFields` and a matching tenant frontend release. Before applying
the migration, generate and review the idempotent EF SQL, take the approved backup, verify the target
schema/history, and keep the rollback plan. After deployment, require `/health` HTTP 200 with the
expected healthy response before enabling the screens. Smoke-test aggregate workout/diet create and
update, cross-tenant and unassigned access rejection, client session start/set/end, meal logging,
and the no-partial-write retry behavior. This task branch is not production-verified yet.

## Workspace capability release gate (Issue #296)

This change adds server authorization policies and changes the seeded `FreelanceOwner` permission
set, so it requires the normal migration-aware release process even though no new database table is
needed. Before release, review the generated endpoint catalog, apply any pending migrations using
the idempotent script, verify the seeder against a representative Gym and FreelanceCoach tenant,
and take the approved backup.

After deployment, require the protected `/health` endpoint to return HTTP 200 and the expected
healthy response. Smoke-test workspace selection/refresh response capabilities, a FreelanceCoach
request to every denied Gym endpoint (403 capability code), a Gym request to its allowed endpoints,
and a cross-tenant request with a valid identity. The branch is not merged, deployed, or production
verified by this document.

## Subscriber, training and nutrition release gate (Issue #313)

Before release, run the shared and tenant idempotent migration scripts for the parity migrations,
review the SQL and backup/rollback plan, then verify schema history on a representative Gym and
FreelanceCoach tenant. Do not mark a workspace active until its database is ready and health checks
pass.

Required smoke tests cover: member creation and multiple memberships; subscription create,
payment, renew, freeze, cancel, and overpayment rejection; complete workout and nutrition
aggregate create/update with stale-version rejection; soft deletion with historical session/meal
log reads; serving-size macro calculation; measurement update; one-per-day check-in; and the
combined client training overview. Repeat authorization tests with a different tenant, an
unassigned coach, and a different client.

The required local gates are:

1. `dotnet build LogicFit.API/LogicFit.API.csproj --no-restore /m:1`.
2. `dotnet test --no-restore /m:1`.
3. Tenant Angular production build and unit tests.
4. Generated API catalog review.
5. Deployed `/health` HTTP 200 with body `Healthy`.

This is an implementation branch until PR review, merge, migration application, deployment,
and post-deployment smoke tests complete.
