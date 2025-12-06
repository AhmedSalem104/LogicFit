# LogicFit - دليل مطور الـ Frontend

## مرحباً! 👋

هذا الدليل يحتوي على كل ما تحتاجه لبناء تطبيق Frontend متكامل لنظام LogicFit لإدارة الصالات الرياضية.

---

## 📁 الملفات المرجعية

| الملف | الوصف |
|-------|-------|
| `PROJECT_DOCUMENTATION.md` | التوثيق الكامل للمشروع (API, Entities, Business Logic) |
| هذا الملف | دليل سريع للبدء |

---

## 🚀 للبدء السريع

### 1. Base URL
```
Development: https://localhost:7xxx/api
Production: https://your-domain.com/api
```

### 2. Authentication
```javascript
// بعد تسجيل الدخول، احفظ الـ Token
const token = response.token;
localStorage.setItem('token', token);

// أضف الـ Token لكل Request
headers: {
  'Authorization': `Bearer ${token}`,
  'Content-Type': 'application/json'
}
```

---

## 👥 أدوار المستخدمين (3 أدوار)

### 1. Owner (مالك الصالة)
```
الصفحات المطلوبة:
├── Dashboard (لوحة تحكم شاملة)
├── إدارة المدربين (إضافة/تعديل/حذف)
├── إدارة العملاء (جميع العملاء)
├── خطط الاشتراك (إنشاء/تعديل)
├── الاشتراكات (عرض/تجميد/إلغاء)
├── التقارير المالية
├── تقارير العملاء
├── تقارير الاشتراكات
├── تقارير المدربين (عرض أي مدرب)
├── إعدادات الصالة (اللوجو، الصور، البيانات)
└── تعيين عملاء لمدربين
```

### 2. Coach (المدرب)
```
الصفحات المطلوبة:
├── Dashboard المدرب
├── متدربيني (العملاء المعينين لي)
├── تعيين عملاء جدد (لنفسي فقط)
├── برامج التمارين (إنشاء/تعديل للمتدربين)
├── الخطط الغذائية (إنشاء/تعديل للمتدربين)
├── مكتبة التمارين
├── قاعدة الأطعمة
├── تقرير المتدربين
├── تقرير تقدم متدرب معين
├── قياسات المتدربين
└── إنشاء اشتراكات (دور المبيعات)
```

### 3. Client (العميل)
```
الصفحات المطلوبة:
├── الملف الشخصي
├── برنامج التمارين الخاص بي
├── بدء جلسة تمرين
├── الخطة الغذائية
├── تسجيل الوجبات
├── قياسات جسمي
├── اشتراكاتي
└── تقدمي (الإحصائيات)
```

---

## 📱 الصفحات الرئيسية المطلوبة

### صفحات عامة (Public)
```
/login                    - تسجيل الدخول
/register                 - تسجيل صالة جديدة
/forgot-password          - نسيت كلمة المرور
/reset-password           - إعادة تعيين كلمة المرور
```

### صفحات Owner
```
/dashboard                - لوحة التحكم الرئيسية
/coaches                  - إدارة المدربين
/coaches/new              - إضافة مدرب
/coaches/:id              - تفاصيل/تعديل مدرب

/clients                  - إدارة العملاء
/clients/new              - إضافة عميل
/clients/:id              - تفاصيل عميل

/subscription-plans       - خطط الاشتراك
/subscriptions            - اشتراكات العملاء
/subscriptions/:id        - تفاصيل اشتراك

/reports/dashboard        - تقرير Dashboard
/reports/clients          - تقرير العملاء
/reports/subscriptions    - تقرير الاشتراكات
/reports/financial        - التقرير المالي
/reports/coaches          - تقارير المدربين
/reports/coach/:id        - تقرير مدرب معين

/gym-settings             - إعدادات الصالة
```

### صفحات Coach
```
/coach/dashboard          - لوحة تحكم المدرب
/coach/trainees           - متدربيني
/coach/trainees/:id       - تقدم متدرب معين
/coach/assign-client      - تعيين عميل جديد

/workout-programs         - برامج التمارين
/workout-programs/new     - إنشاء برنامج
/workout-programs/:id     - تفاصيل/تعديل برنامج

/diet-plans               - الخطط الغذائية
/diet-plans/new           - إنشاء خطة
/diet-plans/:id           - تفاصيل/تعديل خطة

/exercises                - مكتبة التمارين
/exercises/new            - إضافة تمرين
/foods                    - قاعدة الأطعمة
/foods/new                - إضافة طعام

/body-measurements        - قياسات المتدربين
/body-measurements/new    - إضافة قياس
```

### صفحات Client
```
/my-profile               - ملفي الشخصي
/my-program               - برنامج تمارينى
/my-session               - جلسة التمرين الحالية
/my-diet                  - خطتي الغذائية
/my-meals                 - تسجيل الوجبات
/my-measurements          - قياساتي
/my-subscriptions         - اشتراكاتي
/my-progress              - تقدمي
```

---

## 🔗 API Endpoints حسب الصفحة

### Login Page
```javascript
POST /api/auth/login
Body: { phoneNumber, password }
Response: { token, expiresAt, user: { id, email, role, tenantId } }
```

### Dashboard (Owner)
```javascript
GET /api/reports/dashboard
Response: {
  totalClients, activeClients, newClientsThisMonth,
  totalCoaches, activeSubscriptions, expiringSubscriptions,
  revenueThisMonth, revenueLastMonth, workoutsThisMonth, activeDietPlans
}
```

### Dashboard (Coach)
```javascript
GET /api/reports/coach/dashboard
Response: {
  totalTrainees, activeTrainees, newTraineesThisMonth,
  activeWorkoutPrograms, activeDietPlans,
  totalSessionsThisMonth, totalVolumeThisMonth,
  topTraineesByProgress, topTraineesBySessions
}
```

### Clients List
```javascript
GET /api/clients?search=xxx&isActive=true&page=1&pageSize=10
Response: {
  items: [...],
  pageNumber, pageSize, totalPages, totalCount,
  hasPreviousPage, hasNextPage
}
```

### Coach Trainees (متدربين المدرب)
```javascript
GET /api/coach-clients
Response: [{
  id, coachId, coachName, clientId, clientName, clientPhone,
  assignedAt, isActive, hasActiveSubscription, subscriptionEndDate,
  workoutProgramsCount, dietPlansCount, workoutSessionsCount
}]
```

### Assign Client to Coach
```javascript
POST /api/coach-clients
Body: { coachId?, clientId, notes? }
// coachId اختياري - إذا فارغ يعين لنفسه
```

### Unassign Client
```javascript
DELETE /api/coach-clients/{clientId}
```

### Trainee Progress Report
```javascript
GET /api/reports/coach/trainee/{clientId}
Response: {
  clientId, clientName, clientPhone, assignedAt,
  bodyMeasurements: [...],
  startWeight, currentWeight, totalWeightChange,
  startBodyFat, currentBodyFat, totalBodyFatChange,
  totalSessions, totalVolumeLifted, monthlySessions,
  workoutPrograms, dietPlans, personalRecords
}
```

### Workout Programs
```javascript
// قائمة البرامج
GET /api/workoutprograms?clientId=xxx

// تفاصيل برنامج
GET /api/workoutprograms/{id}

// إنشاء برنامج
POST /api/workoutprograms
Body: { clientId, name, description }

// إضافة روتين
POST /api/workoutprograms/{programId}/routines
Body: { name, dayOfWeek, orderIndex }

// إضافة تمرين للروتين
POST /api/workoutprograms/routines/{routineId}/exercises
Body: { exerciseId, sets, minReps, maxReps, restSeconds, orderIndex, supersetGroup? }
```

### Workout Sessions
```javascript
// بدء جلسة
POST /api/workoutsessions/start
Body: { routineId }

// تسجيل Set
POST /api/workoutsessions/{sessionId}/sets
Body: { exerciseId, setNumber, weight, reps, rpe? }

// إنهاء جلسة
POST /api/workoutsessions/{sessionId}/end
```

### Diet Plans
```javascript
// قائمة الخطط
GET /api/dietplans?clientId=xxx

// إنشاء خطة
POST /api/dietplans
Body: { clientId, name, dailyCalories, dailyProtein, dailyCarbs, dailyFats, startDate, endDate? }

// إضافة وجبة
POST /api/dietplans/{planId}/meals
Body: { mealName, orderIndex }

// إضافة طعام للوجبة
POST /api/dietplans/meals/{mealId}/items
Body: { foodId, quantity }
```

### Subscriptions
```javascript
// خطط الاشتراك
GET /api/subscriptions/plans

// إنشاء خطة
POST /api/subscriptions/plans
Body: { name, price, durationMonths, isActive }

// اشتراكات العملاء
GET /api/subscriptions?clientId=xxx&status=Active

// إنشاء اشتراك
POST /api/subscriptions
Body: { clientId, planId, startDate, amountPaid }

// تجميد اشتراك
POST /api/subscriptions/{id}/freeze
Body: { startDate, endDate, reason? }

// إلغاء اشتراك
POST /api/subscriptions/{id}/cancel
```

### Body Measurements
```javascript
// قائمة القياسات
GET /api/bodymeasurements?clientId=xxx

// إضافة قياس (Form Data)
POST /api/bodymeasurements
FormData: {
  clientId, dateRecorded, weightKg,
  skeletalMuscleMass?, bodyFatMass?, bodyFatPercent?,
  totalBodyWater?, bmr?, visceralFatLevel?,
  inbodyImage?, frontPhoto?, sidePhoto?, backPhoto?
}
```

### Exercises
```javascript
// قائمة التمارين
GET /api/exercises?targetMuscleId=1

// إنشاء تمرين (Form Data)
POST /api/exercises
FormData: { name, targetMuscleId, equipment?, isHighImpact, image?, video? }
```

### Foods
```javascript
// قائمة الأطعمة
GET /api/foods?category=Protein

// إنشاء طعام
POST /api/foods
Body: { name, category?, caloriesPer100g, proteinPer100g, carbsPer100g, fatsPer100g, fiberPer100g? }
```

### Gym Profile
```javascript
// عرض
GET /api/gymprofile

// تحديث
PUT /api/gymprofile
Body: { name, description, phone, email, address, facebook, instagram, website }

// رفع اللوجو
POST /api/gymprofile/logo
FormData: { logoFile }

// رفع صورة الغلاف
POST /api/gymprofile/cover
FormData: { coverFile }

// رفع صور المعرض
POST /api/gymprofile/gallery
FormData: { galleryFiles }
```

---

## 🎨 UI Components المقترحة

### مكونات مشتركة
```
├── Navbar (مع قائمة حسب الدور)
├── Sidebar (التنقل الجانبي)
├── DataTable (جدول بيانات مع Pagination)
├── SearchInput (بحث)
├── FilterDropdown (فلترة)
├── Modal (نوافذ منبثقة)
├── Form Components (Input, Select, DatePicker, FileUpload)
├── Card (بطاقات الإحصائيات)
├── Chart (رسوم بيانية للتقارير)
├── Avatar (صورة المستخدم)
├── Badge (حالة الاشتراك، الدور)
├── Toast/Notification (رسائل النجاح/الخطأ)
└── Loading/Skeleton (التحميل)
```

### مكونات خاصة
```
├── WorkoutProgramCard (بطاقة برنامج التمارين)
├── ExerciseCard (بطاقة تمرين مع صورة/فيديو)
├── SetLogger (تسجيل Sets أثناء التمرين)
├── MealPlanCard (بطاقة الوجبة)
├── ProgressChart (رسم بياني للتقدم)
├── BodyMeasurementCard (بطاقة القياسات)
├── SubscriptionCard (بطاقة الاشتراك مع الحالة)
├── TraineeCard (بطاقة المتدرب)
└── PRBadge (شارة الرقم القياسي)
```

---

## 📊 الرسوم البيانية المطلوبة

### Dashboard Owner
```
1. Line Chart: الإيرادات الشهرية (آخر 6 أشهر)
2. Pie Chart: توزيع حالات الاشتراكات
3. Bar Chart: العملاء الجدد شهرياً
4. Stats Cards: إجمالي العملاء، الاشتراكات، الإيرادات
```

### Dashboard Coach
```
1. Line Chart: جلسات التمرين شهرياً
2. Bar Chart: أفضل المتدربين
3. Progress Chart: تقدم المتدربين (الوزن/الدهون)
4. Stats Cards: المتدربين، البرامج، الجلسات
```

### Trainee Progress
```
1. Line Chart: تغير الوزن عبر الزمن
2. Line Chart: نسبة الدهون عبر الزمن
3. Bar Chart: الجلسات الشهرية
4. Table: الأرقام القياسية (PRs)
```

---

## ⚠️ معالجة الأخطاء

```javascript
// Error Response Format
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
200 - OK (نجاح)
201 - Created (تم الإنشاء)
204 - No Content (نجاح بدون محتوى)
400 - Bad Request (خطأ في البيانات)
401 - Unauthorized (غير مصرح - انتهى الـ Token)
403 - Forbidden (ليس لديك صلاحية)
404 - Not Found (غير موجود)
500 - Internal Server Error (خطأ في السيرفر)

// معالجة 401
if (response.status === 401) {
  localStorage.removeItem('token');
  redirect('/login');
}
```

---

## 🔐 Route Protection

```javascript
// حماية الـ Routes حسب الدور
const ProtectedRoute = ({ allowedRoles, children }) => {
  const user = getCurrentUser();

  if (!user) return <Navigate to="/login" />;
  if (!allowedRoles.includes(user.role)) return <Navigate to="/unauthorized" />;

  return children;
};

// استخدام
<Route path="/dashboard" element={
  <ProtectedRoute allowedRoles={['Owner']}>
    <OwnerDashboard />
  </ProtectedRoute>
} />

<Route path="/coach/dashboard" element={
  <ProtectedRoute allowedRoles={['Coach']}>
    <CoachDashboard />
  </ProtectedRoute>
} />
```

---

## 📱 Responsive Design

```
الأولويات:
1. Mobile First للعميل (بدء جلسة التمرين)
2. Desktop First للـ Owner (التقارير والإدارة)
3. Tablet Friendly للمدرب (إنشاء البرامج)

Breakpoints المقترحة:
- Mobile: < 640px
- Tablet: 640px - 1024px
- Desktop: > 1024px
```

---

## 🛠️ التقنيات المقترحة

```
Frontend Framework: React / Next.js / Vue.js
State Management: Redux Toolkit / Zustand / Pinia
UI Library: Tailwind CSS / Material UI / Ant Design
Charts: Chart.js / Recharts / ApexCharts
Forms: React Hook Form / Formik
HTTP Client: Axios / Fetch API
Date: Day.js / date-fns
File Upload: React Dropzone
Tables: TanStack Table / AG Grid
```

---

## ✅ Checklist للتسليم

### المرحلة 1: الأساسيات
- [ ] صفحة تسجيل الدخول
- [ ] صفحة تسجيل صالة جديدة
- [ ] نسيت كلمة المرور
- [ ] Navbar + Sidebar حسب الدور
- [ ] Route Protection

### المرحلة 2: Owner Features
- [ ] Dashboard مع الإحصائيات
- [ ] إدارة المدربين (CRUD)
- [ ] إدارة العملاء (CRUD)
- [ ] خطط الاشتراك
- [ ] اشتراكات العملاء
- [ ] تعيين عملاء لمدربين
- [ ] التقارير (Dashboard, Clients, Subscriptions, Financial)
- [ ] إعدادات الصالة

### المرحلة 3: Coach Features
- [ ] Dashboard المدرب
- [ ] قائمة المتدربين
- [ ] تعيين عملاء جدد
- [ ] برامج التمارين (CRUD)
- [ ] الخطط الغذائية (CRUD)
- [ ] مكتبة التمارين
- [ ] قاعدة الأطعمة
- [ ] قياسات المتدربين
- [ ] تقارير المتدربين

### المرحلة 4: Client Features
- [ ] الملف الشخصي
- [ ] عرض برنامج التمارين
- [ ] بدء وتسجيل جلسة تمرين
- [ ] عرض الخطة الغذائية
- [ ] تسجيل الوجبات
- [ ] عرض القياسات
- [ ] عرض الاشتراكات
- [ ] صفحة التقدم

### المرحلة 5: التحسينات
- [ ] Dark Mode
- [ ] Notifications
- [ ] PWA Support
- [ ] Performance Optimization
- [ ] Error Boundaries
- [ ] Loading States

---

## 📞 للتواصل

إذا واجهت أي مشكلة أو احتجت توضيح:
1. راجع `PROJECT_DOCUMENTATION.md` للتفاصيل الكاملة
2. تواصل مع فريق الـ Backend

---

*بالتوفيق في المشروع! 🚀*
