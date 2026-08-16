# كتالوج ميزات LogicFit

## Platform gym lifecycle safety (Issue #214)

Platform tenant management now has explicit credential and deletion actions. Credential viewing
returns owner email and account/membership status only; password reset sends a short-lived email
link and never exposes the existing password. Soft delete blocks workspace access while preserving
the tenant and its mapping for restore. Permanent delete is `PlatformOwner`-only, requires exact
name confirmation, a successful tenant backup, a provider-backed database purge, resource release,
and audit events for every stage. Global Identity is preserved; only the deleted workspace
membership and other workspace associations are revoked.

> **Current auth contract (Issue #161, merged to `develop`):** Identity and Platform authentication
> are Email + Password only. Phone Login, OTP, Passkey, and WebAuthn are not active routes,
> providers, or UI flows. Email verification and password reset use single-use email links.
> Phone Login, OTP verification, Passkey, and WebAuthn are not active API features. Email
> verification and password reset continue to use one-time links.

> **Gym owner login repair (Issue #217):** an already-Active Gym with a legacy pending owner
> membership is reconciled to `Active` during identity login. The repair is owner-only and never
> promotes pending client/workspace memberships.

> حالة المرجع: مبني على نسخة الإصدار `master` عند `8ddc5db`، مع اعتماد معلّق على PR #109 الذي يُبقي Controller مراجعة الطلبات ضمن الـAPI الموحد (2026-07-30). الشفرة هي مصدر الحقيقة؛ لا تعني أي بطاقة أدناه أن واجهة مستقبلية أو فرعًا غير مدمج أصبح متاحًا في الإنتاج.

هذا هو الفهرس المركزي لكل مجال وظيفي موجود في نظام LogicFit. لا يكرر قائمة الـendpoints؛ المرجع التفصيلي المولّد لها هو [كتالوج API](API-ENDPOINT-CATALOG.md). الغرض من هذا الملف أن يعرف مالك المنتج، الدعم، QA، والفرق الثلاثة أين توجد كل ميزة وما هو التدفق الذي يجب تحديثه عندما تتغير.

> **Historical note:** earlier releases briefly contained Phone/OTP authentication. That behavior is
> superseded by the Email + Password-only contract above and must not be enabled again.

> **Issue #147 source implementation; production deployment not yet verified:** the Backend startup
> checks and applies pending compiled EF migrations before seeding, serialized across SQL Server
> workers. This is an operational Backend change only; neither Angular repository has a screen,
> route, or documentation impact.

## متى تُعد الميزة مسجلة؟

تُعد الميزة مسجلة فقط عندما يربط هذا الكتالوج بين:

1. مصدر التنفيذ الفعلي في Backend أو أحد مشروعي الواجهة.
2. تدفق المستخدم أو الحالة التي تحكم سلوكها.
3. صلاحية الوصول أو الدور المسؤول عنها.
4. عائلة الـAPI المولدة، إذا كانت الميزة تعرض API.
5. الاختبارات والوثائق التشغيلية ذات الصلة عند وجودها.

لا يوثق هذا الكتالوج أسرارًا أو كلمات مرور أو connection strings أو ملفات نشر.

## مصادر الحقيقة وحدود المشاريع

| المشروع | ما يملكه | الوثائق المتخصصة |
|---|---|---|
| `LogicFit` | الـAPI الموحد، الـDomain، الـMigrations، الاختبارات، وقواعد المنصة والمساحات | هذا الكتالوج، [التدفقات](PRODUCT-AND-FLOWS.md)، [المصادقة ومساحات العمل](AUTHENTICATION-AND-WORKSPACE-FLOWS.md)، [الصلاحيات](USERS-AND-PERMISSIONS.md) |
| `LogicFit_Angular` | تطبيق الجيم: مالك المساحة، الموظف، المدرب، والعميل | [كتالوج الشاشات](../../LogicFit_Angular/docs/SCREEN-CATALOG.md)، [تدفقات المساحة](../../LogicFit_Angular/docs/WORKSPACE-FLOWS.md)، [مرجع المشروع](../../LogicFit_Angular/docs/PROJECT_REFERENCE.md) |
| `LogiFit_Platform_Admin_Dashboard` | لوحة إدارة الـSaaS المركزية | [كتالوج الشاشات](../../LogiFit_Platform_Admin_Dashboard/docs/SCREEN-CATALOG.md)، [التكامل والمعمارية](../../LogiFit_Platform_Admin_Dashboard/docs/ARCHITECTURE-AND-INTEGRATION.md)، [مساحة الإدارة](../../LogiFit_Platform_Admin_Dashboard/docs/ADMIN-WORKSPACE.md) |

إذا اختلف فرع واجهة عن `master` في Backend، لا يوثّق تدفق الواجهة على أنه متاح في الإصدار قبل دمج فرعيه ومراجعة تكامله مع العقد المنشور.

## المنصة المركزية (SaaS)

| المجال | ما هو مسجل حاليًا | مصدر التنفيذ / عائلة API | من يديره |
|---|---|---|---|
| دخول إدارة المنصة | بريد وكلمة مرور، refresh cookie وتدوير/إلغاء الجلسات | `Features/Platform/Auth`، `/api/platform/auth/*` | `PlatformOwner`، `PlatformAdmin` |
| لوحة المتابعة | مؤشرات المنصة وقائمة المساحات | `Features/Platform/Dashboard`، `/api/platform/dashboard/*` | صلاحيات Platform المناسبة |
| إدارة المساحات | إنشاء، قائمة، اعتماد، تعليق، تفعيل وأرشفة الجيم/المساحة؛ تفعيل عضوية مالك الجيم المنتظرة مع اعتماد المساحة | `Features/Platform/Tenants`، `/api/platform/tenants/*` | `ManageTenants` |
| طلبات مساحات العمل | إنشاء ومراجعة وطلب معلومات ورفض واعتماد/تجهيز وإعادة محاولة لكل من `Gym` و`FreelanceCoach` مع حالات الدفع والمساحة وقاعدة البيانات والاشتراك والوصول | `LogicFit.API/Features/Platform/WorkspaceApplications`، `/api/platform/workspace-applications/*` | `ManageTenants` |

طلب الاستكمال لمساحة Gym أو FreelanceCoach يستخدم حقول payload المشتركة المسموحة، بينما تظل طلبات
العضوية مقصورة على `FullName`؛ الحقول غير المعتمدة مثل `Address` لا تُقبل من الخادم.
| الخطط والميزات | الخطط، feature catalog، overrides، quotas، dependencies | `Features/Platform/Plans` و`FeatureCatalog`، `/api/platform/plans/*` و`/api/platform/features/*` | `ManagePlans` / `ManageFeatures` |
| اشتراكات الـSaaS | العرض، الاستهلاك، lifecycle، التمديد ومعاينة الترقية | `Features/Platform/Subscriptions`، `/api/platform/subscriptions/*` | `ManageSubscriptions` |
| الفوترة اليدوية | طرق الدفع، طلبات إثبات الدفع، معاينة محمية، سجل إصدارات دائم مع SHA-256، الاعتماد/الرفض، فواتير المنصة؛ لا يعتمد دفع مساحة عمل بلا إثبات حالي | `PaymentMethods` و`PaymentRequests` و`Invoices`، `/api/platform/payment-*` و`/api/platform/invoices` | صلاحيات الفوترة المركزية |
| مسؤولو المنصة وRBAC | إنشاء/تعطيل مسؤول، أدوار المنصة وخريطة صلاحياتها | `Administrators` و`Authorization`، `/api/platform/administrators/*` و`/api/platform/roles/*` | `PlatformOwner` |
| المراقبة والتدقيق | alerts، audit logs، Outbox/jobs مع تنسيق متعدد النسخ، نسخ BACPAC مستقلة للمنصة أو مساحة عمل واحدة أو نطاق جماعي، والتقارير | `Alerts`، `Audit`، `Operations`، `Backups`، `Reports` | أدوار تشغيل المنصة |
| عقد تشغيل لوحة المنصة | ملخصات مراجعة الطلبات والدفع، سعة Pool، Provisioning، النسخ والاستعادة، وتشخيص إصدار الـAPI | `Features/Platform/Dashboard`، `DatabaseResources`، `Diagnostics`، `Operations`؛ `/api/platform/dashboard/*`، `/api/platform/database-resources`، `/api/platform/diagnostics/version`، `/api/platform/operations/provisioning` | `ManagePlatformReports` / `ManagePlatformBackups` |
| الإشعارات المركزية | عرض الإشعارات وتعليمها كمقروءة | `Features/Platform/Notifications` | مسؤول المنصة المستهدف |

## الهوية والوصول ومساحات العمل

| المجال | ما هو مسجل حاليًا | مصدر التنفيذ / عائلة API | الأدوار/الحدود |
|---|---|---|---|
| المصادقة المتوافقة | refresh، logout-all، استعادة وتغيير كلمة المرور بعد الانتقال للهوية | `Features/Auth`، `/api/auth/*` | لا توجد Legacy login/register routes فعالة |
| الهوية المستقلة | هوية عالمية، Email + Password، اختيار مساحة، تأكيد البريد وemail-link recovery | `Features/Identity`، `/api/identity/*` | `IdentityAccount` لا يمنح دخول مساحة وحده؛ الهاتف للتواصل فقط |
| اختيار مساحة العمل | استبدال token اختيار قصير العمر بـJWT/refresh tenant الموجودين | `IdentityWorkspaceSession` و`WorkspaceMembership` | عضوية `Active` فقط؛ يطبق حارس المساحة قبل إصدار الجلسة |
| حارس الهوية والعضوية | فحص موحد للحساب المحلي والهوية المرتبطة والعضوية عند login وrefresh واختيار المساحة وكل طلب tenant مصادق عليه | `IIdentityWorkspaceAccessGuard` و`IdentityWorkspaceAccessMiddleware` | الحسابات القديمة غير المرتبطة تعمل مؤقتًا بوضع توافق صريح قابل للإيقاف بعد ترحيل الربط المثبت بالبريد |
| طلب مساحة مدرب حر | تقديم إنشاء مساحة وهوية وهوية بصرية مستقلة، جلسة متابعة محدودة، تعديل الحقول المطلوبة وإعادة التقديم؛ ويمكن لمسؤول المنصة بدء نفس المسار الموحد | `Features/WorkspaceApplications`، `/api/workspace-applications/*` و`/api/platform/workspace-applications/*` | public قبل الاعتماد؛ token المتابعة ليس JWT؛ لوحة المنصة تتطلب `ManageTenants` |
| فريق المدرب الحر | ترشيح Coach/Assistant/Client لمساحة مستقلة ثم اعتماد Platform | `FreelanceTeamApplicationsController`، `/api/freelance/team/applications` | Freelance Owner مع `ManageCoaches`؛ لا توجد صلاحية مباشرة قبل الاعتماد |
| المستخدمون والأدوار | مستخدمون، profiles، الأدوار، permissions وإصدارات الصلاحيات | `Features/Users`، `Profile`، `Authorization` | TenantId وملكية المورد حد أمني |
| هوية وهوية بصرية المساحة | gym profile، branding، media وبيانات العلامة | `GymProfile`، `Branding`، `Media` | هوية مساحة المدرب الحر تسري على كل أعضائها |

الشرح التفصيلي لهذه التدفقات، الجلسات، الحالات والـendpoints موجود في [AUTHENTICATION-AND-WORKSPACE-FLOWS.md](AUTHENTICATION-AND-WORKSPACE-FLOWS.md).

## دورة الاشتراك الموحدة (Issue #248)

`Gym` و`FreelanceCoach` يمران بنفس رحلة الاشتراك: نوع المساحة، الباقة، البيانات الأساسية، إثبات
الدفع، ثم المراجعة والتجهيز والتفعيل. يعيد الخادم snapshot آمنًا يفصل بين حالة الطلب والدفع
والـTenant والاشتراك وقاعدة البيانات والـprovisioning والعضوية، ولا يسمح بالـDashboard قبل اكتمال
كل بوابات الوصول. حجز الـDatabaseResource يتم ذريًا من الـPool، وتبقى connection material داخل
الخادم، مع migrations و`CanConnect` وhealth check قبل إنشاء الـmapping. الطلبات التي تحتاج سعة أو
إعادة محاولة لا تنشئ كيانات مكررة.

إثبات الدفع جزء من السجل التشغيلي طويل الأجل: كل رفع ينشئ `PaymentProof` version جديدة مع اسم الملف
والنوع والحجم و`SHA-256` ووقت الرفع، وتبقى النسخ السابقة محفوظة عند الاستبدال. مسارات المنصة
المحمية تعرض metadata فقط في سجل التاريخ، وتسترجع الملف الحالي أو إصدارًا تاريخيًا دون كشف مفتاح
التخزين؛ اعتماد الدفع منفصل عن اعتماد المساحة وبدء provisioning.

## تشغيل مساحة الجيم أو المدرب الحر

| المجال | ما هو مسجل حاليًا | مصدر التنفيذ / عائلة API | المستخدمون الرئيسيون |
|---|---|---|---|
| الفروع والأماكن | الفروع، الغرف، الجداول التشغيلية وإعدادات الجيم | `Branches`، `Rooms`، `GymProfile` | Owner / Manager |
| العملاء | ملف العميل، قياسات الجسم، لوحة العميل وبطاقات العضوية | `Clients`، `BodyMeasurements`، `ClientDashboard`، `MembershipCards` | Owner / Reception / Coach / Client بحسب المورد |
| المدربون وعلاقاتهم | مدربون، ربط المدرب بالعملاء وإدارة اختصاص المدرب | `Coaches`، `CoachClients` | Owner / Manager / Coach |
| المواعيد والحصص | مواعيد، حصص جماعية، جداول الحصص وتسجيل الحضور في الحصة | `Appointments`، `GroupClasses`، `ClassSchedules`، `ClassEnrollments` | Owner / Reception / Coach / Client |
| الحضور والدخول | حضور العميل، حضور الموظف، بوابة الدخول وبطاقة العضوية | `Attendance`، `StaffAttendance`، `GateAccess`، `MembershipCards` | Reception / Owner / Manager |
| الموظفون وHR | موظفون، shifts، إجازات، payroll وعمولات | `Employees`، `Shifts`، `Leaves`، `Payroll`، `Commissions` | Owner / Manager / Accountant |
| التدريب | برامج التدريب، جلسات التدريب، التمارين والعضلات | `WorkoutPrograms`، `WorkoutSessions`، `Exercises`، `Muscles` | Coach / Client / Owner |
| التغذية | أطعمة، خطط غذائية وسجل الوجبات | `Foods`، `DietPlans`، `MealLogs` | Coach / Client |
| التفاعل | تحديات، chat، إشعارات | `Challenges`، `Chat`، `Notifications` | حسب عضوية وملكية كل مساحة |
| التقارير | تقارير تشغيل ومالية ورياضية | `Reports` | الأدوار صاحبة صلاحية التقرير |

## حسابات الفريق والوصول (Issues #246 and #65)

The owner-managed team surface is separate from the legacy HR profile form. `WorkspaceMembersController`
and the `WorkspaceMembers` application feature provide a single operation for identity, tenant-local
user, membership, role assignment, one-time credentials, password reset, and access lifecycle. The
surface is protected by `ManageEmployees`, is tenant-scoped, rejects duplicate active memberships,
supports identity reuse across workspaces, and records security audit events without credentials.
The Tenant UI route is `/owner/workspace-access` with explicit loading, empty, error, credential,
and state-action screens.

## المالية، التجارة والمخزون داخل المساحة

| المجال | ما هو مسجل حاليًا | مصدر التنفيذ / عائلة API | المستخدمون الرئيسيون |
|---|---|---|---|
| اشتراكات العملاء | باقات واشتراكات العملاء داخل الجيم، حالة العضوية وتجديدها | `Subscriptions` | Owner / Reception / Accountant |
| المبيعات والفواتير | POS، المبيعات، الفواتير، المدفوعات والمعاملات مع خصم مخزون ذري | `Sales`، `Invoices`، `Payments`، `Transactions` | Reception / Accountant / Owner |
| فوترة المساحة | بيانات اشتراك الجيم/المدرب الحر وطلبات الدفع اليدوي | `TenantBilling` | Owner / Platform review |
| العروض والضرائب | كوبونات، إعدادات الضريبة وفئات المصروفات والمصروفات | `Coupons`، `TaxSettings`، `ExpenseCategories`، `Expenses` | Owner / Accountant |
| الكتالوج والمخزون | فئات المنتجات، المنتجات، المخزون والموردون مع تحديثات كمية آمنة للتزامن | `ProductCategories`، `Products`، `Stock`، `Suppliers` | Owner / Manager / Accountant |
| الأصول والصيانة | المعدات وخطط/سجلات الصيانة | `Equipment`، `Maintenance` | Owner / Manager |

## حالات الوصول التي يجب أن تراعيها كل ميزة

لا تكفي صلاحية المستخدم وحدها. ترتيب الحراسة هو: **حالة المساحة ثم حالة العضوية ثم حالة اشتراك المنصة ثم الدور والصلاحيات**.

| الحالة | الوصول الفعلي |
|---|---|
| مساحة `Suspended` أو `Archived` أو `Provisioning` أو `ProvisioningFailed` | منع تشغيلي كامل، حتى لو كان الدور أو الاشتراك صالحًا |
| اشتراك `None` أو `PendingPayment` | billing فقط؛ والمدرب الحر الجديد بلا اشتراك يدخل هذه الحالة |
| `Trial` أو `Active` أو `PastDue` أو `GracePeriod` | تشغيل عادي ضمن الخطة |
| `Cancelled` ووقت التشغيل قبل `EndDate` | وصول كامل حتى نهاية الدورة المدفوعة مع إيقاف التجديد |
| `Expired` أو `Cancelled` عند/بعد `EndDate` أو subscription `Suspended` | قراءة فقط مع إتاحة الفوترة/التجديد حيث تسمح السياسة |
| جيم قديم بلا سجل SaaS subscription | يحافظ مؤقتًا على وصوله التشغيلي للتوافق أثناء الترحيل |

## خريطة الأدوار المرجعية

| النطاق | الأدوار النظامية |
|---|---|
| داخل مساحة الجيم | `Owner`، `Manager`، `Receptionist`، `Accountant`، `Coach`، `Trainer`، `Client` |
| داخل مساحة المدرب الحر | `FreelanceOwner`، `FreelanceCoach`، `FreelanceAssistant`، `Client` |
| المنصة | `PlatformOwner`، `PlatformAdmin` |

مرجع منح الصلاحيات وحدود العزل هو [USERS-AND-PERMISSIONS.md](USERS-AND-PERMISSIONS.md)، وليس هذه القائمة المختصرة.

## قاعدة الصيانة الإلزامية

عند إنشاء أو تعديل أو إزالة أي ميزة، يجب في Pull Request نفسه:

1. تعديل صفها هنا أو إضافته، مع بيان مصدر التنفيذ والأدوار والتدفق المتأثر.
2. تعديل التدفق المتخصص: [تدفقات المنتج](PRODUCT-AND-FLOWS.md)، [المصادقة والمساحات](AUTHENTICATION-AND-WORKSPACE-FLOWS.md)، أو دليل الواجهة المعني.
3. تحديث توثيق الواجهة في مستودعها إذا تغيرت شاشة أو route أو رحلة مستخدم.
4. تشغيل مولد [كتالوج API](API-ENDPOINT-CATALOG.md) إذا تغير Controller أو route أو policy أو DTO/API contract.
5. تحديث [حالة المشروع](LOGICFIT-PROJECT-STATUS.md) عند أي أثر سلوكي أو أمني أو تشغيلي أو قاعدة بيانات.

### Coach plan aggregate authoring and client execution (Issues #272/#69, task branches)

- The workout and nutrition builders use the aggregate `POST`/`PUT` contracts for a complete plan,
  including nested routines/exercises or meals/items, metadata, status, and client assignment.
- `CoachPlanAccessService` enforces tenant scope plus active coach-client assignment for coach-facing
  mutations and client ownership for workout sessions and meal logs.
- Client screens call the session and meal-log APIs and render server-confirmed progress; mock session
  history and legacy sequential child-save behavior are no longer the active path.
- Backend migration: `20260810125711_CoachPlanExecutionFields`. Availability remains task-branch only
  until merge/release/deployment/health verification.

### Workspace-specific product surface (Issue #296)

The shared API now exposes a `WorkspaceCapabilities` contract derived from `WorkspaceType`.
`Gym` keeps facilities, staff, attendance, inventory, POS, gate access, membership cards, gym
membership plans, and gym reports. `FreelanceCoach` receives the coaching surface (clients,
training, nutrition, progress, appointments, finance, reports) plus a small assistant-team
surface, but not Gym-only features. Billing, settings, backups, and shared coaching components
remain available when their existing permission and plan rules allow them.

Gym-only controllers enforce the capability on the server; hiding a navigation item is not the
security boundary. See [WORKSPACE-CAPABILITIES.md](WORKSPACE-CAPABILITIES.md) for the complete
mapping and the `WORKSPACE_CAPABILITY_NOT_AVAILABLE` response contract. This implementation is
task-branch only until review, merge, release, deployment, and health verification.

## Subscriber, membership, training and nutrition parity (Issue #313)

| Capability | Backend contract | Business rule |
|---|---|---|
| Member identity and memberships | `Clients`, `ClientSubscriptions`, coach-client assignments | One member identity may have multiple memberships; tenant and ownership checks remain mandatory. |
| Membership billing | Subscription create/update/renew/payment and `Payments` history | Payment rows are append-only, receipts are generated server-side, dates are inclusive, and duplicate/overlapping or overpaid operations are rejected. |
| Training authoring | `WorkoutPrograms` aggregate create/update/duplicate/delete | Nested routines and exercises are saved atomically; `Version` prevents lost updates; removed children are soft-deleted. |
| Nutrition authoring | `DietPlans` aggregate create/update/duplicate/delete | Nested meals and items are saved atomically; calculator metadata and notes are preserved; removed children remain available to history. |
| Training execution | `WorkoutSessions`, `SessionSets`, client training overview | The client can execute only an active plan belonging to the active tenant and receives server-confirmed progress. |
| Nutrition execution | `meal-logs`, nutrition summary, food/meal snapshots | Macros use the food's serving size and the consumed quantity; historical logs do not change when a plan child is removed. |
| Measurements and readiness | `BodyMeasurements`, `clients/{id}/checkins` | Measurements are tenant-scoped; check-ins are unique per client/day and expose a calculated readiness score. |

The implementation is tracked in Backend Issue #313 and Tenant UI Issue #102. The Platform Admin
repository has no source or documentation impact from this feature. Generated endpoint details
are in [API-ENDPOINT-CATALOG.md](API-ENDPOINT-CATALOG.md); the migration/release gate is documented
in [OPERATIONS-AND-DEPLOYMENT.md](OPERATIONS-AND-DEPLOYMENT.md).
