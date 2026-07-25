# مرجع API

> هذا مرجع تشغيلي للمسارات. الـControllers وDTOs وSwagger في بيئة التشغيل هي العقد
> التنفيذي عند وجود تعارض. لا تضع أسراراً أو Tokens في أمثلة الطلبات أو سجلات الدعم.

## قواعد مشتركة

- عنوان Platform API الإنتاجي الحالي: `https://logicfit-saas.runasp.net`.
- كل المسارات المحمية تستقبل `Authorization: Bearer <access-token>`.
- تسجيل الدخول وتجديد الجلسة يعيدان Access Token وRefresh Token؛ لا تسجلهما في logs.
- يستخدم Platform API المسار `/api/platform/...`، بينما API الصالات يستخدم `/api/...`.
- الخطأ `401` يعني جلسة غير موجودة/منتهية أو صلاحية غير كافية؛ `403` منع سياسة؛ `404`
  مورد غير موجود أو Endpoint غير منشور؛ `500/503` خطأ/عدم توفر خدمة على الخادم.
- الترقيم القياسي لإدارة المنصة:

```json
{
  "items": [], "totalCount": 0, "page": 1, "pageSize": 20,
  "totalPages": 0, "hasPreviousPage": false, "hasNextPage": false
}
```

`page` يبدأ من 1 و`pageSize` من 1 إلى 100. لا تبنِ ترقيم المتصفح على قائمة غير محدودة.

## Platform Authentication

| Method | Endpoint | الوصول | الغرض |
|---|---|---|---|
| POST | `/api/platform/auth/login` | عام | دخول مسؤول المنصة بالبريد وكلمة المرور. |
| POST | `/api/platform/auth/refresh` | Refresh Token | تدوير Access Token. |
| POST | `/api/platform/auth/logout-all` | منصة مسجلة | إبطال جلسات الحساب من الخادم. |

## Platform إدارة SaaS

| Method | Endpoint | Policy | المعنى |
|---|---|---|---|
| GET | `/api/platform/dashboard` | `ManagePlatformReports` | مؤشرات لوحة المتابعة. |
| GET | `/api/platform/reports/overview` | `ManagePlatformReports` | ملخص تقارير المنصة. |
| GET | `/api/platform/alerts?page=&pageSize=` | `ManagePlatformReports` | تنبيهات تشغيلية مرقمة. |
| GET | `/api/platform/audit-logs?page=&pageSize=&search=` | `ManagePlatformReports` | سجل مراجعة للقراءة. |
| GET | `/api/platform/invoices?page=&pageSize=&search=` | `ManagePlatformReports` | فواتير SaaS للقراءة. |
| GET | `/api/platform/operations/jobs?page=&pageSize=` | `ManagePlatformReports` | سجل Jobs. |
| GET | `/api/platform/operations/outbox?page=&pageSize=` | `ManagePlatformReports` | رسائل Outbox. |

## Platform الصالات والاشتراكات

| Method | Endpoint | Policy | المعنى |
|---|---|---|---|
| GET | `/api/platform/tenants?page=&pageSize=` | `ManageTenants` | الصالات/المستأجرون. |
| POST | `/api/platform/tenants` | `ManageTenants` | إنشاء صالة ومالكها وفق Command. |
| POST | `/api/platform/tenants/{id}/approve` | `ManageTenants` | اعتماد الصالة. |
| POST | `/api/platform/tenants/{id}/suspend` | `ManageTenants` | تعليق الوصول. |
| POST | `/api/platform/tenants/{id}/activate` | `ManageTenants` | إعادة التفعيل. |
| POST | `/api/platform/tenants/{id}/archive` | `ManageTenants` | أرشفة تاريخية. |
| GET | `/api/platform/subscriptions?page=&pageSize=` | `ManageTenants` | دورات اشتراك الصالات. |
| GET | `/api/platform/subscriptions/usage` | `ManageTenants` | استهلاك الميزات/الحدود. |
| POST | `/api/platform/subscriptions/{id}/transition` | `ManageTenants` | انتقال حالة مسموح فقط. |
| POST | `/api/platform/subscriptions/{id}/extend` | `ManageTenants` | إضافة أيام للدورة الحالية. |
| GET | `/api/platform/subscriptions/{id}/upgrade-preview/{targetPlanId}` | `ManageTenants` | معاينة فرق الترقية قبل قرار العمل. |

## Platform الخطط والميزات

| Method | Endpoint | Policy | المعنى |
|---|---|---|---|
| GET/POST | `/api/platform/plans?activeOnly=&page=&pageSize=` | `ManagePlans` | قراءة/إنشاء خطط. |
| PUT/DELETE | `/api/platform/plans/{id}` | `ManagePlans` | تعديل/حذف محكوم بخطّة؛ لا تمس Snapshot الاشتراكات. |
| GET/POST | `/api/platform/features?page=&pageSize=` | `ManagePlans` | كتالوج الميزات/إنشاؤها. |
| PUT | `/api/platform/features/{id}` | `ManagePlans` | تعديل بيانات الميزة؛ FeatureKey ثابت. |
| GET/POST | `/api/platform/features/tenant-overrides?page=&pageSize=` | `ManagePlans` | عرض/تسجيل استثناءات الصالات. |
| GET/POST | `/api/platform/features/quota-definitions?page=&pageSize=` | `ManagePlans` | عرض/إنشاء تعريفات الحدود. |
| PUT | `/api/platform/features/quota-definitions/{id}` | `ManagePlans` | تعديل/تعطيل تعريف الحد. |
| GET/POST | `/api/platform/features/dependencies?page=&pageSize=` | `ManagePlans` | عرض/إضافة اعتماديات الميزات. |
| DELETE | `/api/platform/features/dependencies/{id}` | `ManagePlans` | إزالة علاقة إعداد آمنة فقط. |

## Platform المال والحسابات والنسخ

| Method | Endpoint | Policy | المعنى |
|---|---|---|---|
| GET/POST | `/api/platform/payment-methods?page=&pageSize=` | `ManagePaymentRequests` | وسائل الدفع اليدوي. |
| PUT/DELETE | `/api/platform/payment-methods/{id}` | `ManagePaymentRequests` | تعديل/حذف إعداد طريقة الدفع. |
| GET | `/api/platform/payment-requests?page=&pageSize=` | `ManagePaymentRequests` | طلبات الدفع. |
| POST | `/api/platform/payment-requests/{id}/approve` | `ManagePaymentRequests` | اعتماد طلب بعد المراجعة. |
| POST | `/api/platform/payment-requests/{id}/reject` | `ManagePaymentRequests` | رفض بسبب واضح. |
| GET/POST | `/api/platform/administrators?page=&pageSize=` | `ManagePlatformReports` | حسابات الإدارة وخلق حساب. |
| PATCH | `/api/platform/administrators/{id}/status` | `ManagePlatformReports` | تفعيل/تعطيل الحساب. |
| GET | `/api/platform/roles` | `ManagePlatformReports` | الأدوار. |
| GET | `/api/platform/roles/permissions` | `ManagePlatformReports` | كتالوج الصلاحيات. |
| PUT | `/api/platform/roles/{id}/permissions` | `ManagePlatformReports` | تغيير صلاحيات دور. |
| GET | `/api/platform/backups?page=&pageSize=` | `ManagePlatformBackups` | قائمة النسخ. |
| GET | `/api/platform/backups/status` | `ManagePlatformBackups` | جاهزية خدمة النسخ. |
| POST | `/api/platform/backups` | `ManagePlatformBackups` | طلب إنشاء نسخة. |
| GET | `/api/platform/backups/{fileName}/download` | `ManagePlatformBackups` | تنزيل ملف نسخة مكتملة. |

## API الصالات: المصادقة والهوية

| Method | Endpoint | الغرض |
|---|---|---|
| POST | `/api/Auth/register` | تسجيل مستخدم جديد كـClient؛ الدور لا يختاره المتصفح. |
| POST | `/api/Auth/login` | دخول مستخدم صالة؛ يحدد السياق/الدور. |
| POST | `/api/Auth/refresh` | تجديد Access Token. |
| POST | `/api/Auth/logout-all` | إبطال جلسات المستخدم. |
| POST | `/api/Auth/forget-password` | بدء استعادة كلمة المرور. |
| POST | `/api/Auth/reset-password` | ضبط كلمة مرور عبر رمز صالح. |
| POST | `/api/Auth/change-password` | تغيير كلمة المرور للمستخدم المسجل. |
| GET | `/api/Profile` | ملف المستخدم الحالي؛ وتوجد عمليات تحديث الملف حسب Controller. |
| GET | `/api/Branding/{identifier}` | Branding عام للصالة/النطاق. |

## API الصالات: كتالوج الموارد

هذه الموارد تتبع غالباً نمط `GET collection`, `GET {id}`, `POST`, `PUT {id}`,
`DELETE {id}` عندما تكون العملية مناسبة للأعمال. تحكم Policies والملكية في الحقول
والنطاق الفعلي، وقد تكون بعض الحذف Soft Delete أو غير مسموحة حسب الـDomain.

| المورد | المسار الأساسي | الغرض/عمليات إضافية معروفة |
|---|---|---|
| العملاء | `/api/Clients` | إدارة الأعضاء. |
| المدربون | `/api/Coaches` | ملفات المدربين وربطهم. |
| علاقات المدرب-العميل | `/api/coach-clients` | `assign`، قراءة العلاقة وحذفها. |
| الحضور | `/api/Attendance` | `summary`، `check-in`، `{id}/check-out`. |
| الفروع | `/api/Branches` | `{id}/operating-hours`. |
| الأجهزة والصيانة | `/api/Equipment`, `/api/Maintenance` | حالة الجهاز وسجلات الصيانة. |
| بطاقات العضوية والبوابة | `/api/MembershipCards`, `/api/GateAccess` | تعريف البطاقة وتسجيل الدخول للبوابة. |
| اشتراكات العملاء | `/api/Subscriptions`, `/api/TenantBilling` | دورات عميل الصالة وخطط التحصيل الخاصة بها. |
| الفواتير والمدفوعات | `/api/Invoices`, `/api/Payments`, `/api/Transactions` | إنشاء/إصدار/إلغاء الفاتورة وعمليات الدفع والسجل. |
| المصروفات | `/api/Expenses`, `/api/ExpenseCategories`, `/api/TaxSettings`, `/api/Coupons` | المال التشغيلي؛ `Coupons/validate` للتحقق. |
| نقطة البيع والمخزون | `/api/Sales`, `/api/Products`, `/api/ProductCategories`, `/api/Stock`, `/api/Suppliers` | البيع، المنتجات، المخزون، الموردون وطلبات الشراء. |
| الموظفون | `/api/Employees`, `/api/Shifts`, `/api/Leaves`, `/api/Payroll`, `/api/Commissions` | `{id}/terminate` للموظف، مراجعة الإجازات، قواعد العمولات والرواتب. |
| الحصص | `/api/GroupClasses`, `/api/ClassSchedules` | `cancel`، `book`، enrollments و`attended`. |
| المواعيد | `/api/Appointments` | قراءة، إنشاء، `{id}/status`، إلغاء عند السماح. |
| التمرين | `/api/Exercises`, `/api/Muscles`, `/api/WorkoutPrograms`, `/api/WorkoutSessions` | تمارين، عضلات، برامج وروتينات وجلسات. |
| التغذية | `/api/Foods`, `/api/DietPlans`, `/api/MealLogs` | تكرار خطة، Meals وMeal Items، سجل الوجبات. |
| المجتمع والتواصل | `/api/Chat`, `/api/Challenges`, `/api/Notifications` | محادثات، تحديات، إشعارات وقراءة الرسائل. |
| ملف الصالة | `/api/GymProfile` | بيانات الصالة ورفع logo/cover/gallery. |
| المستخدمون | `/api/Users`, `/api/Tenants` | مستخدمو الصالة وإدارة الصالة ضمن الصلاحية. |
| لوحات العميل | `/api/client/dashboard`, `/api/client/my-programs`, `/api/client/my-diet-plans`, `/api/client/my-subscriptions`, `/api/client/my-measurements`, `/api/client/my-coach`, `/api/client/my-appointments` | قراءة شخصية للـClient فقط. |

## قاعدة تغيير الـAPI

أي تغيير Endpoint أو request/response أو Code خطأ أو Policy يحدث معه: تحديث DTO/Controller
واختبارات، تحديث هذا الملف و`LOGICFIT-PROJECT-STATUS.md`، ثم اختبار الواجهتين قبل
النشر. لا تكسر Endpoint مستهلكاً دون Versioning أو خطة ترحيل واضحة.
