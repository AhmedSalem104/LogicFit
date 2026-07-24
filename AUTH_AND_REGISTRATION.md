<div align="center">

# LogicFit — دليل المصادقة والتسجيل الكامل

### Authentication & Registration — Full Flow Reference

</div>

> مرجع كامل لكل ما يخص **الدخول والتسجيل وإنشاء المستخدمين** في منصة LogicFit — لكل نوع مستخدم، مع الـ requests والـ responses حرفياً.
> كل الـ JSON بصيغة **camelCase**، وكل التواريخ **UTC (ISO 8601)**.

---

## جدول المحتويات
1. [المعمارية: API-ان منفصلان](#1-المعمارية-apiان-منفصلان)
2. [السيناريو الكامل من الصفر](#2-السيناريو-الكامل-من-الصفر)
3. [تحديد الجيم بالـ subdomain](#3-تحديد-الجيم-بالـ-subdomain)
4. [إنشاء وتسجيل كل نوع مستخدم](#4-إنشاء-وتسجيل-كل-نوع-مستخدم)
5. [مرجع كل endpoints المصادقة](#5-مرجع-كل-endpoints-المصادقة)
6. [الصلاحيات RBAC وبناء الواجهة](#6-الصلاحيات-rbac-وبناء-الواجهة)
7. [التوكنات والتجديد](#7-التوكنات-والتجديد)
8. [الأخطاء](#8-الأخطاء)
9. [Checklist للفرونت](#9-checklist-للفرونت)

---

## 1) المعمارية: API-ان منفصلان

النظام مكوّن من **API-ين** يشاركان **نفس قاعدة البيانات** لكن معزولين بالـ JWT audience:

| الـ API | لمين؟ | أمثلة endpoints | Base URL (Production) |
|---------|-------|------------------|----------|
| **Platform API** | السوبر أدمن (صاحب المنصة) | إنشاء الجيمات، الباقات، مراجعة المدفوعات | `https://logicfit-platform.runasp.net` |
| **Tenant API** | تطبيق الجيم (Owner/Coach/Client...) | login, register, branding, clients, workouts | `https://logicfit.runasp.net` |

> **الـ placeholders في هذا الملف**: `{{PLATFORM_API}}` = `https://logicfit-platform.runasp.net` · `{{TENANT_API}}` = `https://logicfit.runasp.net`. اضبطهم كمتغيرات بيئة في الفرونت.

> **قاعدة ذهبية**: `login`، `register`، `branding`، وكل شاشات الجيم على **Tenant API**. إنشاء الجيمات والموافقة على المدفوعات على **Platform API**. توكن جهة لا يعمل على الجهة الأخرى (audience مختلف).

> ⚠️ **CORS**: الأصول (origins) مغلقة حالياً في الـ Production على الهوستين. أي فرونت ويب لازم يتضاف دومينه في `Cors.AllowedOrigins` قبل ما يشتغل من المتصفح (تطبيقات الموبايل غير متأثرة).

المصادقة لكل الطلبات المحمية:
```
Authorization: Bearer <accessToken>
```

---

## 2) السيناريو الكامل من الصفر

```
[1] السوبر أدمن يدخل المنصة
    POST {{PLATFORM_API}}/api/platform/auth/login   { email, password }
        │
        ▼
[2] ينشئ جيم جديد + حساب صاحبه (Owner) في نداء واحد
    POST {{PLATFORM_API}}/api/platform/tenants
        │   → الجيم (حالة PendingApproval) + الـ Owner بصلاحياته كاملة
        ▼
[3] الـ Owner يدخل تطبيق الجيم
    POST {{TENANT_API}}/api/auth/login   { phoneNumber, password, subdomain }
        │   → توكن فيه permissions[] لكل صلاحيات الجيم
        ▼
[4] الـ Owner يضيف فريق العمل من داخل التطبيق:
    • مدرب:   POST {{TENANT_API}}/api/coaches
    • موظف:   POST {{TENANT_API}}/api/users/staff   (Manager/Receptionist/Accountant/Trainer)
    • عميل:   POST {{TENANT_API}}/api/clients
        │
        ▼
[5] العميل يقدر يسجّل نفسه ذاتياً
    POST {{TENANT_API}}/api/auth/register   { fullName, phoneNumber, password, subdomain }
        │
        ▼
[6] الجيم يدير اشتراكه في المنصة (فوترة يدوية)
    plans → select-plan → payment-methods → رفع إثبات دفع → موافقة الأدمن
```

**الفكرة**: الجيم + الـ Owner بيتولدوا من **المنصة** (خطوة 2). كل الباقي على **تطبيق الجيم**.

---

## 3) تحديد الجيم بالـ subdomain

كل مستخدم في جيم ينتمي لـ Tenant. لتحديد الجيم في الدخول/التسجيل **أرسل `subdomain` بتاع الجيم** (اللي في اللينك، مثل `goldgym`) — الـ API يحلّه تلقائياً. **مش محتاج TenantId GUID**.

بدائل مقبولة (اختياري): تمرير `tenantId` (GUID) في الجسم، أو هيدر `X-Tenant-Id: <GUID>`. **الأبسط دايماً**: `subdomain`.

> مستخدمو **المنصة** لا يحتاجون subdomain — هم فوق كل الجيمات، ويسجّلون دخول بالإيميل + كلمة السر.

---

## 4) إنشاء وتسجيل كل نوع مستخدم

جدول سريع:

| المستخدم | مين بيعمله | Endpoint الإنشاء | الدخول |
|----------|------------|------------------|--------|
| **Platform Owner / Admin** | مزروع / أدمن منصة آخر | (Seeding) | `POST /api/platform/auth/login` (email+password) |
| **Owner** (صاحب الجيم) | Platform Admin | `POST /api/platform/tenants` | `POST /api/auth/login` (phone+password+subdomain) |
| **Manager / Receptionist / Accountant / Trainer** | Owner / Manager | `POST /api/users/staff` | نفس دخول الجيم |
| **Coach** | Owner / Manager | `POST /api/coaches` | نفس دخول الجيم |
| **Client** | نفسه **أو** الريسبشن | `POST /api/auth/register` **أو** `POST /api/clients` | نفس دخول الجيم |

> **مبدأ**: العميل فقط هو من يسجّل نفسه ذاتياً. باقي الأنواع يُنشَأون من مستخدم أعلى. كل مستخدم جديد يحصل على **دور RBAC تلقائياً** وقت إنشائه، فتظهر صلاحياته في التوكن مباشرة عند أول دخول.

### 4.1 Platform Owner / Admin
- **الإنشاء**: الـ PlatformOwner **مزروع تلقائياً** أول تشغيل: `owner@platform.local` / `ChangeMe#12345` (**غيّره فوراً**).
- **الدخول** (Platform API):
```http
POST {{PLATFORM_API}}/api/platform/auth/login
Content-Type: application/json

{ "email": "owner@platform.local", "password": "ChangeMe#12345" }
```
**Response** — نفس شكل `AuthResponseDto` لكن **بدون tenantId** وبصلاحيات المنصة:
```json
{
  "userId": "GUID", "email": "owner@platform.local", "phoneNumber": null, "fullName": "Platform Owner",
  "role": "PlatformOwner", "roles": ["PlatformOwner"],
  "permissions": ["ManagePlatform","ManageTenants","ManagePlans","ManagePaymentRequests","ManagePlatformReports"],
  "tenantId": "00000000-0000-0000-0000-0000000000a1",
  "accessToken": "JWT...", "refreshToken": "opaque...", "expiresAt": "2026-07-08T12:15:00Z"
}
```

### 4.2 Owner (صاحب الجيم) — يُنشأ من المنصة مع الجيم
```http
POST {{PLATFORM_API}}/api/platform/tenants        (Auth: ManageTenants)
Content-Type: application/json

{
  "name": "Gold Gym",
  "subdomain": "goldgym",
  "email": "info@goldgym.com",
  "phoneNumber": "0223456789",
  "ownerEmail": "owner@goldgym.com",
  "ownerPhoneNumber": "01000000000",
  "ownerPassword": "Owner@123",
  "ownerFullName": "أحمد صالح"
}
```
**Response** (`PlatformTenantDto`) — الجيم يُنشأ بحالة `PendingApproval`:
```json
{ "id": "GUID", "name": "Gold Gym", "subdomain": "goldgym", "status": 6,
  "email": "info@goldgym.com", "phoneNumber": "0223456789", "membersCount": 0,
  "createdAt": "2026-07-08T..." }
```
> `status: 6` = PendingApproval. الأدمن يفعّله بـ `POST /api/platform/tenants/{id}/approve` (أو يُفعّل تلقائياً بعد أول دفعة مقبولة).
> **دخول الـ Owner** بعدها على **Tenant API**: `{ phoneNumber:"01000000000", password:"Owner@123", subdomain:"goldgym" }`.

### 4.3 الموظفون الإداريون (Manager / Receptionist / Accountant / Trainer)
```http
POST {{TENANT_API}}/api/users/staff               (Auth: ManageSettings)
Content-Type: application/json
Authorization: Bearer <owner-token>

{ "phoneNumber": "01011111111", "email": "mona@goldgym.com",
  "password": "Staff@123", "fullName": "منى", "role": 5 }
```
`role` بالقيمة الرقمية — المسموح هنا فقط: **Manager=4, Receptionist=5, Accountant=6, Trainer=7**.
**Response**: `Guid` لمعرّف المستخدم الجديد (`201 Created`).
> يُعيّن له دور RBAC المناسب تلقائياً. الدخول: نفس شاشة دخول الجيم.

### 4.4 Coach (مدرب)
```http
POST {{TENANT_API}}/api/coaches                    (Auth: ManageCoaches)
Content-Type: application/json
Authorization: Bearer <owner-or-manager-token>

{ "phoneNumber": "01022222222", "email": "coach@goldgym.com",
  "password": "Coach@123", "fullName": "كابتن كريم", "gender": 1, "birthDate": "1990-05-01" }
```
**Response**: `Guid` (`201`). يُعيّن دور "Coach" (صلاحياته: `ViewMembers`, `ManageAttendance`, `ViewReports`).

### 4.5 Client (عميل) — طريقتان

**(أ) تسجيل ذاتي** (شاشة إنشاء حساب في التطبيق) — **عام**:
```http
POST {{TENANT_API}}/api/auth/register
Content-Type: application/json

{ "fullName": "سارة", "phoneNumber": "01033333333", "email": "sara@mail.com",
  "password": "Client@123", "confirmPassword": "Client@123", "subdomain": "goldgym" }
```
**Response** (`AuthResponseDto`) — دخول مباشر (توكن جاهز)، الدور **Client دائماً**:
```json
{ "userId":"GUID", "email":"sara@mail.com", "phoneNumber":"01033333333", "fullName":"سارة",
  "role":"Client", "roles":["Client"], "permissions":[], "tenantId":"GUID",
  "accessToken":"JWT...", "refreshToken":"opaque...", "expiresAt":"2026-07-08T12:15:00Z" }
```

**(ب) إنشاء بواسطة الريسبشن/المدرب** — `ManageMembers`:
```http
POST {{TENANT_API}}/api/clients
Content-Type: application/json
Authorization: Bearer <staff-token>

{ "phoneNumber":"01044444444", "fullName":"محمد", "password":"Client@123",
  "email":"m@mail.com", "gender":1, "birthDate":"1995-03-10",
  "heightCm":178, "activityLevel":"moderate", "medicalHistory":null, "coachId":null }
```
**Response**: `Guid` (`201`).
> **مهم**: مرّر `password` عشان العميل يقدر يدخل. لو سِبته فاضي بيتولّد تلقائياً **ومش بيترجّع** (المستخدم مش هيعرفه) — ساعتها يستخدم "نسيت كلمة المرور".

---

## 5) مرجع كل endpoints المصادقة

### Tenant API — `/api/auth`

| Endpoint | Auth | الوصف |
|----------|------|-------|
| `POST /api/auth/login` | Anonymous | دخول مستخدم الجيم |
| `POST /api/auth/register` | Anonymous | تسجيل ذاتي (Client فقط) |
| `POST /api/auth/refresh` | Anonymous | تجديد التوكن (Rotation) |
| `POST /api/auth/logout-all` | Authenticated | إبطال كل الـ refresh tokens |
| `POST /api/auth/forget-password` | Anonymous | طلب كود إعادة تعيين |
| `POST /api/auth/reset-password` | Anonymous | إعادة التعيين بالكود |
| `GET /api/branding/{identifier}` | Anonymous | هوية الجيم (subdomain أو domain) |

**`POST /api/auth/login`**
```json
// Request
{ "phoneNumber": "01000000000", "password": "Owner@123", "subdomain": "goldgym" }
// Response — AuthResponseDto
{ "userId":"GUID","email":"owner@goldgym.com","phoneNumber":"01000000000","fullName":"أحمد",
  "role":"Owner","roles":["Owner"],
  "permissions":["ManageMembers","ViewMembers","ManageCoaches","ManageAttendance","ManageClientSubscriptions","ManagePOS","ManageInventory","ManageEmployees","ManageBranches","ManageFinance","ViewReports","ManageReports","ManageSettings","ManageTenantBilling"],
  "tenantId":"GUID","accessToken":"JWT...","refreshToken":"opaque...","expiresAt":"2026-07-08T12:15:00Z" }
```

**`POST /api/auth/refresh`**
```json
// Request
{ "refreshToken": "opaque-string" }
// Response — نفس AuthResponseDto بتوكنات جديدة (التوكن القديم يُبطَل)
```

**`POST /api/auth/logout-all`** → `204 No Content` (يتطلب توكن).

**`POST /api/auth/forget-password`**
```json
// Request
{ "phoneNumber": "01000000000", "subdomain": "goldgym" }
// Response — ForgetPasswordResponse (resetToken يُرجَّع في التطوير فقط؛ SMS في الإنتاج)
{ "success": true, "message": "Reset code has been sent to your phone number.", "resetToken": "123456" }
```

**`POST /api/auth/reset-password`**
```json
// Request
{ "phoneNumber":"01000000000", "resetToken":"123456", "newPassword":"NewPass@123", "subdomain":"goldgym" }
// Response
{ "success": true, "message": "Password has been reset successfully." }
```

**`GET /api/branding/{identifier}`** — للثيم قبل الدخول:
```json
// GET /api/branding/goldgym
{ "tenantId":"GUID","name":"Gold Gym","subdomain":"goldgym","appName":"Gold Gym App",
  "logoUrl":"https://...","coverImageUrl":"https://...","primaryColor":"#3B82F6","secondaryColor":"#1E40AF",
  "fontFamily":"Cairo","customCss":null,"invoiceLogoUrl":null,"supportPhone":"0100...","supportEmail":"support@goldgym.com" }
```
> يرجّع `404` لو الـ subdomain غير موجود.

### Platform API — `/api/platform/auth`

| Endpoint | Auth | الوصف |
|----------|------|-------|
| `POST /api/platform/auth/login` | Anonymous | دخول مستخدم المنصة (email+password) |
| `POST /api/platform/auth/refresh` | Anonymous | تجديد توكن المنصة |
| `POST /api/platform/auth/logout-all` | Authenticated | إبطال refresh tokens |

الأشكال نفس الـ Tenant API لكن الدخول بـ `{ email, password }` (بلا subdomain)، والتوكن بـ audience المنصة.

---

## 6) الصلاحيات RBAC وبناء الواجهة

التوكن (ورد الدخول) يحمل **`permissions[]`** — **اعرض كل شاشة/زر حسب امتلاك الصلاحية**. أي استدعاء بلا صلاحية → `403`.

**كتالوج صلاحيات الجيم**:
```
ManageMembers, ViewMembers, ManageCoaches, ManageAttendance, ManageClientSubscriptions,
ManagePOS, ManageInventory, ManageEmployees, ManageBranches, ManageFinance,
ViewReports, ManageReports, ManageSettings, ManageTenantBilling
```

**الأدوار وصلاحياتها الافتراضية**:
| الدور | الصلاحيات |
|-------|-----------|
| **Owner** | كل صلاحيات الجيم |
| **Manager** | الكل عدا `ManageSettings` و`ManageTenantBilling` |
| **Receptionist** | `ViewMembers, ManageMembers, ManageAttendance, ManageClientSubscriptions, ManagePOS` |
| **Accountant** | `ManageFinance, ViewReports, ManageReports, ManageTenantBilling` |
| **Coach** | `ViewMembers, ManageAttendance, ViewReports` |
| **Client** | لا صلاحيات back-office (شاشات self-service فقط) |

**خريطة الصلاحية المطلوبة لكل منطقة**:
| المنطقة | الصلاحية |
|---------|----------|
| Clients, MembershipCards | `ManageMembers` |
| Coaches, CoachClients | `ManageCoaches` |
| Employees, Payroll, Shifts, Leaves, Users(staff) | `ManageEmployees` / `ManageSettings` |
| Branches, Rooms, Equipment, Maintenance | `ManageBranches` |
| Attendance, GateAccess | `ManageAttendance` |
| Subscriptions (اشتراكات العملاء) | `ManageClientSubscriptions` |
| Invoices, Payments, Transactions, Expenses, Commissions, Coupons | `ManageFinance` |
| Sales | `ManagePOS` |
| Products, ProductCategories, Stock, Suppliers | `ManageInventory` |
| Reports | `ViewReports` |
| GymProfile, TaxSettings, Users | `ManageSettings` |
| اشتراك الجيم في المنصة (billing) | `ManageTenantBilling` |

> شاشات **self-service** (بلا صلاحية محددة، أي مستخدم مسجّل): Profile, ClientDashboard, Notifications, Chat, Appointments, WorkoutPrograms, WorkoutSessions, DietPlans, BodyMeasurements, Exercises, Foods, Muscles, GroupClasses, ClassSchedules, Challenges.

---

## 7) التوكنات والتجديد

- **`accessToken`** عمره **15 دقيقة** — أرسله في هيدر `Authorization` لكل طلب محمي.
- **`refreshToken`** لتجديد التوكن. **Rotation**: كل استخدام يُصدر توكن جديد ويُبطل القديم — استبدل المخزّن دايماً بالجديد.
- **نمط مقترح**: عند `401` → جرّب `refresh` مرة واحدة ثم أعِد الطلب؛ لو فشل الـ refresh → logout.
- `logout-all` يُبطل كل جلسات المستخدم.
- توكن المنصة وتوكن الجيم **لا يتبادلان** (audience مختلف) — كل واحد لـ API بتاعه.

---

## 8) الأخطاء

كل الأخطاء بنفس الشكل:
```json
{ "statusCode": 400, "message": "Validation failed",
  "errors": { "phoneNumber": ["Invalid phone number format"] } }   // errors فقط في أخطاء التحقق
```
| Status | المعنى | تعامل الفرونت |
|--------|--------|----------------|
| `400` | تحقق فاشل | اعرض `errors` لكل حقل |
| `401` | توكن منتهي/غير صالح | refresh ثم أعد؛ وإلا logout |
| `402` | تجاوز حد الباقة / ميزة غير مشمولة | شاشة الترقية |
| `403` | لا يملك الصلاحية | أخفِ/عطّل العنصر |
| `404` | غير موجود (مثلاً subdomain غلط) | — |
| `409` | تعارض (هاتف/اسم مكرر) | اعرض الرسالة |

---

## 9) Checklist للفرونت

- [ ] عند فتح التطبيق: `GET /api/branding/{subdomain}` → طبّق الثيم (اختياري).
- [ ] الدخول/التسجيل يمرّر **`subdomain`** (مش TenantId GUID).
- [ ] خزّن `accessToken` + `refreshToken` + `permissions[]`.
- [ ] أرسل `Authorization: Bearer <token>` في كل طلب محمي.
- [ ] فعّل **auto-refresh** عند `401` (التوكن 15 دقيقة).
- [ ] ابنِ الـ navigation/الأزرار حسب `permissions[]`.
- [ ] عالج `402` بشاشة ترقية، و`403` بإخفاء العنصر.
- [ ] شاشة دخول موحّدة للجيم + شاشة تسجيل ذاتي للعميل + شاشة دخول منفصلة للمنصة.
- [ ] شاشات إدارة (Owner/Manager): إضافة مدرب (`/api/coaches`) / موظف (`/api/users/staff`) / عميل (`/api/clients`).
- [ ] عند إنشاء مستخدم: مرّر `password` وبلّغه للمستخدم (وإلا يستخدم "نسيت كلمة المرور").

---

## مرجع أدوار المستخدم (UserRole)
```
Owner=1, Coach=2, Client=3, Manager=4, Receptionist=5, Accountant=6, Trainer=7
(PlatformOwner=8, PlatformAdmin=9 — على المنصة فقط)
```

> للمرجع الكامل لكل الـ features والـ endpoints (286 endpoint) شوف **`PROJECT_REFERENCE.md`**.
