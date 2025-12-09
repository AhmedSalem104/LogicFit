# LogicFit API - Frontend Implementation Guide

> **هذا الملف يحتوي على جميع الـ API endpoints مع التفاصيل الكاملة لتنفيذها في الـ Frontend بدون أخطاء**

---

## جدول المحتويات

1. [Authentication (المصادقة)](#1-authentication-المصادقة)
2. [Tenants (الصالات)](#2-tenants-الصالات)
3. [Coach-Clients (المدرب والمتدربين)](#3-coach-clients-المدرب-والمتدربين)
4. [Clients (العملاء)](#4-clients-العملاء)
5. [Users (المستخدمين)](#5-users-المستخدمين)
6. [Workout Programs (برامج التمرين)](#6-workout-programs-برامج-التمرين)
7. [Diet Plans (خطط التغذية)](#7-diet-plans-خطط-التغذية)
8. [Workout Sessions (جلسات التمرين)](#8-workout-sessions-جلسات-التمرين)
9. [Exercises (التمارين)](#9-exercises-التمارين)
10. [Foods (الأطعمة)](#10-foods-الأطعمة)
11. [Body Measurements (قياسات الجسم)](#11-body-measurements-قياسات-الجسم)
12. [Subscriptions (الاشتراكات)](#12-subscriptions-الاشتراكات)
13. [Reports (التقارير)](#13-reports-التقارير)
14. [Muscles (العضلات)](#14-muscles-العضلات)
15. [Gym Profile (ملف الصالة)](#15-gym-profile-ملف-الصالة)

---

## ملاحظات عامة مهمة

### Headers المطلوبة لكل Request

```typescript
// للـ Endpoints المحمية (تحتاج تسجيل دخول)
const headers = {
  'Content-Type': 'application/json',
  'Authorization': `Bearer ${accessToken}`
};

// للـ Endpoints العامة (لا تحتاج تسجيل دخول)
const headers = {
  'Content-Type': 'application/json'
};

// لرفع الملفات (multipart/form-data)
// لا تضع Content-Type - المتصفح سيضعها تلقائياً
const headers = {
  'Authorization': `Bearer ${accessToken}`
};
```

### رموز الاستجابة (Response Status Codes)

| Code | المعنى | الإجراء |
|------|--------|---------|
| 200 | نجاح | عرض البيانات |
| 201 | تم الإنشاء | عرض رسالة نجاح + تحديث القائمة |
| 204 | نجاح بدون محتوى | عرض رسالة نجاح |
| 400 | خطأ في البيانات | عرض رسالة الخطأ للمستخدم |
| 401 | غير مصرح | توجيه لصفحة تسجيل الدخول |
| 403 | ممنوع | عرض رسالة "ليس لديك صلاحية" |
| 404 | غير موجود | عرض رسالة "العنصر غير موجود" |
| 409 | تعارض (مكرر) | عرض رسالة "البيانات موجودة مسبقاً" |
| 500 | خطأ في السيرفر | عرض رسالة "حدث خطأ، حاول مرة أخرى" |

---

## 1. Authentication (المصادقة)

### 1.1 تسجيل مستخدم جديد

```
POST /api/auth/register
```

**Authorization:** ❌ غير مطلوب (AllowAnonymous)

**Request Body:**
```typescript
interface RegisterRequest {
  email: string;           // مطلوب - البريد الإلكتروني
  phoneNumber?: string;    // اختياري - رقم الهاتف
  password: string;        // مطلوب - كلمة المرور (8 أحرف على الأقل، حرف كبير، حرف صغير، رقم)
  confirmPassword: string; // مطلوب - تأكيد كلمة المرور (يجب أن يطابق password)
  tenantId: string;        // مطلوب - معرف الصالة (GUID)
  role?: number;           // اختياري - الدور (1=Owner, 2=Coach, 3=Client) - افتراضي: 3
  fullName: string;        // مطلوب - الاسم الكامل
}
```

**مثال الإرسال:**
```typescript
const body = {
  email: "ahmed@example.com",
  phoneNumber: "01012345678",
  password: "Ahmed@123",
  confirmPassword: "Ahmed@123",
  tenantId: "11111111-1111-1111-1111-111111111111",
  role: 3,
  fullName: "أحمد محمد"
};

this.http.post<AuthResponse>(`${apiUrl}/auth/register`, body);
```

**Response (200):**
```typescript
interface AuthResponse {
  userId: string;      // معرف المستخدم
  email: string;       // البريد الإلكتروني
  phoneNumber: string; // رقم الهاتف
  role: string;        // الدور ("Owner" | "Coach" | "Client")
  tenantId: string;    // معرف الصالة
  accessToken: string; // توكن الوصول
  refreshToken: string;// توكن التجديد
  expiresAt: string;   // وقت انتهاء التوكن
}
```

---

### 1.2 تسجيل الدخول

```
POST /api/auth/login
```

**Authorization:** ❌ غير مطلوب (AllowAnonymous)

**Request Body:**
```typescript
interface LoginRequest {
  phoneNumber: string; // مطلوب - رقم الهاتف
  password: string;    // مطلوب - كلمة المرور
  tenantId: string;    // مطلوب - معرف الصالة (GUID)
}
```

**مثال الإرسال:**
```typescript
const body = {
  phoneNumber: "01012345678",
  password: "Ahmed@123",
  tenantId: "11111111-1111-1111-1111-111111111111"
};

this.http.post<AuthResponse>(`${apiUrl}/auth/login`, body);
```

**Response (200):** نفس AuthResponse أعلاه

**Response (401):**
```json
{
  "statusCode": 401,
  "message": "Invalid credentials"
}
```

---

### 1.3 نسيت كلمة المرور

```
POST /api/auth/forget-password
```

**Authorization:** ❌ غير مطلوب

**Request Body:**
```typescript
interface ForgetPasswordRequest {
  phoneNumber: string; // مطلوب
  tenantId: string;    // مطلوب
}
```

**Response (200):**
```typescript
interface ForgetPasswordResponse {
  success: boolean;
  message: string;
  resetToken?: string; // كود التفعيل (6 أرقام)
}
```

---

### 1.4 إعادة تعيين كلمة المرور

```
POST /api/auth/reset-password
```

**Authorization:** ❌ غير مطلوب

**Request Body:**
```typescript
interface ResetPasswordRequest {
  phoneNumber: string; // مطلوب
  resetToken: string;  // مطلوب - كود التفعيل (6 أحرف/أرقام)
  newPassword: string; // مطلوب - كلمة المرور الجديدة (6 أحرف على الأقل)
  tenantId: string;    // مطلوب
}
```

**Response (200):**
```json
{
  "success": true,
  "message": "Password reset successfully"
}
```

---

## 2. Tenants (الصالات)

### 2.1 جلب جميع الصالات

```
GET /api/tenants
```

**Authorization:** ❌ غير مطلوب (للاختيار عند تسجيل الدخول)

**Parameters:** لا يوجد

**مثال الإرسال:**
```typescript
this.http.get<TenantResponse[]>(`${apiUrl}/tenants`);
```

**Response (200):**
```typescript
interface TenantResponse {
  id: string;           // معرف الصالة
  name: string;         // اسم الصالة
  subdomain: string;    // الـ subdomain
  status: number;       // الحالة (1=Active, 2=Suspended, 3=Trial, 4=Expired, 5=Cancelled)
  createdAt: string;    // تاريخ الإنشاء
}
```

---

### 2.2 إنشاء صالة جديدة

```
POST /api/tenants
```

**Authorization:** ❌ غير مطلوب (للتسجيل الأولي)

**Request Body:**
```typescript
interface CreateTenantRequest {
  name: string;           // مطلوب - اسم الصالة (حد أقصى 200 حرف)
  subdomain: string;      // مطلوب - الـ subdomain (حروف صغيرة وأرقام وشرطات فقط، حد أقصى 100)
  logoUrl?: string;       // اختياري - رابط اللوجو (حد أقصى 500 حرف)
  primaryColor?: string;  // اختياري - اللون الأساسي (مثل #3b82f6)
  secondaryColor?: string;// اختياري - اللون الثانوي (مثل #8b5cf6)
}
```

**مثال الإرسال:**
```typescript
const body = {
  name: "صالة القوة",
  subdomain: "power-gym",
  primaryColor: "#3b82f6"
};

this.http.post<TenantResponse>(`${apiUrl}/tenants`, body);
```

**Response (201):** نفس TenantResponse

---

## 3. Coach-Clients (المدرب والمتدربين)

### 3.1 جلب المتدربين المعينين للمدرب

```
GET /api/coach-clients
```

**Authorization:** ✅ مطلوب (Bearer Token)

**Query Parameters:**
```typescript
interface GetCoachClientsParams {
  coachId?: string;  // اختياري - معرف المدرب (إذا فارغ يستخدم المستخدم الحالي)
  isActive?: boolean;// اختياري - فلترة حسب الحالة (افتراضي: true)
}
```

**مثال الإرسال:**
```typescript
// جلب متدربين المدرب الحالي
this.http.get<CoachClientDto[]>(`${apiUrl}/coach-clients`);

// جلب متدربين مدرب معين
this.http.get<CoachClientDto[]>(`${apiUrl}/coach-clients?coachId=${coachId}`);

// جلب جميع المتدربين (نشط وغير نشط)
this.http.get<CoachClientDto[]>(`${apiUrl}/coach-clients?isActive=`);
```

**Response (200):**
```typescript
interface CoachClientDto {
  id: string;                    // معرف العلاقة
  coachId: string;               // معرف المدرب
  coachName: string;             // اسم المدرب
  clientId: string;              // معرف المتدرب
  clientName: string;            // اسم المتدرب
  clientPhone: string;           // رقم هاتف المتدرب
  clientEmail?: string;          // بريد المتدرب
  assignedAt: string;            // تاريخ التعيين
  unassignedAt?: string;         // تاريخ إلغاء التعيين
  isActive: boolean;             // هل نشط؟
  notes?: string;                // ملاحظات
  hasActiveSubscription: boolean;// هل لديه اشتراك نشط؟
  subscriptionEndDate?: string;  // تاريخ انتهاء الاشتراك
  workoutProgramsCount: number;  // عدد برامج التمرين
  dietPlansCount: number;        // عدد خطط التغذية
  workoutSessionsCount: number;  // عدد الجلسات
  lastSessionDate?: string;      // تاريخ آخر جلسة
}
```

---

### 3.2 إضافة متدرب جديد (⭐ الأهم)

```
POST /api/coach-clients
```

**Authorization:** ✅ مطلوب (Bearer Token)

**Request Body:**
```typescript
interface AddTraineeRequest {
  clientName: string;      // ⚠️ مطلوب - اسم المتدرب (ليس fullName!)
  clientPhone: string;     // ⚠️ مطلوب - رقم الهاتف (ليس phoneNumber!)
  clientEmail?: string;    // اختياري - البريد الإلكتروني (ليس email!)
  gender?: number;         // اختياري - الجنس (0=Male, 1=Female)
  birthDate?: string;      // اختياري - تاريخ الميلاد (YYYY-MM-DD)
  heightCm?: number;       // اختياري - الطول بالسنتيمتر
  activityLevel?: string;  // اختياري - مستوى النشاط
  medicalHistory?: string; // اختياري - التاريخ الطبي
  notes?: string;          // اختياري - ملاحظات
}
```

**⚠️ تحذير مهم - أسماء الحقول:**
| الـ Frontend يرسل | الـ Backend يتوقع |
|------------------|------------------|
| ❌ `fullName` | ✅ `clientName` |
| ❌ `phoneNumber` | ✅ `clientPhone` |
| ❌ `email` | ✅ `clientEmail` |

**مثال الإرسال الصحيح:**
```typescript
const body = {
  clientName: "أحمد محمد",        // ✅ صحيح
  clientPhone: "01012345678",     // ✅ صحيح
  clientEmail: "ahmed@email.com", // ✅ صحيح (اختياري)
  notes: "متدرب جديد"             // اختياري
};

this.http.post<string>(`${apiUrl}/coach-clients`, body);
```

**Response (200):**
```typescript
// يرجع معرف المتدرب الجديد (GUID string)
"a1b2c3d4-e5f6-7890-abcd-ef1234567890"
```

**ماذا يحدث في الـ Backend:**
1. ✅ ينشئ حساب client جديد
2. ✅ يولد password تلقائياً
3. ✅ يربط المتدرب بالمدرب الحالي تلقائياً

---

### 3.3 تعيين متدرب موجود لمدرب

```
POST /api/coach-clients/assign
```

**Authorization:** ✅ مطلوب

**Request Body:**
```typescript
interface AssignClientRequest {
  clientId: string;  // مطلوب - معرف المتدرب الموجود
  coachId?: string;  // اختياري - معرف المدرب (إذا فارغ يستخدم المدرب الحالي)
  notes?: string;    // اختياري - ملاحظات
}
```

**مثال:**
```typescript
const body = {
  clientId: "client-guid-here"
};

this.http.post<string>(`${apiUrl}/coach-clients/assign`, body);
```

---

### 3.4 إلغاء تعيين متدرب

```
DELETE /api/coach-clients/{clientId}
```

**Authorization:** ✅ مطلوب

**Path Parameters:**
- `clientId` (string): معرف المتدرب - **مطلوب**

**مثال:**
```typescript
this.http.delete(`${apiUrl}/coach-clients/${clientId}`);
```

**Response:** 204 No Content

---

## 4. Clients (العملاء)

### 4.1 جلب جميع العملاء

```
GET /api/clients
```

**Authorization:** ✅ مطلوب

**Query Parameters:**
```typescript
interface GetClientsParams {
  searchTerm?: string; // اختياري - بحث بالاسم أو الهاتف أو البريد
  isActive?: boolean;  // اختياري - فلترة حسب الحالة
}
```

**مثال:**
```typescript
// جلب الكل
this.http.get<ClientDto[]>(`${apiUrl}/clients`);

// بحث
this.http.get<ClientDto[]>(`${apiUrl}/clients?searchTerm=أحمد`);

// النشطين فقط
this.http.get<ClientDto[]>(`${apiUrl}/clients?isActive=true`);
```

**Response (200):**
```typescript
interface ClientDto {
  id: string;
  tenantId: string;
  email?: string;
  phoneNumber: string;
  isActive: boolean;
  walletBalance: number;
  profile?: {
    fullName?: string;
    gender?: number;
    birthDate?: string;
    heightCm?: number;
    activityLevel?: string;
    medicalHistory?: string;
  };
  activeSubscription?: {
    id: string;
    planName: string;
    startDate: string;
    endDate: string;
    status: string;
  };
}
```

---

### 4.2 جلب عميل بالمعرف

```
GET /api/clients/{id}
```

**Authorization:** ✅ مطلوب

**Path Parameters:**
- `id` (string): معرف العميل - **مطلوب**

**Response (200):** ClientDto
**Response (404):** Not Found

---

### 4.3 إنشاء عميل

```
POST /api/clients
```

**Authorization:** ✅ مطلوب

**Request Body:**
```typescript
interface CreateClientRequest {
  phoneNumber: string;     // مطلوب
  email?: string;          // اختياري
  password?: string;       // اختياري (يتولد تلقائياً)
  fullName?: string;       // اختياري
  gender?: number;         // اختياري (0=Male, 1=Female)
  birthDate?: string;      // اختياري
  heightCm?: number;       // اختياري
  activityLevel?: string;  // اختياري
  medicalHistory?: string; // اختياري
  coachId?: string;        // اختياري (يعين للمدرب تلقائياً)
}
```

**Response (200):** GUID string

---

### 4.4 تحديث عميل

```
PUT /api/clients/{id}
```

**Authorization:** ✅ مطلوب

**Request Body:**
```typescript
interface UpdateClientRequest {
  phoneNumber: string;     // مطلوب
  email?: string;
  isActive: boolean;
  fullName?: string;
  gender?: number;
  birthDate?: string;
  heightCm?: number;
  activityLevel?: string;
  medicalHistory?: string;
}
```

**Response:** 204 No Content

---

### 4.5 حذف عميل

```
DELETE /api/clients/{id}
```

**Authorization:** ✅ مطلوب

**Response:** 204 No Content

---

## 5. Users (المستخدمين)

### 5.1 جلب جميع المستخدمين

```
GET /api/users
```

**Authorization:** ✅ مطلوب

**Query Parameters:**
```typescript
interface GetUsersParams {
  role?: number;       // اختياري (1=Owner, 2=Coach, 3=Client)
  isActive?: boolean;  // اختياري
  searchTerm?: string; // اختياري
}
```

**مثال:**
```typescript
// جلب المدربين فقط
this.http.get<UserDto[]>(`${apiUrl}/users?role=2`);
```

**Response (200):**
```typescript
interface UserDto {
  id: string;
  tenantId: string;
  email: string;
  phoneNumber?: string;
  role: number;        // 1=Owner, 2=Coach, 3=Client
  isActive: boolean;
  walletBalance: number;
  profile?: {
    fullName?: string;
    gender?: number;
    birthDate?: string;
    heightCm?: number;
    activityLevel?: string;
    medicalHistory?: string;
  };
}
```

---

### 5.2 جلب مستخدم بالمعرف

```
GET /api/users/{id}
```

**Response (200):** UserDto
**Response (404):** Not Found

---

### 5.3 تحديث مستخدم

```
PUT /api/users/{id}
```

**Request Body:**
```typescript
interface UpdateUserRequest {
  phoneNumber?: string;
  isActive: boolean;
}
```

**Response:** 204 No Content

---

### 5.4 تحديث ملف المستخدم الشخصي

```
PUT /api/users/{id}/profile
```

**Request Body:**
```typescript
interface UpdateUserProfileRequest {
  fullName?: string;
  gender?: number;
  birthDate?: string;
  heightCm?: number;
  activityLevel?: string;
  medicalHistory?: string;
}
```

**Response:** 204 No Content

---

## 6. Workout Programs (برامج التمرين)

### 6.1 جلب برامج التمرين

```
GET /api/workoutprograms
```

**Authorization:** ✅ مطلوب

**Query Parameters:**
```typescript
interface GetWorkoutProgramsParams {
  coachId?: string;  // اختياري - فلترة حسب المدرب
  clientId?: string; // اختياري - فلترة حسب المتدرب
}
```

**Response (200):**
```typescript
interface WorkoutProgramDto {
  id: string;
  tenantId: string;
  coachId: string;
  coachName?: string;
  clientId: string;
  clientName?: string;
  name: string;
  startDate: string;
  endDate?: string;
  routines: ProgramRoutineDto[];
}

interface ProgramRoutineDto {
  id: string;
  programId: string;
  name: string;
  dayOfWeek: number;  // 0=Sunday, 1=Monday, ...
  exercises: RoutineExerciseDto[];
}

interface RoutineExerciseDto {
  id: string;
  routineId: string;
  exerciseId: number;
  exerciseName?: string;
  sets: number;
  repsMin: number;
  repsMax: number;
  restSec: number;
  supersetGroupId?: string;
}
```

---

### 6.2 جلب برنامج واحد

```
GET /api/workoutprograms/{id}
```

---

### 6.3 إنشاء برنامج تمرين

```
POST /api/workoutprograms
```

**Request Body:**
```typescript
interface CreateWorkoutProgramRequest {
  clientId: string;    // مطلوب - معرف المتدرب
  name: string;        // مطلوب - اسم البرنامج
  startDate: string;   // مطلوب - تاريخ البدء (YYYY-MM-DD)
  endDate?: string;    // اختياري - تاريخ الانتهاء
}
```

**مثال:**
```typescript
const body = {
  clientId: "client-guid",
  name: "برنامج بناء العضلات",
  startDate: "2024-01-01",
  endDate: "2024-03-31"
};

this.http.post<string>(`${apiUrl}/workoutprograms`, body);
```

**Response (201):** GUID string (معرف البرنامج)

---

### 6.4 إضافة روتين للبرنامج

```
POST /api/workoutprograms/{programId}/routines
```

**Path Parameters:**
- `programId` (string): معرف البرنامج - **مطلوب**

**Request Body:**
```typescript
interface CreateRoutineRequest {
  name: string;      // مطلوب - اسم الروتين
  dayOfWeek: number; // مطلوب - اليوم (0=الأحد إلى 6=السبت)
}
```

**Response (200):** GUID string (معرف الروتين)

---

### 6.5 إضافة تمرين للروتين

```
POST /api/workoutprograms/routines/{routineId}/exercises
```

**Path Parameters:**
- `routineId` (string): معرف الروتين - **مطلوب**

**Request Body:**
```typescript
interface CreateRoutineExerciseRequest {
  exerciseId: number;     // مطلوب - معرف التمرين
  sets: number;           // مطلوب - عدد المجموعات
  repsMin: number;        // مطلوب - الحد الأدنى للتكرارات
  repsMax: number;        // مطلوب - الحد الأقصى للتكرارات
  restSec: number;        // مطلوب - وقت الراحة بالثواني
  supersetGroupId?: string; // اختياري - معرف مجموعة السوبرست
}
```

**Response (200):** GUID string

---

### 6.6 حذف برنامج

```
DELETE /api/workoutprograms/{id}
```

**Response:** 204 No Content

---

## 7. Diet Plans (خطط التغذية)

### 7.1 جلب خطط التغذية

```
GET /api/dietplans
```

**Query Parameters:**
```typescript
interface GetDietPlansParams {
  coachId?: string;
  clientId?: string;
  status?: string;  // "Active" | "Completed" | "Cancelled"
}
```

**Response (200):**
```typescript
interface DietPlanDto {
  id: string;
  tenantId: string;
  coachId: string;
  coachName?: string;
  clientId: string;
  clientName?: string;
  name: string;
  startDate: string;
  endDate?: string;
  status: string;
  targetCalories?: number;
  targetProtein?: number;
  targetCarbs?: number;
  targetFats?: number;
  meals: DailyMealDto[];
}

interface DailyMealDto {
  id: string;
  planId: string;
  name: string;
  orderIndex: number;
  items: MealItemDto[];
}

interface MealItemDto {
  id: string;
  mealId: string;
  foodId: number;
  foodName?: string;
  assignedQuantity: number;
  calcCalories: number;
  calcProtein: number;
  calcCarbs: number;
  calcFats: number;
}
```

---

### 7.2 إنشاء خطة تغذية

```
POST /api/dietplans
```

**Request Body:**
```typescript
interface CreateDietPlanRequest {
  clientId: string;       // مطلوب
  name: string;           // مطلوب
  startDate: string;      // مطلوب
  endDate?: string;       // اختياري
  targetCalories?: number;// اختياري
  targetProtein?: number; // اختياري
  targetCarbs?: number;   // اختياري
  targetFats?: number;    // اختياري
}
```

**Response (201):** GUID string

---

### 7.3 إضافة وجبة للخطة

```
POST /api/dietplans/{planId}/meals
```

**Request Body:**
```typescript
interface CreateMealRequest {
  name: string;       // مطلوب - اسم الوجبة (فطور، غداء، عشاء...)
  orderIndex: number; // مطلوب - ترتيب الوجبة
}
```

**Response (200):** GUID string

---

### 7.4 إضافة عنصر للوجبة

```
POST /api/dietplans/meals/{mealId}/items
```

**Request Body:**
```typescript
interface CreateMealItemRequest {
  foodId: number;          // مطلوب - معرف الطعام
  assignedQuantity: number;// مطلوب - الكمية بالجرام
}
```

**Response (200):** GUID string

---

### 7.5 حذف خطة

```
DELETE /api/dietplans/{id}
```

**Response:** 204 No Content

---

## 8. Workout Sessions (جلسات التمرين)

### 8.1 جلب الجلسات

```
GET /api/workoutsessions
```

**Query Parameters:**
```typescript
interface GetSessionsParams {
  clientId?: string;
  fromDate?: string;  // YYYY-MM-DD
  toDate?: string;    // YYYY-MM-DD
}
```

**Response (200):**
```typescript
interface WorkoutSessionDto {
  id: string;
  tenantId: string;
  clientId: string;
  clientName?: string;
  routineId: string;
  routineName?: string;
  startedAt: string;
  endedAt?: string;
  totalVolumLifted: number;
  notes?: string;
  sets: SessionSetDto[];
}

interface SessionSetDto {
  id: string;
  sessionId: string;
  exerciseId: number;
  exerciseName?: string;
  setNumber: number;
  weightKg: number;
  reps: number;
  rpe?: number;
  volumeLoad: number;
  isPr: boolean;
}
```

---

### 8.2 بدء جلسة تمرين

```
POST /api/workoutsessions/start
```

**Request Body:**
```typescript
interface StartSessionRequest {
  routineId: string; // مطلوب - معرف الروتين
}
```

**Response (201):** GUID string (معرف الجلسة)

---

### 8.3 إنهاء جلسة تمرين

```
POST /api/workoutsessions/{sessionId}/end
```

**Request Body:**
```typescript
interface EndSessionRequest {
  notes?: string; // اختياري - ملاحظات
}
```

**Response:** 204 No Content

---

### 8.4 إضافة مجموعة للجلسة

```
POST /api/workoutsessions/{sessionId}/sets
```

**Request Body:**
```typescript
interface CreateSetRequest {
  exerciseId: number; // مطلوب
  setNumber: number;  // مطلوب
  weightKg: number;   // مطلوب
  reps: number;       // مطلوب
  rpe?: number;       // اختياري (Rate of Perceived Exertion 1-10)
}
```

**Response (200):** GUID string

---

## 9. Exercises (التمارين)

### 9.1 جلب التمارين

```
GET /api/exercises
```

**Query Parameters:**
```typescript
interface GetExercisesParams {
  targetMuscleId?: number;
  equipment?: string;
  isHighImpact?: boolean;
}
```

**Response (200):**
```typescript
interface ExerciseDto {
  id: number;
  tenantId?: string;
  name: string;
  targetMuscleId: number;
  targetMuscleName?: string;
  imageUrl?: string;
  videoUrl?: string;
  equipment?: string;
  isHighImpact: boolean;
}
```

---

### 9.2 إنشاء تمرين (مع رفع صور)

```
POST /api/exercises
```

**Content-Type:** `multipart/form-data`

**Form Data:**
```typescript
const formData = new FormData();
formData.append('name', 'ضغط الصدر');           // مطلوب
formData.append('targetMuscleId', '1');         // مطلوب
formData.append('equipment', 'بار أو دمبل');    // اختياري
formData.append('isHighImpact', 'false');       // اختياري
formData.append('image', imageFile);            // اختياري - ملف الصورة
formData.append('video', videoFile);            // اختياري - ملف الفيديو

this.http.post<number>(`${apiUrl}/exercises`, formData);
```

**Response (201):** number (معرف التمرين)

---

### 9.3 تحديث تمرين

```
PUT /api/exercises/{id}
```

**Content-Type:** `multipart/form-data`

**Response:** 204 No Content

---

### 9.4 حذف تمرين

```
DELETE /api/exercises/{id}
```

**Response:** 204 No Content

---

## 10. Foods (الأطعمة)

### 10.1 جلب الأطعمة

```
GET /api/foods
```

**Query Parameters:**
```typescript
interface GetFoodsParams {
  category?: string;
  searchTerm?: string;
  isVerified?: boolean;
}
```

**Response (200):**
```typescript
interface FoodDto {
  id: number;
  tenantId?: string;
  name: string;
  category?: string;
  caloriesPer100g: number;
  proteinPer100g: number;
  carbsPer100g: number;
  fatsPer100g: number;
  fiberPer100g?: number;
  alternativeGroupId?: string;
  isVerified: boolean;
}
```

---

### 10.2 إنشاء طعام

```
POST /api/foods
```

**Request Body:**
```typescript
interface CreateFoodRequest {
  name: string;              // مطلوب
  category?: string;         // اختياري
  caloriesPer100g: number;   // مطلوب
  proteinPer100g: number;    // مطلوب
  carbsPer100g: number;      // مطلوب
  fatsPer100g: number;       // مطلوب
  fiberPer100g?: number;     // اختياري
  alternativeGroupId?: string;// اختياري
}
```

**Response (201):** number

---

### 10.3 تحديث طعام

```
PUT /api/foods/{id}
```

**Response:** 204 No Content

---

### 10.4 حذف طعام

```
DELETE /api/foods/{id}
```

**Response:** 204 No Content

---

## 11. Body Measurements (قياسات الجسم)

### 11.1 جلب القياسات

```
GET /api/bodymeasurements
```

**Query Parameters:**
```typescript
interface GetMeasurementsParams {
  clientId?: string;
  fromDate?: string;
  toDate?: string;
}
```

**Response (200):**
```typescript
interface BodyMeasurementDto {
  id: string;
  tenantId: string;
  clientId: string;
  clientName?: string;
  dateRecorded: string;
  weightKg?: number;
  skeletalMuscleMass?: number;
  bodyFatMass?: number;
  bodyFatPercent?: number;
  totalBodyWater?: number;
  bmr?: number;
  visceralFatLevel?: number;
  inbodyImageUrl?: string;
  frontPhotoUrl?: string;
  sidePhotoUrl?: string;
  backPhotoUrl?: string;
}
```

---

### 11.2 إنشاء قياس (مع رفع صور)

```
POST /api/bodymeasurements
```

**Content-Type:** `multipart/form-data`

**Form Data:**
```typescript
const formData = new FormData();
formData.append('clientId', 'client-guid');           // مطلوب
formData.append('dateRecorded', '2024-01-15');        // مطلوب
formData.append('weightKg', '75.5');                  // اختياري
formData.append('skeletalMuscleMass', '35.2');        // اختياري
formData.append('bodyFatMass', '12.3');               // اختياري
formData.append('bodyFatPercent', '16.4');            // اختياري
formData.append('totalBodyWater', '45.0');            // اختياري
formData.append('bmr', '1650');                       // اختياري
formData.append('visceralFatLevel', '8');             // اختياري
formData.append('inbodyImage', inbodyFile);           // اختياري
formData.append('frontPhoto', frontFile);             // اختياري
formData.append('sidePhoto', sideFile);               // اختياري
formData.append('backPhoto', backFile);               // اختياري

this.http.post<string>(`${apiUrl}/bodymeasurements`, formData);
```

**Response (200):** GUID string

---

### 11.3 حذف قياس

```
DELETE /api/bodymeasurements/{id}
```

**Response:** 204 No Content

---

## 12. Subscriptions (الاشتراكات)

### 12.1 جلب خطط الاشتراك

```
GET /api/subscriptions/plans
```

**Response (200):**
```typescript
interface SubscriptionPlanDto {
  id: string;
  tenantId: string;
  name: string;
  price: number;
  durationMonths: number;
}
```

---

### 12.2 إنشاء خطة اشتراك

```
POST /api/subscriptions/plans
```

**Request Body:**
```typescript
interface CreatePlanRequest {
  name: string;          // مطلوب
  price: number;         // مطلوب
  durationMonths: number;// مطلوب
}
```

**Response (200):** GUID string

---

### 12.3 جلب اشتراكات العملاء

```
GET /api/subscriptions
```

**Query Parameters:**
```typescript
interface GetSubscriptionsParams {
  clientId?: string;
  status?: string; // "Active" | "Expired" | "Cancelled" | "Frozen"
}
```

**Response (200):**
```typescript
interface ClientSubscriptionDto {
  id: string;
  tenantId: string;
  clientId: string;
  clientName?: string;
  planId: string;
  planName?: string;
  startDate: string;
  endDate: string;
  status: string;
  salesCoachId?: string;
  salesCoachName?: string;
  freezes: SubscriptionFreezeDto[];
}

interface SubscriptionFreezeDto {
  id: string;
  subscriptionId: string;
  startDate: string;
  endDate: string;
  reason?: string;
}
```

---

### 12.4 إنشاء اشتراك لعميل

```
POST /api/subscriptions
```

**Request Body:**
```typescript
interface CreateSubscriptionRequest {
  clientId: string;  // مطلوب
  planId: string;    // مطلوب
  startDate: string; // مطلوب
}
```

**Response (200):** GUID string

---

### 12.5 تجميد اشتراك

```
POST /api/subscriptions/{subscriptionId}/freeze
```

**Request Body:**
```typescript
interface FreezeRequest {
  startDate: string;  // مطلوب
  endDate: string;    // مطلوب
  reason?: string;    // اختياري
}
```

**Response (200):** GUID string

---

### 12.6 إلغاء اشتراك

```
POST /api/subscriptions/{subscriptionId}/cancel
```

**Request Body:** فارغ `{}`

**Response:** 204 No Content

---

## 13. Reports (التقارير)

### 13.1 تقرير لوحة التحكم (Owner)

```
GET /api/reports/dashboard
```

**Response (200):**
```typescript
interface DashboardReportDto {
  totalClients: number;
  activeClients: number;
  newClientsThisMonth: number;
  totalCoaches: number;
  activeSubscriptions: number;
  expiringSubscriptions: number;
  totalRevenueThisMonth: number;
  totalRevenueLastMonth: number;
  totalWorkoutsThisMonth: number;
  totalDietPlansActive: number;
}
```

---

### 13.2 تقرير العملاء

```
GET /api/reports/clients
```

**Query Parameters:**
- `fromDate?: string`
- `toDate?: string`

**Response (200):**
```typescript
interface ClientsReportDto {
  totalClients: number;
  activeClients: number;
  inactiveClients: number;
  newClientsThisMonth: number;
  clientsWithActiveSubscription: number;
  clientsWithoutSubscription: number;
  topClients: ClientSummaryDto[];
  monthlyTrend: MonthlyClientDto[];
}
```

---

### 13.3 تقرير الاشتراكات

```
GET /api/reports/subscriptions
```

**Query Parameters:**
- `fromDate?: string`
- `toDate?: string`

**Response (200):**
```typescript
interface SubscriptionsReportDto {
  totalSubscriptions: number;
  activeSubscriptions: number;
  expiredSubscriptions: number;
  cancelledSubscriptions: number;
  expiringIn7Days: number;
  expiringIn30Days: number;
  totalRevenue: number;
  revenueThisMonth: number;
  planStatistics: SubscriptionPlanStatsDto[];
  monthlyRevenue: MonthlyRevenueDto[];
}
```

---

### 13.4 التقرير المالي

```
GET /api/reports/financial
```

**Query Parameters:**
- `fromDate?: string`
- `toDate?: string`

**Response (200):**
```typescript
interface FinancialReportDto {
  totalRevenue: number;
  revenueThisMonth: number;
  revenueLastMonth: number;
  growthPercentage: number;
  averageSubscriptionValue: number;
  totalWalletBalance: number;
  monthlyRevenue: MonthlyRevenueDto[];
  paymentMethods: PaymentMethodStatsDto[];
}
```

---

### 13.5 تقرير لوحة تحكم المدرب

```
GET /api/reports/coach/dashboard
```

**Query Parameters:**
- `coachId?: string` (إذا فارغ يستخدم المستخدم الحالي)

**Response (200):**
```typescript
interface CoachDashboardReportDto {
  totalTrainees: number;
  activeTrainees: number;
  newTraineesThisMonth: number;
  activeWorkoutPrograms: number;
  activeDietPlans: number;
  totalSessionsThisMonth: number;
  totalVolumeThisMonth: number;
  topTraineesByProgress: TopTraineeDto[];
  topTraineesBySessions: TopTraineeDto[];
}
```

---

### 13.6 تقرير متدربين المدرب

```
GET /api/reports/coach/trainees
```

**Query Parameters:**
- `coachId?: string`

**Response (200):**
```typescript
interface CoachTraineesReportDto {
  totalTrainees: number;
  withActiveSubscription: number;
  withoutSubscription: number;
  trainees: TraineeDetailDto[];
}
```

---

### 13.7 تقرير تقدم متدرب

```
GET /api/reports/coach/trainee/{clientId}
```

**Path Parameters:**
- `clientId` (string): معرف المتدرب - **مطلوب**

**Response (200):**
```typescript
interface TraineeProgressReportDto {
  clientId: string;
  clientName: string;
  clientPhone?: string;
  assignedAt: string;
  bodyMeasurements: BodyProgressDto[];
  startWeight?: number;
  currentWeight?: number;
  totalWeightChange?: number;
  startBodyFat?: number;
  currentBodyFat?: number;
  totalBodyFatChange?: number;
  startMuscleMass?: number;
  currentMuscleMass?: number;
  totalMuscleMassChange?: number;
  totalSessions: number;
  totalVolumeLifted: number;
  monthlySessions: MonthlySessionsDto[];
  workoutPrograms: ActiveProgramDto[];
  dietPlans: ActivePlanDto[];
  personalRecords: PersonalRecordDto[];
}
```

---

## 14. Muscles (العضلات)

### 14.1 جلب العضلات

```
GET /api/muscles
```

**Query Parameters:**
- `bodyPart?: string` (مثل "chest", "back", "legs")

**Response (200):**
```typescript
interface MuscleDto {
  id: number;
  name: string;
  bodyPart?: string;
}
```

---

### 14.2 جلب عضلة واحدة

```
GET /api/muscles/{id}
```

**Response (200):** MuscleDto
**Response (404):** Not Found

---

## 15. Gym Profile (ملف الصالة)

### 15.1 جلب ملف الصالة

```
GET /api/gymprofile
```

**Response (200):**
```typescript
interface GymProfileDto {
  id: string;
  name: string;
  subdomain?: string;
  description?: string;
  address?: string;
  phoneNumber?: string;
  email?: string;
  logoUrl?: string;
  coverImageUrl?: string;
  galleryImages: string[];
  status: string;
  brandingSettings?: {
    primaryColor?: string;
    secondaryColor?: string;
    logoUrl?: string;
  };
  statistics: {
    totalClients: number;
    activeClients: number;
    totalCoaches: number;
    totalSubscriptionPlans: number;
    activeSubscriptions: number;
  };
}
```

---

### 15.2 تحديث ملف الصالة

```
PUT /api/gymprofile
```

**Request Body:**
```typescript
interface UpdateGymProfileRequest {
  name?: string;
  description?: string;
  address?: string;
  phoneNumber?: string;
  email?: string;
  logoUrl?: string;
  coverImageUrl?: string;
  galleryImages?: string[];
  primaryColor?: string;
  secondaryColor?: string;
}
```

**Response:** 204 No Content

---

### 15.3 رفع شعار الصالة

```
POST /api/gymprofile/logo
```

**Content-Type:** `multipart/form-data`

**Form Data:**
```typescript
const formData = new FormData();
formData.append('file', logoFile); // مطلوب

this.http.post<{url: string}>(`${apiUrl}/gymprofile/logo`, formData);
```

**Response (200):**
```json
{
  "url": "/uploads/logos/gym-logo.png"
}
```

---

### 15.4 رفع صورة الغلاف

```
POST /api/gymprofile/cover
```

**Content-Type:** `multipart/form-data`

**Response (200):**
```json
{
  "url": "/uploads/covers/gym-cover.png"
}
```

---

### 15.5 رفع صور المعرض

```
POST /api/gymprofile/gallery
```

**Content-Type:** `multipart/form-data`

**Form Data:**
```typescript
const formData = new FormData();
files.forEach(file => {
  formData.append('files', file);
});

this.http.post<{urls: string[]}>(`${apiUrl}/gymprofile/gallery`, formData);
```

**Response (200):**
```json
{
  "urls": [
    "/uploads/gallery/image1.png",
    "/uploads/gallery/image2.png"
  ]
}
```

---

## ملخص سريع - جدول جميع الـ Endpoints

| # | Method | Route | Auth | الوصف |
|---|--------|-------|------|-------|
| 1 | POST | `/api/auth/register` | ❌ | تسجيل مستخدم |
| 2 | POST | `/api/auth/login` | ❌ | تسجيل دخول |
| 3 | POST | `/api/auth/forget-password` | ❌ | نسيت كلمة المرور |
| 4 | POST | `/api/auth/reset-password` | ❌ | إعادة تعيين كلمة المرور |
| 5 | GET | `/api/tenants` | ❌ | جلب الصالات |
| 6 | POST | `/api/tenants` | ❌ | إنشاء صالة |
| 7 | GET | `/api/coach-clients` | ✅ | جلب متدربين المدرب |
| 8 | POST | `/api/coach-clients` | ✅ | ⭐ إضافة متدرب جديد |
| 9 | POST | `/api/coach-clients/assign` | ✅ | تعيين متدرب موجود |
| 10 | DELETE | `/api/coach-clients/{id}` | ✅ | إلغاء تعيين متدرب |
| 11 | GET | `/api/clients` | ✅ | جلب العملاء |
| 12 | GET | `/api/clients/{id}` | ✅ | جلب عميل |
| 13 | POST | `/api/clients` | ✅ | إنشاء عميل |
| 14 | PUT | `/api/clients/{id}` | ✅ | تحديث عميل |
| 15 | DELETE | `/api/clients/{id}` | ✅ | حذف عميل |
| 16 | GET | `/api/users` | ✅ | جلب المستخدمين |
| 17 | GET | `/api/users/{id}` | ✅ | جلب مستخدم |
| 18 | PUT | `/api/users/{id}` | ✅ | تحديث مستخدم |
| 19 | PUT | `/api/users/{id}/profile` | ✅ | تحديث الملف الشخصي |
| 20 | GET | `/api/workoutprograms` | ✅ | جلب برامج التمرين |
| 21 | GET | `/api/workoutprograms/{id}` | ✅ | جلب برنامج |
| 22 | POST | `/api/workoutprograms` | ✅ | إنشاء برنامج |
| 23 | POST | `/api/workoutprograms/{id}/routines` | ✅ | إضافة روتين |
| 24 | POST | `/api/workoutprograms/routines/{id}/exercises` | ✅ | إضافة تمرين للروتين |
| 25 | DELETE | `/api/workoutprograms/{id}` | ✅ | حذف برنامج |
| 26 | GET | `/api/dietplans` | ✅ | جلب خطط التغذية |
| 27 | GET | `/api/dietplans/{id}` | ✅ | جلب خطة |
| 28 | POST | `/api/dietplans` | ✅ | إنشاء خطة |
| 29 | POST | `/api/dietplans/{id}/meals` | ✅ | إضافة وجبة |
| 30 | POST | `/api/dietplans/meals/{id}/items` | ✅ | إضافة عنصر للوجبة |
| 31 | DELETE | `/api/dietplans/{id}` | ✅ | حذف خطة |
| 32 | GET | `/api/workoutsessions` | ✅ | جلب الجلسات |
| 33 | GET | `/api/workoutsessions/{id}` | ✅ | جلب جلسة |
| 34 | POST | `/api/workoutsessions/start` | ✅ | بدء جلسة |
| 35 | POST | `/api/workoutsessions/{id}/end` | ✅ | إنهاء جلسة |
| 36 | POST | `/api/workoutsessions/{id}/sets` | ✅ | إضافة مجموعة |
| 37 | GET | `/api/exercises` | ✅ | جلب التمارين |
| 38 | GET | `/api/exercises/{id}` | ✅ | جلب تمرين |
| 39 | POST | `/api/exercises` | ✅ | إنشاء تمرين |
| 40 | PUT | `/api/exercises/{id}` | ✅ | تحديث تمرين |
| 41 | DELETE | `/api/exercises/{id}` | ✅ | حذف تمرين |
| 42 | GET | `/api/foods` | ✅ | جلب الأطعمة |
| 43 | GET | `/api/foods/{id}` | ✅ | جلب طعام |
| 44 | POST | `/api/foods` | ✅ | إنشاء طعام |
| 45 | PUT | `/api/foods/{id}` | ✅ | تحديث طعام |
| 46 | DELETE | `/api/foods/{id}` | ✅ | حذف طعام |
| 47 | GET | `/api/bodymeasurements` | ✅ | جلب القياسات |
| 48 | POST | `/api/bodymeasurements` | ✅ | إنشاء قياس |
| 49 | DELETE | `/api/bodymeasurements/{id}` | ✅ | حذف قياس |
| 50 | GET | `/api/subscriptions/plans` | ✅ | جلب خطط الاشتراك |
| 51 | POST | `/api/subscriptions/plans` | ✅ | إنشاء خطة اشتراك |
| 52 | GET | `/api/subscriptions` | ✅ | جلب الاشتراكات |
| 53 | POST | `/api/subscriptions` | ✅ | إنشاء اشتراك |
| 54 | POST | `/api/subscriptions/{id}/freeze` | ✅ | تجميد اشتراك |
| 55 | POST | `/api/subscriptions/{id}/cancel` | ✅ | إلغاء اشتراك |
| 56 | GET | `/api/reports/dashboard` | ✅ | تقرير لوحة التحكم |
| 57 | GET | `/api/reports/clients` | ✅ | تقرير العملاء |
| 58 | GET | `/api/reports/subscriptions` | ✅ | تقرير الاشتراكات |
| 59 | GET | `/api/reports/financial` | ✅ | التقرير المالي |
| 60 | GET | `/api/reports/coach/dashboard` | ✅ | تقرير لوحة تحكم المدرب |
| 61 | GET | `/api/reports/coach/trainees` | ✅ | تقرير متدربين المدرب |
| 62 | GET | `/api/reports/coach/trainee/{id}` | ✅ | تقرير تقدم متدرب |
| 63 | GET | `/api/muscles` | ✅ | جلب العضلات |
| 64 | GET | `/api/muscles/{id}` | ✅ | جلب عضلة |
| 65 | GET | `/api/gymprofile` | ✅ | جلب ملف الصالة |
| 66 | PUT | `/api/gymprofile` | ✅ | تحديث ملف الصالة |
| 67 | POST | `/api/gymprofile/logo` | ✅ | رفع الشعار |
| 68 | POST | `/api/gymprofile/cover` | ✅ | رفع صورة الغلاف |
| 69 | POST | `/api/gymprofile/gallery` | ✅ | رفع صور المعرض |

---

## نصائح مهمة للتنفيذ

### 1. التعامل مع الأخطاء
```typescript
this.http.post(url, body).subscribe({
  next: (response) => {
    // نجاح
    this.messageService.add({
      severity: 'success',
      summary: 'تم بنجاح',
      detail: 'تمت العملية بنجاح'
    });
  },
  error: (err) => {
    // فشل
    if (err.status === 401) {
      this.router.navigate(['/auth/login']);
    } else if (err.status === 400) {
      this.messageService.add({
        severity: 'error',
        summary: 'خطأ',
        detail: err.error?.message || 'بيانات غير صحيحة'
      });
    } else {
      this.messageService.add({
        severity: 'error',
        summary: 'خطأ',
        detail: 'حدث خطأ، حاول مرة أخرى'
      });
    }
  }
});
```

### 2. إضافة Token تلقائياً (HTTP Interceptor)
```typescript
// auth.interceptor.ts
intercept(req: HttpRequest<any>, next: HttpHandler) {
  const token = localStorage.getItem('access_token');
  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }
  return next.handle(req);
}
```

### 3. التحقق قبل الإرسال
```typescript
// تأكد من إرسال الحقول الصحيحة
const body = {
  clientName: formValue.fullName,      // ✅ تحويل الاسم
  clientPhone: formValue.phoneNumber,  // ✅ تحويل الهاتف
  clientEmail: formValue.email         // ✅ تحويل البريد
};
```


