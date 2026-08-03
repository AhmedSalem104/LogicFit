# التشغيل والنشر والاستعادة

## Issue #161 authentication deployment note

The active login contract is Email + Password for Identity and Platform surfaces. Deploy the
Backend and Platform Dashboard together because `/api/platform/auth/login` returns the session
directly and no OTP verification call is valid. Do not add OTP, Phone Login, Passkey, or WebAuthn
secrets to the server. No Production deployment or migration was performed by this change.

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
2. شغّل build/tests ومراجعة migrations:

```powershell
dotnet build LogicFit.sln -c Release --no-restore
dotnet test LogicFit.sln -c Release --no-build --verbosity minimal
dotnet ef migrations script --idempotent --project LogicFit.Infrastructure --startup-project LogicFit.API
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
