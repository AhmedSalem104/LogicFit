# LogicFit - System Documentation
## نظام إدارة الصالات الرياضية (Gym Management SaaS Platform)

---

# جدول المحتويات
1. [نظرة عامة على المشروع](#1-نظرة-عامة-على-المشروع)
2. [الميزات والوحدات](#2-الميزات-والوحدات)
3. [أدوار المستخدمين وصلاحياتهم](#3-أدوار-المستخدمين-وصلاحياتهم)
4. [الكيانات والعلاقات](#4-الكيانات-والعلاقات)
5. [API Endpoints](#5-api-endpoints)
6. [التقارير والتحليلات](#6-التقارير-والتحليلات)
7. [قواعد العمل Business Logic](#7-قواعد-العمل-business-logic)
8. [رفع الملفات](#8-رفع-الملفات)
9. [هيكل المشروع](#9-هيكل-المشروع)

---

# 1. نظرة عامة على المشروع

## ما هو LogicFit؟
LogicFit هو نظام **SaaS متعدد المستأجرين (Multi-tenant)** لإدارة الصالات الرياضية. يدعم المدربين في إنشاء برامج تمارين وخطط غذائية مخصصة للعملاء، مع إدارة الاشتراكات والتقارير المالية وتتبع قياسات الجسم.

## التقنيات المستخدمة
- **Backend:** .NET 8 (C#)
- **Architecture:** Clean Architecture + CQRS Pattern
- **ORM:** Entity Framework Core
- **Authentication:** JWT Bearer Tokens
- **Database:** SQL Server
- **Libraries:** MediatR, AutoMapper, FluentValidation, BCrypt, Serilog

## Multi-Tenant Architecture
كل صالة رياضية (Tenant) لها بياناتها المعزولة تماماً عن الصالات الأخرى:
- كل مستخدم ينتمي لـ Tenant واحد فقط
- البيانات مفلترة تلقائياً حسب TenantId
- لا يمكن الوصول لبيانات Tenant آخر

---

# 2. الميزات والوحدات

## الوحدات الرئيسية

| الوحدة | الوصف |
|--------|-------|
| **Authentication** | تسجيل، دخول، استعادة كلمة المرور |
| **Client Management** | إدارة العملاء وملفاتهم الشخصية |
| **Workout Programs** | برامج التمارين مع الروتينات والتمارين |
| **Workout Sessions** | تتبع جلسات التمرين والـ Sets |
| **Diet Plans** | خطط غذائية يومية مع الوجبات |
| **Exercise Library** | مكتبة التمارين مع استهداف العضلات |
| **Food Database** | قاعدة بيانات الأطعمة مع القيم الغذائية |
| **Body Measurements** | قياسات الجسم وصور التقدم |
| **Subscriptions** | إدارة الاشتراكات والتجميد |
| **Gym Profile** | إعدادات الصالة والعلامة التجارية |
| **Reports** | تقارير مالية وإحصائيات |
| **Coach Clients** | إدارة متدربين المدرب (تعيين/إلغاء تعيين) |
| **Coach Reports** | تقارير خاصة بالمدرب ومتدربينه |

## الفصل بين المشتركين والمتدربين

| النوع | الوصف | الجداول |
|-------|-------|---------|
| **مشترك الجيم** | أي شخص عنده اشتراك نشط | User + ClientSubscription |
| **متدرب المدرب** | عميل معين لمدرب معين | User + CoachClient |
| **مشترك ومتدرب** | عنده اشتراك ومعين لمدرب | User + ClientSubscription + CoachClient |
| **متدرب فقط** | معين لمدرب بدون اشتراك | User + CoachClient (بدون ClientSubscription) |

---

# 3. أدوار المستخدمين وصلاحياتهم

## الأدوار الثلاثة

### 1. Owner (مالك الصالة)
```
الصلاحيات الكاملة:
├── إدارة ملف الصالة (الاسم، اللوجو، الصور)
├── إنشاء وإدارة خطط الاشتراك
├── عرض جميع التقارير (Dashboard, Financial, Clients, Subscriptions)
├── عرض تقارير أي مدرب
├── إدارة المدربين (إضافة، تعديل، حذف)
├── إدارة العملاء
├── تعيين عملاء لأي مدرب
└── الوصول الكامل للنظام
```

### 2. Coach (المدرب)
```
صلاحيات المدرب:
├── إنشاء وإدارة برامج التمارين للمتدربين المعينين له
├── إنشاء وإدارة الخطط الغذائية لمتدربينه
├── عرض جلسات التمرين وتقدم متدربينه فقط
├── تعيين عملاء لنفسه (كمتدربين)
├── عرض تقارير المدرب الخاصة به (Dashboard, Trainees, Progress)
├── إنشاء/تعديل التمارين
├── إدارة قاعدة بيانات الأطعمة
├── إنشاء اشتراكات للعملاء (دور المبيعات)
└── محدود ببيانات الـ Tenant + متدربينه فقط
```

### 3. Client (العميل)
```
صلاحيات العميل:
├── عرض برامج التمارين المخصصة له
├── بدء وإنهاء جلسات التمرين
├── تسجيل الوجبات المستهلكة
├── عرض قياسات الجسم الخاصة به
├── عرض الملف الشخصي
└── قراءة فقط للتمارين والأطعمة
```

## نموذج المصادقة (Authentication Model)
```
┌─────────────────────────────────────────────────────┐
│                   JWT Token                          │
├─────────────────────────────────────────────────────┤
│  Claims:                                            │
│  ├── UserId: Guid                                   │
│  ├── Email: string                                  │
│  ├── UserRole: Owner/Coach/Client                   │
│  ├── TenantId: Guid                                 │
│  └── exp: 60 minutes                                │
└─────────────────────────────────────────────────────┘

Login: Phone Number + Password (per Tenant)
Password Hashing: BCrypt with Salt
```

---

# 4. الكيانات والعلاقات

## Entity Relationship Diagram (Text)

```
┌──────────────────────────────────────────────────────────────────────────┐
│                              TENANT                                       │
│  (الصالة الرياضية - Multi-tenant Container)                              │
├──────────────────────────────────────────────────────────────────────────┤
│  Id, Name, LogoUrl, CoverImageUrl, GalleryImages, Description            │
│  Phone, Email, Address, Facebook, Instagram, Website                     │
└─────────────────────────────────┬────────────────────────────────────────┘
                                  │
                                  │ 1:N
                                  ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                               USER                                        │
│  (المستخدم - Owner/Coach/Client)                                         │
├──────────────────────────────────────────────────────────────────────────┤
│  Id, TenantId, Email, PhoneNumber, PasswordHash, Role                    │
│  IsActive, WalletBalance, PasswordResetToken, ResetTokenExpiry           │
├──────────────────────────────────────────────────────────────────────────┤
│  Relationships:                                                           │
│  ├── UserProfile (1:1) - بيانات شخصية إضافية                             │
│  ├── WorkoutPrograms (1:N) - برامج التمارين (كمدرب)                      │
│  ├── ClientWorkoutPrograms (1:N) - برامج التمارين (كعميل)                │
│  ├── DietPlans (1:N) - الخطط الغذائية (كمدرب)                            │
│  ├── ClientDietPlans (1:N) - الخطط الغذائية (كعميل)                      │
│  ├── BodyMeasurements (1:N) - قياسات الجسم                               │
│  ├── ClientSubscriptions (1:N) - الاشتراكات                              │
│  ├── SalesSubscriptions (1:N) - الاشتراكات المباعة (كمدرب)               │
│  ├── Trainees (1:N) - المتدربين (كمدرب) via CoachClient                  │
│  └── AssignedCoaches (1:N) - المدربين المعينين (كعميل) via CoachClient   │
└──────────────────────────────────────────────────────────────────────────┘
```

## تفصيل الكيانات

### User & Profile
```csharp
User {
    Guid Id
    Guid TenantId
    string Email
    string PhoneNumber          // فريد لكل Tenant
    string PasswordHash
    UserRole Role               // Owner, Coach, Client
    bool IsActive
    decimal WalletBalance
    string? PasswordResetToken
    DateTime? ResetTokenExpiry
}

UserProfile {
    Guid Id
    Guid UserId
    string FullName
    GenderType? Gender          // Male, Female
    DateTime? BirthDate
    double? HeightCm
    string? ActivityLevel
    string? MedicalHistory
}
```

### Subscription System
```csharp
SubscriptionPlan {
    Guid Id
    Guid TenantId
    string Name                 // "Monthly", "Quarterly", "Annual"
    decimal Price
    int DurationMonths
    bool IsActive
}

ClientSubscription {
    Guid Id
    Guid TenantId
    Guid ClientId               // -> User
    Guid PlanId                 // -> SubscriptionPlan
    Guid? SalesCoachId          // -> User (المدرب الذي باع)
    DateTime StartDate
    DateTime EndDate            // محسوب: StartDate + DurationMonths
    SubscriptionStatus Status   // Active, Suspended, Trial, Expired, Cancelled
    decimal AmountPaid
}

SubscriptionFreeze {
    Guid Id
    Guid SubscriptionId
    DateTime StartDate
    DateTime EndDate
    string? Reason
}

// SubscriptionStatus Enum
enum SubscriptionStatus {
    Active,
    Suspended,
    Trial,
    Expired,
    Cancelled
}
```

### Coach-Client System (نظام ربط المدرب بالمتدربين)
```csharp
CoachClient {
    Guid Id
    Guid TenantId
    Guid CoachId               // -> User (المدرب)
    Guid ClientId              // -> User (المتدرب) - Unique per Tenant
    DateTime AssignedAt        // تاريخ التعيين
    DateTime? UnassignedAt     // تاريخ إلغاء التعيين
    bool IsActive              // هل التعيين نشط؟
    string? Notes              // ملاحظات
}

// ملاحظة: كل عميل يمكن أن يكون معين لمدرب واحد فقط في نفس الوقت
// Unique Index: (TenantId, ClientId, IsActive) WHERE IsActive = 1
```

### Workout System
```csharp
WorkoutProgram {
    Guid Id
    Guid TenantId
    Guid CoachId                // -> User (المدرب)
    Guid ClientId               // -> User (العميل)
    string Name
    string? Description
    bool IsActive
}

ProgramRoutine {
    Guid Id
    Guid ProgramId
    string Name                 // "Day 1 - Chest & Triceps"
    int DayOfWeek               // 1-7
    int OrderIndex
}

RoutineExercise {
    Guid Id
    Guid RoutineId
    int ExerciseId              // -> Exercise
    int Sets
    int MinReps
    int MaxReps
    int RestSeconds
    int OrderIndex
    int? SupersetGroup          // للتمارين المتتالية
}

Exercise {
    int Id
    Guid TenantId
    string Name
    int TargetMuscleId          // -> Muscle
    string? Equipment
    bool IsHighImpact
    string? ImageUrl
    string? VideoUrl
    bool IsDeleted              // Soft Delete
}

Muscle {
    int Id
    string Name                 // "Chest", "Back", "Shoulders"
    string BodyPart             // "Upper Body", "Lower Body", "Core"
}
```

### Workout Session Tracking
```csharp
WorkoutSession {
    Guid Id
    Guid TenantId
    Guid ClientId               // -> User
    Guid RoutineId              // -> ProgramRoutine
    DateTime StartTime
    DateTime? EndTime
    string? Notes
}

SessionSet {
    Guid Id
    Guid SessionId
    int ExerciseId              // -> Exercise
    int SetNumber
    double Weight
    int Reps
    int? RPE                    // Rate of Perceived Exertion (1-10)
    double VolumeLoad           // محسوب: Weight × Reps
    bool IsPR                   // Personal Record
}
```

### Nutrition System
```csharp
DietPlan {
    Guid Id
    Guid TenantId
    Guid CoachId                // -> User
    Guid ClientId               // -> User
    string Name
    int DailyCalories
    int DailyProtein
    int DailyCarbs
    int DailyFats
    DateTime StartDate
    DateTime? EndDate
    PlanStatus Status           // Active, Archived, Draft
}

DailyMeal {
    Guid Id
    Guid DietPlanId
    string MealName             // "Breakfast", "Lunch", "Dinner"
    int OrderIndex
}

MealItem {
    Guid Id
    Guid MealId
    int FoodId                  // -> Food
    double Quantity             // بالجرام
    // Computed on insert:
    double Calories
    double Protein
    double Carbs
    double Fats
}

Food {
    int Id
    Guid TenantId
    string Name
    string? Category
    // Per 100g:
    double CaloriesPer100g
    double ProteinPer100g
    double CarbsPer100g
    double FatsPer100g
    double? FiberPer100g
    bool IsVerified
    int? AlternativeGroupId     // للبدائل
    bool IsDeleted
}

MealLog {
    Guid Id
    Guid TenantId
    Guid ClientId
    Guid MealItemId
    int? AlternativeFoodId      // إذا استبدل الطعام
    double ActualQuantity       // الكمية الفعلية المستهلكة
    DateTime LoggedAt
}
```

### Body Measurements
```csharp
BodyMeasurement {
    Guid Id
    Guid TenantId
    Guid ClientId               // -> User
    DateTime DateRecorded
    double WeightKg
    double? SkeletalMuscleMass  // كتلة العضلات
    double? BodyFatMass         // كتلة الدهون
    double? BodyFatPercent      // نسبة الدهون %
    double? TotalBodyWater      // نسبة الماء %
    double? Bmr                 // معدل الأيض الأساسي
    int? VisceralFatLevel       // مستوى الدهون الحشوية
    // صور التقدم:
    string? InbodyImageUrl
    string? FrontPhotoUrl
    string? SidePhotoUrl
    string? BackPhotoUrl
}
```

---

# 5. API Endpoints

## Authentication `/api/auth`
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/register` | تسجيل صالة جديدة ومالكها | Public |
| POST | `/login` | تسجيل الدخول | Public |
| POST | `/forget-password` | طلب استعادة كلمة المرور | Public |
| POST | `/reset-password` | إعادة تعيين كلمة المرور | Public |

### Register Request
```json
{
  "gymName": "FitZone Gym",
  "ownerName": "Ahmed Mohamed",
  "email": "owner@fitzone.com",
  "phoneNumber": "01012345678",
  "password": "SecurePass123!"
}
```

### Login Request/Response
```json
// Request
{
  "phoneNumber": "01012345678",
  "password": "SecurePass123!"
}

// Response
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2024-01-15T15:30:00Z",
  "user": {
    "id": "guid",
    "email": "owner@fitzone.com",
    "role": "Owner",
    "tenantId": "guid"
  }
}
```

---

## Clients `/api/clients`
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | قائمة العملاء (مع فلترة) |
| GET | `/{id}` | تفاصيل عميل مع اشتراكاته |
| POST | `/` | إنشاء عميل جديد |
| PUT | `/{id}` | تحديث بيانات عميل |
| DELETE | `/{id}` | حذف عميل (Soft Delete) |

### Query Parameters (GET /)
```
?search=ahmed           // بحث بالاسم أو الهاتف
&isActive=true          // فلترة حسب الحالة
&page=1&pageSize=10     // Pagination
```

### Create Client Request
```json
{
  "email": "client@email.com",
  "phoneNumber": "01098765432",
  "password": "ClientPass123!",
  "profile": {
    "fullName": "محمد أحمد",
    "gender": "Male",
    "birthDate": "1995-05-15",
    "heightCm": 175,
    "activityLevel": "Moderate",
    "medicalHistory": "لا يوجد"
  }
}
```

---

## Users `/api/users`
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | قائمة المستخدمين |
| GET | `/{id}` | تفاصيل مستخدم |
| PUT | `/{id}` | تحديث مستخدم |
| PUT | `/{id}/profile` | تحديث الملف الشخصي |

### Query Parameters (GET /)
```
?role=Coach             // فلترة حسب الدور
&isActive=true
&search=ahmed
```

---

## Workout Programs `/api/workoutprograms`
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | قائمة البرامج |
| GET | `/{id}` | تفاصيل برنامج مع الروتينات |
| POST | `/` | إنشاء برنامج |
| POST | `/{programId}/routines` | إضافة روتين |
| POST | `/routines/{routineId}/exercises` | إضافة تمرين للروتين |
| DELETE | `/{id}` | حذف برنامج |

### Query Parameters (GET /)
```
?coachId=guid           // برامج مدرب معين
&clientId=guid          // برامج عميل معين
&isActive=true
```

### Create Program Request
```json
{
  "clientId": "guid",
  "name": "برنامج بناء العضلات",
  "description": "برنامج 4 أيام في الأسبوع"
}
```

### Add Routine Request
```json
{
  "name": "Day 1 - Chest & Triceps",
  "dayOfWeek": 1,
  "orderIndex": 1
}
```

### Add Exercise to Routine Request
```json
{
  "exerciseId": 1,
  "sets": 4,
  "minReps": 8,
  "maxReps": 12,
  "restSeconds": 90,
  "orderIndex": 1,
  "supersetGroup": null
}
```

---

## Workout Sessions `/api/workoutsessions`
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | قائمة الجلسات |
| GET | `/{id}` | تفاصيل جلسة مع الـ Sets |
| POST | `/start` | بدء جلسة جديدة |
| POST | `/{sessionId}/end` | إنهاء جلسة |
| POST | `/{sessionId}/sets` | تسجيل Set |

### Query Parameters (GET /)
```
?clientId=guid
&fromDate=2024-01-01
&toDate=2024-01-31
```

### Start Session Request
```json
{
  "routineId": "guid"
}
```

### Record Set Request
```json
{
  "exerciseId": 1,
  "setNumber": 1,
  "weight": 80,
  "reps": 10,
  "rpe": 8
}
```

---

## Diet Plans `/api/dietplans`
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | قائمة الخطط الغذائية |
| GET | `/{id}` | تفاصيل خطة مع الوجبات |
| POST | `/` | إنشاء خطة |
| POST | `/{planId}/meals` | إضافة وجبة |
| POST | `/meals/{mealId}/items` | إضافة طعام للوجبة |
| DELETE | `/{id}` | حذف خطة |

### Query Parameters (GET /)
```
?coachId=guid
&clientId=guid
&status=Active          // Active, Archived, Draft
```

### Create Diet Plan Request
```json
{
  "clientId": "guid",
  "name": "خطة تخسيس",
  "dailyCalories": 2000,
  "dailyProtein": 150,
  "dailyCarbs": 200,
  "dailyFats": 67,
  "startDate": "2024-01-01",
  "endDate": "2024-03-31"
}
```

### Add Meal Request
```json
{
  "mealName": "Breakfast",
  "orderIndex": 1
}
```

### Add Food to Meal Request
```json
{
  "foodId": 1,
  "quantity": 150
}
```

---

## Exercises `/api/exercises`
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | قائمة التمارين |
| GET | `/{id}` | تفاصيل تمرين |
| POST | `/` | إنشاء تمرين (multipart/form-data) |
| PUT | `/{id}` | تحديث تمرين (multipart/form-data) |
| DELETE | `/{id}` | حذف تمرين |

### Query Parameters (GET /)
```
?targetMuscleId=1
&equipment=Barbell
&isHighImpact=false
```

### Create Exercise Request (Form Data)
```
name: "Bench Press"
targetMuscleId: 1
equipment: "Barbell"
isHighImpact: false
image: [file]
video: [file]
```

---

## Foods `/api/foods`
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | قائمة الأطعمة |
| GET | `/{id}` | تفاصيل طعام |
| POST | `/` | إنشاء طعام |
| PUT | `/{id}` | تحديث طعام |
| DELETE | `/{id}` | حذف طعام |

### Query Parameters (GET /)
```
?category=Protein
&isVerified=true
&search=chicken
```

### Create Food Request
```json
{
  "name": "صدور دجاج مشوية",
  "category": "Protein",
  "caloriesPer100g": 165,
  "proteinPer100g": 31,
  "carbsPer100g": 0,
  "fatsPer100g": 3.6,
  "fiberPer100g": 0
}
```

---

## Muscles `/api/muscles`
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | قائمة العضلات |
| GET | `/{id}` | تفاصيل عضلة |

### Query Parameters (GET /)
```
?bodyPart=Upper Body
```

---

## Body Measurements `/api/bodymeasurements`
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | قائمة القياسات |
| POST | `/` | إنشاء قياس (multipart/form-data) |
| DELETE | `/{id}` | حذف قياس |

### Query Parameters (GET /)
```
?clientId=guid
&fromDate=2024-01-01
&toDate=2024-01-31
```

### Create Measurement Request (Form Data)
```
clientId: "guid"
dateRecorded: "2024-01-15"
weightKg: 85.5
skeletalMuscleMass: 38.2
bodyFatMass: 15.3
bodyFatPercent: 17.9
totalBodyWater: 55.2
bmr: 1850
visceralFatLevel: 8
inbodyImage: [file]
frontPhoto: [file]
sidePhoto: [file]
backPhoto: [file]
```

---

## Subscriptions `/api/subscriptions`
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/plans` | قائمة خطط الاشتراك |
| POST | `/plans` | إنشاء خطة اشتراك |
| GET | `/` | قائمة اشتراكات العملاء |
| POST | `/` | إنشاء اشتراك لعميل |
| POST | `/{id}/freeze` | تجميد اشتراك |
| POST | `/{id}/cancel` | إلغاء اشتراك |

### Query Parameters (GET /)
```
?clientId=guid
&status=Active          // Active, Suspended, Trial, Expired, Cancelled
&expiringInDays=7       // ينتهي خلال 7 أيام
```

### Create Subscription Plan Request
```json
{
  "name": "Monthly",
  "price": 500,
  "durationMonths": 1,
  "isActive": true
}
```

### Create Client Subscription Request
```json
{
  "clientId": "guid",
  "planId": "guid",
  "startDate": "2024-01-01",
  "amountPaid": 500
}
```

### Freeze Subscription Request
```json
{
  "startDate": "2024-01-15",
  "endDate": "2024-01-22",
  "reason": "سفر"
}
```

---

## Gym Profile `/api/gymprofile`
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | بيانات الصالة |
| PUT | `/` | تحديث بيانات الصالة |
| POST | `/logo` | رفع اللوجو |
| POST | `/cover` | رفع صورة الغلاف |
| POST | `/gallery` | رفع صور المعرض |

### Update Gym Profile Request
```json
{
  "name": "FitZone Gym",
  "description": "أفضل صالة رياضية في المدينة",
  "phone": "01012345678",
  "email": "info@fitzone.com",
  "address": "123 شارع النصر، القاهرة",
  "facebook": "https://facebook.com/fitzone",
  "instagram": "https://instagram.com/fitzone",
  "website": "https://fitzone.com"
}
```

---

## Coach Clients `/api/coach-clients`
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | قائمة متدربين المدرب |
| POST | `/` | تعيين عميل لمدرب |
| DELETE | `/{clientId}` | إلغاء تعيين عميل |

### Query Parameters (GET /)
```
?coachId=guid           // متدربين مدرب معين (للـ Owner فقط)
&isActive=true          // فلترة حسب حالة التعيين
```

### Assign Client Request
```json
{
  "coachId": "guid",      // اختياري - إذا فارغ يعين لنفسه
  "clientId": "guid",
  "notes": "ملاحظات اختيارية"
}
```

### Coach Clients Response
```json
[
  {
    "id": "guid",
    "coachId": "guid",
    "coachName": "أحمد المدرب",
    "clientId": "guid",
    "clientName": "محمد العميل",
    "clientPhone": "01012345678",
    "clientEmail": "client@email.com",
    "assignedAt": "2024-01-01T00:00:00Z",
    "isActive": true,
    "notes": "ملاحظات",
    "hasActiveSubscription": true,
    "subscriptionEndDate": "2024-06-01T00:00:00Z",
    "workoutProgramsCount": 2,
    "dietPlansCount": 1,
    "workoutSessionsCount": 15,
    "lastSessionDate": "2024-01-15T10:00:00Z"
  }
]
```

---

## Reports `/api/reports`

### تقارير الصالة (Owner)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/dashboard` | ملخص لوحة التحكم |
| GET | `/clients` | تقرير العملاء |
| GET | `/subscriptions` | تقرير الاشتراكات |
| GET | `/financial` | التقرير المالي |

### تقارير المدرب (Coach Reports)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/coach/dashboard` | لوحة تحكم المدرب |
| GET | `/coach/trainees` | تقرير المتدربين |
| GET | `/coach/trainee/{clientId}` | تقرير تقدم متدرب معين |

### Query Parameters (Coach Reports)
```
?coachId=guid           // للـ Owner لعرض تقارير مدرب معين
                        // المدرب يرى تقاريره فقط بدون هذا الفلتر
```

### Dashboard Response
```json
{
  "totalClients": 150,
  "activeClients": 120,
  "newClientsThisMonth": 15,
  "totalCoaches": 5,
  "activeSubscriptions": 100,
  "expiringSubscriptions": 8,
  "revenueThisMonth": 50000,
  "revenueLastMonth": 45000,
  "workoutsThisMonth": 450,
  "activeDietPlans": 80
}
```

### Clients Report Response
```json
{
  "totalClients": 150,
  "activeClients": 120,
  "inactiveClients": 30,
  "clientsWithActiveSubscription": 100,
  "clientsWithoutSubscription": 50,
  "topClientsBySessionsCount": [
    { "clientId": "guid", "clientName": "محمد أحمد", "sessionsCount": 25 }
  ],
  "topClientsByRevenue": [
    { "clientId": "guid", "clientName": "أحمد محمود", "totalRevenue": 5000 }
  ],
  "monthlyTrend": [
    { "month": "2024-01", "newClients": 15, "churnedClients": 3 }
  ]
}
```

### Subscriptions Report Response
```json
{
  "totalSubscriptions": 200,
  "activeSubscriptions": 100,
  "expiredSubscriptions": 80,
  "cancelledSubscriptions": 20,
  "expiringSoon7Days": 8,
  "expiringSoon30Days": 25,
  "totalRevenue": 500000,
  "monthlyRevenue": 50000,
  "planStatistics": [
    { "planId": "guid", "planName": "Monthly", "activeCount": 60, "totalRevenue": 300000 }
  ],
  "monthlyRevenueTrend": [
    { "month": "2024-01", "revenue": 50000 }
  ]
}
```

### Financial Report Response
```json
{
  "totalRevenue": 500000,
  "monthlyRevenue": 50000,
  "growthPercentage": 11.1,
  "averageSubscriptionValue": 500,
  "totalWalletBalances": 25000,
  "monthlyBreakdown": [
    { "month": "2024-01", "revenue": 50000 }
  ],
  "paymentMethodStats": [
    { "method": "Cash", "count": 150, "total": 300000 }
  ]
}
```

---

# 6. التقارير والتحليلات

## Dashboard Report
ملخص سريع للوحة التحكم:
- إجمالي العملاء والعملاء النشطين
- العملاء الجدد هذا الشهر
- عدد المدربين
- الاشتراكات النشطة والمنتهية قريباً
- الإيرادات (هذا الشهر مقارنة بالشهر الماضي)
- عدد جلسات التمرين هذا الشهر
- الخطط الغذائية النشطة

## Clients Report
تحليلات تفصيلية للعملاء:
- إجمالي العملاء النشطين وغير النشطين
- العملاء مع/بدون اشتراكات نشطة
- أفضل العملاء (حسب الجلسات والإيرادات)
- الاتجاه الشهري (العملاء الجدد/المتوقفين لـ 6 أشهر)

## Subscriptions Report
مقاييس الاشتراكات:
- إجمالي/نشط/منتهي/ملغي
- المنتهية قريباً (7 أيام، 30 يوم)
- إجمالي وشهري الإيرادات
- إحصائيات كل خطة
- اتجاه الإيرادات الشهرية

## Financial Report
التحليلات المالية:
- إجمالي وشهري الإيرادات
- نسبة النمو (شهر بشهر)
- متوسط قيمة الاشتراك
- إجمالي أرصدة المحافظ
- تقسيم الإيرادات الشهرية

## Coach Dashboard Report (لوحة تحكم المدرب)
```json
{
  "totalTrainees": 25,
  "activeTrainees": 22,
  "newTraineesThisMonth": 5,
  "activeWorkoutPrograms": 20,
  "activeDietPlans": 15,
  "totalSessionsThisMonth": 150,
  "totalVolumeThisMonth": 125000,
  "topTraineesByProgress": [
    {
      "clientId": "guid",
      "clientName": "محمد أحمد",
      "clientPhone": "01012345678",
      "weightChange": -5.2,
      "bodyFatChange": -3.1
    }
  ],
  "topTraineesBySessions": [
    {
      "clientId": "guid",
      "clientName": "أحمد محمود",
      "clientPhone": "01098765432",
      "sessionsCount": 20
    }
  ]
}
```

## Coach Trainees Report (تقرير المتدربين)
```json
{
  "totalTrainees": 25,
  "withActiveSubscription": 20,
  "withoutSubscription": 5,
  "trainees": [
    {
      "clientId": "guid",
      "name": "محمد أحمد",
      "phone": "01012345678",
      "email": "client@email.com",
      "assignedAt": "2024-01-01T00:00:00Z",
      "hasActiveSubscription": true,
      "subscriptionEndDate": "2024-06-01T00:00:00Z",
      "activeWorkoutPrograms": 2,
      "activeDietPlans": 1,
      "totalSessions": 50,
      "sessionsThisMonth": 12,
      "lastSessionDate": "2024-01-15T10:00:00Z",
      "currentWeight": 80.5,
      "weightChange": -5.2,
      "bodyFatPercent": 18.5,
      "lastMeasurementDate": "2024-01-10T00:00:00Z"
    }
  ]
}
```

## Trainee Progress Report (تقرير تقدم متدرب)
```json
{
  "clientId": "guid",
  "clientName": "محمد أحمد",
  "clientPhone": "01012345678",
  "assignedAt": "2024-01-01T00:00:00Z",

  "bodyMeasurements": [
    {
      "dateRecorded": "2024-01-01T00:00:00Z",
      "weightKg": 85.0,
      "bodyFatPercent": 22.0,
      "muscleMass": 35.0,
      "bmr": 1800
    },
    {
      "dateRecorded": "2024-02-01T00:00:00Z",
      "weightKg": 82.0,
      "bodyFatPercent": 20.0,
      "muscleMass": 36.0,
      "bmr": 1850
    }
  ],
  "startWeight": 85.0,
  "currentWeight": 82.0,
  "totalWeightChange": -3.0,
  "startBodyFat": 22.0,
  "currentBodyFat": 20.0,
  "totalBodyFatChange": -2.0,
  "startMuscleMass": 35.0,
  "currentMuscleMass": 36.0,
  "totalMuscleMassChange": 1.0,

  "totalSessions": 24,
  "totalVolumeLifted": 50000,
  "monthlySessions": [
    { "month": "2024-01", "sessionCount": 12, "totalVolume": 25000 },
    { "month": "2024-02", "sessionCount": 12, "totalVolume": 25000 }
  ],

  "workoutPrograms": [
    { "id": "guid", "name": "برنامج بناء العضلات", "startDate": "2024-01-01", "routinesCount": 4 }
  ],
  "dietPlans": [
    { "id": "guid", "name": "خطة تخسيس", "startDate": "2024-01-01", "targetCalories": 2000 }
  ],

  "personalRecords": [
    {
      "exerciseId": 1,
      "exerciseName": "Bench Press",
      "maxWeight": 100,
      "reps": 5,
      "achievedAt": "2024-02-01T10:00:00Z"
    }
  ]
}
```

---

# 7. قواعد العمل Business Logic

## Authentication Rules
```
1. تسجيل الدخول: رقم الهاتف + كلمة المرور (فريد لكل Tenant)
2. تشفير كلمة المرور: BCrypt مع Salt
3. التوكنات: JWT مع انتهاء صلاحية 60 دقيقة
4. Multi-tenant: كل Tenant معزول؛ المستخدمون ينتمون لـ Tenant واحد
```

## Client Management Rules
```
1. رقم الهاتف فريد لكل Tenant (ليس عالمياً)
2. إنشاء بريد افتراضي إذا لم يُقدم: {phoneNumber}@client.logicfit.com
3. Soft Delete: لا يُحذف فعلياً، يُعلم كمحذوف
4. إنشاء Profile اختياري مع البيانات الشخصية
```

## Subscription Rules
```
1. المدة: تُحسب من PlanDurationMonths
2. دورة الحالة: Active → Suspended/Trial → Expired/Cancelled
3. التجميد: يمكن إيقاف الاشتراكات النشطة مؤقتاً
4. تتبع المبيعات: تسجيل المدرب الذي أنشأ الاشتراك
5. حساب تاريخ النهاية: StartDate + PlanDurationMonths
```

## Workout Rules
```
1. المدرب ينشئ البرامج للعملاء
2. روتينات متعددة لكل برنامج
3. الروتينات لها جدولة حسب يوم الأسبوع
4. دعم Superset (تمارين متتالية)
```

## Exercise Rules
```
1. مكتبة تمارين عامة + تمارين خاصة بكل Tenant
2. Soft Delete
3. العضلة المستهدفة مطلوبة
4. المعدات و High-impact اختياريان
5. صور وفيديو للتعليمات
```

## Nutrition Rules
```
1. القيم الغذائية مخزنة لكل 100 جرام
2. حالة التحقق (IsVerified)
3. دعم مجموعات البدائل
4. الحقول المحسوبة في MealItem تُحسب عند الإدراج
```

## Meal Tracking Rules
```
1. العملاء يسجلون الطعام المستهلك فعلياً (MealLog)
2. يمكن الاستبدال بأطعمة بديلة
3. الكمية تُتبع بشكل منفصل عن الكميات المخططة
```

## Body Measurements Rules
```
1. التسجيلات مرتبطة بالتاريخ
2. تكامل بيانات جهاز InBody
3. دعم صور التقدم (4 زوايا + صورة InBody)
4. مقاييس تفصيلية اختيارية
```

## Workout Session Rules
```
1. العميل يبدأ جلسة لروتين محدد
2. تسجيل Sets متعددة لكل جلسة
3. حساب Volume Load: الوزن × التكرارات
4. علامة PR (رقم قياسي شخصي) للإنجازات
5. RPE اختياري
```

## Coach-Client Assignment Rules
```
1. كل عميل يمكن تعيينه لمدرب واحد فقط في نفس الوقت (Unique per Tenant)
2. المالك يمكنه تعيين أي عميل لأي مدرب
3. المدرب يمكنه تعيين عملاء لنفسه فقط
4. إلغاء التعيين: IsActive = false و UnassignedAt = DateTime.UtcNow
5. يمكن للعميل أن يكون:
   - مشترك فقط (عنده اشتراك، غير معين لمدرب)
   - متدرب فقط (معين لمدرب، بدون اشتراك)
   - مشترك ومتدرب (عنده اشتراك ومعين لمدرب)
6. المدرب يرى فقط متدربينه المعينين له
7. المالك يرى جميع المتدربين وجميع المدربين
```

---

# 8. رفع الملفات

## مسارات التخزين
```
wwwroot/uploads/
├── images/
│   └── {year}/{month}/
│       ├── exercises/          # صور التمارين
│       ├── measurements/       # صور قياسات الجسم
│       ├── gym-logos/          # شعارات الصالات
│       ├── gym-covers/         # صور أغلفة الصالات
│       └── gym-gallery/        # معرض صور الصالات
└── videos/
    └── {year}/{month}/
        └── exercises/          # فيديوهات التمارين
```

## الملفات المدعومة

### الصور
- **الأنواع:** JPG, JPEG, PNG, GIF, WebP
- **الحد الأقصى:** 10 MB
- **الاستخدام:**
  - التمارين (Image)
  - قياسات الجسم (InbodyImage, FrontPhoto, SidePhoto, BackPhoto)
  - الصالة (Logo, Cover, Gallery)

### الفيديو
- **الأنواع:** MP4, AVI, MOV, WMV, WebM
- **الحد الأقصى:** 100 MB
- **الاستخدام:** التمارين (Video)

## Endpoints لرفع الملفات
```
// التمارين - multipart/form-data
POST   /api/exercises          [FromForm] مع Image و Video
PUT    /api/exercises/{id}     [FromForm] مع Image و Video

// قياسات الجسم - multipart/form-data
POST   /api/bodymeasurements   [FromForm] مع 4 صور

// الصالة
POST   /api/gymprofile/logo    [FromForm] مع LogoFile
POST   /api/gymprofile/cover   [FromForm] مع CoverFile
POST   /api/gymprofile/gallery [FromForm] مع GalleryFiles (متعدد)
```

---

# 9. هيكل المشروع

```
LogicFit/
├── LogicFit.API/                      # طبقة العرض (Presentation Layer)
│   ├── Features/                      # Controllers مقسمة حسب الميزة
│   │   ├── Auth/
│   │   │   └── AuthController.cs
│   │   ├── Clients/
│   │   │   └── ClientsController.cs
│   │   ├── Users/
│   │   │   └── UsersController.cs
│   │   ├── WorkoutPrograms/
│   │   │   └── WorkoutProgramsController.cs
│   │   ├── WorkoutSessions/
│   │   │   └── WorkoutSessionsController.cs
│   │   ├── DietPlans/
│   │   │   └── DietPlansController.cs
│   │   ├── Exercises/
│   │   │   └── ExercisesController.cs
│   │   ├── Foods/
│   │   │   └── FoodsController.cs
│   │   ├── Muscles/
│   │   │   └── MusclesController.cs
│   │   ├── BodyMeasurements/
│   │   │   └── BodyMeasurementsController.cs
│   │   ├── Subscriptions/
│   │   │   └── SubscriptionsController.cs
│   │   ├── GymProfile/
│   │   │   └── GymProfileController.cs
│   │   ├── Reports/
│   │   │   └── ReportsController.cs
│   │   ├── CoachClients/
│   │   │   └── CoachClientsController.cs
│   │   └── Tenants/
│   │       └── TenantsController.cs
│   ├── Middleware/
│   │   ├── ExceptionHandlingMiddleware.cs
│   │   └── TenantMiddleware.cs
│   ├── wwwroot/
│   │   └── uploads/                   # ملفات مرفوعة
│   └── Program.cs
│
├── LogicFit.Application/              # طبقة التطبيق (Application Layer)
│   ├── Features/                      # CQRS Commands & Queries
│   │   ├── Auth/
│   │   │   ├── Commands/
│   │   │   │   ├── Register/
│   │   │   │   ├── Login/
│   │   │   │   ├── ForgetPassword/
│   │   │   │   └── ResetPassword/
│   │   │   └── DTOs/
│   │   ├── Clients/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateClient/
│   │   │   │   ├── UpdateClient/
│   │   │   │   └── DeleteClient/
│   │   │   ├── Queries/
│   │   │   │   ├── GetClients/
│   │   │   │   └── GetClientById/
│   │   │   └── DTOs/
│   │   ├── [... باقي الميزات بنفس الهيكل]
│   │   ├── CoachClients/
│   │   │   ├── Commands/
│   │   │   │   ├── AssignClientToCoach/
│   │   │   │   └── UnassignClientFromCoach/
│   │   │   ├── Queries/
│   │   │   │   └── GetCoachClients/
│   │   │   └── DTOs/
│   │   │       └── CoachClientDto.cs
│   │   └── Reports/
│   │       ├── Queries/
│   │       │   ├── GetDashboardReport/
│   │       │   ├── GetClientsReport/
│   │       │   ├── GetSubscriptionsReport/
│   │       │   ├── GetFinancialReport/
│   │       │   ├── GetCoachDashboardReport/
│   │       │   ├── GetCoachTraineesReport/
│   │       │   └── GetTraineeProgressReport/
│   │       └── DTOs/
│   ├── Common/
│   │   ├── Behaviors/
│   │   │   ├── LoggingBehavior.cs
│   │   │   ├── ValidationBehavior.cs
│   │   │   └── ExceptionHandlingBehavior.cs
│   │   ├── Interfaces/
│   │   │   ├── IApplicationDbContext.cs
│   │   │   ├── ICurrentUserService.cs
│   │   │   ├── ITenantService.cs
│   │   │   ├── ITokenService.cs
│   │   │   ├── IFileUploadService.cs
│   │   │   └── IDateTimeService.cs
│   │   └── Models/
│   │       ├── PaginatedList.cs
│   │       └── Result.cs
│   └── Mappings/
│       └── MappingProfile.cs
│
├── LogicFit.Domain/                   # طبقة المجال (Domain Layer)
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── UserProfile.cs
│   │   ├── Tenant.cs
│   │   ├── CoachClient.cs
│   │   ├── SubscriptionPlan.cs
│   │   ├── ClientSubscription.cs
│   │   ├── SubscriptionFreeze.cs
│   │   ├── WorkoutProgram.cs
│   │   ├── ProgramRoutine.cs
│   │   ├── RoutineExercise.cs
│   │   ├── Exercise.cs
│   │   ├── Muscle.cs
│   │   ├── WorkoutSession.cs
│   │   ├── SessionSet.cs
│   │   ├── DietPlan.cs
│   │   ├── DailyMeal.cs
│   │   ├── MealItem.cs
│   │   ├── Food.cs
│   │   ├── MealLog.cs
│   │   ├── BodyMeasurement.cs
│   │   └── AuditLog.cs
│   ├── Enums/
│   │   ├── UserRole.cs
│   │   ├── SubscriptionStatus.cs
│   │   ├── PlanStatus.cs
│   │   └── GenderType.cs
│   ├── Exceptions/
│   │   ├── NotFoundException.cs
│   │   ├── ValidationException.cs
│   │   └── UnauthorizedException.cs
│   ├── ValueObjects/
│   │   └── BrandingSettings.cs
│   └── Common/
│       ├── AuditableEntity.cs
│       ├── TenantAuditableEntity.cs
│       ├── ITenantEntity.cs
│       └── ISoftDeletable.cs
│
└── LogicFit.Infrastructure/           # طبقة البنية التحتية (Infrastructure Layer)
    ├── Identity/
    │   ├── TokenService.cs
    │   └── ApplicationUser.cs
    ├── Persistence/
    │   ├── ApplicationDbContext.cs
    │   ├── Configurations/            # Entity Configurations
    │   │   ├── UserConfiguration.cs
    │   │   ├── TenantConfiguration.cs
    │   │   ├── CoachClientConfiguration.cs
    │   │   └── [...]
    │   ├── Migrations/
    │   └── SeedData/
    │       ├── DataSeeder.cs
    │       ├── tenants.json
    │       ├── muscles.json
    │       ├── exercises.json
    │       ├── foods.json
    │       └── users.json
    └── Services/
        ├── CurrentUserService.cs
        ├── DateTimeService.cs
        ├── FileUploadService.cs
        └── TenantService.cs
```

---

# 10. ملاحظات للـ Frontend Developer

## Headers المطلوبة
```javascript
// لجميع الطلبات المصادقة
headers: {
  'Authorization': 'Bearer {token}',
  'Content-Type': 'application/json'
}

// لرفع الملفات
headers: {
  'Authorization': 'Bearer {token}',
  'Content-Type': 'multipart/form-data'
}
```

## معالجة الأخطاء
```javascript
// Response Format للأخطاء
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "errors": {
    "PhoneNumber": ["Phone number is required"],
    "Password": ["Password must be at least 6 characters"]
  }
}

// HTTP Status Codes
200 - OK
201 - Created
204 - No Content (للـ Delete/Update بدون response body)
400 - Bad Request (validation errors)
401 - Unauthorized (token مفقود أو منتهي)
403 - Forbidden (ليس لديك صلاحية)
404 - Not Found
500 - Internal Server Error
```

## Pagination
```javascript
// Request
GET /api/clients?page=1&pageSize=10

// Response
{
  "items": [...],
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 5,
  "totalCount": 50,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

## Date Format
```javascript
// ISO 8601 Format
"2024-01-15T10:30:00Z"

// للإرسال
{
  "startDate": "2024-01-15",
  "dateRecorded": "2024-01-15T10:30:00"
}
```

## File Upload Example
```javascript
const formData = new FormData();
formData.append('clientId', 'guid-here');
formData.append('dateRecorded', '2024-01-15');
formData.append('weightKg', '85.5');
formData.append('inbodyImage', file1);
formData.append('frontPhoto', file2);

fetch('/api/bodymeasurements', {
  method: 'POST',
  headers: {
    'Authorization': 'Bearer ' + token
  },
  body: formData
});
```

---

# 11. الخلاصة

LogicFit هو نظام شامل لإدارة الصالات الرياضية يدعم:

✅ **Multi-tenant Architecture** - عزل كامل للبيانات
✅ **Role-Based Access Control** - Owner, Coach, Client
✅ **Coach-Client Management** - فصل المشتركين عن المتدربين، تعيين عملاء لمدربين
✅ **Workout Management** - برامج، روتينات، جلسات، تتبع
✅ **Nutrition Management** - خطط غذائية، وجبات، تتبع الاستهلاك
✅ **Subscription Management** - خطط، اشتراكات، تجميد، تقارير
✅ **Body Tracking** - قياسات InBody، صور التقدم
✅ **Reports & Analytics** - Dashboard، تقارير مالية وعملاء
✅ **Coach Reports** - لوحة تحكم المدرب، تقارير المتدربين، تقارير التقدم
✅ **File Upload** - صور وفيديوهات للتمارين والقياسات

---

*آخر تحديث: 6 ديسمبر 2024*
