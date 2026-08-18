# LogicFit: المنتج والتدفقات الأساسية

## 1. الفكرة وحدود المنتج

LogicFit منصة SaaS متعددة المستأجرين لإدارة الصالات الرياضية. المنصة المركزية تدير
العملاء التجاريين (الصالات)، خطط الـSaaS والميزات والمدفوعات والنسخ الاحتياطية. كل
صالة تعمل كـTenant معزول، وتدير أعضاءها ومدربيها وفروعها واشتراكاتها وعملياتها من
واجهة الصالة.

```text
Platform Owner / Platform Admin
        │ يدير الصالات والخطط والصلاحيات والدفع اليدوي
        ▼
Platform API + SaaS Domain
        │ يعزل البيانات ويحسب الوصول للميزات والحدود
        ▼
Tenant (Gym) ── Owner / Manager / Reception / Accountant / Coach / Client
        │
        └── الأعضاء، الحضور، الاشتراكات، التدريب، التغذية، المبيعات، المخزون…
```

**نموذج الدفع الحالي يدوي.** لا توجد بوابة دفع تلقائية ضمن هذا الإصدار. يرفع العميل
إثباتاً/طلباً، ثم تراجعه الإدارة المركزية وتوافق أو ترفض وفق السجل المالي.

## 2. التسجيل وإنشاء صالة

1. مسؤول المنصة ينشئ الصالة من `الصالـات والمستأجرون` ويحدد بياناتها ومالكها.
2. ينشئ النظام Tenant وحساب المالك في نطاق الصالة؛ لا تثق الخدمات بـ`TenantId`
   القادم من المتصفح، بل تحدده من هوية المستخدم/سياق المستأجر.
3. يختار المسؤول خطة أو يبدأ دورة اشتراك وفق العملية المتاحة.
4. عند تفعيل الاشتراك تُحفظ نسخة Snapshot للسعر والعملة والمدة والحدود والميزات
   وفترة السماح. لا تعدّل هذه النسخة لاحقاً.
5. يدخل مالك الصالة إلى واجهة الصالة ويشغّل الفروع والعملاء والموظفين حسب صلاحياته.

الصالة لا تُحذف مباشرة عند وجود تاريخ تشغيلي؛ تستخدم حالات `Approve` و`Suspend` و
`Activate` و`Archive` للحفاظ على الأدلة والسجلات المرتبطة.

## 3. دورة الاشتراك والدفع اليدوي

```text
طلب دفع جديد
    ├─ رفض → يسجل سبب الرفض، ولا يُنشأ أثر مالي معتمد
    └─ موافقة → Subscription Payment + Invoice + تفعيل/تجديد/تغيير حالة مسجل

اشتراك فعّال ──انتهاء EndDate غير شامل──> Grace (إن كانت السياسة تسمح)
                                     └──> Expired
```

سياسات الأعمال الثابتة:

- الترقية تطبق فوراً وتحسب فرق السعر تناسبياً وفق الأيام المتبقية ودقة `decimal`
  وقواعد التقريب في الـDomain.
- تخفيض الخطة يطبق مع بداية دورة التجديد القادمة، لا فوراً.
- التجديد قبل الانتهاء يبدأ من `EndDate` الحالية؛ وبعد الانتهاء يبدأ من وقت موافقة
  الإدارة على الدفع.
- الإلغاء يمنع التجديد التالي ويظل الوصول فعالاً حتى `EndDate`.
- التعليق `Suspend` يمنع الوصول ولا يوقف عداد المدة؛ `Extend` يضيف أياماً ولا ينشئ
  دورة جديدة.
- `EndDate` غير شامل: ينتهي الوصول عند بلوغه. كل التواريخ تحفظ UTC.
- عمليات الدفع والفواتير المعتمدة غير قابلة للتحرير؛ التصحيح بعملية عكسية جديدة.

### Wallet وPOS والمخزون

خصم أو إضافة رصيد العميل يحدث في SQL داخل Transaction واحدة مع سجل الـWallet؛ لذلك لا
تستخدم العملية قراءة آخر Ledger لتحديد الرصيد. في POS والتعديل والتحويل، ينجح خصم
المخزون فقط إذا كانت الكمية الحالية كافية، وتُحفظ حركة المخزون والفاتورة/البيع معاً.
التعارض أو عدم كفاية الرصيد/المخزون يفشل العملية كلها ولا يترك أثراً جزئياً.

## 4. حسم الوصول للميزات والحدود

الترتيب ملزم ولا يجوز للواجهة تجاوزه:

```text
Global Disable
  → Subscription Access (الحالة/الانتهاء/Grace)
    → Tenant Override (سبب + منفذ + بداية/نهاية)
      → Plan Feature
        → Default Deny
```

- المنحة الإدارية المؤقتة قد تتجاوز انتهاء الاشتراك فقط، ولا تتجاوز `Global Disable`
  ولا إيقاف الـTenant.
- الميزة الجديدة في لوحة المنصة تعني تعريفاً تجارياً فقط؛ لا تنشئ كوداً. قبل إتاحتها
  يجب تطويرها في Backend وFrontend، اختيار `FeatureKey` ثابت، وحماية Endpoints
  الخاصة بها بـFeature Guard.
- الـQuota تتحقق من الخادم داخل Transaction ومع Concurrency Control. حجوزاتها لها
  انتهاء وتحرر تلقائياً عند الفشل.

## 5. الأحداث والموثوقية

الأعمال المؤجلة مثل تغيير حالات الانتهاء وفترة السماح والإشعارات تعمل عبر Background
Jobs. العمليات الحساسة والـJobs قابلة لإعادة التنفيذ `Idempotent`. تستخدم أحداث
النطاق وOutbox حتى لا يضيع حدث بعد نجاح معاملة البيانات. لا تحذف رسائل الـOutbox
مباشرة: تعلّم منفذة ثم تؤرشف حسب سياسة الاحتفاظ.
وعند تشغيل أكثر من نسخة API، يتولى قفل SQL Server نسخة واحدة لكل دورة من دورة الـJob؛
كما يضمن مفتاح Outbox الفريد عدم تسجيل نفس الحدث مرتين.

### مواعيد المدرب والمتدرب

يستخدم المدرب مسارات `/api/Appointments` الحالية لإنشاء الموعد وتأكيده أو إلغائه أو إكماله.
يقرأ المتدرب مواعيده فقط من `/api/client/my-appointments`؛ يحدد الخادم `TenantId` من السياق
والعميل من الهوية الحالية، ولا يقبل معرف عميل من الواجهة لتوسيع نطاق القراءة. حالة الموعد في
الاستجابة هي `AppointmentStatus` المشتركة (`Pending=1`, `Confirmed=2`, `Cancelled=3`,
`Completed=4`) حتى تتطابق شاشة المدرب وشاشة المتدرب ولا تتحول الحالة إلى نص غير متوافق.

## 6. النسخ الاحتياطي والاستعادة

خدمة النسخ تنشئ ملف قاعدة بيانات يحتوي Schema وData الفعليين وفق إعداد الخادم.
التنزيل محمي بصلاحية مستقلة. قبل الاستعادة:

1. نزّل الملف إلى مكان خاص وآمن، وليس مساراً عاماً.
2. اختبر الاستعادة على قاعدة منفصلة أولاً.
3. وثّق اسم النسخة ووقت الإنشاء والسبب ومنفذ العملية.
4. نفّذ خطة Rollback وراجع سلامة الخدمة قبل لمس الإنتاج.

## 7. حالة الصالة مقابل حالة الاشتراك

حالة الصالة (`Tenant`) وحالة اشتراكها قراران منفصلان. إيقاف الصالة يمنع الوصول حتى
لو كان الاشتراك صالحاً؛ اشتراك منتهٍ يمنع الميزات المدفوعة حتى لو كانت الصالة نشطة.
لا تنفذ الواجهة هذا القرار بنفسها، بل تعرض نتيجة قرار الـBackend.

## Coach plan authoring and client execution (Issues #272/#69, task branches)

The coach flow is now an end-to-end aggregate journey:

`select assigned client → build workout or nutrition plan → validate nested items → save atomically → client reads active plan → client records workout sets/meal logs → coach reviews sessions and progress`.

The API, not the browser, decides tenant and assignment access. An owner/manager can manage active
clients in the workspace; a coach/trainer can manage only active `CoachClient` assignments; a client
can read only active plans assigned to that client. Create/update sends the complete nested aggregate
in one request, so a failed child item cannot leave a half-created plan.

The client execution screens show loading, empty, blocked, and error states. Workout actions are
server-confirmed before the UI marks a set complete, active sessions resume safely, and ending an
already-ended session is idempotent. Meal-log responses expose the meal item, food, unit, quantity,
and calculated macros so the daily log and summary use real server data rather than mock values.

## Subscriber and coaching lifecycle (Issue #313)

The complete member journey is:

`create/find member -> create membership -> record immutable payment -> assign coach -> build
workout/diet aggregate -> member executes sessions/meals -> record measurements/check-in -> coach
reviews the combined overview`.

Membership state is separate from member identity and does not remove coaching history. The
subscription endpoints validate the active tenant, dates, overlap, payment amount, and frozen or
cancelled state. Each successful money operation appends a server-generated receipt to the payment
ledger; the UI displays it as history rather than allowing edits to old transactions.

The workout and diet screens submit all nested routines/exercises or meals/items in one aggregate
request. The API validates all references and uses `ExpectedVersion`; a stale update is rejected
instead of overwriting a newer coach edit. Removing a child is a soft delete, preserving historical
sessions and meal logs. Meal calculations use the food's configured serving size and store snapshots
on the log.

The member can record one daily readiness check-in with sleep, recovery, soreness, stress, mood,
vitals, bodyweight, and notes. The client training overview reads plans, sessions, meals, logs,
measurements, and check-ins from the server and returns clear loading, empty, blocked, and error
states in the UI.

### Financial report source of truth

Revenue fields in the financial, subscription, and dashboard reports represent collected cash from
`ClientSubscription.AmountPaid`. `SubscriptionPlan.Price` and `ClientSubscription.TotalAmount`
remain expected/contract values and must not be counted as paid revenue when a subscription is
unpaid or partially paid.

The clients report uses `ClientSubscription.AmountPaid` for its per-client collected total as well.
The current domain has no immutable refund/reversal entity; reports therefore do not claim net
revenue after refunds or cancellations until that ledger is introduced and reviewed.

All subscription payment entry points reject amounts above the remaining balance, and a later
discount cannot lower the contract total below already-collected cash.
