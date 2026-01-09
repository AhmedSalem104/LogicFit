# LogicFit API Documentation
# دليل واجهة برمجة التطبيقات (API)

> **Base URL:** `https://your-domain.com/api`
>
> **Authentication:** JWT Bearer Token (ما عدا Auth endpoints)

---

## فهرس المحتويات

1. [Authentication (المصادقة)](#1-authentication-المصادقة)
2. [Profile (الملف الشخصي)](#2-profile-الملف-الشخصي)
3. [Tenants (المنشآت)](#3-tenants-المنشآت)
4. [Gym Profile (ملف الصالة)](#4-gym-profile-ملف-الصالة)
5. [Users (المستخدمين)](#5-users-المستخدمين)
6. [Clients (العملاء)](#6-clients-العملاء)
7. [Coach-Clients (ربط المدرب بالعملاء)](#7-coach-clients-ربط-المدرب-بالعملاء)
8. [Muscles (العضلات)](#8-muscles-العضلات)
9. [Exercises (التمارين)](#9-exercises-التمارين)
10. [Foods (الأطعمة)](#10-foods-الأطعمة)
11. [Workout Programs (برامج التدريب)](#11-workout-programs-برامج-التدريب)
12. [Diet Plans (خطط التغذية)](#12-diet-plans-خطط-التغذية)
13. [Workout Sessions (جلسات التدريب)](#13-workout-sessions-جلسات-التدريب)
14. [Body Measurements (قياسات الجسم)](#14-body-measurements-قياسات-الجسم)
15. [Subscriptions (الاشتراكات)](#15-subscriptions-الاشتراكات)
16. [Reports (التقارير)](#16-reports-التقارير)

---

## إعداد الـ Frontend

### Headers المطلوبة

```javascript
// للـ endpoints المحمية
const headers = {
  'Content-Type': 'application/json',
  'Authorization': `Bearer ${accessToken}`
};

// لرفع الملفات
const fileHeaders = {
  'Authorization': `Bearer ${accessToken}`
  // لا تضع Content-Type - سيتم تعيينه تلقائياً
};
```

### مثال على Axios Instance

```javascript
import axios from 'axios';

const api = axios.create({
  baseURL: 'https://your-domain.com/api',
  headers: {
    'Content-Type': 'application/json'
  }
});

// Interceptor لإضافة Token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Interceptor للتعامل مع الأخطاء
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // إعادة التوجيه لصفحة تسجيل الدخول
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default api;
```

---

## 1. Authentication (المصادقة)

> **ملاحظة:** جميع endpoints المصادقة لا تحتاج Token

### 1.1 تسجيل حساب جديد

```http
POST /api/auth/register
```

**Request Body:**
```json
{
  "email": "user@example.com",
  "phoneNumber": "01012345678",
  "password": "Password123!",
  "confirmPassword": "Password123!",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "role": 3,
  "fullName": "Ahmed Mohamed"
}
```

**Role Values:**
| Value | Role |
|-------|------|
| 1 | Owner (مالك) |
| 2 | Coach (مدرب) |
| 3 | Client (عميل) |

**Response (200):**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "phoneNumber": "01012345678",
  "role": "Client",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
  "expiresAt": "2024-01-15T10:30:00Z"
}
```

**Frontend Example:**
```javascript
const register = async (userData) => {
  try {
    const response = await api.post('/auth/register', userData);

    // حفظ الـ tokens
    localStorage.setItem('accessToken', response.data.accessToken);
    localStorage.setItem('refreshToken', response.data.refreshToken);
    localStorage.setItem('user', JSON.stringify(response.data));

    return response.data;
  } catch (error) {
    throw error.response?.data || error;
  }
};
```

---

### 1.2 تسجيل الدخول

```http
POST /api/auth/login
```

**Request Body:**
```json
{
  "phoneNumber": "01012345678",
  "password": "Password123!",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Response (200):**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "phoneNumber": "01012345678",
  "role": "Client",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
  "expiresAt": "2024-01-15T10:30:00Z"
}
```

**Frontend Example:**
```javascript
const login = async (phoneNumber, password, tenantId) => {
  try {
    const response = await api.post('/auth/login', {
      phoneNumber,
      password,
      tenantId
    });

    localStorage.setItem('accessToken', response.data.accessToken);
    localStorage.setItem('refreshToken', response.data.refreshToken);
    localStorage.setItem('user', JSON.stringify(response.data));

    return response.data;
  } catch (error) {
    if (error.response?.status === 401) {
      throw new Error('رقم الهاتف أو كلمة المرور غير صحيحة');
    }
    throw error;
  }
};
```

---

### 1.3 نسيت كلمة المرور

```http
POST /api/auth/forget-password
```

**Request Body:**
```json
{
  "email": "user@example.com",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

---

### 1.4 إعادة تعيين كلمة المرور

```http
POST /api/auth/reset-password
```

**Request Body:**
```json
{
  "email": "user@example.com",
  "token": "reset-token-from-email",
  "newPassword": "NewPassword123!",
  "confirmPassword": "NewPassword123!"
}
```

---

## 2. Profile (الملف الشخصي)

> **Authorization:** مطلوب

### 2.1 الحصول على الملف الشخصي

```http
GET /api/profile
```

**Response (200):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "phoneNumber": "01012345678",
  "role": 3,
  "isActive": true,
  "walletBalance": 0.00,
  "profile": {
    "fullName": "Ahmed Mohamed",
    "profilePictureUrl": "https://storage.com/images/profile.jpg",
    "gender": 1,
    "birthDate": "1995-05-15",
    "heightCm": 175.5,
    "weightKg": 80.0,
    "activityLevel": "Moderate",
    "fitnessGoal": "Build Muscle",
    "medicalHistory": null
  }
}
```

**Frontend Example:**
```javascript
const getProfile = async () => {
  const response = await api.get('/profile');
  return response.data;
};

// في React Component
const ProfilePage = () => {
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getProfile()
      .then(setProfile)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <Spinner />;

  return (
    <div>
      <h1>{profile.profile?.fullName}</h1>
      <img src={profile.profile?.profilePictureUrl} alt="Profile" />
    </div>
  );
};
```

---

### 2.2 تحديث الملف الشخصي

```http
PUT /api/profile
```

**Request Body:**
```json
{
  "fullName": "Ahmed Mohamed Ali",
  "gender": 1,
  "birthDate": "1995-05-15",
  "heightCm": 175.5,
  "weightKg": 78.0,
  "activityLevel": "Active",
  "fitnessGoal": "Lose Weight",
  "medicalHistory": "لا يوجد"
}
```

**Gender Values:**
| Value | Gender |
|-------|--------|
| 1 | Male (ذكر) |
| 2 | Female (أنثى) |

**Response:** `204 No Content`

---

### 2.3 رفع صورة الملف الشخصي

```http
POST /api/profile/picture
Content-Type: multipart/form-data
```

**Frontend Example:**
```javascript
const uploadProfilePicture = async (file) => {
  const formData = new FormData();
  formData.append('file', file);

  const response = await api.post('/profile/picture', formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  });

  return response.data.url;
};
```

---

### 2.4 حذف صورة الملف الشخصي

```http
DELETE /api/profile/picture
```

---

## 3. Tenants (المنشآت)

### 3.1 الحصول على قائمة المنشآت

```http
GET /api/tenants
```

**Response (200):**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Fitness Club",
    "subdomain": "fitness-club",
    "status": 1,
    "createdAt": "2024-01-01T00:00:00Z"
  }
]
```

---

### 3.2 إنشاء منشأة جديدة

```http
POST /api/tenants
```

**Request Body:**
```json
{
  "name": "New Gym",
  "subdomain": "new-gym"
}
```

---

## 4. Gym Profile (ملف الصالة)

> **Authorization:** مطلوب (Owner فقط)

### 4.1 الحصول على ملف الصالة

```http
GET /api/gymprofile
```

**Response (200):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Fitness Club",
  "subdomain": "fitness-club",
  "description": "أفضل صالة رياضية في المدينة",
  "address": "123 شارع الرياضة",
  "phoneNumber": "01012345678",
  "email": "info@fitnessclub.com",
  "logoUrl": "https://storage.com/logo.png",
  "coverImageUrl": "https://storage.com/cover.jpg",
  "galleryImages": ["https://storage.com/gallery1.jpg"],
  "status": "Active",
  "brandingSettings": {
    "primaryColor": "#FF5733",
    "secondaryColor": "#333333"
  },
  "statistics": {
    "totalClients": 150,
    "activeClients": 120,
    "totalCoaches": 5,
    "totalSubscriptionPlans": 3,
    "activeSubscriptions": 100
  }
}
```

---

### 4.2 تحديث ملف الصالة

```http
PUT /api/gymprofile
```

---

### 4.3 رفع شعار الصالة

```http
POST /api/gymprofile/logo
Content-Type: multipart/form-data
```

---

### 4.4 رفع صورة الغلاف

```http
POST /api/gymprofile/cover
Content-Type: multipart/form-data
```

---

### 4.5 رفع صور المعرض

```http
POST /api/gymprofile/gallery
Content-Type: multipart/form-data
```

---

## 5. Users (المستخدمين)

> **Authorization:** مطلوب (Owner فقط)

### 5.1 الحصول على قائمة المستخدمين

```http
GET /api/users?role=2&isActive=true&searchTerm=ahmed
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| role | int | 1=Owner, 2=Coach, 3=Client |
| isActive | bool | فلترة حسب الحالة |
| searchTerm | string | بحث بالاسم أو الهاتف |

---

### 5.2 الحصول على مستخدم بالـ ID

```http
GET /api/users/{id}
```

---

### 5.3 تحديث مستخدم

```http
PUT /api/users/{id}
```

---

### 5.4 تحديث ملف مستخدم

```http
PUT /api/users/{id}/profile
```

---

## 6. Clients (العملاء)

> **Authorization:** مطلوب

### 6.1 الحصول على قائمة العملاء

```http
GET /api/clients?searchTerm=ahmed&isActive=true
```

**Response (200):**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "client@example.com",
    "phoneNumber": "01012345678",
    "isActive": true,
    "walletBalance": 500.00,
    "profile": {
      "fullName": "Ahmed Client",
      "gender": 1,
      "birthDate": "1995-05-15",
      "heightCm": 175,
      "activityLevel": "Moderate",
      "medicalHistory": null
    },
    "activeSubscription": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "planName": "Premium Monthly",
      "startDate": "2024-01-01",
      "endDate": "2024-02-01",
      "status": "Active"
    }
  }
]
```

---

### 6.2 الحصول على عميل بالـ ID

```http
GET /api/clients/{id}
```

---

### 6.3 إنشاء عميل جديد

```http
POST /api/clients
```

**Request Body:**
```json
{
  "phoneNumber": "01012345678",
  "email": "newclient@example.com",
  "password": "Password123!",
  "fullName": "New Client",
  "gender": 1,
  "birthDate": "1995-05-15",
  "heightCm": 175,
  "activityLevel": "Moderate"
}
```

---

### 6.4 تحديث عميل

```http
PUT /api/clients/{id}
```

---

### 6.5 حذف عميل

```http
DELETE /api/clients/{id}
```

---

## 7. Coach-Clients (ربط المدرب بالعملاء)

> **Authorization:** مطلوب

### 7.1 الحصول على عملاء المدرب

```http
GET /api/coach-clients?coachId={coachId}&isActive=true
```

**Response (200):**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "coachId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "coachName": "Coach Ahmed",
    "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientName": "Mohamed Ali",
    "clientPhone": "01012345678",
    "assignedAt": "2024-01-01T00:00:00Z",
    "isActive": true,
    "hasActiveSubscription": true,
    "subscriptionEndDate": "2024-02-01",
    "workoutProgramsCount": 2,
    "dietPlansCount": 1,
    "workoutSessionsCount": 15,
    "lastSessionDate": "2024-01-14T10:00:00Z"
  }
]
```

---

### 7.2 إضافة متدرب جديد للمدرب

```http
POST /api/coach-clients
```

---

### 7.3 تعيين عميل موجود لمدرب

```http
POST /api/coach-clients/assign
```

---

### 7.4 إلغاء تعيين عميل من مدرب

```http
DELETE /api/coach-clients/{clientId}
```

---

## 8. Muscles (العضلات)

> **Authorization:** مطلوب

### 8.1 الحصول على قائمة العضلات

```http
GET /api/muscles?bodyPart=Chest
```

**Response (200):**
```json
[
  {
    "id": 1,
    "name": "Chest",
    "nameAr": "الصدر",
    "bodyPart": "Upper Body",
    "description": "The main chest muscle responsible for pushing movements",
    "descriptionAr": "العضلة الرئيسية للصدر المسؤولة عن حركات الدفع",
    "icon": "💪"
  }
]
```

---

### 8.2 الحصول على عضلة بالـ ID

```http
GET /api/muscles/{id}
```

---

## 9. Exercises (التمارين)

> **Authorization:** مطلوب

### 9.1 الحصول على قائمة التمارين

```http
GET /api/exercises?targetMuscleId=1&equipment=Barbell&isHighImpact=false
```

**Response (200):**
```json
[
  {
    "id": 1,
    "name": "Bench Press",
    "nameAr": "ضغط البنش",
    "description": "Classic chest exercise for building strength",
    "descriptionAr": "تمرين كلاسيكي للصدر لبناء القوة",
    "targetMuscleId": 1,
    "targetMuscleName": "Chest",
    "primaryMuscleContributionPercent": 70,
    "secondaryMuscles": [
      { "muscleId": 5, "muscleName": "Triceps", "contributionPercent": 20 }
    ],
    "imageUrl": "https://storage.com/exercises/bench-press.jpg",
    "videoUrl": "https://youtube.com/watch?v=...",
    "equipment": "Barbell",
    "difficulty": "Intermediate",
    "category": "Strength",
    "instructions": [
      "استلقِ على البنش مع القدمين على الأرض",
      "امسك البار بعرض أكبر من الكتفين",
      "أنزل البار ببطء إلى منتصف الصدر",
      "ادفع البار للأعلى حتى تمتد الذراعين"
    ],
    "tips": ["Keep your back slightly arched"],
    "commonMistakes": ["Bouncing the bar off chest"],
    "repsRange": "8-12",
    "setsRange": "3-4",
    "restSeconds": 90,
    "tempo": "2-1-2-0"
  }
]
```

---

### 9.2 الحصول على تمرين بالـ ID

```http
GET /api/exercises/{id}
```

---

### 9.3 إنشاء تمرين جديد

```http
POST /api/exercises
Content-Type: multipart/form-data
```

---

### 9.4 تحديث تمرين

```http
PUT /api/exercises/{id}
Content-Type: multipart/form-data
```

---

### 9.5 حذف تمرين

```http
DELETE /api/exercises/{id}
```

---

## 10. Foods (الأطعمة)

> **Authorization:** مطلوب

### 10.1 الحصول على قائمة الأطعمة

```http
GET /api/foods?category=Proteins&searchTerm=chicken&isVerified=true
```

**Response (200):**
```json
[
  {
    "id": 1,
    "name": "Chicken Breast (Grilled)",
    "nameAr": "صدر دجاج مشوي",
    "category": "Proteins",
    "caloriesPer100g": 165.0,
    "proteinPer100g": 31.0,
    "carbsPer100g": 0.0,
    "fatsPer100g": 3.6,
    "fiberPer100g": 0.0,
    "sugarPer100g": 0.0,
    "sodiumPer100g": 74.0,
    "servingSize": 100.0,
    "servingUnit": "g",
    "isVerified": true
  }
]
```

---

### 10.2 الحصول على طعام بالـ ID

```http
GET /api/foods/{id}
```

---

### 10.3 إنشاء طعام جديد

```http
POST /api/foods
```

---

### 10.4 تحديث طعام

```http
PUT /api/foods/{id}
```

---

### 10.5 حذف طعام

```http
DELETE /api/foods/{id}
```

---

## 11. Workout Programs (برامج التدريب)

> **Authorization:** مطلوب

### 11.1 الحصول على قائمة البرامج

```http
GET /api/workoutprograms?coachId={coachId}&clientId={clientId}
```

**Response (قائمة بدون routines للأداء)**

---

### 11.2 الحصول على برنامج بالـ ID (مع كل التفاصيل)

```http
GET /api/workoutprograms/{id}
```

**Response (200):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "coachId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "coachName": "Coach Ahmed",
  "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientName": "Mohamed Ali",
  "name": "برنامج بناء العضلات",
  "startDate": "2024-01-01",
  "endDate": "2024-03-01",
  "routines": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "يوم الصدر والترايسبس",
      "dayOfWeek": 0,
      "exercises": [
        {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "exerciseId": 1,
          "exerciseName": "Bench Press",
          "sets": 4,
          "repsMin": 8,
          "repsMax": 12,
          "restSec": 90,
          "supersetGroupId": null
        }
      ]
    }
  ]
}
```

**dayOfWeek Values:**
| Value | Day |
|-------|-----|
| 0 | الأحد |
| 1 | الإثنين |
| 2 | الثلاثاء |
| 3 | الأربعاء |
| 4 | الخميس |
| 5 | الجمعة |
| 6 | السبت |

---

### 11.3 إنشاء برنامج تدريب

```http
POST /api/workoutprograms
```

**Request Body:**
```json
{
  "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "برنامج جديد",
  "startDate": "2024-01-15",
  "endDate": "2024-04-15"
}
```

---

### 11.4 تحديث برنامج

```http
PUT /api/workoutprograms/{id}
```

---

### 11.5 حذف برنامج

```http
DELETE /api/workoutprograms/{id}
```

---

### 11.6 تكرار برنامج

```http
POST /api/workoutprograms/{id}/duplicate
```

---

### 11.7 إضافة روتين للبرنامج

```http
POST /api/workoutprograms/{programId}/routines
```

**Request Body:**
```json
{
  "name": "يوم الظهر والبايسبس",
  "dayOfWeek": 1
}
```

---

### 11.8 تحديث روتين

```http
PUT /api/workoutprograms/routines/{routineId}
```

---

### 11.9 حذف روتين

```http
DELETE /api/workoutprograms/routines/{routineId}
```

---

### 11.10 إضافة تمرين للروتين

```http
POST /api/workoutprograms/routines/{routineId}/exercises
```

**Request Body:**
```json
{
  "exerciseId": 1,
  "sets": 4,
  "repsMin": 8,
  "repsMax": 12,
  "restSec": 90,
  "supersetGroupId": null
}
```

---

### 11.11 تحديث تمرين في الروتين

```http
PUT /api/workoutprograms/routines/exercises/{exerciseId}
```

---

### 11.12 حذف تمرين من الروتين

```http
DELETE /api/workoutprograms/routines/exercises/{exerciseId}
```

---

## 12. Diet Plans (خطط التغذية)

> **Authorization:** مطلوب

### 12.1 الحصول على قائمة الخطط

```http
GET /api/dietplans?coachId={coachId}&clientId={clientId}&status=Active
```

**Status Values:**
| Value | Status |
|-------|--------|
| 0 | Draft (مسودة) |
| 1 | Active (نشط) |
| 2 | Archived (مؤرشف) |

---

### 12.2 الحصول على خطة بالـ ID (مع كل التفاصيل)

```http
GET /api/dietplans/{id}
```

**Response (200):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "coachId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "coachName": "Coach Ahmed",
  "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientName": "Mohamed Ali",
  "name": "خطة تنشيف",
  "startDate": "2024-01-01",
  "endDate": "2024-03-01",
  "status": 1,
  "targetCalories": 2000,
  "targetProtein": 150,
  "targetCarbs": 200,
  "targetFats": 70,
  "meals": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "الإفطار",
      "orderIndex": 1,
      "items": [
        {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "foodId": 1,
          "foodName": "Oatmeal",
          "assignedQuantity": 100,
          "calcCalories": 389,
          "calcProtein": 16.9,
          "calcCarbs": 66.3,
          "calcFats": 6.9
        }
      ]
    }
  ]
}
```

---

### 12.3 إنشاء خطة تغذية

```http
POST /api/dietplans
```

**Request Body:**
```json
{
  "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "خطة جديدة",
  "startDate": "2024-01-15",
  "endDate": "2024-04-15",
  "targetCalories": 2500,
  "targetProtein": 180,
  "targetCarbs": 250,
  "targetFats": 80
}
```

---

### 12.4 تحديث خطة

```http
PUT /api/dietplans/{id}
```

---

### 12.5 حذف خطة

```http
DELETE /api/dietplans/{id}
```

---

### 12.6 تكرار خطة

```http
POST /api/dietplans/{id}/duplicate
```

---

### 12.7 إضافة وجبة للخطة

```http
POST /api/dietplans/{planId}/meals
```

**Request Body:**
```json
{
  "name": "وجبة خفيفة",
  "orderIndex": 4
}
```

---

### 12.8 تحديث وجبة

```http
PUT /api/dietplans/meals/{mealId}
```

---

### 12.9 حذف وجبة

```http
DELETE /api/dietplans/meals/{mealId}
```

---

### 12.10 إضافة عنصر للوجبة

```http
POST /api/dietplans/meals/{mealId}/items
```

**Request Body:**
```json
{
  "foodId": 1,
  "assignedQuantity": 150
}
```

**ملاحظة:** القيم الغذائية (calcCalories, calcProtein, etc.) تُحسب تلقائياً من الـ Food والكمية.

---

### 12.11 تحديث عنصر الوجبة

```http
PUT /api/dietplans/meals/items/{itemId}
```

---

### 12.12 حذف عنصر من الوجبة

```http
DELETE /api/dietplans/meals/items/{itemId}
```

---

## 13. Workout Sessions (جلسات التدريب)

> **Authorization:** مطلوب

### 13.1 الحصول على الجلسات

```http
GET /api/workoutsessions?clientId={clientId}&fromDate=2024-01-01&toDate=2024-01-31
```

**Response (200):**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientName": "Mohamed Ali",
    "routineId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "routineName": "يوم الصدر والترايسبس",
    "startedAt": "2024-01-15T10:00:00Z",
    "endedAt": "2024-01-15T11:30:00Z",
    "totalVolumLifted": 5400.0,
    "notes": "تمرين ممتاز!",
    "sets": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "exerciseId": 1,
        "exerciseName": "Bench Press",
        "setNumber": 1,
        "weightKg": 80.0,
        "reps": 10,
        "rpe": 8.0,
        "volumeLoad": 800.0,
        "isPr": false
      }
    ]
  }
]
```

---

### 13.2 الحصول على جلسة بالـ ID

```http
GET /api/workoutsessions/{id}
```

---

### 13.3 بدء جلسة تدريب

```http
POST /api/workoutsessions/start
```

**Request Body:**
```json
{
  "routineId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

---

### 13.4 إنهاء جلسة تدريب

```http
POST /api/workoutsessions/{sessionId}/end
```

**Request Body:**
```json
{
  "notes": "تمرين ممتاز!"
}
```

---

### 13.5 إضافة مجموعة (Set) للجلسة

```http
POST /api/workoutsessions/{sessionId}/sets
```

**Request Body:**
```json
{
  "exerciseId": 1,
  "setNumber": 1,
  "weightKg": 80.0,
  "reps": 10,
  "rpe": 8.0
}
```

**RPE (Rate of Perceived Exertion):**
| Value | Description |
|-------|-------------|
| 6-7 | سهل - يمكن عمل 4+ تكرارات إضافية |
| 8 | معتدل - يمكن عمل 2-3 تكرارات إضافية |
| 9 | صعب - يمكن عمل تكرار واحد إضافي |
| 10 | أقصى جهد - لا يمكن عمل تكرارات إضافية |

---

## 14. Body Measurements (قياسات الجسم)

> **Authorization:** مطلوب

### 14.1 الحصول على القياسات

```http
GET /api/bodymeasurements?clientId={clientId}&fromDate=2024-01-01&toDate=2024-12-31
```

**Response (200):**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientName": "Mohamed Ali",
    "dateRecorded": "2024-01-15",
    "weightKg": 80.0,
    "skeletalMuscleMass": 35.0,
    "bodyFatMass": 15.0,
    "bodyFatPercent": 18.75,
    "totalBodyWater": 45.0,
    "bmr": 1800,
    "visceralFatLevel": 8,
    "inbodyImageUrl": "https://storage.com/inbody/123.jpg",
    "frontPhotoUrl": "https://storage.com/photos/front.jpg",
    "sidePhotoUrl": "https://storage.com/photos/side.jpg",
    "backPhotoUrl": "https://storage.com/photos/back.jpg"
  }
]
```

---

### 14.2 إضافة قياس جديد

```http
POST /api/bodymeasurements
Content-Type: multipart/form-data
```

---

### 14.3 حذف قياس

```http
DELETE /api/bodymeasurements/{id}
```

---

## 15. Subscriptions (الاشتراكات)

> **Authorization:** مطلوب

### 15.1 الحصول على خطط الاشتراك

```http
GET /api/subscriptions/plans
```

**Response (200):**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Premium Monthly",
    "price": 500.00,
    "durationMonths": 1
  }
]
```

---

### 15.2 إنشاء خطة اشتراك

```http
POST /api/subscriptions/plans
```

---

### 15.3 الحصول على اشتراكات العملاء

```http
GET /api/subscriptions?clientId={clientId}&status=Active
```

**Status Values:**
| Value | Status |
|-------|--------|
| 0 | Pending (معلق) |
| 1 | Active (نشط) |
| 2 | Frozen (مجمد) |
| 3 | Expired (منتهي) |
| 4 | Cancelled (ملغي) |

---

### 15.4 إنشاء اشتراك لعميل

```http
POST /api/subscriptions
```

**Request Body:**
```json
{
  "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "planId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "startDate": "2024-01-15"
}
```

---

### 15.5 تجميد اشتراك

```http
POST /api/subscriptions/{subscriptionId}/freeze
```

**Request Body:**
```json
{
  "startDate": "2024-01-20",
  "endDate": "2024-01-27",
  "reason": "سفر عمل"
}
```

---

### 15.6 إلغاء اشتراك

```http
POST /api/subscriptions/{subscriptionId}/cancel
```

---

## 16. Reports (التقارير)

> **Authorization:** مطلوب

### 16.1 لوحة التحكم الرئيسية (Dashboard)

```http
GET /api/reports/dashboard
```

**Response (200):**
```json
{
  "totalClients": 150,
  "activeClients": 120,
  "newClientsThisMonth": 15,
  "totalCoaches": 5,
  "activeSubscriptions": 100,
  "expiringSubscriptions": 8,
  "totalRevenueThisMonth": 75000.00,
  "totalRevenueLastMonth": 65000.00,
  "totalWorkoutsThisMonth": 450,
  "totalDietPlansActive": 80
}
```

---

### 16.2 تقرير العملاء

```http
GET /api/reports/clients?fromDate=2024-01-01&toDate=2024-12-31
```

---

### 16.3 تقرير الاشتراكات

```http
GET /api/reports/subscriptions?fromDate=2024-01-01&toDate=2024-12-31
```

---

### 16.4 التقرير المالي

```http
GET /api/reports/financial?fromDate=2024-01-01&toDate=2024-12-31
```

---

### 16.5 لوحة تحكم المدرب

```http
GET /api/reports/coach/dashboard?coachId={coachId}
```

---

### 16.6 تقرير متدربي المدرب

```http
GET /api/reports/coach/trainees?coachId={coachId}
```

---

### 16.7 تقرير تقدم متدرب

```http
GET /api/reports/coach/trainee/{clientId}
```

**Response (200):**
```json
{
  "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientName": "Mohamed Ali",
  "clientPhone": "01012345678",
  "assignedAt": "2024-01-01T00:00:00Z",
  "bodyMeasurements": [
    {
      "dateRecorded": "2024-01-01",
      "weightKg": 80.0,
      "bodyFatPercent": 20.0,
      "muscleMass": 34.0,
      "bmr": 1750
    }
  ],
  "startWeight": 80.0,
  "currentWeight": 78.0,
  "totalWeightChange": -2.0,
  "startBodyFat": 20.0,
  "currentBodyFat": 18.5,
  "totalBodyFatChange": -1.5,
  "totalSessions": 15,
  "totalVolumeLifted": 45000.0,
  "workoutPrograms": [],
  "dietPlans": [],
  "personalRecords": [
    {
      "exerciseId": 1,
      "exerciseName": "Bench Press",
      "maxWeight": 100.0,
      "reps": 5,
      "achievedAt": "2024-01-14T10:30:00Z"
    }
  ]
}
```

---

## أكواد الخطأ الشائعة

| Code | Description | الوصف |
|------|-------------|-------|
| 200 | OK | نجاح |
| 201 | Created | تم الإنشاء |
| 204 | No Content | نجاح بدون محتوى |
| 400 | Bad Request | طلب غير صحيح |
| 401 | Unauthorized | غير مصرح |
| 403 | Forbidden | ممنوع الوصول |
| 404 | Not Found | غير موجود |
| 409 | Conflict | تعارض (مثل بيانات مكررة) |
| 500 | Internal Server Error | خطأ في الخادم |

**مثال على معالجة الأخطاء:**
```javascript
const handleApiError = (error) => {
  const status = error.response?.status;
  const message = error.response?.data?.message || error.message;

  switch (status) {
    case 400:
      toast.error(`خطأ في البيانات: ${message}`);
      break;
    case 401:
      toast.error('انتهت الجلسة، يرجى تسجيل الدخول مرة أخرى');
      logout();
      break;
    case 403:
      toast.error('ليس لديك صلاحية لهذا الإجراء');
      break;
    case 404:
      toast.error('العنصر المطلوب غير موجود');
      break;
    case 409:
      toast.error('البيانات موجودة مسبقاً');
      break;
    default:
      toast.error('حدث خطأ، يرجى المحاولة لاحقاً');
  }
};
```

---

## ملخص إحصائي

| Category | Endpoints |
|----------|-----------|
| Authentication | 4 |
| Profile | 4 |
| Tenants | 2 |
| Gym Profile | 5 |
| Users | 4 |
| Clients | 5 |
| Coach-Clients | 4 |
| Muscles | 2 |
| Exercises | 5 |
| Foods | 5 |
| Workout Programs | 12 |
| Diet Plans | 12 |
| Workout Sessions | 5 |
| Body Measurements | 3 |
| Subscriptions | 6 |
| Reports | 7 |
| **Total** | **85** |

---

> تم إنشاء هذا التوثيق تلقائياً من كود المشروع.
>
> آخر تحديث: 2024-01-09
