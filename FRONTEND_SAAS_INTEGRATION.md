# LogicFit — دليل تكامل الفرونت إند (SaaS Features)

> **النطاق**: كل ما أُضيف حديثاً لتطبيق الجيم (Tenant App). **مستثنى**: لوحة تحكم المنصة (Platform Admin Dashboard) — تُوثّق منفصلة.
>
> كل الـ JSON بصيغة **camelCase**. كل التواريخ **UTC (ISO 8601)**.

---

## 0) المعمارية والـ Base URLs

النظام مكوّن من **API-ين منفصلين** يشاركان نفس قاعدة البيانات:

| API | الاستخدام | Base URL |
|-----|-----------|----------|
| **Tenant API** | تطبيق الجيم (Owner / Manager / Receptionist / Accountant / Coach / Client) | `{{TENANT_API}}` *(يُنشر لاحقاً، مثال: `https://logicfit.runasp.net`)* |
| Platform API | لوحة الأدمن (منفصلة — خارج هذا المستند) | `https://logicfit-platform.runasp.net` |

كل المسارات في هذا المستند على **Tenant API** ما لم يُذكر غير ذلك.

**المصادقة**: JWT Bearer. أضف لكل طلب محمي:
```
Authorization: Bearer <accessToken>
```

---

## 1) المصادقة (Authentication)

### مفهوم مهم: تحديد الجيم (بالـ subdomain)
كل مستخدم ينتمي لجيم (Tenant). لتحديد الجيم في الدخول/التسجيل، **أرسل `subdomain` بتاع الجيم مباشرةً** (اللي في اللينك، مثل `goldgym`) — الـ API يحلّه تلقائياً. **مش محتاج تجيب أو تعرف الـ TenantId (GUID).**
> بدائل مقبولة (اختياري): تمرير `tenantId` GUID في الجسم، أو هيدر `X-Tenant-Id: <GUID>`، أو الاستضافة على subdomain الجيم. الأبسط دايماً: حقل `subdomain`.

### 1.1 تسجيل الدخول
`POST /api/auth/login` — **عام (Anonymous)**
```json
{ "phoneNumber": "01000000000", "password": "P@ssw0rd", "subdomain": "goldgym" }
```
**الرد** (`AuthResponseDto`):
```json
{
  "userId": "GUID",
  "email": "user@gym.com",
  "phoneNumber": "01000000000",
  "fullName": "أحمد",
  "role": "Owner",
  "roles": ["Owner"],
  "permissions": ["ManageMembers", "ViewMembers", "ManageFinance", "..."],
  "tenantId": "GUID",
  "accessToken": "JWT...",
  "refreshToken": "opaque-string",
  "expiresAt": "2026-07-08T12:15:00Z"
}
```
> ⚠️ **`accessToken` عمره 15 دقيقة**. استخدم `refreshToken` للتجديد (1.4). خزّن `permissions[]` واستخدمها لإظهار/إخفاء عناصر الواجهة (القسم 2).

### 1.2 التسجيل (إنشاء عميل)
`POST /api/auth/register` — **عام**. **ينشئ عميل (Client) فقط دائماً** — لا يمكن اختيار الدور.
```json
{ "email": "c@gym.com", "phoneNumber": "0100...", "password": "P@ssw0rd",
  "confirmPassword": "P@ssw0rd", "subdomain": "goldgym", "fullName": "سارة" }
```
الرد نفس `AuthResponseDto`.
> إنشاء موظفين/مدربين يتم من داخل التطبيق عبر شاشات محمية (Clients/Coaches/Employees) — مش من التسجيل العام.

### 1.3 نسيت / إعادة تعيين كلمة المرور
- `POST /api/auth/forget-password` → `{ "phoneNumber": "...", "subdomain": "goldgym" }` → يرجّع `resetToken` (في الإنتاج يُرسل SMS/Email).
- `POST /api/auth/reset-password` → `{ "phoneNumber", "resetToken", "newPassword", "subdomain": "goldgym" }`.

### 1.4 تجديد التوكن (Refresh) — **مهم**
`POST /api/auth/refresh` — **عام**
```json
{ "refreshToken": "opaque-string" }
```
الرد: نفس `AuthResponseDto` بتوكنات جديدة. **التوكن القديم يُبطَل (Rotation)** — استبدل المخزّن دائماً بالجديد.
> **نمط مقترح**: عند أي `401`، جرّب refresh مرة واحدة ثم أعد الطلب؛ لو فشل الـ refresh → سجّل خروج المستخدم.

### 1.5 تسجيل الخروج من كل الأجهزة
`POST /api/auth/logout-all` — **محمي**. يُبطل كل الـ refresh tokens للمستخدم.

---

## 2) الصلاحيات (RBAC) — كيف يتحكم الفرونت في الواجهة

الـ JWT يحمل مصفوفة **`permissions[]`** (موجودة أيضاً في رد الدخول). **اعرض كل شاشة/زرّ حسب امتلاك المستخدم الصلاحية**. لو استدعى المستخدم endpoint بدون صلاحية → **`403`**.

### 2.1 كتالوج الصلاحيات (Tenant)
```
ManageMembers, ViewMembers, ManageCoaches, ManageAttendance,
ManageClientSubscriptions, ManagePOS, ManageInventory, ManageEmployees,
ManageBranches, ManageFinance, ViewReports, ManageReports,
ManageSettings, ManageTenantBilling
```

### 2.2 خريطة الصلاحية المطلوبة لكل منطقة (Endpoints المحمية بصلاحية)
| المنطقة (Controllers) | الصلاحية المطلوبة |
|---|---|
| Clients, MembershipCards | `ManageMembers` |
| Coaches, CoachClients | `ManageCoaches` |
| Employees, Payroll, Shifts, Leaves | `ManageEmployees` |
| Branches, Rooms, Equipment, Maintenance | `ManageBranches` |
| Attendance, GateAccess | `ManageAttendance` |
| Subscriptions (اشتراكات العملاء داخل الجيم) | `ManageClientSubscriptions` |
| Invoices, Payments, Transactions, Expenses, ExpenseCategories, Commissions, Coupons | `ManageFinance` |
| Sales | `ManagePOS` |
| Products, ProductCategories, Stock, Suppliers | `ManageInventory` |
| Reports | `ViewReports` |
| GymProfile, Users, TaxSettings | `ManageSettings` |
| **Subscription/Billing (اشتراك الجيم في المنصة)** | `ManageTenantBilling` |

> **Endpoints بدون صلاحية محددة** (أي مستخدم مسجّل، مع فحص ملكية داخلي): `Profile`, `ClientDashboard`, `Notifications`, `Chat`, `Appointments`, `WorkoutPrograms`, `WorkoutSessions`, `DietPlans`, `BodyMeasurements`, `Exercises`, `Foods`, `Muscles`, `GroupClasses`, `ClassSchedules`, `Challenges`.

### 2.3 الأدوار وصلاحياتها الافتراضية
| الدور | الصلاحيات |
|-------|-----------|
| **Owner** | كل صلاحيات الجيم |
| **Manager** | الكل عدا `ManageSettings` و`ManageTenantBilling` |
| **Receptionist** | `ViewMembers, ManageMembers, ManageAttendance, ManageClientSubscriptions, ManagePOS` |
| **Accountant** | `ManageFinance, ViewReports, ManageReports, ManageTenantBilling` |
| **Coach** | `ViewMembers, ManageAttendance, ViewReports` (متدربيه فقط) |
| **Client** | لا شيء من الـ back-office (شاشات self-service فقط) |

---

## 3) اشتراك الجيم في المنصة والفوترة (Subscription & Billing)

كل المسارات تحت `/api/tenant` وتتطلب صلاحية **`ManageTenantBilling`** (غالباً الـ Owner). هذه شاشات **صاحب الجيم** لإدارة اشتراك جيمه في منصة LogicFit والدفع **يدوياً**.

### 3.1 المسار الكامل (Flow)
```
1) GET  /api/tenant/plans                 → اعرض الباقات المتاحة
2) POST /api/tenant/subscription/select-plan  { planId }   → يفتح اشتراك PendingPayment
3) GET  /api/tenant/payment-methods       → اعرض طرق الدفع (حساب بنكي/انستاباي/محفظة...)
4) [يدفع خارج النظام]
5) POST /api/tenant/payment-requests (multipart + صورة الإثبات)  → يرسل الطلب
6) [الأدمن يراجع ويوافق]  → يتفعّل الاشتراك تلقائياً
7) GET  /api/tenant/my-subscription       → الحالة أصبحت Active
8) GET  /api/tenant/invoices              → الفاتورة المدفوعة
```
**التجديد**: `POST /api/tenant/subscription/renew`. **الترقية**: `POST /api/tenant/subscription/upgrade { planId }` (ثم نفس خطوات الدفع 3-6).

### 3.2 الباقات المتاحة
`GET /api/tenant/plans` → `PlanDto[]`
```json
[{
  "id": "GUID", "name": "Professional", "description": null,
  "price": 499.00, "currency": "EGP", "billingCycle": 1, "durationInDays": 30,
  "maxMembers": 500, "maxCoaches": 10, "maxBranches": 3, "maxEmployees": 20, "maxStorageMB": 10240,
  "isActive": true, "displayOrder": 2,
  "features": ["POS","Inventory","AdvancedReports","MultiBranch","EmployeeManagement","FinanceModule","ClientMobileApp"]
}]
```
> `maxX = null` تعني **غير محدود** (باقة Enterprise). `billingCycle` enum (القسم 7).

### 3.3 اشتراكي الحالي
`GET /api/tenant/my-subscription` → `MySubscriptionDto`
```json
{
  "hasSubscription": true,
  "subscriptionId": "GUID", "planId": "GUID", "planName": "Professional",
  "status": 3,                       // TenantSubscriptionStatus (القسم 7)
  "startDate": "2026-07-08T...", "endDate": "2026-08-07T...", "trialEndsAt": null,
  "remainingDays": 30, "amount": 499.00, "currency": "EGP", "autoRenew": false,
  "features": ["POS","Inventory","..."],
  "members":   { "used": 128, "limit": 500 },
  "coaches":   { "used": 4,   "limit": 10 },
  "branches":  { "used": 2,   "limit": 3 },
  "employees": { "used": 7,   "limit": 20 }
}
```
> لو `hasSubscription=false` → الجيم لسه مالوش اشتراك (اعرض شاشة اختيار باقة). `limit=null` = غير محدود.

### 3.4 الاستخدام مقابل الحدود
`GET /api/tenant/usage` → `{ members, coaches, branches, employees }` كل واحد `{ used, limit }`. (مفيد لعرض progress bars.)

### 3.5 الفواتير
`GET /api/tenant/invoices` → `SubscriptionInvoiceDto[]`
```json
[{ "id":"GUID", "invoiceNumber":"INV-20260708-AB12CD34", "amount":499.00,
   "currency":"EGP", "status":3, "issueDate":"...", "dueDate":null, "paidAt":"..." }]
```
`status` = `SubscriptionInvoiceStatus` (القسم 7).

### 3.6 طرق الدفع المتاحة
`GET /api/tenant/payment-methods` → `PaymentMethodDto[]`
```json
[{ "id":"GUID", "name":"InstaPay", "type":"InstaPay", "accountName":"LogicFit",
   "accountNumber":"...", "iban":"...", "walletNumber":"0100...",
   "instructions":"حوّل ثم ارفع الإيصال", "qrImageUrl":"https://...", "isActive":true, "displayOrder":1 }]
```

### 3.7 اختيار / ترقية / تجديد الباقة
- `POST /api/tenant/subscription/select-plan` → `{ "planId":"GUID" }`
- `POST /api/tenant/subscription/upgrade` → `{ "planId":"GUID" }`
- `POST /api/tenant/subscription/renew` → *(بلا body)*

الرد (الثلاثة): `TenantSubscriptionSummaryDto`
```json
{ "subscriptionId":"GUID", "planId":"GUID", "planName":"Professional",
  "status":1, "amount":499.00, "currency":"EGP" }
```
> `status=1` = PendingPayment → انتقل لرفع إثبات الدفع.

### 3.8 رفع إثبات الدفع (Payment Request)
`POST /api/tenant/payment-requests` — **`multipart/form-data`**

| الحقل | النوع | إلزامي |
|-------|------|--------|
| `planId` | Guid | ✅ |
| `proof` | ملف صورة | (مستحسن) |
| `paymentMethodId` | Guid | ❌ |
| `transactionNumber` | string | ❌ |
| `paymentDate` | datetime | ❌ |
| `notes` | string | ❌ |

الرد: `PaymentRequestDto`
```json
{ "id":"GUID","tenantId":"GUID","planId":"GUID","planName":"Professional",
  "tenantSubscriptionId":"GUID","amount":499.00,"currency":"EGP",
  "paymentMethodId":"GUID","transactionNumber":"TX123","paymentDate":"...",
  "proofFileUrl":"/uploads/payment-proofs/...","notes":null,
  "status":1,"reviewedBy":null,"reviewedAt":null,"rejectReason":null,"createdAt":"..." }
```

### 3.9 طلباتي (سجل الدفع)
`GET /api/tenant/payment-requests` → `PaymentRequestDto[]` (`status` = `PaymentRequestStatus`).
> عند الرفض: `status=3` و`rejectReason` مملوء — اعرضه للمستخدم واسمح بإعادة الرفع.

---

## 4) حدود الباقة (Feature Gating) — التعامل مع `402`

عند محاولة تجاوز حد الباقة (مثلاً إضافة عميل بعد الوصول للحد الأقصى) أو استخدام ميزة غير مشمولة، الـ API يرجّع **`402 Payment Required`**:
```json
{ "statusCode": 402,
  "message": "You have reached your plan limit for Members (100). Upgrade your plan to add more." }
```
**تعامل الفرونت**: اعرض رسالة الترقية ووجّه المستخدم لشاشة الباقات (`/api/tenant/plans`). ينطبق على: إضافة **Members / Coaches / Branches / Employees**، وميزة **EmployeeManagement** (وحدة الموظفين).

---

## 5) العلامة البيضاء (White-Label Branding)

### 5.1 جلب هوية الجيم قبل الدخول — **عام (Anonymous)**
`GET /api/branding/{identifier}` — `identifier` = الـ subdomain أو الـ custom domain.
```json
{ "tenantId":"GUID", "name":"Gold Gym", "subdomain":"goldgym",
  "appName":"Gold Gym App", "logoUrl":"https://...", "coverImageUrl":"https://...",
  "primaryColor":"#3B82F6", "secondaryColor":"#1E40AF", "fontFamily":"Cairo",
  "customCss":".btn{...}", "invoiceLogoUrl":"https://...",
  "supportPhone":"0100...", "supportEmail":"support@goldgym.com" }
```
**الاستخدام**: استدعِه من الـ subdomain الحالي عند فتح التطبيق → طبّق الألوان/اللوجو/اسم التطبيق/الـ CSS على شاشة الدخول، **واحفظ `tenantId` لاستخدامه في تسجيل الدخول** (القسم 1). يرجّع `404` لو الـ subdomain غير موجود.

### 5.2 تعديل هوية الجيم (Owner) — `ManageSettings`
`PUT /api/gymprofile` (محمي). كل الحقول اختيارية (أرسل اللي تغيّر فقط):
```json
{ "name":"...", "description":"...", "address":"...", "phoneNumber":"...", "email":"...",
  "logoUrl":"...", "coverImageUrl":"...", "galleryImages":["..."],
  "primaryColor":"#...", "secondaryColor":"#...",
  "appName":"...", "fontFamily":"...", "customCss":"...",
  "invoiceLogoUrl":"...", "supportPhone":"...", "supportEmail":"...",
  "customDomain":"app.goldgym.com" }
```
رفع اللوجو: `POST /api/gymprofile/logo` (`multipart`, حقل `file`) → يرجّع `{ url }`.

---

## 6) صيغة الأخطاء الموحّدة

كل الأخطاء بنفس الشكل:
```json
{ "statusCode": 400, "message": "Validation failed",
  "errors": { "Email": ["Invalid email format"] } }   // errors فقط في أخطاء التحقق
```
| Status | المعنى | تعامل الفرونت |
|--------|--------|----------------|
| `400` | تحقق فاشل | اعرض `errors` لكل حقل |
| `401` | غير مصادَق / توكن منتهي | جرّب refresh ثم أعد؛ وإلا logout |
| `402` | تجاوز حد الباقة / ميزة غير مشمولة | شاشة الترقية |
| `403` | لا يملك الصلاحية | أخفِ/عطّل العنصر |
| `404` | غير موجود | — |
| `409` | تعارض (اسم مكرر...) | اعرض الرسالة |

---

## 7) مرجع الـ Enums (قيم رقمية)

```
TenantSubscriptionStatus:  PendingPayment=1, Trial=2, Active=3, PastDue=4, Suspended=5, Cancelled=6, Expired=7
PaymentRequestStatus:      Pending=1, Approved=2, Rejected=3, Cancelled=4, Expired=5
SubscriptionInvoiceStatus: Unpaid=1, PendingReview=2, Paid=3, Cancelled=4, Overdue=5
BillingCycle:              Monthly=1, Quarterly=2, Annual=3
TenantStatus (الجيم):      Active=1, Suspended=2, Trial=3, PastDue=4, Cancelled=5, PendingApproval=6, Archived=7, Deleted=8
UserRole:                  Owner=1, Coach=2, Client=3, Manager=4, Receptionist=5, Accountant=6, Trainer=7
FeatureCodes:              POS, Inventory, AdvancedReports, MultiBranch, WhiteLabel, EmployeeManagement, FinanceModule, ClientMobileApp, CustomDomain
```

---

## 8) ملاحظات تنفيذية للفرونت (Checklist)

- [ ] عند فتح التطبيق: `GET /api/branding/{subdomain}` → طبّق الثيم (اختياري).
- [ ] تسجيل الدخول/التسجيل يمرّر **`subdomain`** الجيم (مش محتاج TenantId GUID). خزّن `accessToken` + `refreshToken` + `permissions[]`.
- [ ] أرسل `Authorization: Bearer <token>` في كل طلب محمي.
- [ ] فعّل **auto-refresh** عند `401` (التوكن 15 دقيقة).
- [ ] ابنِ الـ Navigation/الأزرار حسب `permissions[]` (خريطة القسم 2.2).
- [ ] عالج `402` بشاشة ترقية، و`403` بإخفاء العنصر.
- [ ] شاشة اشتراك الجيم (Owner فقط): plans → select → payment-methods → رفع الإثبات → متابعة الحالة.
- [ ] بديل الـ subdomain للتطوير: أرسل هيدر `X-Tenant-Id: <GUID>` لتحديد الجيم في الطلبات غير المسجّلة.

---

## 9) مستثنى من هذا المستند (منفصل)

**Platform Admin Dashboard** (لوحة تحكم المنصة — سوبر أدمن): إدارة الجيمات، الموافقة/رفض المدفوعات، إدارة الباقات والميزات وطرق الدفع، تقارير المنصة. موجودة على **Platform API** (`logicfit-platform.runasp.net`) وتُوثّق في مستند منفصل حسب طلبك.
