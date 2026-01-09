# LogicFit - التعديلات الكاملة و API Reference

---

# الجزء الأول: التعديلات التي تمت

---

## 1. إصلاح مشكلة Soft Deleted Foods

### المشكلة
كانت الـ Foods تحصل على IDs جديدة في كل مرة يتم فيها إعادة تشغيل الـ Seeder:
- المرة الأولى: IDs 1-98
- المرة الثانية: IDs 491-588
- المرة الثالثة: IDs 1177-1274

### السبب
الكود القديم كان يحذف كل الـ Foods ويعيد إدخالها، مما يسبب زيادة الـ Identity.

### الحل

#### تعديل 1: `DataSeeder.cs` - تغيير من DELETE-INSERT إلى UPSERT

**الملف:** `LogicFit.Infrastructure/Persistence/DataSeeder.cs`

**الكود القديم (محذوف):**
```csharp
private async Task SeedFoodsAsync()
{
    // Delete all global foods and reseed fresh
    var existingGlobalFoods = await _context.Foods
        .IgnoreQueryFilters()
        .Where(f => f.TenantId == null)
        .ToListAsync();

    if (existingGlobalFoods.Any())
    {
        _context.Foods.RemoveRange(existingGlobalFoods);
        await _context.SaveChangesAsync();
    }

    foreach (var item in seedData)
    {
        var food = new Food { ... };
        _context.Foods.Add(food);
    }
    await _context.SaveChangesAsync();
}
```

**الكود الجديد:**
```csharp
private async Task SeedFoodsAsync()
{
    var jsonPath = Path.Combine(_seedDataPath, "foods.json");
    if (!File.Exists(jsonPath))
    {
        _logger.LogWarning("foods.json not found at {Path}", jsonPath);
        return;
    }

    var json = await File.ReadAllTextAsync(jsonPath);
    var seedData = JsonSerializer.Deserialize<List<FoodSeedDto>>(json, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

    if (seedData == null || !seedData.Any()) return;

    // Get existing global foods for UPSERT (including soft-deleted ones)
    var existingGlobalFoods = await _context.Foods
        .IgnoreQueryFilters()
        .Where(f => f.TenantId == null)
        .ToListAsync();

    // Create lookup by name for efficient matching (handle duplicates by taking first)
    var existingByName = existingGlobalFoods
        .GroupBy(f => f.Name)
        .ToDictionary(g => g.Key, g => g.First());

    int added = 0, updated = 0, restored = 0;

    foreach (var item in seedData)
    {
        if (existingByName.TryGetValue(item.NameEn, out var existing))
        {
            // UPDATE existing food (preserve ID)
            existing.NameAr = item.NameAr;
            existing.Category = item.Category;
            existing.CaloriesPer100g = (double)item.Calories;
            existing.ProteinPer100g = (double)item.Protein;
            existing.CarbsPer100g = (double)item.Carbs;
            existing.FatsPer100g = (double)item.Fat;
            existing.FiberPer100g = (double?)item.Fiber;
            existing.ServingSize = (double?)item.ServingSize;
            existing.ServingUnit = item.ServingUnit;
            existing.IsVerified = true;

            // Restore if soft-deleted
            if (existing.IsDeleted)
            {
                existing.IsDeleted = false;
                existing.DeletedAt = null;
                restored++;
            }
            updated++;
        }
        else
        {
            // INSERT new food
            var food = new Food
            {
                TenantId = item.TenantId,
                Name = item.NameEn,
                NameAr = item.NameAr,
                Category = item.Category,
                CaloriesPer100g = (double)item.Calories,
                ProteinPer100g = (double)item.Protein,
                CarbsPer100g = (double)item.Carbs,
                FatsPer100g = (double)item.Fat,
                FiberPer100g = (double?)item.Fiber,
                ServingSize = (double?)item.ServingSize,
                ServingUnit = item.ServingUnit,
                IsVerified = true
            };
            _context.Foods.Add(food);
            added++;
        }
    }

    await _context.SaveChangesAsync();
    _logger.LogInformation("Foods: {Added} added, {Updated} updated ({Restored} restored from deleted)",
        added, updated, restored);
}
```

---

#### تعديل 2: إضافة `ForceResetFoodsAsync()` Method

**الملف:** `LogicFit.Infrastructure/Persistence/DataSeeder.cs`

```csharp
/// <summary>
/// Force reset foods with identity reseed. Use this to fix the database if food IDs
/// have become out of sync due to previous delete/reseed cycles.
/// WARNING: This will delete all MealItems, RecipeIngredients, and FoodMicronutrients!
/// </summary>
public async Task ForceResetFoodsAsync()
{
    _logger.LogWarning("Starting force reset of Foods table...");

    // Delete related data first (foreign key constraints)
    var mealItems = await _context.Set<MealItem>().IgnoreQueryFilters().ToListAsync();
    if (mealItems.Any())
    {
        _context.Set<MealItem>().RemoveRange(mealItems);
        _logger.LogInformation("Deleted {Count} meal items", mealItems.Count);
    }

    var recipeIngredients = await _context.Set<RecipeIngredient>().IgnoreQueryFilters().ToListAsync();
    if (recipeIngredients.Any())
    {
        _context.Set<RecipeIngredient>().RemoveRange(recipeIngredients);
        _logger.LogInformation("Deleted {Count} recipe ingredients", recipeIngredients.Count);
    }

    var foodMicronutrients = await _context.Set<FoodMicronutrient>().IgnoreQueryFilters().ToListAsync();
    if (foodMicronutrients.Any())
    {
        _context.Set<FoodMicronutrient>().RemoveRange(foodMicronutrients);
        _logger.LogInformation("Deleted {Count} food micronutrients", foodMicronutrients.Count);
    }

    // Delete all foods
    var foods = await _context.Foods.IgnoreQueryFilters().ToListAsync();
    if (foods.Any())
    {
        _context.Foods.RemoveRange(foods);
        _logger.LogInformation("Deleted {Count} foods", foods.Count);
    }

    await _context.SaveChangesAsync();

    // Reset identity seed using raw SQL
    await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Foods', RESEED, 0)");
    _logger.LogInformation("Reset Foods identity seed to 0");

    // Now seed fresh
    await SeedFoodsAsync();
    _logger.LogWarning("Force reset of Foods completed. New foods seeded with IDs starting from 1.");
}
```

---

#### تعديل 3: إضافة Environment Variable في `Program.cs`

**الملف:** `LogicFit.API/Program.cs`

```csharp
// Seed data on startup
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();

    // Check if force reset of foods is requested (to fix identity issues)
    var resetFoods = Environment.GetEnvironmentVariable("RESET_FOODS");
    if (!string.IsNullOrEmpty(resetFoods) && resetFoods.Equals("true", StringComparison.OrdinalIgnoreCase))
    {
        await seeder.ForceResetFoodsAsync();
    }

    await seeder.SeedAsync();
}
```

**طريقة الاستخدام:**
```bash
# لإصلاح الـ Database (مرة واحدة فقط)
RESET_FOODS=true dotnet run

# للتشغيل العادي
dotnet run
```

---

#### تعديل 4: إضافة SQL Script

**الملف:** `LogicFit/Scripts/fix_foods_identity.sql`

```sql
-- Fix Foods Identity Issue
-- Run this ONCE to fix the database, then restart the API to reseed

-- Step 1: Delete MealItems first (foreign key)
DELETE FROM MealItems;

-- Step 2: Delete RecipeIngredients
DELETE FROM RecipeIngredients;

-- Step 3: Delete FoodMicronutrients
DELETE FROM FoodMicronutrients;

-- Step 4: Hard delete ALL foods
DELETE FROM Foods;

-- Step 5: Reset the identity seed to 1
DBCC CHECKIDENT ('Foods', RESEED, 0);

-- Step 6: Verify
SELECT IDENT_CURRENT('Foods') AS CurrentIdentity;
```

---

### النتيجة النهائية

| قبل الإصلاح | بعد الإصلاح |
|-------------|-------------|
| Food IDs: 1177, 1178, 1179... | Food IDs: 1, 2, 3, 4... |
| White Rice = ID 1177 | White Rice = ID 1 |
| Grilled Chicken = ID 1180 | Grilled Chicken = ID 4 |
| 1274 سجل (duplicates + deleted) | 98 سجل نظيف |

---

## 2. ملخص الملفات المُعدلة

| الملف | نوع التعديل | الوصف |
|-------|------------|-------|
| `DataSeeder.cs` | تعديل | تغيير `SeedFoodsAsync()` من DELETE-INSERT إلى UPSERT |
| `DataSeeder.cs` | إضافة | Method جديدة `ForceResetFoodsAsync()` |
| `Program.cs` | تعديل | إضافة فحص `RESET_FOODS` environment variable |
| `Scripts/fix_foods_identity.sql` | جديد | SQL script للإصلاح اليدوي |

---

---

# الجزء الثاني: API Reference الكامل

---

## معلومات عامة

### Base URL
```
http://localhost:5026/api
```

### Headers
```http
Authorization: Bearer {JWT_TOKEN}
Content-Type: application/json
```

---

## Authentication

### POST /api/auth/register
تسجيل مستخدم جديد

**Request:**
```json
{
  "email": "user@example.com",
  "phoneNumber": "01012345678",
  "password": "Password123!",
  "fullName": "محمد أحمد",
  "tenantId": "608e0707-ddc9-4ac5-ac82-a6db5313d29e"
}
```

**Response:** `200 OK`
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "6e2d2bed-1766-4071-870e-53d03f962485",
  "email": "user@example.com",
  "role": 0
}
```

---

### POST /api/auth/login
تسجيل الدخول

**Request:**
```json
{
  "phoneNumber": "01012345678",
  "password": "Password123!",
  "tenantId": "608e0707-ddc9-4ac5-ac82-a6db5313d29e"
}
```

**Response:** `200 OK`
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "6e2d2bed-1766-4071-870e-53d03f962485",
  "email": "user@example.com",
  "role": 0
}
```

---

## Diet Plans (خطط التغذية)

### POST /api/dietplans
إنشاء خطة تغذية جديدة

**Request:**
```json
{
  "clientId": "33c883a3-a499-4816-84a6-7e3acca3ab1e",
  "name": "خطة تنشيف - يناير 2025",
  "startDate": "2025-01-15",
  "endDate": "2025-02-15",
  "status": 1,
  "targetCalories": 2000,
  "targetProtein": 150,
  "targetCarbs": 200,
  "targetFats": 65
}
```

**Response:** `200 OK`
```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

### GET /api/dietplans
الحصول على قائمة خطط التغذية

**Query Parameters:**
- `PageNumber` (int, default: 1)
- `PageSize` (int, default: 10)
- `ClientId` (Guid, optional)

**Response:** `200 OK`
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "tenantId": "608e0707-ddc9-4ac5-ac82-a6db5313d29e",
    "coachId": "6e2d2bed-1766-4071-870e-53d03f962485",
    "coachName": "كابتن أحمد",
    "clientId": "33c883a3-a499-4816-84a6-7e3acca3ab1e",
    "clientName": "محمد علي",
    "name": "خطة تنشيف - يناير 2025",
    "startDate": "2025-01-15T00:00:00",
    "endDate": "2025-02-15T00:00:00",
    "status": 1,
    "targetCalories": 2000,
    "targetProtein": 150,
    "targetCarbs": 200,
    "targetFats": 65
  }
]
```

---

### GET /api/dietplans/{id}
الحصول على تفاصيل خطة تغذية (مع الوجبات والعناصر)

**Response:** `200 OK`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "tenantId": "608e0707-ddc9-4ac5-ac82-a6db5313d29e",
  "coachId": "6e2d2bed-1766-4071-870e-53d03f962485",
  "coachName": "كابتن أحمد",
  "clientId": "33c883a3-a499-4816-84a6-7e3acca3ab1e",
  "clientName": "محمد علي",
  "name": "خطة تنشيف - يناير 2025",
  "startDate": "2025-01-15T00:00:00",
  "endDate": "2025-02-15T00:00:00",
  "status": 1,
  "targetCalories": 2000,
  "targetProtein": 150,
  "targetCarbs": 200,
  "targetFats": 65,
  "meals": [
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "name": "الفطور",
      "orderIndex": 0,
      "items": [
        {
          "id": "item-guid-1",
          "foodId": 1,
          "foodName": "White Rice (Cooked)",
          "assignedQuantity": 200,
          "calcCalories": 260,
          "calcProtein": 5.4,
          "calcCarbs": 56.4,
          "calcFats": 0.6
        },
        {
          "id": "item-guid-2",
          "foodId": 4,
          "foodName": "Grilled Chicken Breast",
          "assignedQuantity": 150,
          "calcCalories": 247.5,
          "calcProtein": 46.5,
          "calcCarbs": 0,
          "calcFats": 5.4
        }
      ]
    },
    {
      "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "name": "الغداء",
      "orderIndex": 1,
      "items": []
    }
  ]
}
```

---

### PUT /api/dietplans/{id}
تحديث خطة تغذية

**Request:**
```json
{
  "name": "خطة تنشيف محدثة",
  "startDate": "2025-01-20",
  "endDate": "2025-02-20",
  "status": 1,
  "targetCalories": 1800,
  "targetProtein": 160,
  "targetCarbs": 180,
  "targetFats": 60
}
```

**Response:** `204 No Content`

---

### DELETE /api/dietplans/{id}
حذف خطة تغذية

**Response:** `204 No Content`

---

### POST /api/dietplans/{id}/duplicate
نسخ خطة تغذية

**Response:** `200 OK`
```json
"new-plan-guid-here"
```

---

## Meals (الوجبات)

### POST /api/dietplans/{planId}/meals
إضافة وجبة لخطة

**Request:**
```json
{
  "name": "الفطور",
  "orderIndex": 0
}
```

**Response:** `200 OK`
```json
"meal-guid-here"
```

**orderIndex Values:**
| Index | الوجبة |
|-------|--------|
| 0 | الفطور |
| 1 | سناك صباحي |
| 2 | الغداء |
| 3 | سناك مسائي |
| 4 | العشاء |

---

### PUT /api/dietplans/meals/{mealId}
تحديث وجبة

**Request:**
```json
{
  "name": "الفطور المحدث",
  "orderIndex": 0
}
```

**Response:** `204 No Content`

---

### DELETE /api/dietplans/meals/{mealId}
حذف وجبة

**Response:** `204 No Content`

---

## Meal Items (عناصر الوجبة)

### POST /api/dietplans/meals/{mealId}/items
إضافة طعام للوجبة

**Request:**
```json
{
  "foodId": 1,
  "assignedQuantity": 200
}
```

**Response:** `200 OK`
```json
"item-guid-here"
```

---

### PUT /api/dietplans/meals/items/{itemId}
تحديث كمية الطعام

**Request:**
```json
{
  "assignedQuantity": 250
}
```

**Response:** `204 No Content`

---

### DELETE /api/dietplans/meals/items/{itemId}
حذف طعام من الوجبة

**Response:** `204 No Content`

---

## Workout Programs (برامج التدريب)

### POST /api/workoutprograms
إنشاء برنامج تدريب جديد

**Request:**
```json
{
  "clientId": "33c883a3-a499-4816-84a6-7e3acca3ab1e",
  "name": "برنامج بناء العضلات - 12 أسبوع",
  "startDate": "2025-01-15",
  "endDate": "2025-04-15"
}
```

**Response:** `200 OK`
```json
"program-guid-here"
```

---

### GET /api/workoutprograms
الحصول على قائمة برامج التدريب

**Query Parameters:**
- `PageNumber` (int, default: 1)
- `PageSize` (int, default: 10)
- `ClientId` (Guid, optional)

**Response:** `200 OK`
```json
[
  {
    "id": "program-guid-here",
    "tenantId": "608e0707-ddc9-4ac5-ac82-a6db5313d29e",
    "coachId": "6e2d2bed-1766-4071-870e-53d03f962485",
    "coachName": "كابتن أحمد",
    "clientId": "33c883a3-a499-4816-84a6-7e3acca3ab1e",
    "clientName": "محمد علي",
    "name": "برنامج بناء العضلات - 12 أسبوع",
    "startDate": "2025-01-15T00:00:00",
    "endDate": "2025-04-15T00:00:00"
  }
]
```

---

### GET /api/workoutprograms/{id}
الحصول على تفاصيل برنامج تدريب (مع الروتين والتمارين)

**Response:** `200 OK`
```json
{
  "id": "program-guid-here",
  "tenantId": "608e0707-ddc9-4ac5-ac82-a6db5313d29e",
  "coachId": "6e2d2bed-1766-4071-870e-53d03f962485",
  "coachName": "كابتن أحمد",
  "clientId": "33c883a3-a499-4816-84a6-7e3acca3ab1e",
  "clientName": "محمد علي",
  "name": "برنامج بناء العضلات - 12 أسبوع",
  "startDate": "2025-01-15T00:00:00",
  "endDate": "2025-04-15T00:00:00",
  "routines": [
    {
      "id": "routine-guid-1",
      "name": "يوم الصدر والترايسبس",
      "dayOfWeek": 0,
      "exercises": [
        {
          "id": "exercise-guid-1",
          "exerciseId": 1,
          "exerciseName": "Bench Press",
          "sets": 4,
          "repsMin": 8,
          "repsMax": 12,
          "restSec": 90,
          "supersetGroupId": null
        },
        {
          "id": "exercise-guid-2",
          "exerciseId": 15,
          "exerciseName": "Tricep Pushdown",
          "sets": 3,
          "repsMin": 12,
          "repsMax": 15,
          "restSec": 60,
          "supersetGroupId": null
        }
      ]
    },
    {
      "id": "routine-guid-2",
      "name": "يوم الظهر والبايسبس",
      "dayOfWeek": 2,
      "exercises": []
    }
  ]
}
```

---

### PUT /api/workoutprograms/{id}
تحديث برنامج تدريب

**Request:**
```json
{
  "name": "برنامج بناء العضلات المحدث",
  "startDate": "2025-01-20",
  "endDate": "2025-04-20"
}
```

**Response:** `204 No Content`

---

### DELETE /api/workoutprograms/{id}
حذف برنامج تدريب

**Response:** `204 No Content`

---

### POST /api/workoutprograms/{id}/duplicate
نسخ برنامج تدريب

**Response:** `200 OK`
```json
"new-program-guid-here"
```

---

## Routines (الروتين)

### POST /api/workoutprograms/{programId}/routines
إضافة روتين لبرنامج

**Request:**
```json
{
  "name": "يوم الصدر والترايسبس",
  "dayOfWeek": 0
}
```

**Response:** `200 OK`
```json
"routine-guid-here"
```

**dayOfWeek Values:**
| Value | اليوم |
|-------|-------|
| 0 | الأحد |
| 1 | الإثنين |
| 2 | الثلاثاء |
| 3 | الأربعاء |
| 4 | الخميس |
| 5 | الجمعة |
| 6 | السبت |

---

### PUT /api/workoutprograms/routines/{routineId}
تحديث روتين

**Request:**
```json
{
  "name": "يوم الصدر المحدث",
  "dayOfWeek": 1
}
```

**Response:** `204 No Content`

---

### DELETE /api/workoutprograms/routines/{routineId}
حذف روتين

**Response:** `204 No Content`

---

## Routine Exercises (تمارين الروتين)

### POST /api/workoutprograms/routines/{routineId}/exercises
إضافة تمرين للروتين

**Request:**
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

**Response:** `200 OK`
```json
"routine-exercise-guid-here"
```

---

### PUT /api/workoutprograms/routines/exercises/{id}
تحديث تمرين في الروتين

**Request:**
```json
{
  "sets": 5,
  "repsMin": 6,
  "repsMax": 10,
  "restSec": 120
}
```

**Response:** `204 No Content`

---

### DELETE /api/workoutprograms/routines/exercises/{id}
حذف تمرين من الروتين

**Response:** `204 No Content`

---

## Foods (الأطعمة)

### GET /api/foods
الحصول على قائمة الأطعمة

**Query Parameters:**
- `PageNumber` (int, default: 1)
- `PageSize` (int, default: 10)
- `Search` (string, optional)
- `Category` (string, optional)

**Response:** `200 OK`
```json
[
  {
    "id": 1,
    "tenantId": null,
    "name": "White Rice (Cooked)",
    "nameAr": "أرز أبيض مطبوخ",
    "category": "Grains",
    "caloriesPer100g": 130,
    "proteinPer100g": 2.7,
    "carbsPer100g": 28.2,
    "fatsPer100g": 0.3,
    "fiberPer100g": 0.4,
    "servingSize": 150,
    "servingUnit": "g",
    "isVerified": true
  },
  {
    "id": 4,
    "tenantId": null,
    "name": "Grilled Chicken Breast",
    "nameAr": "صدر دجاج مشوي",
    "category": "Protein",
    "caloriesPer100g": 165,
    "proteinPer100g": 31,
    "carbsPer100g": 0,
    "fatsPer100g": 3.6,
    "fiberPer100g": 0,
    "servingSize": 120,
    "servingUnit": "g",
    "isVerified": true
  }
]
```

---

### GET /api/foods/{id}
الحصول على طعام بالـ ID

**Response:** `200 OK`
```json
{
  "id": 1,
  "tenantId": null,
  "name": "White Rice (Cooked)",
  "nameAr": "أرز أبيض مطبوخ",
  "category": "Grains",
  "caloriesPer100g": 130,
  "proteinPer100g": 2.7,
  "carbsPer100g": 28.2,
  "fatsPer100g": 0.3,
  "fiberPer100g": 0.4,
  "servingSize": 150,
  "servingUnit": "g",
  "isVerified": true
}
```

---

### POST /api/foods
إنشاء طعام جديد

**Request:**
```json
{
  "name": "Custom Protein Bar",
  "nameAr": "بروتين بار مخصص",
  "category": "Snacks",
  "caloriesPer100g": 350,
  "proteinPer100g": 25,
  "carbsPer100g": 40,
  "fatsPer100g": 12,
  "fiberPer100g": 5,
  "servingSize": 60,
  "servingUnit": "g"
}
```

**Response:** `200 OK`
```json
99
```

---

### PUT /api/foods/{id}
تحديث طعام

**Request:**
```json
{
  "name": "Updated Food Name",
  "caloriesPer100g": 360
}
```

**Response:** `204 No Content`

---

### DELETE /api/foods/{id}
حذف طعام

**Response:** `204 No Content`

---

## Exercises (التمارين)

### GET /api/exercises
الحصول على قائمة التمارين

**Query Parameters:**
- `PageNumber` (int, default: 1)
- `PageSize` (int, default: 10)
- `Search` (string, optional)
- `MuscleId` (int, optional)
- `Equipment` (string, optional)

**Response:** `200 OK`
```json
[
  {
    "id": 1,
    "tenantId": null,
    "name": "Bench Press",
    "targetMuscleId": 1,
    "targetMuscleName": "Chest",
    "equipment": "Barbell",
    "isHighImpact": false,
    "secondaryMuscles": [
      {
        "muscleId": 3,
        "muscleName": "Shoulders",
        "contributionPercent": 30
      },
      {
        "muscleId": 5,
        "muscleName": "Triceps",
        "contributionPercent": 25
      }
    ]
  }
]
```

---

### GET /api/exercises/{id}
الحصول على تمرين بالـ ID

**Response:** `200 OK`
```json
{
  "id": 1,
  "tenantId": null,
  "name": "Bench Press",
  "targetMuscleId": 1,
  "targetMuscleName": "Chest",
  "equipment": "Barbell",
  "isHighImpact": false,
  "secondaryMuscles": [
    {
      "muscleId": 3,
      "muscleName": "Shoulders",
      "contributionPercent": 30
    },
    {
      "muscleId": 5,
      "muscleName": "Triceps",
      "contributionPercent": 25
    }
  ]
}
```

---

### POST /api/exercises
إنشاء تمرين جديد

**Request:**
```json
{
  "name": "Custom Exercise",
  "targetMuscleId": 1,
  "equipment": "Dumbbell",
  "isHighImpact": false,
  "secondaryMuscles": [
    {
      "muscleId": 5,
      "contributionPercent": 20
    }
  ]
}
```

**Response:** `200 OK`
```json
140
```

---

### PUT /api/exercises/{id}
تحديث تمرين

**Request:**
```json
{
  "name": "Updated Exercise",
  "equipment": "Cable"
}
```

**Response:** `204 No Content`

---

### DELETE /api/exercises/{id}
حذف تمرين

**Response:** `204 No Content`

---

## Muscles (العضلات)

### GET /api/muscles
الحصول على قائمة العضلات

**Response:** `200 OK`
```json
[
  { "id": 1, "name": "Chest", "bodyPart": "Upper Body" },
  { "id": 2, "name": "Back", "bodyPart": "Upper Body" },
  { "id": 3, "name": "Shoulders", "bodyPart": "Upper Body" },
  { "id": 4, "name": "Biceps", "bodyPart": "Arms" },
  { "id": 5, "name": "Triceps", "bodyPart": "Arms" },
  { "id": 6, "name": "Forearms", "bodyPart": "Arms" },
  { "id": 7, "name": "Quadriceps", "bodyPart": "Lower Body" },
  { "id": 8, "name": "Hamstrings", "bodyPart": "Lower Body" },
  { "id": 9, "name": "Glutes", "bodyPart": "Lower Body" },
  { "id": 10, "name": "Calves", "bodyPart": "Lower Body" },
  { "id": 11, "name": "Abs", "bodyPart": "Core" },
  { "id": 12, "name": "Obliques", "bodyPart": "Core" },
  { "id": 13, "name": "Lower Back", "bodyPart": "Core" },
  { "id": 14, "name": "Traps", "bodyPart": "Upper Body" },
  { "id": 15, "name": "Lats", "bodyPart": "Upper Body" }
]
```

---

### GET /api/muscles/{id}
الحصول على عضلة بالـ ID

**Response:** `200 OK`
```json
{
  "id": 1,
  "name": "Chest",
  "bodyPart": "Upper Body"
}
```

---

## Profile (البروفايل)

### GET /api/profile
الحصول على بروفايل المستخدم الحالي

**Response:** `200 OK`
```json
{
  "id": "6e2d2bed-1766-4071-870e-53d03f962485",
  "tenantId": "608e0707-ddc9-4ac5-ac82-a6db5313d29e",
  "email": "user@example.com",
  "phoneNumber": "01012345678",
  "role": 0,
  "isActive": true,
  "walletBalance": 0.00,
  "profile": {
    "fullName": "Ahmed Mohamed",
    "profilePictureUrl": "/uploads/images/2025/01/profile-pictures/abc123.jpg",
    "gender": 1,
    "birthDate": "1995-05-15T00:00:00",
    "heightCm": 175.5,
    "weightKg": 80.0,
    "activityLevel": "Moderate",
    "fitnessGoal": "Build Muscle",
    "medicalHistory": "None"
  }
}
```

---

### PUT /api/profile
تحديث بروفايل المستخدم الحالي

**Request:**
```json
{
  "fullName": "Ahmed Mohamed",
  "gender": 1,
  "birthDate": "1995-05-15",
  "heightCm": 175.5,
  "weightKg": 80.0,
  "activityLevel": "Moderate",
  "fitnessGoal": "Build Muscle",
  "medicalHistory": "None"
}
```

**Response:** `204 No Content`

**Gender Values:**
| Value | الجنس |
|-------|-------|
| 1 | ذكر (Male) |
| 2 | أنثى (Female) |

**Activity Level Options:**
- `Sedentary` - قليل الحركة
- `Light` - نشاط خفيف
- `Moderate` - نشاط متوسط
- `Active` - نشيط
- `Very Active` - نشيط جداً

---

### POST /api/profile/picture
رفع صورة البروفايل

**Request:** `multipart/form-data`
```
file: [image file]
```

**Supported Formats:** `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`
**Max Size:** 5MB

**Response:** `200 OK`
```json
{
  "url": "/uploads/images/2025/01/profile-pictures/abc123.jpg"
}
```

---

### DELETE /api/profile/picture
حذف صورة البروفايل

**Response:** `204 No Content`

---

# الجزء الثالث: TypeScript Interfaces

```typescript
// ==================== Authentication ====================
interface LoginRequest {
  phoneNumber: string;
  password: string;
  tenantId: string;
}

interface RegisterRequest {
  email: string;
  phoneNumber: string;
  password: string;
  fullName: string;
  tenantId: string;
}

interface AuthResponse {
  token: string;
  userId: string;
  email: string;
  role: number;
}

// ==================== Diet Plans ====================
interface DietPlanDto {
  id: string;
  tenantId: string;
  coachId: string;
  coachName: string;
  clientId: string;
  clientName: string;
  name: string;
  startDate: string;
  endDate?: string;
  status: DietPlanStatus;
  targetCalories: number;
  targetProtein: number;
  targetCarbs: number;
  targetFats: number;
  meals?: MealDto[];
}

interface CreateDietPlanRequest {
  clientId: string;
  name: string;
  startDate: string;
  endDate?: string;
  status: DietPlanStatus;
  targetCalories: number;
  targetProtein: number;
  targetCarbs: number;
  targetFats: number;
}

interface UpdateDietPlanRequest {
  name?: string;
  startDate?: string;
  endDate?: string;
  status?: DietPlanStatus;
  targetCalories?: number;
  targetProtein?: number;
  targetCarbs?: number;
  targetFats?: number;
}

enum DietPlanStatus {
  Draft = 0,
  Active = 1,
  Archived = 2
}

// ==================== Meals ====================
interface MealDto {
  id: string;
  name: string;
  orderIndex: number;
  items: MealItemDto[];
}

interface CreateMealRequest {
  name: string;
  orderIndex: number;
}

interface UpdateMealRequest {
  name?: string;
  orderIndex?: number;
}

// ==================== Meal Items ====================
interface MealItemDto {
  id: string;
  foodId: number;
  foodName: string;
  assignedQuantity: number;
  calcCalories: number;
  calcProtein: number;
  calcCarbs: number;
  calcFats: number;
}

interface CreateMealItemRequest {
  foodId: number;
  assignedQuantity: number;
}

interface UpdateMealItemRequest {
  assignedQuantity: number;
}

// ==================== Workout Programs ====================
interface WorkoutProgramDto {
  id: string;
  tenantId: string;
  coachId: string;
  coachName: string;
  clientId: string;
  clientName: string;
  name: string;
  startDate: string;
  endDate?: string;
  routines?: RoutineDto[];
}

interface CreateWorkoutProgramRequest {
  clientId: string;
  name: string;
  startDate: string;
  endDate?: string;
}

interface UpdateWorkoutProgramRequest {
  name?: string;
  startDate?: string;
  endDate?: string;
}

// ==================== Routines ====================
interface RoutineDto {
  id: string;
  name: string;
  dayOfWeek: number;
  exercises: RoutineExerciseDto[];
}

interface CreateRoutineRequest {
  name: string;
  dayOfWeek: number;
}

interface UpdateRoutineRequest {
  name?: string;
  dayOfWeek?: number;
}

// ==================== Routine Exercises ====================
interface RoutineExerciseDto {
  id: string;
  exerciseId: number;
  exerciseName: string;
  sets: number;
  repsMin: number;
  repsMax: number;
  restSec: number;
  supersetGroupId?: string;
}

interface CreateRoutineExerciseRequest {
  exerciseId: number;
  sets: number;
  repsMin: number;
  repsMax: number;
  restSec: number;
  supersetGroupId?: string;
}

interface UpdateRoutineExerciseRequest {
  sets?: number;
  repsMin?: number;
  repsMax?: number;
  restSec?: number;
  supersetGroupId?: string;
}

// ==================== Foods ====================
interface FoodDto {
  id: number;
  tenantId?: string;
  name: string;
  nameAr?: string;
  category?: string;
  caloriesPer100g: number;
  proteinPer100g: number;
  carbsPer100g: number;
  fatsPer100g: number;
  fiberPer100g?: number;
  servingSize?: number;
  servingUnit?: string;
  isVerified: boolean;
}

interface CreateFoodRequest {
  name: string;
  nameAr?: string;
  category?: string;
  caloriesPer100g: number;
  proteinPer100g: number;
  carbsPer100g: number;
  fatsPer100g: number;
  fiberPer100g?: number;
  servingSize?: number;
  servingUnit?: string;
}

// ==================== Exercises ====================
interface ExerciseDto {
  id: number;
  tenantId?: string;
  name: string;
  targetMuscleId: number;
  targetMuscleName: string;
  equipment?: string;
  isHighImpact: boolean;
  secondaryMuscles: SecondaryMuscleDto[];
}

interface SecondaryMuscleDto {
  muscleId: number;
  muscleName: string;
  contributionPercent: number;
}

interface CreateExerciseRequest {
  name: string;
  targetMuscleId: number;
  equipment?: string;
  isHighImpact: boolean;
  secondaryMuscles?: {
    muscleId: number;
    contributionPercent: number;
  }[];
}

// ==================== Muscles ====================
interface MuscleDto {
  id: number;
  name: string;
  bodyPart?: string;
}

// ==================== Profile ====================
interface ProfileResponse {
  id: string;
  tenantId: string;
  email: string;
  phoneNumber?: string;
  role: number;
  isActive: boolean;
  walletBalance: number;
  profile?: UserProfileDto;
}

interface UpdateProfileRequest {
  fullName?: string;
  gender?: number;
  birthDate?: string;
  heightCm?: number;
  weightKg?: number;
  activityLevel?: string;
  fitnessGoal?: string;
  medicalHistory?: string;
}

interface UploadProfilePictureResponse {
  url: string;
}
```

---

# الجزء الرابع: ملخص الـ Endpoints

## Diet Plans (12 endpoints)

| Method | Endpoint | الوصف |
|--------|----------|-------|
| `POST` | `/api/dietplans` | إنشاء خطة |
| `GET` | `/api/dietplans` | قائمة الخطط |
| `GET` | `/api/dietplans/{id}` | تفاصيل خطة |
| `PUT` | `/api/dietplans/{id}` | تحديث خطة |
| `DELETE` | `/api/dietplans/{id}` | حذف خطة |
| `POST` | `/api/dietplans/{id}/duplicate` | نسخ خطة |
| `POST` | `/api/dietplans/{planId}/meals` | إضافة وجبة |
| `PUT` | `/api/dietplans/meals/{mealId}` | تحديث وجبة |
| `DELETE` | `/api/dietplans/meals/{mealId}` | حذف وجبة |
| `POST` | `/api/dietplans/meals/{mealId}/items` | إضافة طعام |
| `PUT` | `/api/dietplans/meals/items/{itemId}` | تحديث طعام |
| `DELETE` | `/api/dietplans/meals/items/{itemId}` | حذف طعام |

## Workout Programs (12 endpoints)

| Method | Endpoint | الوصف |
|--------|----------|-------|
| `POST` | `/api/workoutprograms` | إنشاء برنامج |
| `GET` | `/api/workoutprograms` | قائمة البرامج |
| `GET` | `/api/workoutprograms/{id}` | تفاصيل برنامج |
| `PUT` | `/api/workoutprograms/{id}` | تحديث برنامج |
| `DELETE` | `/api/workoutprograms/{id}` | حذف برنامج |
| `POST` | `/api/workoutprograms/{id}/duplicate` | نسخ برنامج |
| `POST` | `/api/workoutprograms/{programId}/routines` | إضافة روتين |
| `PUT` | `/api/workoutprograms/routines/{routineId}` | تحديث روتين |
| `DELETE` | `/api/workoutprograms/routines/{routineId}` | حذف روتين |
| `POST` | `/api/workoutprograms/routines/{routineId}/exercises` | إضافة تمرين |
| `PUT` | `/api/workoutprograms/routines/exercises/{id}` | تحديث تمرين |
| `DELETE` | `/api/workoutprograms/routines/exercises/{id}` | حذف تمرين |

## Foods (5 endpoints)

| Method | Endpoint | الوصف |
|--------|----------|-------|
| `GET` | `/api/foods` | قائمة الأطعمة |
| `GET` | `/api/foods/{id}` | تفاصيل طعام |
| `POST` | `/api/foods` | إنشاء طعام |
| `PUT` | `/api/foods/{id}` | تحديث طعام |
| `DELETE` | `/api/foods/{id}` | حذف طعام |

## Exercises (5 endpoints)

| Method | Endpoint | الوصف |
|--------|----------|-------|
| `GET` | `/api/exercises` | قائمة التمارين |
| `GET` | `/api/exercises/{id}` | تفاصيل تمرين |
| `POST` | `/api/exercises` | إنشاء تمرين |
| `PUT` | `/api/exercises/{id}` | تحديث تمرين |
| `DELETE` | `/api/exercises/{id}` | حذف تمرين |

## Muscles (2 endpoints)

| Method | Endpoint | الوصف |
|--------|----------|-------|
| `GET` | `/api/muscles` | قائمة العضلات |
| `GET` | `/api/muscles/{id}` | تفاصيل عضلة |

## Profile (4 endpoints)

| Method | Endpoint | الوصف |
|--------|----------|-------|
| `GET` | `/api/profile` | الحصول على بيانات البروفايل |
| `PUT` | `/api/profile` | تحديث بيانات البروفايل |
| `POST` | `/api/profile/picture` | رفع صورة البروفايل |
| `DELETE` | `/api/profile/picture` | حذف صورة البروفايل |

## Authentication (2 endpoints)

| Method | Endpoint | الوصف |
|--------|----------|-------|
| `POST` | `/api/auth/login` | تسجيل الدخول |
| `POST` | `/api/auth/register` | تسجيل مستخدم |

---

**المجموع الكلي: 42 Endpoint**

---

**تم إنشاء هذا التوثيق في:** يناير 2025
**الإصدار:** 1.0
