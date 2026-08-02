# المستخدمون والصلاحيات والعزل

## مبدأ الحماية

الـUI يحسن التجربة بإخفاء ما لا يملكه المستخدم، لكنه ليس حد أمان. كل Endpoint حساس
يتحقق من JWT والسياسة في الـBackend. لا يعتمد النظام على `TenantId` يرسله المتصفح؛
هوية المستخدم وسياق الطلب هما مصدر تحديد المستأجر. سجلات المال والمراجعة لا تحذف أو
تعدل من واجهة عامة.

## Identity and OTP security (Issue #118, unreleased)

An identity-first account may sign in with its verified, globally unique email and password,
or a verified, unique E.164 phone and a purpose-bound OTP challenge. Email verification and
password-reset links remain opaque, one-use, short-lived hash records. OTP codes are also
one-use, short-lived HMAC records and are never returned by the API. Platform Owner/Admin
must complete password plus OTP during login. No post-login Platform or Tenant operation requires
another OTP challenge. Authentication never replaces authorization: `WorkspaceMembership.Active`,
local `User.Active`, workspace/subscription gates, permissions, and ownership checks still
decide access. Password reset/change and confirmed phone change revoke linked refresh and
workspace-selection sessions.

## مستخدمو المنصة المركزية

| الدور | الاستخدام | النطاق |
|---|---|---|
| `PlatformOwner` | مالك المنصة؛ يضبط الإدارة العليا ويملك الوصول الشامل. | كل الصالات وكل صلاحيات المنصة. |
| `PlatformAdmin` | مشغّل المنصة حسب الصلاحيات الممنوحة. | فقط سياسات Permission المربوطة بحسابه. |

`ManagePlatform` هو **god mode**: حامله يجتاز كل صلاحيات المنصة. لا يمنح إلا عند
الحاجة الحقيقية وبحساب شخصي يمكن مراجعته.

### كتالوج صلاحيات المنصة

| Permission | ماذا تسمح؟ | الشاشات/المسارات الأساسية |
|---|---|---|
| `ManagePlatform` | وصول شامل لكل ما يلي. | كل اللوحة. |
| `ManageTenants` | الصالات، حالات الصالات، ودورات الاشتراك. | `/tenants`, `/subscriptions` |
| `ManagePlans` | الخطط، كتالوج الميزات، الحدود، الاعتماديات والاستثناءات. | `/plans`, `/features`, `/feature-overrides`, `/quota-definitions`, `/feature-dependencies` |
| `ManagePaymentRequests` | طرق الدفع اليدوي وقرارات قبول/رفض الطلبات. | `/payment-methods`, `/payment-requests` |
| `ManagePlatformReports` | لوحة المتابعة، التقارير، التنبيهات، السجل، الفواتير، العمليات، حسابات الإدارة والأدوار. | `/dashboard`, `/reports`, `/alerts`, `/audit-logs`, `/invoices`, `/operations`, `/administrators`, `/roles` |
| `ManagePlatformBackups` | إنشاء/قراءة/تنزيل النسخ الاحتياطية. | `/backups` |

الفصل بين صلاحيات الموافقة والتفعيل والتعليق والإلغاء والـOverride مطلوب في سياسة
الأدوار. تعديل Role يطبق في الخادم؛ لا تكتف بإخفاء عنصر من القائمة.

## مستخدمو الصالة (Tenant)

| الدور | الاستخدام المعتاد | حدود الوصول |
|---|---|---|
| `Owner` | إدارة الصالة والموظفين والفروع والمال والاشتراكات. | نطاق صالته فقط؛ يمر عبر صلاحياته التفصيلية. |
| `Manager` | تشغيل يومي وإدارة حسب التفويض. | بيانات نفس الصالة، لا صلاحيات مالك المنصة. |
| `Receptionist` | تسجيل أعضاء وحضور وعمليات الاستقبال. | لا يمنح المال أو الإعدادات إلا بصلاحية صريحة. |
| `Accountant` | الفواتير والمدفوعات والمصروفات والتقارير المالية. | ضمن الصالة فقط وبصلاحيات المالية. |
| `Coach` / `Trainer` | المتدربون المعينون له والبرامج والغذاء والمواعيد. | لا يرى إلا نطاق تدريبه والكيانات المسموح بها. |
| `Client` | بوابته الشخصية: برامج، تغذية، اشتراك، قياسات، مواعيد ومدرب. | بياناته الخاصة فقط. |

الواجهة توجه Owner/Manager/Receptionist/Accountant إلى مساحة back-office، وCoach إلى
`/coach`، وClient إلى `/client`. التوجيه ليس بديلاً عن تحقق الخادم من الدور والملكية.

### أمثلة لصلاحيات الصالة

الصلاحيات الفعلية مفهرسة في `LogicFit.Domain/Authorization/Permissions.cs` وتغطي، من
أهمها: `ManageMembers`/`ViewMembers`، `ManageCoaches`، `ManageAttendance`،
`ManageClientSubscriptions`، `ManagePOS`، `ManageInventory`، `ManageEmployees`،
`ManageBranches`، `ManageFinance` و`ManageSettings`.

## قواعد حسابات الإدارة

1. حساب إداري واحد لكل شخص؛ لا مشاركة حسابات أو Refresh Tokens.
2. لا تحفظ كلمة مرور أو Token أو إثبات دفع في Audit Log أو رسائل خطأ.
3. عطل الحساب بدلاً من حذفه عندما يغادر الموظف، للحفاظ على أثره في السجل.
4. عند الاشتباه، استخدم `logout-all` لإبطال الجلسات ثم غيّر كلمة المرور وراجع Audit.
5. أي تجاوز مؤقت لميزة أو اشتراك يسجل السبب والمنفذ وبداية ونهاية القرار.

## ما الذي لا يحق لأي مستخدم فعله مباشرة؟

- تغيير `TenantId` للحصول على بيانات صالة أخرى.
- تحرير فاتورة أو Payment Request معتمد أو Audit Log أو Outbox Record أو Job history.
- تجاوز Global Disable أو إيقاف الصالة بواسطة Override.
- إنشاء Feature وظيفية بمجرد إضافة سجل الكتالوج؛ الحماية البرمجية مطلوبة أولاً.
