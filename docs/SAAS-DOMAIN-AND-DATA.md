# نموذج بيانات الـSaaS وثوابت الأعمال

## طبقات الحل

```text
LogicFit.API/Features/Platform  واجهة الإدارة المركزية وسياسات المنصة داخل المضيف الموحد
LogicFit.API                    واجهة الصالات والمستخدمين والمضيف الموحد
LogicFit.Application      Commands / Queries / قواعد حالات التطبيق
LogicFit.Domain           الكيانات والثوابت والاستثناءات وقواعد الأعمال
LogicFit.Infrastructure   EF Core, Identity, persistence, jobs, backups, outbox
LogicFit.Tests            اختبارات الانحدار والأمان وقواعد الاشتراك والترقيم
```

## Identity email-security data (Issue #113, unreleased)

`IdentityAccount` owns globally unique `NormalizedEmail` and, when present, a unique E.164
`NormalizedPhoneNumber` for contact data only, plus separate email/phone verification timestamps.
`IdentityEmailActionToken` keeps one-use email verification/reset links as SHA-256 hashes.
`RefreshToken.RowVersion` serializes rotation and reuse detection. The final authentication model
contains no OTP, phone-login, Passkey, or WebAuthn runtime entity. Migration
`20260803090742_RemoveLegacyOtpArtifacts` removes the obsolete `OtpChallenges` table with a guarded
drop and does not change tenant business data.

Migration `20260730143000_AddIdentityEmailSecurity` is additive and guards for existing production schemas. It marks existing identities verified during backfill so deployed identity users are not locked out, then adds the token table. It is applied separately through the reviewed migration procedure; its `Down` path is intentionally non-destructive.

Production schema state is advanced only by the explicit deployment migration stage. The stage compares the released migration plan with the target database, requires a verified BACPAC reference, applies the EF lineage before publishing the API, and verifies that no migration remains pending. Application startup never mutates the schema.

Identity login is also a data-consistency boundary for Gym ownership: when a Gym is already
`Active` but its owner `WorkspaceMembership` still has `PendingPlatformApproval` from an older
release, the session issuer promotes only that owner membership to `Active` and records the
reconciliation timestamp/actor. This is an idempotent data repair with no schema migration; client
memberships in `PendingWorkspaceApproval` remain unchanged.

لا ينبغي للـController أن ينفذ قرار Domain معقداً. يحول الطلب إلى Command/Query؛
المعاملات والـConcurrency والتحقق من الملكية تكون في الطبقات المناسبة.

## الكيانات المركزية

| الكيان | المسؤولية | قاعدة الحماية |
|---|---|---|
| `Tenant` | الصالة/العميل التجاري، النطاق والحالة. | لا حذف عند وجود تاريخ؛ حالة دورة حياة. |
| `WorkspaceMembership` | ربط الهوية العالمية بالمستخدم والمساحة وحالة الوصول. | لا يصدر اختيار مساحة أو JWT إلا للعضوية `Active`؛ تفعيل الجيم يفعّل عضوية المالك المنتظرة فقط. |
| `User`, `Role`, `Permission`, `UserRoleAssignment` | هوية وصلاحيات المستخدمين. | JWT + Policy؛ لا ثقة بدور الواجهة. |
| `Plan`, `PlanFeature` | قالب الخطة التجاري وميزاتها. | التعديل مستقبلي؛ لا يعيد كتابة Snapshot. |
| `Feature`, `FeatureDependency` | كتالوج المزايا والاعتماديات. | `FeatureKey` فريد وثابت؛ Archive بدل الحذف التاريخي. |
| `FeatureQuotaDefinition`, `TenantUsage` | تعريف واستعمال حدود الميزة. | Transaction + optimistic concurrency + Reservation expiry. |
| `TenantFeature` | Override لميزة صالة. | سبب، منفذ، وقت بداية/نهاية؛ لا يتجاوز الحظر الأعلى. |
| `TenantSubscription` | دورة اشتراك SaaS. | انتقالات حالة مسموحة فقط وEndDate غير شامل. |
| `SubscriptionFeatureSnapshot` | ميزات وحدود وسعر دورة مفعلة. | غير قابل للتعديل بعد التفعيل. |
| `SubscriptionPayment` | قرار وسجل الدفع اليدوي. | لا تعديل قرار معتمد؛ Idempotency/مرجع. |
| `SubscriptionInvoice` | فاتورة SaaS مرقمة. | رقم فريد متسلسل، لا يعاد استخدامه. |
| `OutboxMessage`, `JobExecutionLog` | موثوقية الأحداث والأعمال الخلفية. | لا حذف يدوي؛ معالجة/أرشفة فقط. |
| `AuditLog`/سجلات تدقيق التطبيق | أثر كل تغيير حساس. | append-only؛ لا تعديل/حذف. |

## عقد حالة التفعيل في طابور المنصة (Issues #244/#245)

طلبات إنشاء المساحات لا تستخدم `Active` كاختصار لكل المراحل. إسقاط الحالة المعروض في
`PlatformApplicationDto` يفصل القيم التالية:

| الحقل | مصدره | المعنى التشغيلي |
|---|---|---|
| `applicationStatus` | `ApplicationRequest` | مسودة، مقدم، مراجعة، استكمال، مقبول أو مرفوض |
| `paymentStatus` | `PaymentRequest` | حالة إثبات/قرار الدفع؛ الاعتماد لا يساوي التفعيل |
| `workspaceStatus` | `Tenant` | دورة مساحة الجيم أو المدرب الحر |
| `subscriptionStatus` | `TenantSubscription` | دورة اشتراك SaaS المستقلة |
| `databaseStatus` / `databaseStatusCode` | `DatabaseResource`/mapping/provisioning job | السعة والتجهيز والتخصيص دون كشف بيانات الاتصال؛ الرمز التشغيلي يميز `Unassigned`, `Provisioning`, `Ready`, `Unavailable`, `Failed`, و`Released` |
| `provisioningStatus` | `ProvisioningJob` | نتيجة التجهيز وإمكانية إعادة المحاولة |
| `canAccessDashboard` | تقاطع الاشتراك والقاعدة والعضوية والمساحة | قرار الوصول النهائي فقط، وليس قيمة `Tenant.Status` منفردة |

إعادة المحاولة تعيد استخدام `ApplicationRequestId` و`ProvisioningJob.IdempotencyKey` ولا تنشئ
Tenant أو Subscription أو Identity أو Mapping جديدة. `FreelanceCoach` يستخدم نفس الكيانات مع
`WorkspaceType=FreelanceCoach` وعضوية `FreelanceOwner` مستقلة عن أي Gym.

كيانات الصالة ترث في الغالب من `TenantAuditableEntity`: العملاء، الفروع، الحضور،
البرامج، التغذية، المدفوعات، المخزون، الموظفون وغيرها. هذا يجعل `TenantId` وحد
العزل جزءاً من البيانات لا اتفاقاً بين الواجهات.

## حالات الاشتراك

الحالات الفعلية وانتقالاتها يحكمها الـDomain. يجب رفض أي انتقال ليس في جدول
الانتقالات المعتمد؛ مثال: يمنع الانتقال المباشر من `Expired` إلى `Suspended`.

| الحالة | الوصول | ملاحظات |
|---|---|---|
| Active | متاح وفق الخطة والميزات | النهاية عند `EndDate` غير الشامل. |
| Grace | حسب سياسة فترة السماح | قد يسمح بالوصول المحدد فقط. |
| Expired | الميزات المدفوعة محجوبة | منحة إدارية مؤقتة مسجلة هي الاستثناء المحدود. |
| Suspended | ممنوع الوصول | لا يوقف احتساب المدة. |
| Cancelled | لا تجديد تلقائي | يبقى فعالاً حتى نهاية المدة إن كانت السياسة كذلك. |

جميع التواريخ UTC ويستخدم النظام `TimeProvider` موحداً لتسهيل الاختبارات ومنع اختلاف
ساعة خوادم متعددة.

## Snapshot والفواتير

عند تفعيل الاشتراك تحفظ Snapshot تشمل على الأقل: السعر، العملة، مدة الخطة، حدود
الاستخدام، الميزات، فترة السماح وقواعد الوصول المطبقة. تغيير هذا العقد يتم فقط عبر
عملية معلنة مثل `Upgrade` أو `Renew` أو `Extend` مع أثر Audit، لا عبر `UPDATE` صامت.

الفاتورة تشمل رقمها، السعر، الخصم، الضريبة، العملة والمبلغ النهائي. الرقم فريد
ومتسلسل ولا يعاد حتى عند الإلغاء. لا تعدّل عملية مالية معتمدة؛ أنشئ عملية تصحيح
معاكسة جديدة واربطها بالسبب.

## المعاملات والتزامن

- استخدم Transaction عند تفعيل/تجديد/ترقية الاشتراك، قرار الدفع، وفحص/حجز Quota.
- استخدم RowVersion/optimistic concurrency للسجلات المشتركة والقيم التي قد تتغير
  بالتوازي.
- استخدم unique constraints وIdempotency keys لمنع قبول نفس العملية عند إعادة
  المحاولة.
- لا تستخدم `Count + 1` لإنتاج رقم يشارك فيه أكثر من طلب؛ رقم الفاتورة له مولّد
  آمن متسلسل.

تحديث WalletBalance يتم كعملية SQL محروسة داخل Transaction، ثم يكتب سجل
`WalletTransaction` مع `BalanceAfter` الناتج من نفس التحديث. لا يُعاد حساب الرصيد من آخر
سجل Ledger، ولا يسمح شرط الخصم بتجاوز الرصيد المتاح. وبالمثل، تغييرات `StockItem.Quantity`
في التعديل/التحويل/POS تستخدم SQL arithmetic guarded، وتُحفظ حركة المخزون والسجل التجاري
معاً؛ مسارات إنشاء صف المخزون تعمل تحت Serializable transaction.

## Outbox وJobs والمراقبة

Domain Event يكتب مع معاملة الأعمال، ثم يسجل في Outbox. عامل خلفي يعالج الرسالة
ويعلمها `Processed` أو `Failed` مع عدد المحاولات والخطأ. لا تحذف الرسالة مباشرة؛
الأرشفة تأتي بعد فترة احتفاظ محددة. Jobs الانتهاء/Grace/الإشعارات قابلة للتكرار بلا
تكرار أثرها (Idempotent).

عند تشغيل أكثر من نسخة من الـAPI، تستخدم Jobs الخلفية SQL Server application locks
بموارد مستقلة: `LogicFit:Background:TenantSubscriptionLifecycle` و
`LogicFit:Background:PlatformSubscriptionLifecycle` و
`LogicFit:Background:OutboxProcessor`. النسخة التي لا تملك القفل تتخطى الدورة، بينما
يمنع unique index على `OutboxMessages.IdempotencyKey` إنشاء نفس رسالة الحدث مرتين.
Migration التنسيق يوقف التطبيق إذا كانت هناك مفاتيح مكررة تحتاج مراجعة تشغيلية؛ لا يحذف
رسائل تاريخية تلقائياً.

أضف Logs وMetrics وAlerts لفشل المدفوعات، Jobs، Outbox وانتقالات حالات الاشتراك.
قبل نشر Migration كبير: Dry Run، Backup، تقرير مخالفات، Rollback Test وFeature Flag
للتفعيل التدريجي.

## Coach plans and client execution data (Issue #272)

`WorkoutProgram` and `DietPlan` remain tenant-owned aggregates. Their child rows
(`ProgramRoutine`/`RoutineExercise` and `DailyMeal`/`MealItem`) carry the same tenant boundary and
are created or reconciled in one transaction. Plan status is explicit (`Active`, `Archived`,
`Draft`); existing workout rows are migrated with `Active` as the safe default. The tenant migration
`20260810125711_CoachPlanExecutionFields` adds plan metadata, planned exercise instructions, meal
timing, and nutrition-plan metadata.

`WorkoutSession` and `SessionSet` are client execution records. `MealLog` is tied to a tenant meal
item and client; its response includes the meal name, food/unit, consumed quantity, timestamp, and
server-calculated macros. Cross-tenant food/exercise references are rejected before an aggregate is
written. The migration is task-branch only until reviewed, merged, applied with a backup/rollback
plan, and verified by health and schema checks.
