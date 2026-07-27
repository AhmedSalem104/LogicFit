# التشغيل والنشر والاستعادة

## بيئات ومكونات النشر

- `LogicFit.API` هو المضيف الموحد؛ يحتوي Platform وTenant modules ويستخدم إعدادات
  قاعدة بيانات وJWT وPassword Reset والنسخ الاحتياطي من مصدر واحد.
- لوحة الإدارة Angular تنشر كواجهة static وتتصل بمسارات `/api/platform/...` في المضيف الموحد.
- لا تضع Connection String أو أسرار JWT أو Publish Settings في Git أو docs أو logs.

## الإعدادات المطلوبة في الخادم

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

## فحص ما قبل النشر

1. راجع `git status` وتأكد أن النسخة المنشورة هي commit/branch المقصود؛ لا تخلط مجلد
   Visual Studio قديم مع GitHub.
2. شغّل build/tests ومراجعة migrations:

```powershell
dotnet build LogicFit.sln -c Release --no-restore
dotnet test LogicFit.sln -c Release --no-build --verbosity minimal
dotnet ef migrations script --idempotent --project LogicFit.Infrastructure --startup-project LogicFit.API
```

3. خذ Backup وراجع Migration Dry Run وتقرير المخالفات لأي تغيير بيانات كبير.
4. انشر الـAPI الصحيح، ثم طبق migrations في خطوة مراجعة منفصلة، ثم نفذ health check.
5. انشر Dashboard المبني من البيئة التي تشير إلى API الصحيح.
6. اختبر الدخول، لوحة المتابعة، خطط المنصة، تنبيهات، Jobs، ونسخة احتياطية من حساب
   Platform Owner محدود للاختبار.

## CI/CD

CI يعمل على الفروع وPull Requests ويتحقق من البناء والاختبارات ومراجعة migrations
وبناء الصور. إنتاجياً لا يحق للنشر أن يبدأ قبل CI أخضر وبيئة محمية وخطة Rollback.
الـCD التلقائي يظل متوقفاً حتى توثيق host/user/app directory/service command/backup
command/migration command/health URL/rollback command وتخزين أسرار النشر في GitHub
Environment `production` فقط.

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
| `500` متكرر | راجع Application Logs وConnection String وMigration وحالة الجداول، ولا تكشف exception للعميل. |
| `503` من النسخ | افحص تفعيل الخدمة وأداة/مسار النسخ وصلاحيات ملف التخزين ومساحة القرص. |
| تنزيل نسخة `404` | تحقق من اسم الملف ومسار التخزين، ثم إصدار LogicFit.API الموحد المنشور. |
| فشل Job/Outbox | راجع JobExecutionLog/Outbox/Alerts/Audit؛ لا تحذف record لمحاولة إخفاء الفشل. |

## Rollback

Rollback قرار تشغيلي موثق: أوقف rollout عند health check فاشل، أعد binary السابق
المعتمد، لا تعكس Migration بيانات بشكل عشوائي، واستعد من Backup مختبر إذا لزم. استخدم
Feature Flag للتفعيل التدريجي عند إدخال SaaS policy أو Job جديد عالي التأثير.
