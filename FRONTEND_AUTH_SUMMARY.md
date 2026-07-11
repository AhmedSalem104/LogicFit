# LogicFit — ملخص التسجيل والدخول لكل المستخدمين (للفرونت)

> ملخص مركّز يوضّح **إزاي كل نوع مستخدم بيتعمل (Registration) وبيسجّل دخول (Login)** بالتفصيل.
> للتفاصيل الكاملة للـ endpoints شوف [FRONTEND_SAAS_INTEGRATION.md](FRONTEND_SAAS_INTEGRATION.md).

---

## القاعدة الأساسية

- **API-ين**: `Tenant API` (تطبيق الجيم) و `Platform API` (لوحة المنصة — منفصلة).
- **تحديد الجيم بالـ `subdomain`** (اللي في اللينك) — **مش محتاج TenantId GUID**.
- كل الردود فيها `accessToken` + `refreshToken` + **`roles[]`** + **`permissions[]`** → ابنِ الواجهة حسب `permissions[]`.
- **مبدأ مهم**: مفيش تسجيل ذاتي إلا للعميل (Client). باقي الأنواع **بيتعملوا من مستخدم أعلى منهم**، وبعدين بيسجّلوا دخول عادي.

---

## جدول سريع: مين بيعمل مين، وبيسجّل دخول إزاي

| نوع المستخدم | مين بيعمله؟ | Endpoint الإنشاء | بيسجّل دخول فين؟ |
|--------------|-------------|-------------------|-------------------|
| **Client (عميل)** | نفسه (تسجيل ذاتي) **أو** الريسبشن/المدرب | `POST /api/auth/register` **أو** `POST /api/clients` | Tenant API |
| **Coach (مدرب)** | Owner / Manager | `POST /api/coaches` | Tenant API |
| **Manager / Receptionist / Accountant / Trainer** | Owner / Manager | `POST /api/users/staff` | Tenant API |
| **Owner (صاحب الجيم)** | Platform Admin (وقت إنشاء الجيم) | `POST /api/platform/tenants` | Tenant API |
| **Platform Owner / Admin** | مزروع / أدمن منصة آخر | (Seeding) | Platform API |

> **كل الأنواع في الجيم** (Owner لحد Client) بيسجّلوا دخول بنفس الـ endpoint: `POST /api/auth/login` بالـ **phone + password + subdomain**.
> **مستخدمو المنصة** بس بيسجّلوا في `POST /api/platform/auth/login` بـ **email + password** (بدون جيم).

---

## تفاصيل كل نوع

### 1) العميل (Client) — الوحيد اللي عنده تسجيل ذاتي

**طريقة (أ) — تسجيل ذاتي** (شاشة "إنشاء حساب" في تطبيق الجيم):
```http
POST /api/auth/register
{ "fullName": "سارة", "phoneNumber": "01000000000",
  "email": "sara@mail.com", "password": "P@ssw0rd", "confirmPassword": "P@ssw0rd",
  "subdomain": "goldgym" }
```
→ يُنشأ **Client** فوراً، والرد فيه توكن جاهز (دخول مباشر). **الدور ثابت Client دايماً** — العميل مايقدرش يختار دور.

**طريقة (ب) — الريسبشن/المدرب يضيفه** (من داخل التطبيق، صلاحية `ManageMembers`):
```http
POST /api/clients
{ "phoneNumber": "0100...", "fullName": "سارة", "password": "P@ssw0rd", "coachId": null }
```
→ يُنشأ Client مربوط اختيارياً بمدرب. **مرّر `password`** عشان العميل يقدر يدخل (لو سِبته فاضي بيتولّد تلقائياً ومش بيترجّع).

**الدخول**: `POST /api/auth/login` بـ `phoneNumber + password + subdomain`.

---

### 2) المدرب (Coach) — يعمله Owner/Manager

```http
POST /api/coaches            (صلاحية ManageCoaches)
{ "phoneNumber": "0101...", "fullName": "كابتن أحمد", "email": "coach@mail.com", "password": "P@ssw0rd" }
```
→ يُنشأ **Coach** + يتعيّن له دور RBAC "Coach" (صلاحياته: `ViewMembers`, `ManageAttendance`, `ViewReports`).
**الدخول**: نفس شاشة الدخول (`phone + password + subdomain`).

---

### 3) الموظفون الإداريون (Manager / Receptionist / Accountant / Trainer) — يعملهم Owner/Manager

```http
POST /api/users/staff        (صلاحية ManageSettings)
{ "phoneNumber": "0102...", "fullName": "منى", "email": "mona@mail.com",
  "password": "P@ssw0rd", "role": 5 }
```
`role` بالقيمة الرقمية: **Manager=4, Receptionist=5, Accountant=6, Trainer=7** (بس دول مسموحين هنا).
→ يُنشأ المستخدم + يتعيّن له دور RBAC المناسب بصلاحياته.
**الدخول**: نفس شاشة الدخول.

---

### 4) صاحب الجيم (Owner) — يعمله Platform Admin وقت إنشاء الجيم

الجيم + المالك بيتعملوا في **نداء واحد** من لوحة المنصة:
```http
POST /api/platform/tenants   (Platform API — صلاحية ManageTenants)
{ "name": "Gold Gym", "subdomain": "goldgym", "email": "info@goldgym.com",
  "ownerEmail": "owner@goldgym.com", "ownerPhoneNumber": "0100...",
  "ownerPassword": "P@ssw0rd", "ownerFullName": "أحمد" }
```
→ يُنشأ الجيم (حالة `PendingApproval`) + حساب **Owner** بصلاحيات الجيم الكاملة.
**دخول الـ Owner**: على **Tenant API** العادي: `POST /api/auth/login` بـ `ownerPhoneNumber + ownerPassword + subdomain`.

---

### 5) مستخدمو المنصة (Platform Owner / Admin)

- **Platform Owner** بيتزرع تلقائياً أول تشغيل: `owner@platform.local` / `ChangeMe#12345` (**غيّره فوراً**).
- **الدخول**: على **Platform API**:
```http
POST /api/platform/auth/login
{ "email": "owner@platform.local", "password": "..." }
```
→ توكن بصلاحيات المنصة (`ManagePlatform`, `ManageTenants`, ...). **مفيش subdomain** — دول فوق الجيمات كلها.

---

## ملاحظات مهمة للفرونت

1. **كلمات المرور للمستخدمين المُنشَأين**: لما تنشئ Coach/Staff/Client من داخل التطبيق، **مرّر `password`** وبلّغه للمستخدم. لو سِبته فاضي بيتولّد تلقائياً لكن **مش بيترجّع** — فالمستخدم مش هيعرفه (يقدر ساعتها يستخدم "نسيت كلمة المرور").
2. **نسيت كلمة المرور**: `POST /api/auth/forget-password` بـ `phoneNumber + subdomain` → يرجّع `resetToken` (SMS في الإنتاج) → `POST /api/auth/reset-password`.
3. **بعد أي إنشاء**: المستخدم الجديد بيسجّل دخول عادي — التوكن بتاعه هيرجّع صلاحياته الصح تلقائياً (كل نوع بياخد دوره وقت الإنشاء).
4. **الشاشات المطلوبة**:
   - شاشة دخول موحّدة للجيم (phone + password، والـ subdomain من اللينك).
   - شاشة تسجيل ذاتي للعميل.
   - شاشات إدارة (Owner/Manager): إضافة مدرب / موظف / عميل.
   - شاشة دخول منفصلة للمنصة (email + password).
5. **التوكن 15 دقيقة** → فعّل الـ auto-refresh عند `401`.

---

## مرجع أدوار المستخدم (UserRole) بالقيم الرقمية
```
Owner=1, Coach=2, Client=3, Manager=4, Receptionist=5, Accountant=6, Trainer=7
(PlatformOwner=8, PlatformAdmin=9 — على المنصة فقط)
```
