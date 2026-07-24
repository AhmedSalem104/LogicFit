<div align="center">

# LogicFit — Platform Admin Dashboard
### دليل بناء لوحة تحكم المنصة (Frontend Brief)

</div>

> ملف **مستقل وكامل** لبناء **Platform Admin Dashboard** (لوحة السوبر أدمن / مشغّل المنصة) كمشروع فرونت إند منفصل.
> يغطّي: المصادقة، الشاشات، **كل الـ endpoints بالـ request/response**، الـ workflows، الأدوار، الأخطاء، واقتراح البنية.
> كل الـ JSON بصيغة **camelCase**، التواريخ **UTC (ISO 8601)**، والمصادقة **JWT Bearer**.

---

## جدول المحتويات
1. [ما هو الـ Platform Dashboard؟](#1-ما-هو-الـ-platform-dashboard)
2. [الاتصال والمصادقة](#2-الاتصال-والمصادقة)
3. [الأدوار والصلاحيات](#3-الأدوار-والصلاحيات)
4. [خريطة الشاشات (Navigation)](#4-خريطة-الشاشات)
5. [الشاشة 1: Overview / Dashboard](#5-الشاشة-1-overview--dashboard)
6. [الشاشة 2: Tenants / Gyms](#6-الشاشة-2-tenants--gyms)
7. [الشاشة 3: Plans (الباقات)](#7-الشاشة-3-plans-الباقات)
8. [الشاشة 4: Features (الميزات)](#8-الشاشة-4-features-الميزات)
9. [الشاشة 5: Payment Methods](#9-الشاشة-5-payment-methods)
10. [الشاشة 6: Payment Requests (المراجعة)](#10-الشاشة-6-payment-requests-المراجعة)
11. [الشاشة 7: Subscriptions](#11-الشاشة-7-subscriptions)
12. [الـ Workflows الرئيسية](#12-الـ-workflows-الرئيسية)
13. [مرجع الـ Enums](#13-مرجع-الـ-enums)
14. [معالجة الأخطاء](#14-معالجة-الأخطاء)
15. [اقتراح البنية والتقنيات + Checklist](#15-اقتراح-البنية-والتقنيات--checklist)

---

## 1. ما هو الـ Platform Dashboard؟

LogicFit منصة **SaaS متعددة الجيمات**. الـ **Platform Admin Dashboard** هو لوحة **مشغّل المنصة** (إنت/فريقك) لإدارة كل الجيمات المشتركة — **منفصل تماماً** عن تطبيق الجيم نفسه.

**مسؤولياته**:
- إنشاء الجيمات (Tenants) وحساب صاحب كل جيم.
- إدارة دورة حياة الجيم: موافقة · تفعيل · إيقاف · أرشفة.
- إدارة **الباقات (Plans)** و**الميزات (Features)**.
- إدارة **طرق الدفع اليدوية** (بنك / انستاباي / محفظة...).
- **مراجعة إثباتات الدفع** والموافقة/الرفض → تفعيل اشتراك الجيم.
- متابعة اشتراكات الجيمات وإحصائيات المنصة.

> **مهم**: الـ Platform API له **Base URL منفصل** ويستخدم مصادقة مستقلة (email + password، بدون subdomain). لا علاقة له بتطبيق الجيم.

---

## 2. الاتصال والمصادقة

### Base URL
```
Production:  https://logicfit-platform.runasp.net
```
كل المسارات تحت `https://logicfit-platform.runasp.net/api/platform/...`. (الـ placeholder `{{PLATFORM_API}}` في هذا الملف = هذا الـ URL.)

> ⚠️ **CORS**: الأصول (origins) مغلقة حالياً في الـ Production — لازم يتضاف دومين لوحة التحكم (وحتى `http://localhost:<port>` وقت التطوير) في `Cors.AllowedOrigins` على السيرفر قبل ما المتصفح يقدر يكلّم الـ API.

### هيدر المصادقة
كل طلب محمي:
```
Authorization: Bearer <accessToken>
Content-Type: application/json
```

### تسجيل الدخول
`POST /api/platform/auth/login` — **عام (Anonymous)**
```json
// Request
{ "email": "owner@platform.local", "password": "ChangeMe#12345" }
```
```json
// Response — AuthResponseDto
{
  "userId": "guid",
  "email": "owner@platform.local",
  "phoneNumber": null,
  "fullName": "Platform Owner",
  "role": "PlatformOwner",
  "roles": ["PlatformOwner"],
  "permissions": ["ManagePlatform","ManageTenants","ManagePlans","ManagePaymentRequests","ManagePlatformReports"],
  "tenantId": "00000000-0000-0000-0000-0000000000a1",
  "accessToken": "eyJhbGci...",
  "refreshToken": "def502...",
  "expiresAt": "2026-07-08T12:15:00Z"
}
```
> **بيانات الدخول الافتراضية** (بعد أول تشغيل للنظام): `owner@platform.local` / `ChangeMe#12345` — **يُفضّل تغييرها**.
> **مفيش subdomain** — مستخدمو المنصة فوق كل الجيمات.

### تجديد التوكن
- **`accessToken` عمره 15 دقيقة**. عند `401` → نادِ `refresh` مرة واحدة ثم أعِد الطلب.
```json
// POST /api/platform/auth/refresh   (Anonymous)
{ "refreshToken": "def502..." }        // → نفس AuthResponseDto بتوكنات جديدة (القديم يُبطَل — Rotation)
```
- **`POST /api/platform/auth/logout-all`** — Authenticated → `204` (إبطال كل الجلسات).

> خزّن `accessToken` + `refreshToken` + `permissions[]`. ابنِ الواجهة حسب `permissions[]`.

---

## 3. الأدوار والصلاحيات

مستخدمو المنصة نوعان — الواجهة تُبنى حسب `permissions[]` في التوكن:

| الدور | الصلاحيات | ماذا يرى؟ |
|-------|-----------|-----------|
| **PlatformOwner** | `ManagePlatform` (god-mode) + الكل | كل الشاشات |
| **PlatformAdmin** | `ManageTenants, ManagePlans, ManagePaymentRequests, ManagePlatformReports` | كل الشاشات التشغيلية |

**خريطة الصلاحية لكل شاشة**:
| الشاشة | الصلاحية المطلوبة |
|--------|-------------------|
| Dashboard | `ManagePlatformReports` |
| Tenants / Subscriptions | `ManageTenants` |
| Plans / Features | `ManagePlans` |
| Payment Methods / Payment Requests | `ManagePaymentRequests` |

> استدعاء endpoint بلا الصلاحية → **`403`** → أخفِ/عطّل العنصر.

---

## 4. خريطة الشاشات

```
Platform Dashboard
│
├── 📊 Overview            → GET /api/platform/dashboard
│
├── 🏢 Tenants / Gyms      → list · create · approve/suspend/activate/archive
│     ├── All / filter by status
│     └── Create Gym (+ Owner)
│
├── 💳 SaaS Billing
│     ├── Plans            → CRUD
│     ├── Features         → list
│     ├── Payment Methods  → CRUD
│     ├── Payment Requests → review · approve · reject
│     └── Subscriptions    → list
│
└── (Settings — لاحقاً)
```

---

## 5. الشاشة 1: Overview / Dashboard

**الصلاحية**: `ManagePlatformReports`

**`GET /api/platform/dashboard`**
```json
// Response — PlatformDashboardDto
{
  "totalGyms": 42,
  "activeGyms": 30,
  "trialGyms": 5,
  "pendingApprovalGyms": 3,
  "suspendedGyms": 4,
  "totalMembers": 8600
}
```
**اقتراح العرض**: 6 بطاقات إحصائية (KPI cards) + إبراز `pendingApprovalGyms` كتنبيه (جيمات تنتظر الموافقة).

---

## 6. الشاشة 2: Tenants / Gyms

**الصلاحية**: `ManageTenants` · Base: `/api/platform/tenants`

### قائمة الجيمات
**`GET /api/platform/tenants`** — query اختياري: `status` (TenantStatus enum، مثلاً `6` = PendingApproval)
```json
// Response — List<PlatformTenantDto>
[
  {
    "id": "guid",
    "name": "PowerGym",
    "subdomain": "powergym",
    "status": 1,
    "email": "info@powergym.com",
    "phoneNumber": "0100...",
    "membersCount": 320,
    "createdAt": "2026-01-01T00:00:00"
  }
]
```
**العرض**: جدول (name, subdomain, status badge, membersCount, createdAt) + فلتر بالحالة + أزرار الإجراءات.

### إنشاء جيم جديد (+ صاحبه)
**`POST /api/platform/tenants`**
```json
// Request — CreateTenantWithOwnerCommand
{
  "name": "PowerGym",
  "subdomain": "powergym",
  "email": "info@powergym.com",
  "phoneNumber": "0100...",
  "ownerEmail": "owner@powergym.com",
  "ownerPhoneNumber": "01000000000",
  "ownerPassword": "Owner@123",
  "ownerFullName": "Ali Owner"
}
```
```json
// Response — 201 + PlatformTenantDto (status = 6 PendingApproval)
{ "id": "guid", "name": "PowerGym", "subdomain": "powergym", "status": 6,
  "email": "info@powergym.com", "phoneNumber": "0100...", "membersCount": 0, "createdAt": "2026-07-08T..." }
```
> بيتعمل الجيم + حساب الـ Owner (بصلاحياته كاملة). الجيم يبدأ **PendingApproval** — لازم توافق عليه.
> **مهم للفرونت**: اعرض بيانات الـ Owner (`ownerPhoneNumber` + `ownerPassword`) لصاحب الجيم عشان يقدر يدخل تطبيق الجيم لاحقاً.

### إجراءات دورة الحياة (كلها `POST`، ترجّع `PlatformTenantDto` المحدّث)
| الزر | Endpoint | النتيجة |
|------|----------|---------|
| ✅ Approve | `POST /api/platform/tenants/{id}/approve` | status → Active (1) |
| ▶️ Activate | `POST /api/platform/tenants/{id}/activate` | status → Active (1) |
| ⏸️ Suspend | `POST /api/platform/tenants/{id}/suspend` | status → Suspended (2) |
| 🗄️ Archive | `POST /api/platform/tenants/{id}/archive` | status → Archived (7) |

**اقتراح UI**: badge ملوّن للحالة، وأزرار الإجراءات تظهر حسب الحالة (مثلاً "Approve" للـ PendingApproval فقط).

---

## 7. الشاشة 3: Plans (الباقات)

**الصلاحية**: `ManagePlans` · Base: `/api/platform/plans`

### قائمة الباقات
**`GET /api/platform/plans`** — query: `activeOnly` (bool، default false)
```json
// Response — List<PlanDto>
[
  {
    "id": "guid",
    "name": "Pro",
    "description": "Full features",
    "price": 1200.00,
    "currency": "EGP",
    "billingCycle": 1,
    "durationInDays": 30,
    "maxMembers": 1000,
    "maxCoaches": 20,
    "maxBranches": 5,
    "maxEmployees": 50,
    "maxStorageMB": 10240,
    "isActive": true,
    "displayOrder": 1,
    "features": ["reports", "pos", "hr"]
  }
]
```
> `maxX = null` تعني **غير محدود** (باقة Enterprise). `billingCycle`: 1=Monthly, 2=Quarterly, 3=Annual.

### إنشاء باقة
**`POST /api/platform/plans`**
```json
// Request — CreatePlanCommand
{
  "name": "Pro",
  "description": "Full features",
  "price": 1200.00,
  "currency": "EGP",
  "billingCycle": 1,
  "durationInDays": 30,
  "maxMembers": 1000,
  "maxCoaches": 20,
  "maxBranches": 5,
  "maxEmployees": 50,
  "maxStorageMB": 10240,
  "isActive": true,
  "displayOrder": 1,
  "featureCodes": ["POS", "Inventory", "AdvancedReports"]
}
// Response — 201 + PlanDto
```
> `featureCodes[]` = أكواد الميزات (من شاشة Features). للـ "غير محدود" أرسل `null` في `maxX`.

### تعديل / حذف
- **`PUT /api/platform/plans/{id}`** — `UpdatePlanCommand` (نفس حقول الإنشاء + `id`) → `PlanDto`
- **`DELETE /api/platform/plans/{id}`** → `204` — **يمنع حذف باقة عليها اشتراكات فعّالة** (يرجّع `409` — اعرض الرسالة واقترح التعطيل بدل الحذف).

**اقتراح UI**: جدول الباقات + form للحدود + multi-select للـ features + toggle `isActive`.

---

## 8. الشاشة 4: Features (الميزات)

**الصلاحية**: `ManagePlans` · Base: `/api/platform/features`

**`GET /api/platform/features`**
```json
// Response — List<FeatureDto>
[
  { "id": "guid", "code": "POS", "name": "Point of Sale", "description": "...", "isActive": true }
]
```
**الأكواد المتاحة** (مزروعة): `POS`, `Inventory`, `AdvancedReports`, `MultiBranch`, `WhiteLabel`, `EmployeeManagement`, `FinanceModule`, `ClientMobileApp`, `CustomDomain`.

**الاستخدام**: تُعرض كـ multi-select عند إنشاء/تعديل باقة (شاشة Plans).

---

## 9. الشاشة 5: Payment Methods

**الصلاحية**: `ManagePaymentRequests` · Base: `/api/platform/payment-methods`

طرق الدفع اليدوية اللي الجيمات هتدفع من خلالها (بيشوفها صاحب الجيم في تطبيقه).

### قائمة
**`GET /api/platform/payment-methods`** — query: `activeOnly` (bool)
```json
// Response — List<PaymentMethodDto>
[
  {
    "id": "guid",
    "name": "InstaPay",
    "type": "Wallet",
    "accountName": "LogicFit",
    "accountNumber": null,
    "iban": null,
    "walletNumber": "0100...",
    "instructions": "حوّل للمحفظة ثم ارفع الإيصال",
    "qrImageUrl": "https://.../qr.png",
    "isActive": true,
    "displayOrder": 1
  }
]
```
### إنشاء / تعديل / حذف
```json
// POST /api/platform/payment-methods   — SavePaymentMethodCommand
{ "name": "InstaPay", "type": "Wallet", "accountName": "LogicFit", "accountNumber": null, "iban": null,
  "walletNumber": "0100...", "instructions": "حوّل ثم ارفع الإيصال", "qrImageUrl": "https://.../qr.png", "isActive": true, "displayOrder": 1 }
// Response — 201 + PaymentMethodDto
```
- **`PUT /api/platform/payment-methods/{id}`** → `PaymentMethodDto`
- **`DELETE /api/platform/payment-methods/{id}`** → `204`

---

## 10. الشاشة 6: Payment Requests (المراجعة)

**الصلاحية**: `ManagePaymentRequests` · Base: `/api/platform/payment-requests`

**أهم شاشة تشغيلية** — مراجعة إثباتات الدفع من الجيمات والموافقة/الرفض.

### قائمة الطلبات
**`GET /api/platform/payment-requests`** — query: `status` (PaymentRequestStatus، مثلاً `1` = Pending)
```json
// Response — List<PaymentRequestDto>
[
  {
    "id": "guid",
    "tenantId": "guid",
    "tenantName": "PowerGym",
    "planId": "guid",
    "planName": "Pro",
    "tenantSubscriptionId": "guid",
    "amount": 1200.00,
    "currency": "EGP",
    "paymentMethodId": "guid",
    "transactionNumber": "TX123",
    "paymentDate": "2026-07-01T00:00:00",
    "proofFileUrl": "https://.../proof.png",
    "notes": null,
    "status": 1,
    "reviewedBy": null,
    "reviewedAt": null,
    "rejectReason": null,
    "createdAt": "2026-07-01T09:00:00"
  }
]
```
**العرض**: جدول (tenantName, planName, amount, paymentDate, transactionNumber, **معاينة `proofFileUrl`**, status badge) + فلتر افتراضي على `status=1` (Pending).

### الموافقة
**`POST /api/platform/payment-requests/{id}/approve`** → `PaymentRequestDto` (status → Approved)
> يفعّل اشتراك الجيم تلقائياً (تمديد المدة + الجيم → Active + إنشاء دفعة وفاتورة مدفوعة + إشعار للمالك). **عملية ذرّية** — آمنة ضد النقر المزدوج.

### الرفض
**`POST /api/platform/payment-requests/{id}/reject`**
```json
// Request — RejectPaymentRequestCommand
{ "rejectReason": "الصورة غير واضحة / المبلغ غير صحيح" }
// Response — PaymentRequestDto (status → Rejected)
```
> الاشتراك يبقى `PendingPayment` (الجيم يقدر يعيد الرفع)، ويوصل إشعار للمالك بالسبب.

**اقتراح UI**: modal معاينة للإيصال (`proofFileUrl`) + زر Approve أخضر + زر Reject (يفتح حقل السبب).

---

## 11. الشاشة 7: Subscriptions

**الصلاحية**: `ManageTenants` · Base: `/api/platform/subscriptions`

**`GET /api/platform/subscriptions`** — query: `status` (TenantSubscriptionStatus)
```json
// Response — List<PlatformSubscriptionDto>
[
  {
    "id": "guid",
    "tenantId": "guid",
    "tenantName": "PowerGym",
    "planId": "guid",
    "planName": "Pro",
    "status": 3,
    "startDate": "2026-01-01T00:00:00",
    "endDate": "2026-02-01T00:00:00",
    "trialEndsAt": null,
    "amount": 1200.00,
    "currency": "EGP",
    "autoRenew": true
  }
]
```
**العرض**: جدول اشتراكات الجيمات + فلتر بالحالة + إبراز المنتهية/المتأخرة (PastDue=4, Expired=7).

---

## 12. الـ Workflows الرئيسية

### أ) إضافة جيم جديد (Onboarding)
```
1) POST /api/platform/tenants  { name, subdomain, ownerEmail, ownerPhoneNumber, ownerPassword, ownerFullName }
       → الجيم يُنشأ (PendingApproval) + حساب Owner
2) [بلّغ صاحب الجيم ببياناته: subdomain + phone + password → يدخل تطبيق الجيم]
3) POST /api/platform/tenants/{id}/approve   → الجيم Active
   (أو: يفضل PendingApproval لحد ما يدفع أول اشتراك)
```

### ب) مراجعة والموافقة على دفعة
```
1) GET /api/platform/payment-requests?status=1   → طلبات Pending
2) افتح الطلب → عايِن proofFileUrl + راجِع amount/transactionNumber
3) POST /api/platform/payment-requests/{id}/approve   → تفعيل تلقائي للاشتراك + الجيم Active
   أو  POST /{id}/reject { rejectReason }             → رفض بالسبب
```

### ج) إدارة الباقات والميزات
```
1) GET /api/platform/features            → الأكواد المتاحة
2) POST/PUT /api/platform/plans          → أنشئ/عدّل باقة (اربط featureCodes[] + الحدود)
3) DELETE /api/platform/plans/{id}       → حذف (لو مفيش اشتراكات فعّالة، وإلا 409 → عطّلها)
```

### د) إيقاف / تفعيل / أرشفة جيم
```
POST /api/platform/tenants/{id}/suspend   → إيقاف (الجيم مايقدرش يستخدم النظام)
POST /api/platform/tenants/{id}/activate  → إعادة تفعيل
POST /api/platform/tenants/{id}/archive   → أرشفة
```

---

## 13. مرجع الـ Enums

```
TenantStatus:              Active=1, Suspended=2, Trial=3, PastDue=4, Cancelled=5, PendingApproval=6, Archived=7, Deleted=8
TenantSubscriptionStatus:  PendingPayment=1, Trial=2, Active=3, PastDue=4, Suspended=5, Cancelled=6, Expired=7
PaymentRequestStatus:      Pending=1, Approved=2, Rejected=3, Cancelled=4, Expired=5
SubscriptionInvoiceStatus: Unpaid=1, PendingReview=2, Paid=3, Cancelled=4, Overdue=5
BillingCycle:              Monthly=1, Quarterly=2, Annual=3
```
> اعرض القيم كـ badges ملوّنة (مثلاً: Active أخضر، Suspended أحمر، PendingApproval/PendingPayment أصفر، Archived رمادي).

---

## 14. معالجة الأخطاء

كل الأخطاء بنفس الشكل:
```json
{ "statusCode": 400, "message": "Validation failed", "errors": { "field": ["message"] } }
```
`errors` تظهر فقط في أخطاء التحقق (400).

| Status | المعنى | التعامل |
|--------|--------|---------|
| `400` | تحقق فاشل | اعرض `errors` لكل حقل |
| `401` | توكن منتهي/غير صالح | refresh مرة ثم أعد؛ وإلا logout |
| `403` | لا يملك الصلاحية | أخفِ/عطّل العنصر |
| `404` | غير موجود | — |
| `409` | تعارض (مثلاً حذف باقة عليها اشتراكات / subdomain مكرر) | اعرض الرسالة |

---

## 15. اقتراح البنية والتقنيات + Checklist

### بنية مقترحة
- **Layout**: sidebar (Overview · Tenants · Plans · Features · Payment Methods · Payment Requests · Subscriptions) + top bar (اسم المستخدم + logout).
- **Auth guard**: صفحة login مستقلة؛ خزّن التوكن؛ redirect للـ Dashboard بعد الدخول.
- **Interceptor**: أضِف `Authorization` تلقائياً؛ عند `401` جرّب refresh ثم أعِد الطلب.
- **State**: احفظ `permissions[]` وأظهر عناصر الـ nav حسبها.
- **تقنيات مقترحة** (اختيارية): React/Next.js أو Angular/Vue + مكتبة UI (MUI/Ant/Tailwind) + React Query/axios.

### Checklist
- [ ] صفحة Login (email + password) → `POST /api/platform/auth/login`.
- [ ] تخزين `accessToken` + `refreshToken` + `permissions[]` + auto-refresh عند 401.
- [ ] Dashboard (6 KPIs) → `GET /dashboard`.
- [ ] Tenants: جدول + فلتر بالحالة + إنشاء جيم + أزرار approve/suspend/activate/archive.
- [ ] Plans: جدول + form (حدود + features) + create/update/delete (تعامل مع 409).
- [ ] Features: عرض القائمة (تُستخدم في form الباقات).
- [ ] Payment Methods: CRUD.
- [ ] **Payment Requests**: قائمة + معاينة الإيصال + approve/reject (بسبب).
- [ ] Subscriptions: قائمة + فلتر بالحالة.
- [ ] badges ملوّنة للحالات + معالجة موحّدة للأخطاء.
- [ ] إخفاء العناصر حسب `permissions[]`.

---

<div align="center"><sub>LogicFit — Platform Admin Dashboard · دليل فرونت مستقل وكامل</sub></div>
