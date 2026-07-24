# LogicFit - System Reference (المرجع الكامل)
## منصة SaaS متعددة المستأجرين لإدارة الصالات الرياضية (Gym Management SaaS Platform)

> مرجع شامل بدون اختصار: المعمارية، الأدوار، الكيانات والعلاقات، **كل الـ endpoints (286) بالـ request/response الكامل**، قواعد العمل، رفع الملفات، الهيكل، وملاحظات الفرونت.
> مرجع المصادقة والتسجيل المخصّص: **[AUTH_AND_REGISTRATION.md](AUTH_AND_REGISTRATION.md)**.

---

# جدول المحتويات

1. [نظرة عامة على المشروع](#1-نظرة-عامة-على-المشروع)
2. [الميزات والوحدات](#2-الميزات-والوحدات)
3. [أدوار المستخدمين وصلاحياتهم](#3-أدوار-المستخدمين-وصلاحياتهم)
4. [الكيانات والعلاقات](#4-الكيانات-والعلاقات)
5. [API Endpoints (كامل)](#5-api-endpoints)
6. [Enums Reference](#6-enums-reference)
7. [قواعد العمل Business Logic](#7-قواعد-العمل-business-logic)
8. [رفع الملفات](#8-رفع-الملفات)
9. [هيكل المشروع](#9-هيكل-المشروع)
10. [ملاحظات للـ Frontend Developer](#10-ملاحظات-للـ-frontend-developer)
11. [الخلاصة](#11-الخلاصة)

---

# 1. نظرة عامة على المشروع

## ما هو LogicFit؟

**LogicFit** منصة **SaaS متعددة المستأجرين (Multi-Tenant)** لإدارة الصالات الرياضية، مبنية على **.NET 8 + Clean Architecture + CQRS (MediatR)**. تُقدَّم كـ **API-ين منفصلين** يشاركان نفس قاعدة البيانات:

| الـ API | لمين؟ | Base URL (Production) | JWT Audience |
|---------|-------|----------|--------------|
| **Tenant API** (`LogicFit.API`) | تطبيق الجيم (Owner/Manager/Receptionist/Accountant/Coach/Client) | `https://logicfit.runasp.net` | `LogicFitUsers` |
| **Platform API** (`LogicFit.Platform.API`) | مشغّل المنصة (PlatformOwner/PlatformAdmin) | `https://logicfit-platform.runasp.net` | `LogicFitPlatform` |

> **الـ placeholders**: `{{TENANT_API}}` = `https://logicfit.runasp.net` · `{{PLATFORM_API}}` = `https://logicfit-platform.runasp.net`.

توكن جهة **لا يعمل** على الجهة الأخرى (audience مختلف). الأرقام: **286 endpoint** · 51 controller (Tenant) + 8 (Platform).

> ⚠️ **CORS (Production)**: الأصول مغلقة على الهوستين حالياً — أي فرونت ويب لازم يتضاف دومينه في `Cors.AllowedOrigins` قبل الاستخدام من المتصفح (الموبايل غير متأثر).

## التقنيات المستخدمة

| الفئة | التقنية |
|-------|---------|
| Framework | .NET 8، ASP.NET Core Web API |
| اللغة | C# 12 |
| قاعدة البيانات | SQL Server + Entity Framework Core 8 |
| المعمارية | Clean Architecture · CQRS · MediatR · Pipeline Behaviors |
| المكتبات | MediatR · FluentValidation · Serilog · BCrypt.Net |
| الأمان | JWT Bearer · تفويض ديناميكي بالصلاحيات · Refresh rotation |
| العمليات | Health Checks · Docker · GitHub Actions CI · xUnit |
| التوثيق | Swagger / OpenAPI (Development) |

## Multi-Tenant Architecture

```
                         ┌──────────────────────────────────────────┐
   Platform Admin  ────► │  LogicFit.Platform.API  (aud: Platform)  │ ─┐
                         └──────────────────────────────────────────┘  │  نفس
                                                                        │  Application
   Gym users        ────► ┌──────────────────────────────────────────┐  ├─ Infrastructure
   (Owner…Client)         │  LogicFit.API           (aud: Users)      │ ─┤  Domain
                          └──────────────────────────────────────────┘  │  + قاعدة بيانات واحدة
                                                                        │
                          ┌──────────────────────────────────────────┐  │
                          │  Application (CQRS · MediatR · Behaviors) │  │
                          │  Infrastructure (EF Core · JWT · Jobs)    │ ◄┘
                          │  Domain (Entities · Enums · Rules)        │
                          └──────────────────────────────────────────┘
```

- **عزل الجيمات**: EF Core global query filters تفلتر كل كيان بالـ `TenantId`. مستخدمو المنصة يقرؤون عبر الجيمات عبر bypass عند `CurrentTenantId == null`، محميّ بحيث لا يستطيع مستخدم جيم فعل ذلك.
- **عزل الـ API-ين**: JWT audience مختلف لكل host.

---

# 2. الميزات والوحدات

## طبقة المنصة (Platform)
- **Onboarding**: إنشاء جيم + حساب صاحبه في نداء واحد.
- **دورة حياة الجيم**: approve · activate · suspend · archive.
- **الباقات والميزات**: CRUD كامل للـ Plans و Features.
- **الفوترة اليدوية**: طرق دفع، مراجعة إثباتات الدفع (approve/reject).
- **التقارير**: dashboard المنصة، اشتراكات الجيمات.

## طبقة الجيم (Tenant)
| الوحدة | الوصف |
|--------|-------|
| **العضوية والتدريب** | العملاء، المدربون، ربط المدرب بالمتدربين، برامج التمارين، الجلسات، التمارين/العضلات، الخطط الغذائية/الأطعمة، قياسات الجسم، داشبورد العميل |
| **العمليات** | الفروع، الغرف، الأجهزة، الصيانة، الحضور، بوابة QR، بطاقات العضوية |
| **الجدولة والتفاعل** | المواعيد، الحصص الجماعية، جداول الحصص، الإشعارات، الشات، التحديات |
| **التجارة والمالية** | POS، المنتجات، المخزون، المورّدون، الفواتير، المدفوعات، المصروفات، الكوبونات، الضرائب، معاملات المحفظة |
| **الموارد البشرية** | الموظفون، الورديات، الإجازات، العمولات، الرواتب |
| **الإعدادات** | ملف الجيم (White-label)، المستخدمون، إعدادات الضرائب |
| **اشتراك الجيم في المنصة** | تصفّح الباقات، اختيار/ترقية/تجديد، رفع إثبات دفع، الفواتير، الاستخدام مقابل الحدود |

---

# 3. أدوار المستخدمين وصلاحياتهم

نظام **RBAC ديناميكي**: الصلاحيات مخزّنة في قاعدة البيانات (جداول `Roles/Permissions/RolePermissions/UserRoles`)، وتُبنى الـ policies وقت التشغيل. التوكن يحمل `permissions[]` — ابنِ الواجهة عليها.

## أدوار المنصة
| الدور | القيمة | الصلاحيات |
|-------|--------|-----------|
| **PlatformOwner** | 8 | `ManagePlatform` (god-mode) + كل صلاحيات المنصة |
| **PlatformAdmin** | 9 | `ManageTenants, ManagePlans, ManagePaymentRequests, ManagePlatformReports` |

## أدوار الجيم
| الدور | القيمة | الصلاحيات الافتراضية |
|-------|--------|----------------------|
| **Owner** | 1 | كل صلاحيات الجيم |
| **Manager** | 4 | الكل عدا `ManageSettings` و `ManageTenantBilling` |
| **Receptionist** | 5 | `ViewMembers, ManageMembers, ManageAttendance, ManageClientSubscriptions, ManagePOS` |
| **Accountant** | 6 | `ManageFinance, ViewReports, ManageReports, ManageTenantBilling` |
| **Coach** | 2 | `ViewMembers, ManageAttendance, ViewReports` |
| **Client** | 3 | لا صلاحيات back-office (شاشات self-service فقط) |
| **Trainer** | 7 | مثل Coach (`ViewMembers, ManageAttendance, ViewReports`) |

## كتالوج الصلاحيات
```
--- الجيم (Tenant) ---
ManageMembers, ViewMembers, ManageCoaches, ManageAttendance, ManageClientSubscriptions,
ManagePOS, ManageInventory, ManageEmployees, ManageBranches, ManageFinance,
ViewReports, ManageReports, ManageSettings, ManageTenantBilling
--- المنصة (Platform) ---
ManagePlatform, ManageTenants, ManagePlans, ManagePaymentRequests, ManagePlatformReports
```

## خريطة الصلاحية المطلوبة لكل منطقة (Tenant)
| المنطقة | الصلاحية |
|---------|----------|
| Clients, MembershipCards | `ManageMembers` |
| Coaches, CoachClients | `ManageCoaches` |
| Employees, Payroll, Shifts, Leaves | `ManageEmployees` |
| Branches, Rooms, Equipment, Maintenance | `ManageBranches` |
| Attendance, GateAccess | `ManageAttendance` |
| Subscriptions (اشتراكات العملاء) | `ManageClientSubscriptions` |
| Invoices, Payments, Transactions, Expenses, ExpenseCategories, Commissions, Coupons | `ManageFinance` |
| Sales | `ManagePOS` |
| Products, ProductCategories, Stock, Suppliers | `ManageInventory` |
| Reports | `ViewReports` |
| GymProfile, TaxSettings, Users | `ManageSettings` |
| اشتراك الجيم في المنصة (Billing) | `ManageTenantBilling` |

> شاشات **self-service** (بلا صلاحية محددة، أي مستخدم مسجّل): Profile, ClientDashboard, Notifications, Chat, Appointments, WorkoutPrograms, WorkoutSessions, DietPlans, BodyMeasurements, Exercises, Foods, Muscles, GroupClasses, ClassSchedules, Challenges.

## نموذج المصادقة
- **دخول الجيم**: `phone + password + subdomain` (بدل TenantId GUID) على Tenant API.
- **دخول المنصة**: `email + password` على Platform API.
- **JWT**: access token 15 دقيقة، مع refresh token (rotation + revocation + surface-binding).
- التفصيل الكامل في [AUTH_AND_REGISTRATION.md](AUTH_AND_REGISTRATION.md).

---

# 4. الكيانات والعلاقات

## Entity Relationship Diagram (Text)

```
┌────────────────────────────────────────────────────────────────────────────┐
│                          PLATFORM (SaaS layer)                             │
│  PlatformOwner/Admin  ──manage──►  Tenant · Plan · Feature · PaymentMethod  │
│                                    TenantSubscription · PaymentRequest      │
└────────────────────────────────────────────────────────────────────────────┘
                                     │  (Tenant = الجيم)
                                     ▼
┌────────────────────────────────────────────────────────────────────────────┐
│                              TENANT (الجيم)                                 │
│  Id, Name, Subdomain, CustomDomain, Status(TenantStatus), BrandingSettings  │
│  Description, Address, Phone, Email, Logo/Cover, Gallery                     │
└─────────────────────────────────┬──────────────────────────────────────────┘
                                  │ 1:N
                                  ▼
┌────────────────────────────────────────────────────────────────────────────┐
│                               USER                                          │
│  Id, TenantId, Email, PhoneNumber, PasswordHash, Role(legacy enum),         │
│  IsActive, WalletBalance, PermissionsVersion, PasswordResetToken            │
│  ├── UserProfile (1:1)          ├── UserRoles (N:M via UserRoleAssignment)   │
│  ├── RefreshTokens (1:N)        ├── as Coach: Trainees / WorkoutPrograms…    │
│  └── as Client: Subscriptions / WorkoutSessions / MealLogs / Measurements    │
└────────────────────────────────────────────────────────────────────────────┘

RBAC:  Role ──N:M(RolePermission)── Permission        User ──N:M(UserRoleAssignment)── Role
Billing: Tenant ──1:N── TenantSubscription ──N:1── Plan ──N:M(PlanFeature)── Feature
         Tenant ──1:N── PaymentRequest ──(approve)──► SubscriptionPayment + SubscriptionInvoice
```

## تفصيل الكيانات

### Tenant (الجيم / المستأجر)
```csharp
Tenant {
    Guid Id
    string Name
    string? Subdomain              // فريد — يُستخدم للدخول/الـ branding
    string? CustomDomain           // فريد — نطاق مخصّص (White-label)
    TenantStatus Status            // Active, Suspended, Trial, PastDue, Cancelled, PendingApproval, Archived, Deleted
    BrandingSettings? BrandingSettings   // JSON: logo, colors, font, css, appName, invoiceLogo, support*
    string? Description, Address, PhoneNumber, Email, LogoUrl, CoverImageUrl, GalleryImagesJson
}
```

### User & Profile
```csharp
User {
    Guid Id
    Guid TenantId                  // = PlatformTenantId للمستخدمي المنصة
    string Email
    string? PhoneNumber            // فريد لكل Tenant — يُستخدم للدخول
    string PasswordHash            // BCrypt
    UserRole Role                  // enum قديم (يُحتفظ به + UserRoleAssignment هو المصدر الحقيقي)
    bool IsActive
    decimal WalletBalance
    int PermissionsVersion         // يُرفع عند تغيير الأدوار → يُبطل التوكنات عبر refresh
    string? PasswordResetToken, PasswordResetTokenExpiry
}
UserProfile { Guid Id, UserId; string FullName; GenderType? Gender; DateTime? BirthDate;
    double? HeightCm, WeightKg; string? ActivityLevel, FitnessGoal, MedicalHistory, ProfilePictureUrl }
```

### RBAC (نظام الصلاحيات الديناميكي)
```csharp
Role { Guid Id; Guid? TenantId;  // null = دور نظام مشترك؛ set = دور مخصّص للجيم
    string Name, NormalizedName, Description; bool IsSystemRole }
Permission { Guid Id; string Code, DisplayName, Category; bool IsPlatformPermission }   // مرجعي عام
RolePermission { Guid Id, RoleId, PermissionId }                                          // N:M
UserRoleAssignment { Guid Id, UserId, RoleId; Guid? TenantId }                            // N:M (المصدر الحقيقي للأدوار)
RefreshToken { Guid Id, UserId; Guid? TenantId; string Token, Surface("tenant"/"platform");
    DateTime ExpiresAt, CreatedAt; DateTime? RevokedAt; string? ReplacedByToken }         // rotation + revocation
```

### SaaS Billing (اشتراك الجيم في المنصة — Platform-owned)
```csharp
Plan { Guid Id; string Name, Description, Currency; decimal Price; BillingCycle BillingCycle;
    int DurationInDays; int? MaxMembers, MaxCoaches, MaxBranches, MaxEmployees, MaxStorageMB;  // null=غير محدود
    bool IsActive; int DisplayOrder }
Feature { Guid Id; string Code, Name, Description; bool IsActive }
PlanFeature { Guid Id, PlanId, FeatureId; int? LimitValue }
TenantFeature { Guid Id, TenantId, FeatureId; bool IsEnabled; int? LimitOverride }
TenantSubscription { Guid Id, TenantId, PlanId; TenantSubscriptionStatus Status;
    DateTime? StartDate, EndDate, TrialEndsAt, RenewDate, ReminderSentAt;
    BillingCycle BillingCycle; decimal Amount; string Currency; bool AutoRenew;
    string? ApprovedBy; DateTime? ApprovedAt, CancelledAt, SuspendedAt; byte[] RowVersion }
TenantPaymentMethod { Guid Id; string Name, Type, AccountName, AccountNumber, IBAN, WalletNumber,
    Instructions, QRImageUrl; bool IsActive; int DisplayOrder }
PaymentRequest { Guid Id, TenantId, PlanId; Guid? TenantSubscriptionId, PaymentMethodId;
    decimal Amount; string Currency; string? TransactionNumber, ProofFileUrl, Notes;
    DateTime? PaymentDate; PaymentRequestStatus Status; string? ReviewedBy, RejectReason;
    DateTime? ReviewedAt; byte[] RowVersion }
SubscriptionPayment { Guid Id, TenantId, TenantSubscriptionId, PaymentRequestId; decimal Amount;
    string ReceiptNumber; DateTime PaymentDate; string? ApprovedBy }
SubscriptionInvoice { Guid Id, TenantId, TenantSubscriptionId; string InvoiceNumber;
    decimal Amount; SubscriptionInvoiceStatus Status; DateTime IssueDate; DateTime? DueDate, PaidAt }
TenantUsage { Guid Id, TenantId; int MembersCount, CoachesCount, BranchesCount, EmployeesCount,
    StorageUsedMB; DateTime LastCalculatedAt }   // cache للـ dashboard (الفرض بالعدّ الحيّ)
```

### Gym domain — الكيانات الأساسية (ملخّص)
```csharp
// العضوية والتدريب
CoachClient { CoachId, ClientId, AssignedAt, IsActive, Notes }          // ربط المدرب بالمتدرب (واحد نشط لكل عميل)
WorkoutProgram → ProgramRoutine → RoutineExercise → Exercise → Muscle   // هرم التمارين
WorkoutSession → SessionSet                                             // تنفيذ التمرين
DietPlan → DailyMeal → MealItem → Food ; Food → FoodMicronutrient        // هرم التغذية
BodyMeasurement { weightKg, bodyFat, muscleMass, bmr, InBody + photos }
// الاشتراكات (داخل الجيم) — غير اشتراك المنصة
SubscriptionPlan → ClientSubscription → SubscriptionFreeze ; WalletTransaction
// العمليات
Branch → BranchOperatingHours ; Room ; Equipment → MaintenanceRecord
Attendance ; GateAccessLog ; MembershipCard
// الجدولة
Appointment ; GroupClass → ClassSchedule → ClassEnrollment
// التجارة والمالية
Product → ProductCategory ; StockItem → StockMovement ; Supplier → PurchaseOrder → PurchaseOrderItem
Sale → SaleItem ; Invoice → InvoiceItem + Payment ; Expense → ExpenseCategory ; Coupon → CouponUsage ; TaxSetting
// الموارد البشرية
EmployeeProfile → EmployeeBranch ; Shift → ShiftAssignment ; LeaveRequest ; Commission → CommissionRule ; PayrollRun → PayrollItem
// تفاعل + تدقيق
Notification ; ChatConversation → ChatMessage ; Challenge → ClientChallenge ; AuditLog
```

---

# 5. API Endpoints

> **أعراف عامة**: كل طلب محمي يحمل `Authorization: Bearer <accessToken>`. الإنشاء يرجّع غالباً `Guid` خام (`200`/`201`)؛ التعديل/الحذف/تغيير الحالة يرجّع `204 No Content`؛ غير الموجود `404`. القيم الرقمية للـ enums في [القسم 6](#6-enums-reference).

## Authentication `/api/auth` (Tenant API)

### Login
`POST /api/auth/login` — **Anonymous**
```json
// Request
{ "phoneNumber": "01000000000", "password": "Owner@123", "subdomain": "goldgym" }
```
```json
// Response — AuthResponseDto
{
  "userId": "guid", "email": "owner@goldgym.com", "phoneNumber": "01000000000", "fullName": "أحمد",
  "role": "Owner", "roles": ["Owner"],
  "permissions": ["ManageMembers","ViewMembers","ManageCoaches","ManageAttendance","ManageClientSubscriptions","ManagePOS","ManageInventory","ManageEmployees","ManageBranches","ManageFinance","ViewReports","ManageReports","ManageSettings","ManageTenantBilling"],
  "tenantId": "guid", "accessToken": "eyJhbGci...", "refreshToken": "opaque...", "expiresAt": "2026-07-08T12:15:00Z"
}
```
> بدل `subdomain` يمكن تمرير `tenantId` (GUID) أو هيدر `X-Tenant-Id`.

### Register (عميل فقط)
`POST /api/auth/register` — **Anonymous** — ينشئ **Client دائماً**.
```json
// Request
{ "fullName": "سارة", "phoneNumber": "01033333333", "email": "sara@mail.com",
  "password": "Client@123", "confirmPassword": "Client@123", "subdomain": "goldgym" }
```
```json
// Response — AuthResponseDto (توكن جاهز، role=Client، permissions=[])
```

### Refresh / Logout-all
```json
// POST /api/auth/refresh   (Anonymous)
{ "refreshToken": "opaque-string" }   // → AuthResponseDto بتوكنات جديدة (القديم يُبطَل)
```
`POST /api/auth/logout-all` — **Authenticated** → `204` (يُبطل كل الـ refresh tokens).

### Forget / Reset password
```json
// POST /api/auth/forget-password   (Anonymous)
{ "phoneNumber": "01000000000", "subdomain": "goldgym" }
// → { "success": true, "message": "...", "resetToken": "123456" }   (resetToken في التطوير فقط)

// POST /api/auth/reset-password   (Anonymous)
{ "phoneNumber": "01000000000", "resetToken": "123456", "newPassword": "NewPass@123", "subdomain": "goldgym" }
// → { "success": true, "message": "Password has been reset successfully." }
```

### Branding (هوية الجيم قبل الدخول)
`GET /api/branding/{identifier}` — **Anonymous** — `identifier` = subdomain أو custom domain.
```json
// Response — BrandingDto (404 لو غير موجود)
{ "tenantId": "guid", "name": "Gold Gym", "subdomain": "goldgym", "appName": "Gold Gym App",
  "logoUrl": "https://...", "coverImageUrl": "https://...", "primaryColor": "#3B82F6", "secondaryColor": "#1E40AF",
  "fontFamily": "Cairo", "customCss": null, "invoiceLogoUrl": null, "supportPhone": "0100...", "supportEmail": "support@goldgym.com" }
```

> تفاصيل إنشاء ودخول كل نوع مستخدم في **[AUTH_AND_REGISTRATION.md](AUTH_AND_REGISTRATION.md)**.

## Membership & Training

> ملاحظات: `Gender` يُرسَل كـ `int?`. قيم `PlanStatus`: 1=Active, 2=Archived, 3=Draft. قيم `SubscriptionStatus`: 1=Active, 2=Suspended, 3=Trial, 4=Expired, 5=Cancelled.

### Clients — `api/clients` · Auth: `ManageMembers`

**`GET /api/clients`** — قائمة العملاء · query: `searchTerm` (string?), `isActive` (bool?)
```json
// Response: List<ClientDto>
[
  {
    "id": "guid", "tenantId": "guid", "email": "client@example.com", "phoneNumber": "0500000000",
    "isActive": true, "walletBalance": 0.0,
    "profile": {
      "fullName": "Ahmed Ali", "gender": 1, "birthDate": "1995-06-01T00:00:00",
      "heightCm": 178.0, "activityLevel": "Moderate", "medicalHistory": "None"
    },
    "activeSubscription": {
      "id": "guid", "planName": "Gold Plan", "startDate": "2026-01-01T00:00:00",
      "endDate": "2026-12-31T00:00:00", "status": "Active"
    }
  }
]
```

**`GET /api/clients/{id}`** — عميل واحد · Response: `ClientDto` (كما أعلاه) أو `404`.

**`POST /api/clients`** — إنشاء عميل
```json
// Request: CreateClientCommand
{
  "phoneNumber": "0500000000", "email": "client@example.com", "password": "P@ssw0rd",
  "fullName": "Ahmed Ali", "gender": 1, "birthDate": "1995-06-01T00:00:00",
  "heightCm": 178.0, "activityLevel": "Moderate", "medicalHistory": "None", "coachId": "guid-or-null"
}
// Response: Guid
```

**`PUT /api/clients/{id}`** — تعديل عميل
```json
// Request: UpdateClientCommand (الـ Id من المسار)
{
  "email": "client@example.com", "phoneNumber": "0500000000", "isActive": true,
  "fullName": "Ahmed Ali", "gender": 1, "birthDate": "1995-06-01T00:00:00",
  "heightCm": 178.0, "activityLevel": "Moderate", "medicalHistory": "None"
}
// Response: 204 NoContent
```

**`DELETE /api/clients/{id}`** — حذف عميل (soft delete) · Response: `204 NoContent`.

### Coaches — `api/coaches` · Auth: `ManageCoaches`

**`GET /api/coaches`** — query: `searchTerm` (string?), `isActive` (bool?)
```json
// Response: List<CoachDto>
[
  {
    "id": "guid", "tenantId": "guid", "email": "coach@example.com", "phoneNumber": "0500000000", "isActive": true,
    "profile": { "fullName": "Coach Sami", "profilePictureUrl": "https://.../pic.jpg", "gender": 1, "birthDate": "1990-01-01T00:00:00" },
    "traineeCount": 12
  }
]
```

**`GET /api/coaches/{id}`** → `CoachDto` أو `404`.

**`POST /api/coaches`** — إنشاء مدرب
```json
// Request: CreateCoachCommand
{ "phoneNumber": "0500000000", "email": "coach@example.com", "password": "P@ssw0rd",
  "fullName": "Coach Sami", "gender": 1, "birthDate": "1990-01-01T00:00:00" }
// Response: Guid
```

**`PUT /api/coaches/{id}`**
```json
// Request: UpdateCoachCommand
{ "email": "coach@example.com", "phoneNumber": "0500000000", "isActive": true,
  "fullName": "Coach Sami", "gender": 1, "birthDate": "1990-01-01T00:00:00" }
// Response: 204 NoContent
```

**`DELETE /api/coaches/{id}`** → `204 NoContent`.

### CoachClients — `api/coach-clients` · Auth: `ManageCoaches`

**`GET /api/coach-clients`** — query: `coachId` (Guid?), `isActive` (bool?, default `true`)
```json
// Response: List<CoachClientDto>
[
  {
    "id": "guid", "coachId": "guid", "coachName": "Coach Sami",
    "clientId": "guid", "clientName": "Ahmed Ali", "clientPhone": "0500000000", "clientEmail": "client@example.com",
    "assignedAt": "2026-01-10T09:00:00", "unassignedAt": null, "isActive": true, "notes": "VIP",
    "hasActiveSubscription": true, "subscriptionEndDate": "2026-12-31T00:00:00",
    "workoutProgramsCount": 3, "dietPlansCount": 2, "workoutSessionsCount": 18, "lastSessionDate": "2026-07-01T18:00:00"
  }
]
```

**`GET /api/coach-clients/{id}`** → `CoachClientDto` أو `404`.

**`POST /api/coach-clients`** — إضافة متدرب جديد وربطه بالمدرب الحالي
```json
// Request: AddTraineeCommand
{ "clientName": "Ahmed Ali", "clientPhone": "0500000000", "clientEmail": "client@example.com",
  "gender": 1, "birthDate": "1995-06-01T00:00:00", "heightCm": 178.0,
  "activityLevel": "Moderate", "medicalHistory": "None", "notes": "Beginner" }
// Response: Guid
```

**`POST /api/coach-clients/assign`** — ربط عميل موجود بمدرب (اترك `coachId=null` للربط بالنفس)
```json
// Request: AssignClientToCoachCommand
{ "coachId": "guid-or-null", "clientId": "guid", "notes": "Transferred" }
// Response: Guid
```

**`PUT /api/coach-clients/{id}`** — تحديث (نقل لمدرب آخر / تفعيل)
```json
// Request: UpdateCoachClientCommand
{ "newCoachId": "guid-or-null", "isActive": true }
// Response: 204 NoContent (أو 404)
```

**`DELETE /api/coach-clients/{clientId}`** — فك ربط عميل عن مدربه · Response: `204 NoContent`.

### ClientDashboard — `api/client` · Auth: Authenticated (العميل نفسه)

**`GET /api/client/dashboard`** — لوحة العميل
```json
// Response: ClientDashboardDto
{
  "activePrograms": [ { "id": "guid", "name": "Push/Pull", "coachName": "Coach Sami", "startDate": "2026-01-01T00:00:00", "endDate": null } ],
  "activeDietPlans": [ { "id": "guid", "name": "Cutting", "coachName": "Coach Sami", "startDate": "2026-01-01T00:00:00", "endDate": null, "status": 1, "targetCalories": 2000.0, "targetProtein": 180.0, "targetCarbs": 150.0, "targetFats": 60.0 } ],
  "activeSubscription": { "id": "guid", "planName": "Gold Plan", "startDate": "2026-01-01T00:00:00", "endDate": "2026-12-31T00:00:00", "status": 1, "totalAmount": 1200.0, "amountPaid": 1200.0 },
  "recentMeasurements": [ { "id": "guid", "dateRecorded": "2026-07-01T00:00:00", "weightKg": 80.0, "bodyFatPercent": 18.0, "skeletalMuscleMass": 36.0 } ],
  "assignedCoach": { "coachId": "guid", "fullName": "Coach Sami", "email": "coach@example.com", "phoneNumber": "0500000000", "profilePictureUrl": "https://.../pic.jpg", "assignedAt": "2026-01-10T09:00:00" },
  "unreadNotificationCount": 3
}
```
- **`GET /api/client/my-programs`** → `List<MyWorkoutProgramDto>` (`{ id, name, coachName, startDate, endDate }`)
- **`GET /api/client/my-diet-plans`** → `List<MyDietPlanDto>` (`{ id, name, coachName, startDate, endDate, status, targetCalories, targetProtein, targetCarbs, targetFats }`)
- **`GET /api/client/my-subscriptions`** → `List<MySubscriptionSummaryDto>` (`{ id, planName, startDate, endDate, status, totalAmount, amountPaid }`)
- **`GET /api/client/my-measurements`** → `List<MyBodyMeasurementDto>` (`{ id, dateRecorded, weightKg, bodyFatPercent, skeletalMuscleMass }`)
- **`GET /api/client/my-coach`** → `MyCoachDto` أو `404`
- **`GET /api/client/my-appointments`** → `List<MyAppointmentDto>` (`{ id, coachName, startTime, endTime, title, status }`)

### Profile — `api/profile` · Auth: Authenticated

**`GET /api/profile`** → `UserDto` أو `404`
```json
{
  "id": "guid", "tenantId": "guid", "email": "user@example.com", "phoneNumber": "0500000000", "role": 1, "isActive": true, "walletBalance": 0.0,
  "profile": { "fullName": "Ahmed Ali", "profilePictureUrl": "https://.../pic.jpg", "gender": 1, "birthDate": "1995-06-01T00:00:00",
    "heightCm": 178.0, "weightKg": 80.0, "activityLevel": "Moderate", "fitnessGoal": "Muscle Gain", "medicalHistory": "None" }
}
```
**`PUT /api/profile`**
```json
// Request: UpdateMyProfileCommand
{ "fullName": "Ahmed Ali", "gender": 1, "birthDate": "1995-06-01T00:00:00", "heightCm": 178.0,
  "weightKg": 80.0, "activityLevel": "Moderate", "fitnessGoal": "Muscle Gain", "medicalHistory": "None" }
// Response: 204 NoContent
```
- **`POST /api/profile/picture`** — `multipart` حقل `file` → `{ "url": "https://.../profile-pictures/xxx.jpg" }`
- **`DELETE /api/profile/picture`** → `204 NoContent`

### BodyMeasurements — `api/bodymeasurements` · Auth: Authenticated

**`GET /api/bodymeasurements`** — query: `clientId` (Guid?), `fromDate` (DateTime?), `toDate` (DateTime?)
```json
// Response: List<BodyMeasurementDto>
[
  { "id": "guid", "tenantId": "guid", "clientId": "guid", "clientName": "Ahmed Ali", "dateRecorded": "2026-07-01T00:00:00",
    "weightKg": 80.0, "skeletalMuscleMass": 36.0, "bodyFatMass": 14.0, "bodyFatPercent": 18.0, "totalBodyWater": 45.0,
    "bmr": 1700.0, "visceralFatLevel": 8, "inbodyImageUrl": "https://.../inbody.jpg",
    "frontPhotoUrl": "https://.../front.jpg", "sidePhotoUrl": "https://.../side.jpg", "backPhotoUrl": "https://.../back.jpg" }
]
```
**`POST /api/bodymeasurements`** — JSON بدون صور
```json
// Request: CreateBodyMeasurementCommand (حقول الصور IFormFile تُتجاهَل في JSON)
{ "clientId": "guid", "dateRecorded": "2026-07-01T00:00:00", "weightKg": 80.0, "skeletalMuscleMass": 36.0,
  "bodyFatMass": 14.0, "bodyFatPercent": 18.0, "totalBodyWater": 45.0, "bmr": 1700.0, "visceralFatLevel": 8 }
// Response: Guid
```
- **`POST /api/bodymeasurements/with-images`** — `multipart/form-data`: نفس الحقول + الملفات `inbodyImage`, `frontPhoto`, `sidePhoto`, `backPhoto` → `Guid`
- **`DELETE /api/bodymeasurements/{id}`** → `204 NoContent`

### WorkoutPrograms — `api/workoutprograms` · Auth: Authenticated

**`GET /api/workoutprograms`** — query: `coachId` (Guid?), `clientId` (Guid?)
```json
// Response: List<WorkoutProgramDto>
[
  {
    "id": "guid", "tenantId": "guid", "coachId": "guid", "coachName": "Coach Sami",
    "clientId": "guid", "clientName": "Ahmed Ali", "name": "Push/Pull/Legs",
    "startDate": "2026-01-01T00:00:00", "endDate": null,
    "routines": [
      { "id": "guid", "programId": "guid", "name": "Push Day", "dayOfWeek": 1,
        "exercises": [ { "id": "guid", "routineId": "guid", "exerciseId": 10, "exerciseName": "Bench Press",
          "sets": 4, "repsMin": 8, "repsMax": 12, "restSec": 90, "supersetGroupId": null } ] }
    ]
  }
]
```
- **`GET /api/workoutprograms/{id}`** → `WorkoutProgramDto` أو `404`
- **`POST /api/workoutprograms`** — `{ clientId, name, startDate, endDate }` → `Guid` (201)
- **`PUT /api/workoutprograms/{id}`** — `{ name, startDate, endDate }` → `204`
- **`DELETE /api/workoutprograms/{id}`** → `204`
- **`POST /api/workoutprograms/{id}/duplicate`** — `{ newClientId?, newName? }` → `Guid` (201)
- **`POST /api/workoutprograms/{programId}/routines`** — `{ name, dayOfWeek }` → `Guid`
- **`PUT /api/workoutprograms/routines/{routineId}`** — `{ name, dayOfWeek }` → `204`
- **`DELETE /api/workoutprograms/routines/{routineId}`** → `204`
- **`POST /api/workoutprograms/routines/{routineId}/exercises`** — `{ exerciseId, sets, repsMin, repsMax, restSec, supersetGroupId }` → `Guid`
- **`PUT /api/workoutprograms/routines/exercises/{exerciseId}`** — `{ exerciseId?, sets, repsMin, repsMax, restSec, supersetGroupId }` → `204`
- **`DELETE /api/workoutprograms/routines/exercises/{exerciseId}`** → `204`

### WorkoutSessions — `api/workoutsessions` · Auth: Authenticated

**`GET /api/workoutsessions`** — query: `clientId` (Guid?), `fromDate` (DateTime?), `toDate` (DateTime?)
```json
// Response: List<WorkoutSessionDto>  (لاحظ اسم الحقل totalVolumLifted بدون e)
[
  {
    "id": "guid", "tenantId": "guid", "clientId": "guid", "clientName": "Ahmed Ali",
    "routineId": "guid", "routineName": "Push Day", "startedAt": "2026-07-01T18:00:00", "endedAt": "2026-07-01T19:00:00",
    "totalVolumLifted": 5400.0, "notes": "Felt strong",
    "sets": [ { "id": "guid", "sessionId": "guid", "exerciseId": 10, "exerciseName": "Bench Press",
      "setNumber": 1, "weightKg": 80.0, "reps": 10, "rpe": 8.0, "volumeLoad": 800.0, "isPr": false } ]
  }
]
```
- **`GET /api/workoutsessions/{id}`** → `WorkoutSessionDto` أو `404`
- **`POST /api/workoutsessions/start`** — `{ routineId }` → `Guid` (201)
- **`POST /api/workoutsessions/{sessionId}/end`** — `{ notes }` → `204`
- **`POST /api/workoutsessions/{sessionId}/sets`** — `{ exerciseId, setNumber, weightKg, reps, rpe }` → `Guid`

### Exercises — `api/exercises` · Auth: Authenticated

**`GET /api/exercises`** — query: `targetMuscleId` (int?), `equipment` (string?), `isHighImpact` (bool?)
```json
// Response: List<ExerciseDto>
[
  {
    "id": 10, "tenantId": "guid-or-null", "name": "Bench Press", "nameAr": "ضغط بنش", "description": "...", "descriptionAr": "...",
    "targetMuscleId": 3, "targetMuscleName": "Chest", "targetMuscleBodyPart": "Upper Body", "primaryMuscleContributionPercent": 100,
    "secondaryMuscles": [ { "muscleId": 5, "muscleName": "Triceps", "bodyPart": "Arms", "contributionPercent": 20 } ],
    "imageUrl": "https://.../ex.jpg", "videoUrl": "https://.../ex.mp4", "icon": "dumbbell",
    "equipment": "Barbell", "isHighImpact": false, "difficulty": "Intermediate", "category": "Compound",
    "movementPattern": "Push", "mechanic": "Compound", "force": "Push",
    "instructions": ["Step 1","Step 2"], "instructionsAr": ["خطوة 1"], "tips": ["Keep core tight"], "tipsAr": ["..."],
    "commonMistakes": ["Flaring elbows"], "commonMistakesAr": ["..."],
    "repsRange": "8-12", "setsRange": "3-4", "restSeconds": 90, "tempo": "2-0-1"
  }
]
```
- **`GET /api/exercises/{id}`** → `ExerciseDto` أو `404`
- **`POST /api/exercises`** — `multipart`: `name`, `targetMuscleId`, `image?`, `video?`, `equipment?`, `isHighImpact`, `secondaryMuscles[] ({ muscleId, contributionPercent })` → `int` (201)
- **`PUT /api/exercises/{id}`** — `multipart` (نفس حقول الإنشاء) → `204`
- **`DELETE /api/exercises/{id}`** → `204`

### Muscles — `api/muscles` · Auth: Authenticated

**`GET /api/muscles`** — query: `bodyPart` (string?)
```json
// Response: List<MuscleDto>
[ { "id": 3, "name": "Chest", "nameAr": "الصدر", "bodyPart": "Upper Body", "description": "...", "descriptionAr": "...", "icon": "chest" } ]
```
- **`GET /api/muscles/{id}`** → `MuscleDto` أو `404`
- **`POST /api/muscles`** — `{ name, nameAr, bodyPart, description, descriptionAr, icon }` → `MuscleDto` (201)
- **`PUT /api/muscles/{id}`** — `UpdateMuscleCommand` (يجب تطابق `id` وإلا `400`) → `MuscleDto`
- **`DELETE /api/muscles/{id}`** → `204` (soft delete)

### DietPlans — `api/dietplans` · Auth: Authenticated

**`GET /api/dietplans`** — query: `coachId` (Guid?), `clientId` (Guid?), `status` (PlanStatus?)
```json
// Response: List<DietPlanDto>
[
  {
    "id": "guid", "tenantId": "guid", "coachId": "guid", "coachName": "Coach Sami", "clientId": "guid", "clientName": "Ahmed Ali",
    "name": "Cutting Plan", "startDate": "2026-01-01T00:00:00", "endDate": null, "status": 1,
    "targetCalories": 2000.0, "targetProtein": 180.0, "targetCarbs": 150.0, "targetFats": 60.0,
    "meals": [
      { "id": "guid", "planId": "guid", "name": "Breakfast", "orderIndex": 1,
        "items": [ { "id": "guid", "mealId": "guid", "foodId": 5, "foodName": "Oats", "assignedQuantity": 100.0,
          "calcCalories": 389.0, "calcProtein": 16.9, "calcCarbs": 66.3, "calcFats": 6.9 } ] }
    ]
  }
]
```
- **`GET /api/dietplans/{id}`** → `DietPlanDto` أو `404`
- **`POST /api/dietplans`** — `{ clientId, name, startDate, endDate, targetCalories, targetProtein, targetCarbs, targetFats }` → `Guid` (201)
- **`PUT /api/dietplans/{id}`** — `{ name, description, startDate, endDate, targetCalories, targetProtein, targetCarbs, targetFats, status }` → `204`
- **`DELETE /api/dietplans/{id}`** → `204`
- **`POST /api/dietplans/{id}/duplicate`** — `{ newClientId?, newName? }` → `Guid` (201)
- **`POST /api/dietplans/{planId}/meals`** — `{ name, orderIndex }` → `Guid`
- **`PUT /api/dietplans/meals/{mealId}`** — `{ name, orderIndex }` → `204`
- **`DELETE /api/dietplans/meals/{mealId}`** → `204`
- **`POST /api/dietplans/meals/{mealId}/items`** — `{ foodId, assignedQuantity }` → `Guid`
- **`PUT /api/dietplans/meals/items/{itemId}`** — `{ foodId?, assignedQuantity }` → `204`
- **`DELETE /api/dietplans/meals/items/{itemId}`** → `204`

### Foods — `api/foods` · Auth: Authenticated

**`GET /api/foods`** — query: `category` (string?), `searchTerm` (string?), `isVerified` (bool?)
```json
// Response: List<FoodDto>
[
  { "id": 5, "tenantId": "guid-or-null", "name": "Oats", "nameAr": "شوفان", "category": "Grains",
    "caloriesPer100g": 389.0, "proteinPer100g": 16.9, "carbsPer100g": 66.3, "fatsPer100g": 6.9,
    "fiberPer100g": 10.6, "sugarPer100g": 0.99, "sodiumPer100g": 2.0,
    "servingSize": 40.0, "servingUnit": "g", "alternativeGroupId": "grp-1", "isVerified": true }
]
```
- **`GET /api/foods/{id}`** → `FoodDto` أو `404`
- **`POST /api/foods`** — `{ name, category, caloriesPer100g, proteinPer100g, carbsPer100g, fatsPer100g, fiberPer100g, alternativeGroupId }` → `int` (201)
- **`PUT /api/foods/{id}`** — نفس حقول الإنشاء → `204`
- **`DELETE /api/foods/{id}`** → `204`

## Operations & Engagement

### Branches — `api/Branches` · Auth: `ManageBranches`

**`GET /api/Branches`** — query: `isActive` (bool?), `searchTerm` (string?)
```json
// Response: List<BranchDto>
[
  {
    "id": "guid", "tenantId": "guid", "name": "الفرع الرئيسي", "code": "MAIN", "description": "مركز اللياقة الرئيسي",
    "address": "شارع التحرير", "city": "القاهرة", "phoneNumber": "0100000000", "email": "main@gym.com",
    "latitude": 30.0444, "longitude": 31.2357, "isActive": true, "isDefault": true, "capacity": 300,
    "openTime": "08:00:00", "closeTime": "23:00:00", "managerId": "guid", "managerName": "أحمد علي",
    "logoUrl": "https://.../logo.png", "coverImageUrl": "https://.../cover.png",
    "operatingHours": [ { "id": "guid", "dayOfWeek": 1, "openTime": "08:00:00", "closeTime": "23:00:00", "isClosed": false } ],
    "activeClientsCount": 120, "todayCheckInsCount": 45
  }
]
```
**`GET /api/Branches/{id}`** → `BranchDto` أو `404`.
**`POST /api/Branches`** — إنشاء فرع
```json
// Request: CreateBranchCommand
{ "name": "فرع المعادي", "code": "MAADI", "description": "فرع جديد", "address": "شارع 9", "city": "القاهرة",
  "phoneNumber": "0111111111", "email": "maadi@gym.com", "latitude": 29.96, "longitude": 31.25,
  "isActive": true, "isDefault": false, "capacity": 150, "openTime": "09:00:00", "closeTime": "22:00:00",
  "managerId": "guid", "logoUrl": null, "coverImageUrl": null }
// Response: Guid
```
**`PUT /api/Branches/{id}`** — `UpdateBranchCommand` (نفس حقول الإنشاء) → `204`.
**`DELETE /api/Branches/{id}`** → `204`.
**`PUT /api/Branches/{id}/operating-hours`** — ضبط ساعات العمل
```json
// Request: SetOperatingHoursCommand
{ "hours": [ { "dayOfWeek": 0, "openTime": "10:00:00", "closeTime": "20:00:00", "isClosed": false },
             { "dayOfWeek": 5, "openTime": "00:00:00", "closeTime": "00:00:00", "isClosed": true } ] }
// Response: 204
```

### Rooms — `api/Rooms` · Auth: `ManageBranches`
**`GET /api/Rooms`** — query: `branchId?`, `type?`(RoomType), `isActive?`
```json
// Response: List<RoomDto>
[ { "id": "guid", "tenantId": "guid", "branchId": "guid", "branchName": "الفرع الرئيسي", "name": "قاعة الأثقال",
    "type": 0, "typeName": "WeightRoom", "capacity": 40, "description": "أوزان حرة", "imageUrl": null, "isActive": true, "equipmentCount": 25 } ]
```
- **`POST /api/Rooms`** — `{ branchId, name, type, capacity, description, imageUrl, isActive }` → `Guid`
- **`PUT /api/Rooms/{id}`** — `{ name, type, capacity, description, imageUrl, isActive }` → `204`
- **`DELETE /api/Rooms/{id}`** → `204`

### Equipment — `api/Equipment` · Auth: `ManageBranches`
**`GET /api/Equipment`** — query: `branchId?`, `roomId?`, `status?`(EquipmentStatus), `searchTerm?`
```json
// Response: List<EquipmentDto>
[ { "id": "guid", "tenantId": "guid", "branchId": "guid", "branchName": "الفرع الرئيسي", "roomId": "guid", "roomName": "قاعة الكارديو",
    "name": "جهاز مشي", "serialNumber": "SN-12345", "brand": "Technogym", "model": "Run 700", "category": "Cardio",
    "purchaseDate": "2024-01-15T00:00:00Z", "purchasePrice": 45000.00, "status": 0, "statusName": "Active",
    "warrantyUntil": "2026-01-15T00:00:00Z", "imageUrl": null, "notes": null, "openMaintenanceCount": 0 } ]
```
- **`POST /api/Equipment`** — `{ branchId, roomId, name, serialNumber, brand, model, category, purchaseDate, purchasePrice, status, warrantyUntil, imageUrl, notes }` → `Guid`
- **`PUT /api/Equipment/{id}`** — `UpdateEquipmentCommand` (بلا `branchId`) → `204`
- **`PUT /api/Equipment/{id}/status`** — `{ status, notes }` → `204`
- **`DELETE /api/Equipment/{id}`** → `204`

### Maintenance — `api/Maintenance` · Auth: `ManageBranches`
**`GET /api/Maintenance`** — query: `equipmentId?`, `status?`(MaintenanceStatus), `fromDate?`, `toDate?`
```json
// Response: List<MaintenanceRecordDto>
[ { "id": "guid", "tenantId": "guid", "equipmentId": "guid", "equipmentName": "جهاز مشي", "issueDate": "2026-06-01T10:00:00Z",
    "resolvedDate": "2026-06-05T14:00:00Z", "cost": 1200.00, "description": "عطل في المحرك", "technicianName": "خالد",
    "technicianContact": "0122222222", "status": 1, "statusName": "Resolved", "resolutionNotes": "تم استبدال المحرك", "durationDays": 4 } ]
```
- **`POST /api/Maintenance`** — `{ equipmentId, issueDate, description, technicianName, technicianContact, cost, putEquipmentUnderMaintenance }` → `Guid`
- **`POST /api/Maintenance/{id}/resolve`** — `{ resolutionNotes, finalCost, reactivateEquipment }` → `204`

### Attendance — `api/Attendance` · Auth: `ManageAttendance`
**`GET /api/Attendance`** — query: `clientId?`, `fromDate?`, `toDate?`, `checkedInOnly?`
```json
// Response: List<AttendanceDto>
[ { "id": "guid", "clientId": "guid", "clientName": "محمد سمير", "checkInTime": "2026-07-07T09:00:00Z",
    "checkOutTime": "2026-07-07T10:30:00Z", "notes": null, "durationMinutes": 90.0 } ]
```
**`GET /api/Attendance/summary`** — query: `fromDate?`, `toDate?`
```json
// Response: AttendanceSummaryDto
{ "totalCheckIns": 320, "checkedInNow": 12, "averageDurationMinutes": 74.5, "dailyBreakdown": [ { "date": "2026-07-07T00:00:00Z", "count": 45 } ] }
```
- **`POST /api/Attendance/check-in`** — `{ clientId, branchId, notes }` → `Guid`
- **`POST /api/Attendance/{id}/check-out`** → `204`
- **`DELETE /api/Attendance/{id}`** → `204`

### GateAccess — `api/GateAccess` · Auth: `ManageAttendance`
**`POST /api/GateAccess/check-in-qr`** — دخول عبر مسح QR
```json
// Request: GateCheckInByQrCommand
{ "qrCode": "LF-CARD-9F3A...", "branchId": "guid" }
// Response: GateCheckInResultDto
{ "granted": true, "message": "تم السماح بالدخول", "attendanceId": "guid", "clientId": "guid", "clientName": "محمد سمير", "branchId": "guid", "denyReason": 0 }
```
**`GET /api/GateAccess/logs`** — query: `clientId?`, `branchId?`, `result?`(GateAccessResult), `fromDate?`, `toDate?`, `take`(200)
```json
// Response: List<GateAccessLogDto>
[ { "id": "guid", "clientId": "guid", "clientName": "محمد سمير", "branchId": "guid", "branchName": "الفرع الرئيسي",
    "accessTime": "2026-07-07T09:00:00Z", "result": 0, "resultName": "Granted", "method": 0, "methodName": "Qr",
    "denyReason": 0, "denyReasonName": "None", "notes": null, "scannedCode": "LF-CARD-9F3A..." } ]
```

### MembershipCards — `api/MembershipCards` · Auth: `ManageMembers`
**`GET /api/MembershipCards`** — query: `clientId?`, `isActive?`
```json
// Response: List<MembershipCardDto>
[ { "id": "guid", "tenantId": "guid", "clientId": "guid", "clientName": "محمد سمير", "cardNumber": "0001-2345",
    "qrCode": "LF-CARD-9F3A...", "isActive": true, "issuedAt": "2026-01-01T00:00:00Z", "expiresAt": "2026-12-31T00:00:00Z",
    "revokedAt": null, "revokedReason": null, "isExpired": false } ]
```
- **`POST /api/MembershipCards/issue`** — `{ clientId, expiresAt, cardNumber?(auto) }` → `Guid`
- **`POST /api/MembershipCards/{id}/revoke`** — `{ reason }` → `204`

### Appointments — `api/Appointments` · Auth: Authenticated
**`GET /api/Appointments`** — query: `coachId?`, `clientId?`, `fromDate?`, `toDate?`, `status?`(AppointmentStatus)
```json
// Response: List<AppointmentDto>
[ { "id": "guid", "coachId": "guid", "coachName": "كابتن أحمد", "clientId": "guid", "clientName": "محمد سمير",
    "startTime": "2026-07-08T10:00:00Z", "endTime": "2026-07-08T11:00:00Z", "title": "حصة تدريب شخصي", "notes": null, "status": 0 } ]
```
- **`GET /api/Appointments/{id}`** → `AppointmentDto`
- **`POST /api/Appointments`** — `{ coachId, clientId, startTime, endTime, title, notes }` → `Guid` (201)
- **`PUT /api/Appointments/{id}/status`** — `{ status }` → `204`
- **`DELETE /api/Appointments/{id}`** → `204`

### GroupClasses — `api/GroupClasses` · Auth: Authenticated
**`GET /api/GroupClasses`** — query: `branchId?`, `isActive?`, `category?`
```json
// Response: List<GroupClassDto>
[ { "id": "guid", "tenantId": "guid", "branchId": "guid", "branchName": "الفرع الرئيسي", "name": "يوجا الصباح",
    "description": "جلسة يوجا", "category": "Yoga", "durationMinutes": 60, "capacity": 20, "color": "#4CAF50",
    "imageUrl": null, "price": 100.00, "isActive": true, "upcomingSchedulesCount": 5 } ]
```
- **`POST /api/GroupClasses`** — `{ branchId, name, description, category, durationMinutes, capacity, color, imageUrl, price, isActive }` → `Guid`
- **`PUT /api/GroupClasses/{id}`** → `204` · **`DELETE /api/GroupClasses/{id}`** → `204`

### ClassSchedules (+ Enrollments) — `api/ClassSchedules` · Auth: Authenticated
**`GET /api/ClassSchedules`** — query: `groupClassId?`, `coachId?`, `roomId?`, `branchId?`, `fromDate?`, `toDate?`, `includeCancelled?`
```json
// Response: List<ClassScheduleDto>
[ { "id": "guid", "groupClassId": "guid", "groupClassName": "يوجا الصباح", "color": "#4CAF50", "coachId": "guid", "coachName": "كابتن سارة",
    "roomId": "guid", "roomName": "قاعة اليوجا", "startTime": "2026-07-09T08:00:00Z", "endTime": "2026-07-09T09:00:00Z",
    "recurrencePattern": 1, "recurrenceDaysOfWeek": "1,3,5", "recurrenceEndDate": "2026-08-31T00:00:00Z",
    "overrideCapacity": null, "effectiveCapacity": 20, "bookedCount": 12, "waitlistCount": 0, "isFull": false, "isCancelled": false, "cancellationReason": null } ]
```
- **`POST /api/ClassSchedules`** — `{ groupClassId, coachId, roomId, startTime, endTime, recurrencePattern, recurrenceDaysOfWeek, recurrenceEndDate, overrideCapacity }` → `Guid`
- **`POST /api/ClassSchedules/{id}/cancel`** — `{ reason }` → `204`
- **`GET /api/ClassSchedules/{id}/enrollments`** — query: `includeCancelled`(false) → `List<ClassEnrollmentDto>` (`{ id, scheduleId, clientId, clientName, enrolledAt, status, statusName, waitlistPosition, cancelledAt, attendedAt }`)
- **`POST /api/ClassSchedules/{id}/book`** — `{ clientId }` → `ClassEnrollmentDto`
- **`POST /api/ClassSchedules/enrollments/{enrollmentId}/cancel`** — `{ reason }` → `204`
- **`POST /api/ClassSchedules/enrollments/{enrollmentId}/attended`** → `204`

### Notifications — `api/Notifications` · Auth: Authenticated
**`GET /api/Notifications`** — query: `isRead?`, `type?`(NotificationType)
```json
// Response: List<NotificationDto>
[ { "id": "guid", "senderId": "guid", "senderName": "الإدارة", "recipientId": "guid", "recipientName": "محمد سمير",
    "title": "تذكير باشتراكك", "body": "ينتهي اشتراكك بعد 3 أيام", "type": 0, "isRead": false, "readAt": null, "createdAt": "2026-07-07T08:00:00Z" } ]
```
- **`GET /api/Notifications/unread-count`** → `int` (مثال: `5`)
- **`POST /api/Notifications`** — `{ recipientId, title, body, type }` → `Guid`
- **`POST /api/Notifications/bulk`** — `{ recipientIds[], title, body, type }` → `int` (العدد المرسَل)
- **`PUT /api/Notifications/{id}/read`** → `204`
- **`PUT /api/Notifications/read-all`** → `int` (العدد المحدَّث)

### Chat — `api/Chat` · Auth: Authenticated
- **`GET /api/Chat/conversations`** → `List<ConversationDto>`
```json
[ { "id": "guid", "coachId": "guid", "coachName": "كابتن أحمد", "clientId": "guid", "clientName": "محمد سمير",
    "lastMessageAt": "2026-07-07T20:00:00Z", "lastMessagePreview": "تمام، أراك غدًا", "unreadCount": 2 } ]
```
- **`GET /api/Chat/conversations/{conversationId}/messages`** → `List<ChatMessageDto>` (`{ id, conversationId, senderId, senderName, content, isRead, readAt, createdAt }`)
- **`POST /api/Chat/messages`** — `{ conversationId?, recipientId?(لبدء محادثة جديدة), content }` → `Guid`
- **`PUT /api/Chat/conversations/{conversationId}/read`** → `204`

### Challenges — `api/Challenges` · Auth: Authenticated
**`GET /api/Challenges`** — query: `status?`(ChallengeStatus)
```json
// Response: List<ChallengeDto>
[ { "id": "guid", "title": "تحدي 30 يوم", "description": "تمرين يومي لمدة شهر", "startDate": "2026-07-01T00:00:00Z",
    "endDate": "2026-07-31T00:00:00Z", "targetMetric": "Workouts", "targetValue": 30, "status": 1,
    "createdByCoachName": "كابتن أحمد", "participantCount": 40, "completedCount": 12 } ]
```
- **`GET /api/Challenges/{id}`** → `ChallengeDto`
- **`GET /api/Challenges/my`** → `List<ClientChallengeDto>` (`{ id, challengeId, challengeTitle, clientId, clientName, currentProgress, targetValue, isCompleted, completedAt, progressPercentage }`)
- **`POST /api/Challenges`** — `{ title, description, startDate, endDate, targetMetric, targetValue, clientIds[] }` → `Guid` (201)
- **`PUT /api/Challenges/{id}`** — `{ title, description, endDate, status }` → `204`
- **`DELETE /api/Challenges/{id}`** → `204`
- **`POST /api/Challenges/{id}/join`** → `Guid`
- **`PUT /api/Challenges/{challengeId}/progress`** — `{ progress }` → `204`

## Commerce, Finance & HR

### Sales (POS) — `api/Sales` · Auth: `ManagePOS`
**`GET /api/Sales`** — query: `branchId?`, `clientId?`, `cashierId?`, `paymentMethod?`(PaymentMethod), `fromDate?`, `toDate?`
```json
// Response: List<SaleDto>
[ { "id": "guid", "tenantId": "guid", "saleNumber": "SO-0001", "branchId": "guid", "branchName": "Main Branch",
    "clientId": "guid", "clientName": "John Doe", "cashierId": "guid", "cashierName": "Jane Smith",
    "saleDate": "2026-07-08T10:30:00Z", "subtotal": 100.00, "taxAmount": 15.00, "discountAmount": 5.00, "total": 110.00,
    "paymentMethod": "Cash", "paymentMethodName": "Cash", "invoiceId": "guid", "invoiceNumber": "INV-0001", "notes": "Walk-in",
    "items": [ { "id": "guid", "productId": "guid", "productName": "Protein Shake", "quantity": 2, "unitPrice": 50.00, "taxRate": 15.0, "discountAmount": 5.00, "lineTotal": 110.00 } ] } ]
```
**`POST /api/Sales/checkout`**
```json
// Request: CheckoutSaleCommand
{ "branchId": "guid", "clientId": "guid", "paymentMethod": "Cash", "couponId": "guid", "extraDiscount": 0, "notes": "Walk-in",
  "items": [ { "productId": "guid", "quantity": 1, "unitPriceOverride": null, "discountAmount": 0 } ] }
// Response: Guid
```

### Products — `api/Products` · Auth: `ManageInventory`
**`GET /api/Products`** — query: `categoryId?`, `isActive?`, `searchTerm?`, `lowStockOnly?`, `branchId?`
```json
// Response: List<ProductDto>
[ { "id": "guid", "tenantId": "guid", "categoryId": "guid", "categoryName": "Supplements", "name": "Protein Shake",
    "description": "Whey protein", "sku": "PRT-001", "barcode": "6221031000012", "costPrice": 30.00, "sellingPrice": 50.00,
    "taxRate": 15.0, "unit": "pcs", "imageUrl": "https://.../img.png", "isActive": true, "minStockLevel": 10, "trackStock": true, "totalStock": 42, "isLowStock": false } ]
```
- **`POST /api/Products`** — `{ categoryId, name, description, sku, barcode, costPrice, sellingPrice, taxRate, unit, imageUrl, isActive, minStockLevel, trackStock }` → `Guid`
- **`PUT /api/Products/{id}`** → `204` · **`DELETE /api/Products/{id}`** → `204`

### ProductCategories — `api/ProductCategories` · Auth: `ManageInventory`
**`GET /api/ProductCategories`** — query: `isActive?`
```json
// Response: List<ProductCategoryDto>
[ { "id": "guid", "tenantId": "guid", "name": "Supplements", "description": "Nutrition products", "parentCategoryId": "guid", "parentCategoryName": "Store", "imageUrl": "https://.../cat.png", "isActive": true, "productsCount": 12 } ]
```
- **`POST`** — `{ name, description, parentCategoryId, imageUrl, isActive }` → `Guid` · **`PUT /{id}`** → `204` · **`DELETE /{id}`** → `204`

### Stock — `api/Stock` · Auth: `ManageInventory`
**`GET /api/Stock`** — query: `branchId?`, `productId?`, `lowStockOnly?`
```json
// Response: List<StockItemDto>
[ { "id": "guid", "productId": "guid", "productName": "Protein Shake", "sku": "PRT-001", "branchId": "guid", "branchName": "Main Branch", "quantity": 42, "minStockLevel": 10, "isLowStock": false, "lastMovementAt": "2026-07-08T10:30:00Z" } ]
```
**`GET /api/Stock/movements`** — query: `productId?`, `branchId?`, `type?`(StockMovementType), `fromDate?`, `toDate?`
```json
// Response: List<StockMovementDto>
[ { "id": "guid", "productId": "guid", "productName": "Protein Shake", "branchId": "guid", "branchName": "Main Branch",
    "type": "Adjustment", "typeName": "Adjustment", "quantity": 5, "quantityAfter": 42, "reason": "Stock count correction",
    "referenceType": "Sale", "referenceId": "guid", "movedAt": "2026-07-08T10:30:00Z", "movedByName": "Jane Smith",
    "targetBranchId": "guid", "targetBranchName": "Second Branch" } ]
```
- **`POST /api/Stock/adjust`** — `{ productId, branchId, type, quantity, reason }` → `204`
- **`POST /api/Stock/transfer`** — `{ productId, fromBranchId, toBranchId, quantity, reason }` → `204`

### Suppliers — `api/Suppliers` · Auth: `ManageInventory`
**`GET /api/Suppliers`** — query: `isActive?`, `searchTerm?`
```json
// Response: List<SupplierDto>
[ { "id": "guid", "tenantId": "guid", "name": "Acme Supplies", "contactPerson": "John Doe", "phone": "+201000000000",
    "email": "supplier@example.com", "address": "123 Main St", "taxNumber": "TAX-12345", "notes": "Preferred", "isActive": true } ]
```
- **`POST`** → `Guid` · **`PUT /{id}`** → `204` · **`DELETE /{id}`** → `204` (نفس حقول الـ DTO)

### Coupons — `api/Coupons` · Auth: `ManageFinance`
**`GET /api/Coupons`** — query: `isActive?`, `search?`
```json
// Response: List<CouponDto>
[ { "id": "guid", "tenantId": "guid", "code": "SUMMER25", "description": "Summer discount", "discountType": 1, "discountTypeName": "Percentage",
    "discountValue": 25.0, "minimumAmount": 100.0, "maxDiscountAmount": 50.0, "maxUses": 100, "usedCount": 12, "maxUsesPerUser": 1,
    "startDate": "2026-06-01T00:00:00Z", "endDate": "2026-08-31T23:59:59Z", "applicableTo": 1, "applicableToName": "All",
    "isActive": true, "isExpired": false, "usesLimitReached": false } ]
```
**`POST /api/Coupons`** — `CreateCouponCommand` (`{ code, description, discountType, discountValue, minimumAmount, maxDiscountAmount, maxUses, maxUsesPerUser, startDate, endDate, applicableTo, isActive }`) → `Guid`
- **`PUT /{id}`** → `204` · **`DELETE /{id}`** → `204`
**`GET /api/Coupons/validate`** — query: `code`(req), `amount`(req), `context?`(CouponApplicability)
```json
// Response: ValidateCouponResultDto
{ "isValid": true, "errorMessage": null, "coupon": { /* CouponDto */ }, "estimatedDiscount": 25.0 }
```

### TaxSettings — `api/TaxSettings` · Auth: `ManageSettings`
**`GET /api/TaxSettings`** — query: `isActive?` → `List<TaxSettingDto>` (`{ id, tenantId, name, rate, isDefault, isActive, description }`)
- **`POST`** — `{ name, rate, isDefault, isActive, description }` → `Guid` · **`PUT /{id}`** → `204` · **`DELETE /{id}`** → `204`

### Invoices — `api/Invoices` · Auth: `ManageFinance`
**`GET /api/Invoices`** — query: `clientId?`, `branchId?`, `status?`(InvoiceStatus), `fromDate?`, `toDate?`
```json
// Response: List<InvoiceDto>
[ { "id": "guid", "tenantId": "guid", "invoiceNumber": "INV-2026-0001", "clientId": "guid", "clientName": "Ahmed Ali",
    "branchId": "guid", "branchName": "Main Branch", "issueDate": "2026-07-01T10:00:00Z", "dueDate": "2026-07-15T00:00:00Z",
    "subtotal": 200.0, "taxAmount": 30.0, "discountAmount": 20.0, "total": 210.0, "amountPaid": 100.0, "remainingAmount": 110.0,
    "status": 3, "statusName": "PartiallyPaid", "couponId": "guid", "couponCode": "SUMMER25", "notes": "First invoice", "pdfUrl": null,
    "items": [ { "id": "guid", "itemType": 1, "itemTypeName": "Subscription", "referenceId": "guid", "description": "Monthly subscription",
      "quantity": 1, "unitPrice": 200.0, "taxRate": 15.0, "discountAmount": 20.0, "lineTotal": 210.0 } ],
    "payments": [ { "id": "guid", "amount": 100.0, "method": 0, "receivedAt": "2026-07-02T09:00:00Z", "receiptNumber": "RCPT-0001" } ] } ]
```
**`GET /api/Invoices/{id}`** → `InvoiceDto` أو `404`.
**`POST /api/Invoices`**
```json
// Request: CreateInvoiceCommand
{ "clientId": "guid", "branchId": "guid", "issueDate": "2026-07-01T10:00:00Z", "dueDate": "2026-07-15T00:00:00Z", "couponId": "guid", "notes": "First invoice",
  "items": [ { "itemType": 5, "referenceId": null, "description": "Manual line item", "quantity": 1, "unitPrice": 200.0, "taxRate": 15.0, "discountAmount": 20.0 } ], "issueImmediately": true }
// Response: Guid
```
- **`POST /api/Invoices/{id}/issue`** → `204`
- **`POST /api/Invoices/{id}/cancel`** — `{ reason }` → `204`

### Payments — `api/Payments` · Auth: `ManageFinance`
**`GET /api/Payments`** — query: `clientId?`, `branchId?`, `invoiceId?`, `subscriptionId?`, `method?`(PaymentMethod), `fromDate?`, `toDate?`
```json
// Response: List<PaymentDto>
[ { "id": "guid", "tenantId": "guid", "invoiceId": "guid", "invoiceNumber": "INV-2026-0001", "subscriptionId": null,
    "branchId": "guid", "branchName": "Main Branch", "clientId": "guid", "clientName": "Ahmed Ali", "amount": 100.0,
    "method": 0, "methodName": "Cash", "receivedAt": "2026-07-02T09:00:00Z", "receivedByName": "Reception User",
    "receiptNumber": "RCPT-0001", "notes": "Partial payment", "referenceNumber": "TXN-12345" } ]
```
**`POST /api/Payments`** — `RecordPaymentCommand` (`{ invoiceId, subscriptionId, clientId, branchId, amount, method, receivedAt, receiptNumber, notes, referenceNumber }`) → `Guid`

### Transactions (Wallet) — `api/Transactions` · Auth: `ManageFinance`
**`GET /api/Transactions`** — query: `userId?`, `type?`(TransactionType), `fromDate?`, `toDate?`
```json
// Response: List<TransactionDto>
[ { "id": "guid", "tenantId": "guid", "userId": "guid", "userName": "Ahmed Ali", "type": 0, "typeName": "Deposit",
    "amount": 500.0, "balanceAfter": 500.0, "description": "Wallet top-up", "referenceType": "Invoice", "referenceId": "guid",
    "createdAt": "2026-07-01T08:00:00Z", "createdBy": "Reception User" } ]
```
**`GET /api/Transactions/summary`** — query: `userId?`, `fromDate?`, `toDate?` → `TransactionSummaryDto` (`{ totalDeposits, totalWithdrawals, totalPayments, totalRefunds, netBalance, transactionCount }`)
**`GET /api/Transactions/{id}`** → `TransactionDto` أو `404`.
- **`POST /api/Transactions`** — `{ userId, type, amount, description, referenceType, referenceId }` → `Guid` (201)
- **`DELETE /api/Transactions/{id}`** → `204` (أو `404`)

### Expenses — `api/Expenses` · Auth: `ManageFinance`
**`GET /api/Expenses`** — query: `branchId?`, `categoryId?`, `fromDate?`, `toDate?`, `searchTerm?`
```json
// Response: List<ExpenseDto>
[ { "id": "guid", "tenantId": "guid", "branchId": "guid", "branchName": "Main Branch", "categoryId": "guid", "categoryName": "Utilities",
    "amount": 1500.00, "expenseDate": "2026-07-08T00:00:00", "description": "Electricity bill", "vendorName": "Power Co.",
    "paymentMethod": "Cash", "paymentMethodName": "Cash", "receiptImageUrl": "https://.../receipt.jpg", "referenceNumber": "INV-1023",
    "approvedById": "guid", "approvedByName": "Admin User", "approvedAt": "2026-07-08T10:30:00" } ]
```
- **`POST /api/Expenses`** — `{ branchId, categoryId, amount, expenseDate, description, vendorName, paymentMethod, receiptImageUrl, referenceNumber }` → `Guid`
- **`PUT /api/Expenses/{id}`** → `204` · **`DELETE /api/Expenses/{id}`** → `204`

### ExpenseCategories — `api/ExpenseCategories` · Auth: `ManageFinance`
**`GET`** — query: `isActive?` → `List<ExpenseCategoryDto>` (`{ id, tenantId, name, description, parentCategoryId, parentCategoryName, isActive, childrenCount }`)
- **`POST`** — `{ name, description, parentCategoryId, isActive }` → `Guid` · **`PUT /{id}`** → `204` · **`DELETE /{id}`** → `204`

### Commissions — `api/Commissions` · Auth: `ManageFinance`
**`GET /api/Commissions`** — query: `employeeId?`, `status?`(CommissionStatus), `sourceType?`(CommissionSourceType), `fromDate?`, `toDate?`
```json
// Response: List<CommissionDto>
[ { "id": "guid", "employeeId": "guid", "employeeName": "Jane Coach", "sourceType": "Membership", "sourceTypeName": "Membership",
    "referenceId": "guid", "amount": 120.00, "sourceAmount": 1200.00, "earnedDate": "2026-07-08T00:00:00", "status": "Pending",
    "statusName": "Pending", "payrollItemId": null, "description": "10% of membership sale" } ]
```
**`GET /api/Commissions/rules`** — query: `isActive?` → `List<CommissionRuleDto>` (`{ id, employeeId, employeeName, role, sourceType, type, value, minAmount, isActive }`)
- **`POST /api/Commissions/rules`** — `{ employeeId, role, sourceType, type, value, minAmount, isActive }` → `Guid`

### Employees — `api/Employees` · Auth: `ManageEmployees`
**`GET /api/Employees`** — query: `branchId?`, `department?`, `isActive?`, `searchTerm?`
```json
// Response: List<EmployeeDto>
[ { "id": "guid", "userId": "guid", "email": "employee@example.com", "phoneNumber": "+201234567890", "role": 2,
    "employeeCode": "EMP-001", "jobTitle": "Head Coach", "department": "Training", "joinDate": "2025-01-15T00:00:00Z", "terminationDate": null,
    "baseSalary": 8000.00, "salaryType": 1, "hourlyRate": null, "bankAccount": "EG123456789", "bankName": "Bank Misr",
    "nationalId": "29001011234567", "emergencyContactName": "Ahmed Ali", "emergencyContactPhone": "+201111111111",
    "qualifications": "Certified PT Level 3", "branchIds": ["guid"], "isActive": true } ]
```
> `isActive` محسوب (`true` عند `terminationDate == null`). الموظف يُنشأ لمستخدم **موجود** (`userId`).
- **`POST /api/Employees`** — `CreateEmployeeCommand` (`{ userId, employeeCode, jobTitle, department, joinDate, baseSalary, salaryType, hourlyRate, bankAccount, bankName, nationalId, emergencyContactName, emergencyContactPhone, qualifications, branchIds[] }`) → `Guid`
- **`PUT /api/Employees/{id}`** — `UpdateEmployeeCommand` → `204`
- **`POST /api/Employees/{id}/terminate`** — `{ terminationDate, reason }` → `204`

### Payroll — `api/Payroll` · Auth: `ManageEmployees`
**`GET /api/Payroll`** — query: `year?`, `month?`, `branchId?`, `status?`(PayrollStatus)
```json
// Response: List<PayrollRunDto>
[ { "id": "guid", "branchId": "guid", "branchName": "Main Branch", "month": 7, "year": 2026, "status": 1, "statusName": "Draft",
    "totalAmount": 45000.00, "approvedAt": null, "paidAt": null, "notes": null, "itemsCount": 6,
    "items": [ { "id": "guid", "employeeId": "guid", "employeeName": "Ahmed Coach", "baseSalary": 8000.00, "commissionTotal": 1200.00,
      "bonus": 500.00, "deductions": 200.00, "netSalary": 9500.00, "paidAt": null, "notes": null } ] } ]
```
- **`POST /api/Payroll/generate`** — `{ month, year, branchId }` → `Guid`
- **`PUT /api/Payroll/items/{id}`** — `{ bonus, deductions, notes }` → `204`
- **`POST /api/Payroll/{id}/approve`** → `204` · **`POST /api/Payroll/{id}/pay`** → `204`

### Shifts — `api/Shifts` · Auth: `ManageEmployees`
**`GET /api/Shifts`** — query: `branchId?`, `isActive?` → `List<ShiftDto>` (`{ id, branchId, branchName, name, startTime, endTime, color, isActive }`)
- **`POST /api/Shifts`** — `{ branchId, name, startTime, endTime, color, isActive }` → `Guid`
- **`POST /api/Shifts/assign`** — `{ shiftId, employeeId, date, notes }` → `Guid`
- **`GET /api/Shifts/assignments`** — query: `employeeId?`, `shiftId?`, `fromDate?`, `toDate?` → `List<ShiftAssignmentDto>` (`{ id, shiftId, shiftName, employeeId, employeeName, date, actualCheckIn, actualCheckOut, notes }`)

### Leaves — `api/Leaves` · Auth: `ManageEmployees`
**`GET /api/Leaves`** — query: `employeeId?`, `status?`(LeaveStatus), `leaveType?`(LeaveType), `fromDate?`, `toDate?`
```json
// Response: List<LeaveRequestDto>
[ { "id": "guid", "employeeId": "guid", "employeeName": "Ahmed Coach", "fromDate": "2026-07-10T00:00:00Z", "toDate": "2026-07-14T00:00:00Z",
    "durationDays": 5, "leaveType": 1, "leaveTypeName": "Annual", "reason": "Family vacation", "status": 1, "statusName": "Pending",
    "reviewedById": null, "reviewedByName": null, "reviewedAt": null, "reviewNotes": null } ]
```
- **`POST /api/Leaves`** — `{ employeeId, fromDate, toDate, leaveType, reason }` → `Guid`
- **`POST /api/Leaves/{id}/review`** — `{ decision (2=Approved, 3=Rejected), notes }` → `204`

## Reports & Settings (Tenant)

### Reports — `api/reports` · Auth: `ViewReports`
كل الـ endpoints `GET` وترجّع DTO تقرير:

**`GET /api/reports/dashboard`**
```json
// DashboardReportDto
{ "totalClients": 320, "activeClients": 210, "newClientsThisMonth": 24, "totalCoaches": 12,
  "activeSubscriptions": 180, "expiringSubscriptions": 15, "totalRevenueThisMonth": 45200.00, "totalRevenueLastMonth": 39800.00,
  "totalWorkoutsThisMonth": 640, "totalDietPlansActive": 95 }
```
**`GET /api/reports/clients`** — query: `fromDate?`, `toDate?`
```json
// ClientsReportDto
{ "totalClients": 320, "activeClients": 210, "inactiveClients": 110, "newClientsThisMonth": 24,
  "clientsWithActiveSubscription": 180, "clientsWithoutSubscription": 140,
  "topClients": [ { "id": "guid", "name": "Ahmed Ali", "phoneNumber": "0100...", "totalSessions": 48, "totalPaid": 5400.00 } ],
  "monthlyTrend": [ { "month": "2026-06", "newClients": 30, "churnedClients": 8 } ] }
```
**`GET /api/reports/subscriptions`** — query: `fromDate?`, `toDate?`
```json
// SubscriptionsReportDto
{ "totalSubscriptions": 500, "activeSubscriptions": 180, "suspendedSubscriptions": 12, "trialSubscriptions": 8,
  "expiredSubscriptions": 260, "cancelledSubscriptions": 40, "expiringIn7Days": 15, "expiringIn30Days": 60,
  "totalRevenue": 320000.00, "revenueThisMonth": 45200.00, "renewalRate": 62.5, "averageSubscriptionDurationDays": 92.3,
  "planStatistics": [ { "planId": "guid", "planName": "Monthly", "activeCount": 120, "totalSold": 400, "totalRevenue": 220000.00 } ],
  "monthlyRevenue": [ { "month": "2026-06", "revenue": 39800.00, "subscriptionCount": 150 } ],
  "revenueByPaymentMethod": [ { "paymentMethod": "Cash", "count": 300, "totalRevenue": 180000.00 } ] }
```
**`GET /api/reports/financial`** — query: `fromDate?`, `toDate?`
```json
// FinancialReportDto
{ "totalRevenue": 320000.00, "revenueThisMonth": 45200.00, "revenueLastMonth": 39800.00, "growthPercentage": 13.5,
  "averageSubscriptionValue": 640.00, "totalWalletBalance": 12500.00,
  "monthlyRevenue": [ { "month": "2026-06", "revenue": 39800.00, "subscriptionCount": 150 } ],
  "paymentMethods": [ { "paymentMethod": "Cash", "count": 300, "totalAmount": 180000.00 } ] }
```
**`GET /api/reports/coach/dashboard`** — query: `coachId?` → `CoachDashboardReportDto` (totalTrainees, activeTrainees, programs, dietPlans, sessions, volume, topTraineesByProgress[], topTraineesBySessions[])
**`GET /api/reports/coach/trainees`** — query: `coachId?` → `CoachTraineesReportDto` (`{ totalTrainees, withActiveSubscription, withoutSubscription, trainees:[{ clientId, name, phone, email, assignedAt, hasActiveSubscription, subscriptionEndDate, activeWorkoutPrograms, activeDietPlans, totalSessions, sessionsThisMonth, lastSessionDate, currentWeight, weightChange, bodyFatPercent, lastMeasurementDate }] }`)
**`GET /api/reports/coach/trainee/{clientId}`** → `TraineeProgressReportDto` (bodyMeasurements[], weight/bodyFat/muscle start-current-change, totalSessions, totalVolumeLifted, monthlySessions[], workoutPrograms[], dietPlans[], personalRecords[])
**`GET /api/reports/operations-dashboard`** → `OperationsDashboardDto` (activeMembers, todayCheckIns, currentlyInsideCount, expiring/expired subscriptions, month revenue/expenses/profit, today revenue/expenses, lowStockProductsCount, equipmentUnderMaintenanceCount, pendingLeaveRequestsCount, unpaidInvoices, branchKpis[])
**`GET /api/reports/expenses`** — query: `fromDate?`, `toDate?`, `branchId?` → `ExpensesReportDto` (byCategory[], byBranch[], byMonth[])
**`GET /api/reports/pos-sales`** — query: `fromDate?`, `toDate?`, `branchId?`, `topProductsCount`(10) → `PosSalesReportDto` (topProducts[], byCashier[], byBranch[], byPaymentMethod[])
**`GET /api/reports/stock-valuation`** — query: `branchId?` → `StockValuationReportDto` (totalCostValue, totalRetailValue, potentialProfit, products[])
**`GET /api/reports/payroll-summary`** — query: `year?`, `month?` → `PayrollSummaryReportDto` (totalBaseSalaries, totalCommissions, totalBonuses, totalDeductions, totalNetSalaries, byBranch[])
**`GET /api/reports/class-attendance`** — query: `fromDate?`, `toDate?`, `branchId?` → `ClassAttendanceReportDto` (attendanceRatePercent, averageFillRatePercent, topClasses[], coachStats[])
**`GET /api/reports/equipment-utilization`** — query: `branchId?` → `EquipmentUtilizationReportDto` (status counts, purchase/maintenance value, mostCostlyEquipment[], byBranch[])
**`GET /api/reports/branch-comparison`** — query: `fromDate?`, `toDate?` → `BranchComparisonReportDto` (branches[] بمقارنة الإيراد/الربح/الأعضاء)
**`GET /api/reports/commissions`** — query: `fromDate?`, `toDate?`, `employeeId?` → `CommissionReportDto` (totalEarned/Paid/Pending, byEmployee[], bySource[])

### Subscriptions (اشتراكات العملاء داخل الجيم) — `api/subscriptions` · Auth: `ManageClientSubscriptions`
> **تنبيه**: دي اشتراكات **العميل في الجيم** (خطط/تجميد/دفعات) — غير اشتراك **الجيم في المنصة** (قسم SaaS Billing).
- **`GET /api/subscriptions/plans`** — query: `isActive?`
```json
// List<SubscriptionPlanDto>
[ { "id": "guid", "tenantId": "guid", "name": "Monthly", "price": 600.00, "durationMonths": 1, "description": "Standard",
    "features": ["Gym access","1 InBody"], "maxFreezeDays": 7, "maxFreezeCount": 1, "isActive": true,
    "sessionsPerWeek": 3, "inBodyIncluded": true, "privateCoach": false, "activeSubscribersCount": 120 } ]
```
- **`GET /api/subscriptions/plans/{id}`** → `SubscriptionPlanDto` أو `404`
- **`POST /api/subscriptions/plans`** — `CreateSubscriptionPlanCommand` → `Guid` · **`PUT /plans/{id}`** → `204` · **`DELETE /plans/{id}`** → `204`
- **`GET /api/subscriptions`** — query: `clientId?`, `status?`, `planId?`, `expiringWithinDays?`, `searchTerm?`
```json
// List<ClientSubscriptionDto>
[ { "id": "guid", "tenantId": "guid", "clientId": "guid", "clientName": "Sara", "planId": "guid", "planName": "Monthly",
    "startDate": "2026-06-01T00:00:00", "endDate": "2026-07-01T00:00:00", "status": 1, "statusName": "Active",
    "salesCoachId": "guid", "salesCoachName": "Youssef", "paymentMethod": 0, "paymentMethodName": "Cash",
    "totalAmount": 600.00, "amountPaid": 600.00, "remainingAmount": 0.00, "discount": 0.00, "isPaid": true, "notes": null,
    "renewedFromId": null, "remainingDays": 12, "totalFreezeDays": 0,
    "freezes": [ { "id": "guid", "subscriptionId": "guid", "startDate": "2026-06-10T00:00:00", "endDate": "2026-06-15T00:00:00", "reason": "Travel", "isActive": false, "durationDays": 5 } ] } ]
```
- **`GET /api/subscriptions/{id}`** → `ClientSubscriptionDetailDto` (+ planDetails, clientPhone/Email, renewalHistory[])
- **`GET /api/subscriptions/expiring`** — query: `days`(7) → `List<ClientSubscriptionDto>`
- **`POST /api/subscriptions`** — `{ clientId, planId, startDate, paymentMethod, amountPaid, discount, notes, payFromWallet }` → `Guid`
- **`PUT /api/subscriptions/{id}`** — `{ endDate, notes, amountPaid, discount }` → `204`
- **`POST /api/subscriptions/{id}/renew`** — `RenewSubscriptionCommand` → `Guid`
- **`POST /api/subscriptions/{id}/payment`** — `{ amount, paymentMethod, payFromWallet }` → `204`
- **`POST /api/subscriptions/{id}/freeze`** — `{ startDate, endDate, reason }` → `Guid`
- **`POST /api/subscriptions/{id}/cancel`** — `{ refundToWallet, refundAmount }` → `204`
- **`POST /api/subscriptions/freezes/{freezeId}/end`** → `204`

### GymProfile — `api/gymprofile` · Auth: `ManageSettings`
**`GET /api/gymprofile`** → `GymProfileDto`
```json
{ "id": "guid", "name": "PowerGym", "subdomain": "powergym", "description": "Best gym", "address": "Cairo", "phoneNumber": "0100...", "email": "info@powergym.com",
  "logoUrl": "https://.../logo.png", "coverImageUrl": "https://.../cover.png", "galleryImages": ["https://.../1.jpg"], "status": "Active",
  "brandingSettings": { "primaryColor": "#FF5722", "secondaryColor": "#212121", "logoUrl": "https://.../logo.png" },
  "statistics": { "totalClients": 320, "activeClients": 210, "totalCoaches": 12, "totalSubscriptionPlans": 5, "activeSubscriptions": 180 } }
```
**`PUT /api/gymprofile`**
```json
// UpdateGymProfileCommand
{ "name": "PowerGym", "description": "Best gym", "address": "Cairo", "phoneNumber": "0100...", "email": "info@powergym.com",
  "logoUrl": "...", "coverImageUrl": "...", "galleryImages": ["..."], "primaryColor": "#FF5722", "secondaryColor": "#212121",
  "appName": "PowerGym", "fontFamily": "Cairo", "customCss": null, "invoiceLogoUrl": null, "supportPhone": "0100...", "supportEmail": "support@powergym.com", "customDomain": null }
// Response: 204
```
- **`POST /api/gymprofile/logo`** — `multipart file` → `{ "url": "..." }`
- **`POST /api/gymprofile/cover`** — `multipart file` → `{ "url": "..." }`
- **`POST /api/gymprofile/gallery`** — `multipart files[]` → `{ "urls": ["...","..."] }`

### Users — `api/users` · Auth: `ManageSettings`
**`GET /api/users`** — query: `role?`(UserRole), `isActive?`, `searchTerm?` → `List<UserDto>`
- **`GET /api/users/{id}`** → `UserDto` أو `404`
- **`POST /api/users/staff`** — إنشاء موظف إداري
```json
// CreateStaffUserCommand (role: Manager=4 / Receptionist=5 / Accountant=6 / Trainer=7)
{ "phoneNumber": "0100...", "email": "mona@powergym.com", "password": null, "fullName": "Mona Adel", "role": 5 }
// Response: 201 + Guid
```
- **`PUT /api/users/{id}`** — `{ phoneNumber, isActive }` → `204`
- **`PUT /api/users/{id}/profile`** — `UpdateUserProfileCommand` (`{ fullName, gender, birthDate, heightCm, weightKg, activityLevel, fitnessGoal, medicalHistory }`) → `204`

## SaaS Subscription & Billing (Tenant) — `/api/tenant` · Auth: `ManageTenantBilling`

اشتراك **الجيم في المنصة** (دفع يدوي). المسار الكامل:
```
GET /plans → POST /subscription/select-plan → GET /payment-methods → [دفع خارجي]
→ POST /payment-requests (رفع الإثبات) → [الأدمن يوافق] → GET /my-subscription (Active) → GET /invoices
```

- **`GET /api/tenant/plans`** → `List<PlanDto>` (نفس شكل PlanDto في Platform API)
- **`GET /api/tenant/my-subscription`** → `MySubscriptionDto`
```json
{ "hasSubscription": true, "subscriptionId": "guid", "planId": "guid", "planName": "Professional", "status": 3,
  "startDate": "2026-07-08T...", "endDate": "2026-08-07T...", "trialEndsAt": null, "remainingDays": 30,
  "amount": 499.00, "currency": "EGP", "autoRenew": false, "features": ["POS","Inventory","..."],
  "members": { "used": 128, "limit": 500 }, "coaches": { "used": 4, "limit": 10 },
  "branches": { "used": 2, "limit": 3 }, "employees": { "used": 7, "limit": 20 } }
```
- **`GET /api/tenant/usage`** → `{ members, coaches, branches, employees }` كل واحد `{ used, limit }`
- **`GET /api/tenant/invoices`** → `List<SubscriptionInvoiceDto>` (`{ id, invoiceNumber, amount, currency, status, issueDate, dueDate, paidAt }`)
- **`GET /api/tenant/payment-methods`** → `List<PaymentMethodDto>` (طرق الدفع النشطة)
- **`POST /api/tenant/subscription/select-plan`** — `{ planId }` → `TenantSubscriptionSummaryDto` (`{ subscriptionId, planId, planName, status, amount, currency }`)
- **`POST /api/tenant/subscription/upgrade`** — `{ planId }` → `TenantSubscriptionSummaryDto`
- **`POST /api/tenant/subscription/renew`** — (بلا body) → `TenantSubscriptionSummaryDto`
- **`POST /api/tenant/payment-requests`** — `multipart/form-data`: `planId`(req), `proof`(صورة), `paymentMethodId?`, `transactionNumber?`, `paymentDate?`, `notes?` → `PaymentRequestDto`
- **`GET /api/tenant/payment-requests`** → `List<PaymentRequestDto>`

> تجاوز حد الباقة عند الإنشاء يرجّع **`402 Payment Required`** (Members/Coaches/Branches/Employees + ميزة EmployeeManagement).

## Platform API — `{{PLATFORM_API}}/api/platform/...`

### PlatformAuth — `/api/platform/auth`
**`POST /login`** — Anonymous
```json
// Request: PlatformLoginCommand
{ "email": "owner@logicfit.io", "password": "P@ssw0rd" }
// Response: AuthResponseDto (بلا subdomain، صلاحيات منصة)
{ "userId": "guid", "email": "owner@logicfit.io", "phoneNumber": "0100...", "fullName": "Platform Owner",
  "role": "PlatformOwner", "roles": ["PlatformOwner"], "permissions": ["ManagePlatform","ManageTenants","ManagePlans","ManagePaymentRequests","ManagePlatformReports"],
  "tenantId": "00000000-0000-0000-0000-0000000000a1", "accessToken": "eyJhbGci...", "refreshToken": "def502...", "expiresAt": "2026-07-08T12:15:00Z" }
```
- **`POST /refresh`** — Anonymous — `{ refreshToken }` → `AuthResponseDto`
- **`POST /logout-all`** — Authenticated → `204`

### PlatformTenants — `/api/platform/tenants` · Auth: `ManageTenants`
**`GET /api/platform/tenants`** — query: `status?`(TenantStatus)
```json
// List<PlatformTenantDto>
[ { "id": "guid", "name": "PowerGym", "subdomain": "powergym", "status": 1, "email": "info@powergym.com", "phoneNumber": "0100...", "membersCount": 320, "createdAt": "2026-01-01T00:00:00" } ]
```
**`POST /api/platform/tenants`** — إنشاء جيم + Owner
```json
// CreateTenantWithOwnerCommand
{ "name": "PowerGym", "subdomain": "powergym", "email": "info@powergym.com", "phoneNumber": "0100...",
  "ownerEmail": "owner@powergym.com", "ownerPhoneNumber": "0100...", "ownerPassword": "P@ssw0rd", "ownerFullName": "Ali Owner" }
// Response: 201 + PlatformTenantDto (status=6 PendingApproval)
```
- **`POST /api/platform/tenants/{id}/approve`** → `PlatformTenantDto` (status→Active)
- **`POST /api/platform/tenants/{id}/suspend`** → status→Suspended
- **`POST /api/platform/tenants/{id}/activate`** → status→Active
- **`POST /api/platform/tenants/{id}/archive`** → status→Archived

### PlatformPlans — `/api/platform/plans` · Auth: `ManagePlans`
**`GET`** — query: `activeOnly`(false)
```json
// List<PlanDto>
[ { "id": "guid", "name": "Pro", "description": "Full features", "price": 1200.00, "currency": "EGP",
    "billingCycle": 1, "durationInDays": 30, "maxMembers": 1000, "maxCoaches": 20, "maxBranches": 5, "maxEmployees": 50, "maxStorageMB": 10240,
    "isActive": true, "displayOrder": 1, "features": ["reports","pos","hr"] } ]
```
**`POST`** — `CreatePlanCommand` (`{ name, description, price, currency, billingCycle, durationInDays, maxMembers, maxCoaches, maxBranches, maxEmployees, maxStorageMB, isActive, displayOrder, featureCodes[] }`) → `201` + `PlanDto`
- **`PUT /{id}`** — `UpdatePlanCommand` → `PlanDto`
- **`DELETE /{id}`** → `204` (يمنع حذف باقة عليها اشتراكات فعّالة → `409`)

### PlatformFeatures — `/api/platform/features` · Auth: `ManagePlans`
**`GET`** → `List<FeatureDto>` (`{ id, code, name, description, isActive }`)

### PlatformPaymentMethods — `/api/platform/payment-methods` · Auth: `ManagePaymentRequests`
**`GET`** — query: `activeOnly`(false)
```json
// List<PaymentMethodDto>
[ { "id": "guid", "name": "InstaPay", "type": "Wallet", "accountName": "LogicFit", "accountNumber": null, "iban": null,
    "walletNumber": "0100...", "instructions": "Send to wallet", "qrImageUrl": "https://.../qr.png", "isActive": true, "displayOrder": 1 } ]
```
**`POST`** — `SavePaymentMethodCommand` → `201` + `PaymentMethodDto` · **`PUT /{id}`** → `PaymentMethodDto` · **`DELETE /{id}`** → `204`

### PlatformPaymentRequests — `/api/platform/payment-requests` · Auth: `ManagePaymentRequests`
**`GET`** — query: `status?`(PaymentRequestStatus)
```json
// List<PaymentRequestDto>
[ { "id": "guid", "tenantId": "guid", "tenantName": "PowerGym", "planId": "guid", "planName": "Pro", "tenantSubscriptionId": "guid",
    "amount": 1200.00, "currency": "EGP", "paymentMethodId": "guid", "transactionNumber": "TX123", "paymentDate": "2026-07-01T00:00:00",
    "proofFileUrl": "https://.../proof.png", "notes": null, "status": 1, "reviewedBy": null, "reviewedAt": null, "rejectReason": null, "createdAt": "2026-07-01T09:00:00" } ]
```
- **`POST /{id}/approve`** → `PaymentRequestDto` (يفعّل الاشتراك + الجيم Active + ينشئ دفعة وفاتورة، transaction ذرّية)
- **`POST /{id}/reject`** — `{ rejectReason }` → `PaymentRequestDto`

### PlatformSubscriptions — `/api/platform/subscriptions` · Auth: `ManageTenants`
**`GET`** — query: `status?`(TenantSubscriptionStatus)
```json
// List<PlatformSubscriptionDto>
[ { "id": "guid", "tenantId": "guid", "tenantName": "PowerGym", "planId": "guid", "planName": "Pro", "status": 3,
    "startDate": "2026-01-01T00:00:00", "endDate": "2026-02-01T00:00:00", "trialEndsAt": null, "amount": 1200.00, "currency": "EGP", "autoRenew": true } ]
```

### PlatformDashboard — `/api/platform/dashboard` · Auth: `ManagePlatformReports`
**`GET`** → `PlatformDashboardDto`
```json
{ "totalGyms": 42, "activeGyms": 30, "trialGyms": 5, "pendingApprovalGyms": 3, "suspendedGyms": 4, "totalMembers": 8600 }
```

---

# 6. Enums Reference

القيم الرقمية كما تُرسَل/تُستقبَل في الـ JSON (كثير من الـ DTOs تضيف حقل `*Name` نصي بجانب القيمة):

```
UserRole:                  Owner=1, Coach=2, Client=3, Manager=4, Receptionist=5, Accountant=6, Trainer=7, PlatformOwner=8, PlatformAdmin=9
GenderType:                Male=1, Female=2
SubscriptionStatus:        Active=1, Suspended=2, Trial=3, Expired=4, Cancelled=5           (اشتراك العميل في الجيم)
PlanStatus:                Active=1, Archived=2, Draft=3
PaymentMethod:             Cash=0, Wallet=1, Card=2, BankTransfer=3
TransactionType:           Deposit=0, Withdrawal=1, Payment=2, Refund=3, Adjustment=4
NotificationType:          General=1, WorkoutAssigned=2, DietPlanAssigned=3, SubscriptionExpiring=4, Custom=5
AppointmentStatus:         Pending=1, Confirmed=2, Cancelled=3, Completed=4
ChallengeStatus:           Active=1, Completed=2, Expired=3
GateAccessResult:          Granted=1, Denied=2
GateAccessMethod:          Manual=1, Qr=2, Card=3, Face=4, Fingerprint=5
GateDenyReason:            None=0, NoActiveSubscription=1, SessionsPerWeekExceeded=2, BranchCapacityFull=3, OutsideOperatingHours=4,
                           SubscriptionFrozen=5, SubscriptionExpired=6, AlreadyCheckedIn=7, CardInactive=8, CardExpired=9,
                           BranchAccessDenied=10, ClientNotFound=11, BranchInactive=12
RoomType:                  Cardio=1, Weights=2, FreeWeights=3, Studio=4, Cycling=5, Crossfit=6, Pool=7, Boxing=8, Stretching=9, LockerRoom=10, Reception=11, Other=99
EquipmentStatus:           Active=1, UnderMaintenance=2, OutOfService=3, Retired=4
MaintenanceStatus:         Pending=1, InProgress=2, Completed=3, Cancelled=4
InvoiceStatus:             Draft=1, Issued=2, PartiallyPaid=3, Paid=4, Overdue=5, Cancelled=6
InvoiceItemType:           Subscription=1, Product=2, Class=3, PersonalTraining=4, Manual=5, Other=99
DiscountType:              Percentage=1, Fixed=2
CouponApplicability:       All=1, Subscriptions=2, Products=3, Classes=4, PersonalTraining=5
ClassEnrollmentStatus:     Booked=1, Attended=2, Cancelled=3, NoShow=4, Waitlist=5
RecurrencePattern:         None=0, Daily=1, Weekly=2, Monthly=3
StockMovementType:         In=1, Out=2, Adjustment=3, Transfer=4
PurchaseOrderStatus:       Draft=1, Submitted=2, Received=3, PartiallyReceived=4, Cancelled=5
SalaryType:                Monthly=1, Hourly=2, Daily=3
LeaveType:                 Annual=1, Sick=2, Unpaid=3, Maternity=4, Emergency=5, Other=99
LeaveStatus:               Pending=1, Approved=2, Rejected=3, Cancelled=4
CommissionSourceType:      SubscriptionSale=1, ProductSale=2, PersonalTraining=3, Manual=99
CommissionStatus:          Pending=1, Approved=2, Paid=3, Cancelled=4
CommissionRuleType:        Percentage=1, Fixed=2
PayrollStatus:             Draft=1, Approved=2, Paid=3, Cancelled=4
AuditAction:               Create=1, Update=2, Delete=3
--- SaaS (Platform) ---
TenantStatus:              Active=1, Suspended=2, Trial=3, PastDue=4, Cancelled=5, PendingApproval=6, Archived=7, Deleted=8
TenantSubscriptionStatus:  PendingPayment=1, Trial=2, Active=3, PastDue=4, Suspended=5, Cancelled=6, Expired=7
BillingCycle:              Monthly=1, Quarterly=2, Annual=3
PaymentRequestStatus:      Pending=1, Approved=2, Rejected=3, Cancelled=4, Expired=5
SubscriptionInvoiceStatus: Unpaid=1, PendingReview=2, Paid=3, Cancelled=4, Overdue=5
```

---

# 7. قواعد العمل Business Logic

## المصادقة والصلاحيات
- **دخول الجيم** بالهاتف + كلمة السر + `subdomain` (أو `tenantId`/`X-Tenant-Id`). **دخول المنصة** بالإيميل + كلمة السر.
- **التسجيل العام** ينشئ **Client فقط** (لا يقبل دوراً من الطلب — منع تصعيد الصلاحيات).
- **كل مستخدم جديد** (Client/Coach/Staff/Owner) يُعيَّن له **دور RBAC** وقت الإنشاء → صلاحياته تظهر في التوكن من أول دخول.
- **JWT**: access token 15 دقيقة يحمل `permission` claims + `perm_ver`. **Refresh**: rotation + revocation + **surface-binding** (توكن جيم لا يُجدَّد على المنصة والعكس).
- **`403`** عند غياب الصلاحية؛ الفرونت يخفي/يعطّل العنصر حسب `permissions[]`.

## العزل بين المستأجرين (Tenant Isolation)
- كل كيان جيم يحمل `TenantId`، وتُفلتر كل الاستعلامات تلقائياً عبر global query filters.
- الـ middleware يحلّ الجيم **قبل** التفويض ويرفض مستخدماً مصادَقاً بلا جيم محلول (منع تسريب cross-tenant).
- مستخدمو المنصة (`TenantId=null` في التوكن) يقرؤون عبر كل الجيمات.

## الفوترة اليدوية (Manual Billing)
- الـ Owner: يختار باقة → اشتراك `PendingPayment` → يدفع خارج النظام → يرفع إثبات → الأدمن يراجع.
- **الموافقة** (transaction ذرّية + RowVersion لمنع التفعيل المزدوج): تفعيل/تمديد الاشتراك، الجيم → `Active`، إنشاء دفعة + فاتورة مدفوعة، إشعار للمالك.
- **الرفض**: تسجيل السبب، الاشتراك يبقى `PendingPayment`، إشعار.
- **بدون بوابات دفع** — لكن التصميم يسمح بإضافة بوابة لاحقاً.

## حدود الباقة وبوابات الميزات (Feature Gating)
- قبل عمليات الإنشاء الحرجة، pipeline behavior يفحص **العدّ الحيّ** مقابل حد الباقة (Members/Coaches/Branches/Employees) وميزة `EmployeeManagement`.
- التجاوز → **`402 Payment Required`** برسالة ترقية.
- **Grandfathering**: الجيم بلا اشتراك نشط لا يُحجب (الفرض يبدأ عند الاشتراك في باقة).

## الوظائف المجدولة (Background Jobs — يومياً)
- `Trial`/`Active` عند الانتهاء → `PastDue` (+ الجيم PastDue).
- `PastDue` بعد فترة سماح (3 أيام) → `Expired` (+ الجيم `Suspended`).
- تذكير قبل 7 أيام من الانتهاء، أرشفة طلبات الدفع المعلّقة (>14 يوم)، تحديث cache الاستخدام.

## قواعد الجيم (مختصر)
- **الاشتراكات**: `EndDate = StartDate + DurationMonths`؛ تجميد/إلغاء مع استرداد للمحفظة اختيارياً.
- **بوابة QR**: تفحص الاشتراك النشط، السعة، ساعات العمل، التجميد، البطاقة — وترجّع `denyReason` واضح.
- **العميل ↔ المدرب**: كل عميل مرتبط بمدرب واحد نشط في نفس الوقت.
- **المخزون**: كل حركة (بيع/تعديل/تحويل) تُسجَّل في `StockMovement`.
- **التدقيق**: كل تغيير يُسجَّل في `AuditLog` (قيم قبل/بعد + IP + UserAgent).

---

# 8. رفع الملفات

- **الطريقة**: `multipart/form-data`.
- **مسارات التخزين**: `wwwroot/uploads/{type}/{year}/{month}` (محلي). أمثلة الأنواع: `profile-pictures`, `gym-logos`, `gym-covers`, `gym-gallery`, `payment-proofs`, تمارين/قياسات.
- **رد الرفع**: `{ "url": "https://.../uploads/..." }` (أو `{ "urls": [...] }` للمتعدد).

**Endpoints تقبل ملفات**:
| Endpoint | الحقل | النوع |
|----------|------|------|
| `POST /api/profile/picture` | `file` | صورة |
| `POST /api/gymprofile/logo` · `/cover` | `file` | صورة |
| `POST /api/gymprofile/gallery` | `files` | صور متعددة |
| `POST /api/exercises` · `PUT /api/exercises/{id}` | `image`, `video` | صورة/فيديو |
| `POST /api/bodymeasurements/with-images` | `inbodyImage`, `frontPhoto`, `sidePhoto`, `backPhoto` | صور |
| `POST /api/tenant/payment-requests` | `proof` | صورة إثبات الدفع |

**مثال (JavaScript)**:
```js
const fd = new FormData();
fd.append('planId', planId);
fd.append('proof', file);
await fetch(`${TENANT_API}/api/tenant/payment-requests`, {
  method: 'POST', headers: { Authorization: `Bearer ${token}` }, body: fd
});
```

---

# 9. هيكل المشروع

```
LogicFit/
├── LogicFit.Domain/            # Entities · Enums · Value Objects · Authorization catalog · Exceptions
├── LogicFit.Application/        # CQRS (Commands/Queries/Handlers) · Behaviors · Interfaces · Services · DTOs
├── LogicFit.Infrastructure/     # EF Core DbContext · Configurations · Migrations · Identity/JWT
│                                #   Seeders (RBAC/Plans) · Background jobs · Email/Notifications
├── LogicFit.API/                # Tenant API — 51 controller + tenant middleware
├── LogicFit.Platform.API/       # Platform API — 8 controllers (لوحة المشغّل)
└── LogicFit.Tests/              # xUnit
```
معمارية: `Domain` ← `Application` ← `Infrastructure` ← (`API` / `Platform.API`). النشر: Docker/compose، CI (GitHub Actions)، Health `/health`.

---

# 10. ملاحظات للـ Frontend Developer

## Headers المطلوبة
```
Content-Type: application/json          (أو multipart/form-data للرفع)
Authorization: Bearer <accessToken>     (لكل طلب محمي)
X-Tenant-Id: <GUID>                     (اختياري — بديل عن subdomain في الطلبات غير المسجّلة)
```

## تدفّق العمل المقترح
1. عند فتح التطبيق: `GET /api/branding/{subdomain}` → طبّق الثيم (اختياري).
2. الدخول/التسجيل يمرّر **`subdomain`** (مش TenantId GUID).
3. خزّن `accessToken` + `refreshToken` + `permissions[]`.
4. أرسل `Authorization: Bearer <token>` لكل طلب محمي.
5. **auto-refresh** عند `401`: نادِ `/api/auth/refresh` مرة واحدة ثم أعِد الطلب؛ وإلا logout.
6. ابنِ الـ navigation/الأزرار حسب `permissions[]` (خريطة [القسم 3](#3-أدوار-المستخدمين-وصلاحياتهم)).

## معالجة الأخطاء
كل الأخطاء بنفس الشكل:
```json
{ "statusCode": 400, "message": "Validation failed", "errors": { "field": ["message"] } }
```
| Status | المعنى | التعامل |
|--------|--------|---------|
| `400` | تحقق فاشل | اعرض `errors` لكل حقل |
| `401` | توكن منتهي | refresh ثم أعد؛ وإلا logout |
| `402` | تجاوز حد الباقة | شاشة ترقية |
| `403` | لا يملك الصلاحية | أخفِ/عطّل العنصر |
| `404` | غير موجود | — |
| `409` | تعارض (مكرر / لا يمكن الحذف) | اعرض الرسالة |

## Date Format
كل التواريخ **UTC ISO 8601** (`2026-07-08T12:15:00Z`). أرسل التواريخ بنفس الصيغة.

## أعراف الاستجابة
- **قائمة/تفاصيل**: `200` بالـ DTO / `List<DTO>`.
- **إنشاء**: غالباً `200`/`201` بالـ `Guid` الخام.
- **تعديل/حذف/تغيير حالة**: `204 No Content`.
- **enums**: تُرسَل كأرقام؛ وكثير من الـ DTOs تضيف حقل `*Name` نصي.

---

# 11. الخلاصة

**LogicFit** منصة SaaS متكاملة لإدارة الصالات الرياضية بمعمارية نظيفة، مكوّنة من **API-ين** (تطبيق الجيم + لوحة المنصة) يشاركان قاعدة بيانات واحدة، بـ **286 endpoint**، ونظام **RBAC ديناميكي**، و**فوترة يدوية** للاشتراكات، و**عزل كامل** بين الجيمات، و**White-label** لكل جيم.

**للتكامل**: ابدأ بـ [AUTH_AND_REGISTRATION.md](AUTH_AND_REGISTRATION.md) لفهم الدخول والتسجيل، ثم استخدم هذا المرجع لكل الـ endpoints. راعِ عمر التوكن (15 دقيقة) مع auto-refresh، وابنِ الواجهة على `permissions[]`.

---

<div align="center"><sub>LogicFit — مرجع كامل مولّد من الكود الفعلي · 286 endpoint · API-ان + قاعدة بيانات واحدة</sub></div>
