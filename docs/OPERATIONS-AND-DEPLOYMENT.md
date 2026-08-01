# التشغيل والنشر والاستعادة

## بيئات ومكونات النشر

- `LogicFit.API` هو المضيف الموحد؛ يحتوي Platform وTenant modules ويستخدم إعدادات
  قاعدة بيانات وJWT وPassword Reset والنسخ الاحتياطي من مصدر واحد.
- لوحة الإدارة Angular تنشر كواجهة static وتتصل بمسارات `/api/platform/...` في المضيف الموحد.
- لا تضع Connection String أو أسرار JWT أو Publish Settings في Git أو docs أو logs.

## الإعدادات المطلوبة في الخادم

### Identity email delivery (Issue #113, unreleased)

Before enabling email registration or identity password reset, configure these server-only settings (environment-variable form shown; values are never committed): `Email__Provider=smtp`, `Email__Smtp__Host`, `Email__Smtp__Port`, `Email__Smtp__UseSsl`, `Email__Smtp__UserName`, `Email__Smtp__Password`, `Email__Smtp__FromEmail`, `Email__Smtp__FromName`, and `IdentityEmailLinks__FrontendBaseUrl` (HTTPS only). The API returns `503 IDENTITY_EMAIL_NOT_CONFIGURED` until both delivery and HTTPS frontend-link settings are present. Do not log the generated link or raw token. Apply `20260730143000_AddIdentityEmailSecurity` from a reviewed idempotent script after backup, publish, then verify `/health` and a non-production email flow.

### OTP delivery and Meta WhatsApp (Issues #118 and #127)

OTP settings exist only in environment variables or the server secret store. Development uses
`ASPNETCORE_ENVIRONMENT=Development`, `Otp__Provider=Development`,
`Otp__DevelopmentFixedCode=1234`, and a private `Otp__HmacSecret` of at least 32 characters.
The API still creates and hashes a real challenge. Startup fails if the Development provider
or fixed code appears outside Development.

Production uses `Otp__Provider=MetaWhatsApp` and must set `Otp__HmacSecret`,
`MetaWhatsApp__AccessToken`, `MetaWhatsApp__PhoneNumberId`,
`MetaWhatsApp__BusinessAccountId`, `MetaWhatsApp__TemplateName`,
`MetaWhatsApp__TemplateLanguage`, and `MetaWhatsApp__GraphApiVersion`. Secure webhook
verification additionally uses `MetaWhatsApp__WebhookVerifyToken` and
`MetaWhatsApp__AppSecret`. Never put any of these values in a published `appsettings.json`.
There is no fallback to `1234` if Meta fails.

Until the external provider subscription is available, Issue #127 permits a reviewed hosted-test
exception. Configure it only in the server secret store with `Otp__Provider=TemporaryFixed`,
`Otp__AllowTemporaryFixedCode=true`, `Otp__TemporaryFixedCode=1234`,
`Otp__TemporaryFixedCodeExpiresAtUtc=<future UTC value no more than 31 days away>`, and a private
`Otp__HmacSecret` of at least 32 characters. The API still creates, hashes, rate-limits, and atomically
consumes a real challenge; it never returns the code. Startup fails if the explicit flag, exact code,
or bounded future expiry is missing. Runtime requests fail after expiry. There is no automatic fallback
from Meta to this provider. To retire the exception, switch to `MetaWhatsApp` and remove all three
`TemporaryFixedCode` settings from the server.

Before rollout: backup; review/apply
`20260730164313_ReplaceIdentityPasskeysWithCentralizedOtp`; configure the secrets; publish
Backend, Tenant Angular, and Platform Angular as one coordinated release; verify `/health`;
then smoke-test email login, Phone + OTP, Platform password+OTP, sensitive-action step-up,
refresh rotation, logout-all, and password-reset session revocation. Roll back the binaries
and stop the rollout on health/OTP failure; do not reverse the migration destructively.

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
`DoNotDeleteRule`، لذلك لا يحذف هذا الملف أثناء نشر التطبيق. لا تُسجّل محتويات الملف
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

Never apply an EF migration from application startup, including through `Database__ApplyMigrationsOnStartup`. Create and verify a BACPAC, generate and review the idempotent script from the released `origin/master` tree, then let the protected WebDeploy helper apply the migration before publishing. The helper stops before WebDeploy on a missing backup reference, missing protected database connection, unapproved destructive SQL, migration failure, or remaining pending migration. It verifies health after publishing without printing database or publish credentials:

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

The connection is read from `LOGICFIT_PRODUCTION_DB_CONNECTION` in the current protected process and is passed to the EF design-time factory through the short-lived `LOGICFIT_EF_CONNECTION_STRING` operator variable. Without that explicit override, EF remains pinned to LocalDB and cannot reach production accidentally. The GitHub `production` Environment must store the production secret together with `RUNASP_UNIFIED_PUBLISH_SETTINGS_B64` and `RUNASP_UNIFIED_HEALTHCHECK_URL`. The manual workflow also requires `backup_reference`, `migration_review=MIGRATIONS-REVIEWED`, and `confirm=DEPLOY-PRODUCTION`. `-ApproveDestructiveMigrationReview` is used only after reviewing a plan containing intentional `DROP`, `DELETE`, or `TRUNCATE` statements.

3. خذ Backup وراجع Migration Dry Run وتقرير المخالفات لأي تغيير بيانات كبير.
4. طبّق migrations في خطوة مراجعة منفصلة، ثم انشر الـAPI الصحيح، ثم نفّذ health check.
5. انشر Dashboard المبني من البيئة التي تشير إلى API الصحيح.
6. اختبر الدخول، لوحة المتابعة، خطط المنصة، تنبيهات، Jobs، ونسخة احتياطية من حساب
   Platform Owner محدود للاختبار.

## CI/CD

CI يعمل على الفروع وPull Requests ويتحقق من البناء والاختبارات ومراجعة migrations
وبناء الصور. إنتاجياً لا يحق للنشر أن يبدأ قبل CI أخضر وبيئة محمية وخطة Rollback.
يستخدم preflight الخاص بالنشر SQL Server مؤقتًا و`LOGICFIT_TEST_CONNECTION_STRING` مثل CI؛
لا يسمح باختبارات OTP التي تسقط إلى LocalDB غير المدعوم على Linux.
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
