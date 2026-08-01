# كتالوج ميزات LogicFit

> حالة المرجع: مبني على نسخة الإصدار `master` عند `8ddc5db`، مع اعتماد معلّق على PR #109 الذي يُبقي Controller مراجعة الطلبات ضمن الـAPI الموحد (2026-07-30). الشفرة هي مصدر الحقيقة؛ لا تعني أي بطاقة أدناه أن واجهة مستقبلية أو فرعًا غير مدمج أصبح متاحًا في الإنتاج.

هذا هو الفهرس المركزي لكل مجال وظيفي موجود في نظام LogicFit. لا يكرر قائمة الـendpoints؛ المرجع التفصيلي المولّد لها هو [كتالوج API](API-ENDPOINT-CATALOG.md). الغرض من هذا الملف أن يعرف مالك المنتج، الدعم، QA، والفرق الثلاثة أين توجد كل ميزة وما هو التدفق الذي يجب تحديثه عندما تتغير.

> **Unreleased – Issue #118:** the identity feature keeps verified Email + Password and email-link recovery, adds centralized Phone + OTP, Platform Admin OTP, OTP step-up, provider-independent delivery, and HttpOnly refresh cookies. Passkey/WebAuthn is removed. This is local branch behavior until review, merge, migration, secrets, and deployment are complete.

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
| دخول إدارة المنصة | بريد وكلمة مرور ثم OTP إلزامي، refresh cookie وتدوير/إلغاء الجلسات | `Features/Platform/Auth`، `/api/platform/auth/*` | `PlatformOwner`، `PlatformAdmin` |
| لوحة المتابعة | مؤشرات المنصة وقائمة المساحات | `Features/Platform/Dashboard`، `/api/platform/dashboard/*` | صلاحيات Platform المناسبة |
| إدارة المساحات | إنشاء، قائمة، اعتماد، تعليق، تفعيل وأرشفة الجيم/المساحة | `Features/Platform/Tenants`، `/api/platform/tenants/*` | `ManageTenants` |
| طلبات مساحة المدرب الحر | قائمة، بدء مراجعة، طلب معلومات، اعتماد مساحة، اعتماد عضوية ورفض مع `RowVersion` | `LogicFit.API/Features/Platform/WorkspaceApplications`، `/api/platform/workspace-applications/*` | `ManageTenants` |
| الخطط والميزات | الخطط، feature catalog، overrides، quotas، dependencies | `Features/Platform/Plans` و`FeatureCatalog`، `/api/platform/plans/*` و`/api/platform/features/*` | `ManagePlans` / `ManageFeatures` |
| اشتراكات الـSaaS | العرض، الاستهلاك، lifecycle، التمديد ومعاينة الترقية | `Features/Platform/Subscriptions`، `/api/platform/subscriptions/*` | `ManageSubscriptions` |
| الفوترة اليدوية | طرق الدفع، طلبات إثبات الدفع، الاعتماد/الرفض، فواتير المنصة | `PaymentMethods` و`PaymentRequests` و`Invoices`، `/api/platform/payment-*` و`/api/platform/invoices` | صلاحيات الفوترة المركزية |
| مسؤولو المنصة وRBAC | إنشاء/تعطيل مسؤول، أدوار المنصة وخريطة صلاحياتها | `Administrators` و`Authorization`، `/api/platform/administrators/*` و`/api/platform/roles/*` | `PlatformOwner` |
| المراقبة والتدقيق | alerts، audit logs، Outbox/jobs، النسخ الاحتياطي والتقارير | `Alerts`، `Audit`، `Operations`، `Backups`، `Reports` | أدوار تشغيل المنصة |
| الإشعارات المركزية | عرض الإشعارات وتعليمها كمقروءة | `Features/Platform/Notifications` | مسؤول المنصة المستهدف |

## الهوية والوصول ومساحات العمل

| المجال | ما هو مسجل حاليًا | مصدر التنفيذ / عائلة API | الأدوار/الحدود |
|---|---|---|---|
| المصادقة المتوافقة | تسجيل جيم تقليدي، دخول، refresh، logout-all، استعادة وتغيير كلمة المرور | `Features/Auth`، `/api/auth/*` | حساب محلي داخل مساحة محددة |
| الهوية المستقلة | هوية عالمية، Email + Password أو Phone + OTP، اختيار مساحة، تغيير/تأكيد الهاتف، recovery وstep-up | `Features/Identity`، `/api/identity/*` | `IdentityAccount` لا يمنح دخول مساحة وحده؛ OTP لا يتجاوز العضوية/RBAC |
| اختيار مساحة العمل | استبدال token اختيار قصير العمر بـJWT/refresh tenant الموجودين | `IdentityWorkspaceSession` و`WorkspaceMembership` | عضوية `Active` فقط؛ يطبق حارس المساحة قبل إصدار الجلسة |
| حارس الهوية والعضوية | فحص موحد للحساب المحلي والهوية المرتبطة والعضوية عند login وrefresh واختيار المساحة وكل طلب tenant مصادق عليه | `IIdentityWorkspaceAccessGuard` و`IdentityWorkspaceAccessMiddleware` | الحسابات القديمة غير المرتبطة تعمل مؤقتًا بوضع توافق صريح قابل للإيقاف بعد ترحيل الربط المثبت بالبريد |
| طلب مساحة مدرب حر | تقديم إنشاء مساحة وهوية وهوية بصرية مستقلة، جلسة متابعة محدودة، تعديل الحقول المطلوبة وإعادة التقديم | `Features/WorkspaceApplications`، `/api/workspace-applications/*` | public قبل الاعتماد؛ token المتابعة ليس JWT |
| فريق المدرب الحر | ترشيح Coach/Assistant/Client لمساحة مستقلة ثم اعتماد Platform | `FreelanceTeamApplicationsController`، `/api/freelance/team/applications` | Freelance Owner مع `ManageCoaches`؛ لا توجد صلاحية مباشرة قبل الاعتماد |
| المستخدمون والأدوار | مستخدمون، profiles، الأدوار، permissions وإصدارات الصلاحيات | `Features/Users`، `Profile`، `Authorization` | TenantId وملكية المورد حد أمني |
| هوية وهوية بصرية المساحة | gym profile، branding، media وبيانات العلامة | `GymProfile`، `Branding`، `Media` | هوية مساحة المدرب الحر تسري على كل أعضائها |

الشرح التفصيلي لهذه التدفقات، الجلسات، الحالات والـendpoints موجود في [AUTHENTICATION-AND-WORKSPACE-FLOWS.md](AUTHENTICATION-AND-WORKSPACE-FLOWS.md).

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

## المالية، التجارة والمخزون داخل المساحة

| المجال | ما هو مسجل حاليًا | مصدر التنفيذ / عائلة API | المستخدمون الرئيسيون |
|---|---|---|---|
| اشتراكات العملاء | باقات واشتراكات العملاء داخل الجيم، حالة العضوية وتجديدها | `Subscriptions` | Owner / Reception / Accountant |
| المبيعات والفواتير | POS، المبيعات، الفواتير، المدفوعات والمعاملات | `Sales`، `Invoices`، `Payments`، `Transactions` | Reception / Accountant / Owner |
| فوترة المساحة | بيانات اشتراك الجيم/المدرب الحر وطلبات الدفع اليدوي | `TenantBilling` | Owner / Platform review |
| العروض والضرائب | كوبونات، إعدادات الضريبة وفئات المصروفات والمصروفات | `Coupons`، `TaxSettings`، `ExpenseCategories`، `Expenses` | Owner / Accountant |
| الكتالوج والمخزون | فئات المنتجات، المنتجات، المخزون والموردون | `ProductCategories`، `Products`، `Stock`، `Suppliers` | Owner / Manager / Accountant |
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
