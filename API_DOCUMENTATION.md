# LogicFit API Documentation

## Complete Backend API Reference

**Base URL:** `https://your-domain.com/api`
**Authentication:** JWT Bearer Token
**Content-Type:** `application/json` (unless specified otherwise)

---

# Table of Contents

1. [Authentication](#1-authentication)
2. [Tenants](#2-tenants)
3. [Users](#3-users)
4. [Clients](#4-clients)
5. [Exercises](#5-exercises)
6. [Muscles](#6-muscles)
7. [Foods](#7-foods)
8. [Workout Programs](#8-workout-programs)
9. [Workout Sessions](#9-workout-sessions)
10. [Diet Plans](#10-diet-plans)
11. [Body Measurements](#11-body-measurements)
12. [Subscriptions](#12-subscriptions)
13. [Coach Clients](#13-coach-clients)
14. [Gym Profile](#14-gym-profile)
15. [Reports](#15-reports)
16. [Enums Reference](#16-enums-reference)
17. [Error Handling](#17-error-handling)

---

# 1. Authentication

**Base Route:** `/api/auth`
**Authorization:** Public (No token required)

## 1.1 Register

Creates a new user account. Used for registering gym owners, coaches, or clients.

```
POST /api/auth/register
```

### Request Body

```json
{
  "email": "user@example.com",
  "phoneNumber": "01012345678",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "role": 0,
  "fullName": "Ahmed Mohamed"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| email | string | Yes | Valid email address |
| phoneNumber | string | No | Phone number (unique per tenant) |
| password | string | Yes | Min 8 chars, uppercase, lowercase, digit |
| confirmPassword | string | Yes | Must match password |
| tenantId | GUID | Yes | The gym/tenant ID |
| role | integer | Yes | 0=Owner, 1=Coach, 2=Client |
| fullName | string | Yes | User's full name |

### Response (200 OK)

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "phoneNumber": "01012345678",
  "role": 0,
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
  "expiresAt": "2024-12-06T18:00:00Z"
}
```

### Validation Rules

- Email: Required, valid email format
- Password: Required, minimum 8 characters, must contain uppercase, lowercase, and digits
- ConfirmPassword: Must match Password
- TenantId: Required, non-empty GUID

---

## 1.2 Login

Authenticates a user and returns JWT tokens.

```
POST /api/auth/login
```

### Request Body

```json
{
  "phoneNumber": "01012345678",
  "password": "SecurePass123!",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| phoneNumber | string | Yes | User's phone number |
| password | string | Yes | User's password |
| tenantId | GUID | Yes | The gym/tenant ID |

### Response (200 OK)

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "phoneNumber": "01012345678",
  "role": 0,
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
  "expiresAt": "2024-12-06T18:00:00Z"
}
```

### Role-Based Redirect

| Role Value | Role Name | Redirect URL |
|------------|-----------|--------------|
| 0 | Owner | `/owner/dashboard` |
| 1 | Coach | `/coach/dashboard` |
| 2 | Client | `/client/my-program` |

### Error Response (401 Unauthorized)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid phone number or password"
}
```

---

## 1.3 Forget Password

Requests a password reset token.

```
POST /api/auth/forget-password
```

### Request Body

```json
{
  "phoneNumber": "01012345678",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### Response (200 OK)

```json
{
  "success": true,
  "message": "Password reset token generated",
  "resetToken": "abc123xyz789"
}
```

---

## 1.4 Reset Password

Resets the password using a reset token.

```
POST /api/auth/reset-password
```

### Request Body

```json
{
  "phoneNumber": "01012345678",
  "resetToken": "abc123xyz789",
  "newPassword": "NewSecurePass123!",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### Response (200 OK)

```json
{
  "success": true,
  "message": "Password reset successfully"
}
```

---

# 2. Tenants

**Base Route:** `/api/tenants`
**Authorization:** Required (JWT Bearer Token)

## 2.1 Get All Tenants

Returns a list of all gyms/tenants.

```
GET /api/tenants
```

### Response (200 OK)

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "FitZone Gym",
    "subdomain": "fitzone-gym",
    "status": 1,
    "createdAt": "2024-01-01T00:00:00Z"
  }
]
```

---

## 2.2 Create Tenant

Creates a new gym/tenant.

```
POST /api/tenants
```

### Request Body

```json
{
  "name": "FitZone Gym",
  "subdomain": "fitzone-gym",
  "logoUrl": "https://example.com/logo.png",
  "primaryColor": "#FF5722",
  "secondaryColor": "#2196F3"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| name | string | Yes | Gym name |
| subdomain | string | Yes | Unique subdomain identifier |
| logoUrl | string | No | Logo image URL |
| primaryColor | string | No | Primary brand color (hex) |
| secondaryColor | string | No | Secondary brand color (hex) |

### Response (201 Created)

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "FitZone Gym",
  "subdomain": "fitzone-gym",
  "status": 1,
  "createdAt": "2024-12-06T12:00:00Z"
}
```

---

# 3. Users

**Base Route:** `/api/users`
**Authorization:** Required (JWT Bearer Token)

## 3.1 Get All Users

Returns a list of users with optional filters.

```
GET /api/users
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| role | integer | No | Filter by role (0=Owner, 1=Coach, 2=Client) |
| isActive | boolean | No | Filter by active status |
| searchTerm | string | No | Search by name or phone |

### Example Request

```
GET /api/users?role=1&isActive=true&searchTerm=ahmed
```

### Response (200 OK)

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "coach@fitzone.com",
    "phoneNumber": "01012345678",
    "role": 1,
    "isActive": true,
    "walletBalance": 0.00,
    "profile": {
      "fullName": "Ahmed Coach",
      "gender": 1,
      "birthDate": "1990-01-15T00:00:00Z",
      "heightCm": 180.0,
      "activityLevel": "Active",
      "medicalHistory": null
    }
  }
]
```

---

## 3.2 Get User by ID

Returns a specific user by ID.

```
GET /api/users/{id}
```

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | GUID | Yes | User ID |

### Response (200 OK)

Same structure as Get All Users (single object)

### Response (404 Not Found)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "User not found"
}
```

---

## 3.3 Update User

Updates a user's basic information.

```
PUT /api/users/{id}
```

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | GUID | Yes | User ID |

### Request Body

```json
{
  "phoneNumber": "01098765432",
  "isActive": true
}
```

### Response (204 No Content)

No response body

---

## 3.4 Update User Profile

Updates a user's profile information.

```
PUT /api/users/{id}/profile
```

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | GUID | Yes | User ID |

### Request Body

```json
{
  "fullName": "Ahmed Mohamed",
  "gender": 1,
  "birthDate": "1990-01-15",
  "heightCm": 180.0,
  "activityLevel": "Active",
  "medicalHistory": "No known allergies"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| fullName | string | No | User's full name |
| gender | integer | No | 1=Male, 2=Female |
| birthDate | date | No | Date of birth |
| heightCm | double | No | Height in centimeters |
| activityLevel | string | No | Activity level description |
| medicalHistory | string | No | Medical history notes |

### Response (204 No Content)

No response body

---

# 4. Clients

**Base Route:** `/api/clients`
**Authorization:** Required (JWT Bearer Token)

## 4.1 Get All Clients

Returns a list of clients with optional filters.

```
GET /api/clients
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| searchTerm | string | No | Search by name or phone |
| isActive | boolean | No | Filter by active status |

### Response (200 OK)

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "client@email.com",
    "phoneNumber": "01055555555",
    "isActive": true,
    "walletBalance": 150.00,
    "profile": {
      "fullName": "Mohamed Client",
      "gender": 1,
      "birthDate": "1995-05-20T00:00:00Z",
      "heightCm": 175.0,
      "activityLevel": "Moderate",
      "medicalHistory": null
    },
    "activeSubscription": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "planName": "Monthly",
      "startDate": "2024-12-01T00:00:00Z",
      "endDate": "2025-01-01T00:00:00Z",
      "status": "Active"
    }
  }
]
```

---

## 4.2 Get Client by ID

```
GET /api/clients/{id}
```

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | GUID | Yes | Client ID |

### Response (200 OK)

Same structure as Get All Clients (single object)

---

## 4.3 Create Client

Creates a new client account.

```
POST /api/clients
```

### Request Body

```json
{
  "phoneNumber": "01055555555",
  "email": "client@email.com",
  "password": "ClientPass123",
  "fullName": "Mohamed Client",
  "gender": 1,
  "birthDate": "1995-05-20",
  "heightCm": 175.0,
  "activityLevel": "Moderate",
  "medicalHistory": "No allergies"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| phoneNumber | string | Yes | Unique phone number (per tenant) |
| email | string | No | Email address |
| password | string | Yes | Min 6 characters |
| fullName | string | No | Client's full name |
| gender | integer | No | 1=Male, 2=Female |
| birthDate | date | No | Date of birth |
| heightCm | double | No | Height in centimeters |
| activityLevel | string | No | Activity level |
| medicalHistory | string | No | Medical notes |

### Response (200 OK)

```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

## 4.4 Update Client

```
PUT /api/clients/{id}
```

### Request Body

```json
{
  "email": "newemail@email.com",
  "phoneNumber": "01055555555",
  "isActive": true,
  "fullName": "Mohamed Ahmed",
  "gender": 1,
  "birthDate": "1995-05-20",
  "heightCm": 176.0,
  "activityLevel": "Active",
  "medicalHistory": "Updated notes"
}
```

### Response (204 No Content)

---

## 4.5 Delete Client

Soft deletes a client.

```
DELETE /api/clients/{id}
```

### Response (204 No Content)

---

# 5. Exercises

**Base Route:** `/api/exercises`
**Authorization:** Required (JWT Bearer Token)

## 5.1 Get All Exercises

```
GET /api/exercises
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| targetMuscleId | integer | No | Filter by target muscle |
| equipment | string | No | Filter by equipment type |
| isHighImpact | boolean | No | Filter by impact level |

### Response (200 OK)

```json
[
  {
    "id": 1,
    "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Bench Press",
    "targetMuscleId": 1,
    "targetMuscleName": "Chest",
    "imageUrl": "/uploads/images/2024/12/exercises/bench-press.jpg",
    "videoUrl": "/uploads/videos/2024/12/exercises/bench-press.mp4",
    "equipment": "Barbell",
    "isHighImpact": false
  }
]
```

---

## 5.2 Get Exercise by ID

```
GET /api/exercises/{id}
```

### Response (200 OK)

Single exercise object

---

## 5.3 Create Exercise

```
POST /api/exercises
```

**Content-Type:** `multipart/form-data`

### Form Data

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| name | string | Yes | Exercise name |
| targetMuscleId | integer | Yes | Target muscle ID |
| image | file | No | Exercise image (JPG, PNG, GIF, WebP - max 10MB) |
| video | file | No | Exercise video (MP4, AVI, MOV - max 100MB) |
| equipment | string | No | Equipment needed |
| isHighImpact | boolean | No | High impact exercise flag |

### Response (201 Created)

```json
1
```

---

## 5.4 Update Exercise

```
PUT /api/exercises/{id}
```

**Content-Type:** `multipart/form-data`

Same form data as Create Exercise

### Response (204 No Content)

---

## 5.5 Delete Exercise

```
DELETE /api/exercises/{id}
```

### Response (204 No Content)

---

# 6. Muscles

**Base Route:** `/api/muscles`
**Authorization:** Required (JWT Bearer Token)

## 6.1 Get All Muscles

```
GET /api/muscles
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| bodyPart | string | No | Filter by body part |

### Response (200 OK)

```json
[
  {
    "id": 1,
    "name": "Chest",
    "bodyPart": "Upper Body"
  },
  {
    "id": 2,
    "name": "Back",
    "bodyPart": "Upper Body"
  },
  {
    "id": 3,
    "name": "Quadriceps",
    "bodyPart": "Lower Body"
  }
]
```

---

## 6.2 Get Muscle by ID

```
GET /api/muscles/{id}
```

### Response (200 OK)

Single muscle object

---

# 7. Foods

**Base Route:** `/api/foods`
**Authorization:** Required (JWT Bearer Token)

## 7.1 Get All Foods

```
GET /api/foods
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| category | string | No | Filter by category |
| searchTerm | string | No | Search by name |
| isVerified | boolean | No | Filter by verification status |

### Response (200 OK)

```json
[
  {
    "id": 1,
    "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Grilled Chicken Breast",
    "category": "Protein",
    "caloriesPer100g": 165.0,
    "proteinPer100g": 31.0,
    "carbsPer100g": 0.0,
    "fatsPer100g": 3.6,
    "fiberPer100g": 0.0,
    "alternativeGroupId": "poultry",
    "isVerified": true
  }
]
```

---

## 7.2 Get Food by ID

```
GET /api/foods/{id}
```

---

## 7.3 Create Food

```
POST /api/foods
```

### Request Body

```json
{
  "name": "Grilled Chicken Breast",
  "category": "Protein",
  "caloriesPer100g": 165.0,
  "proteinPer100g": 31.0,
  "carbsPer100g": 0.0,
  "fatsPer100g": 3.6,
  "fiberPer100g": 0.0,
  "alternativeGroupId": "poultry"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| name | string | Yes | Food name |
| category | string | No | Food category |
| caloriesPer100g | double | Yes | Calories per 100g |
| proteinPer100g | double | Yes | Protein per 100g |
| carbsPer100g | double | Yes | Carbs per 100g |
| fatsPer100g | double | Yes | Fats per 100g |
| fiberPer100g | double | No | Fiber per 100g |
| alternativeGroupId | string | No | Group ID for alternatives |

### Response (201 Created)

```json
1
```

---

## 7.4 Update Food

```
PUT /api/foods/{id}
```

Same body as Create Food

### Response (204 No Content)

---

## 7.5 Delete Food

```
DELETE /api/foods/{id}
```

### Response (204 No Content)

---

# 8. Workout Programs

**Base Route:** `/api/workoutprograms`
**Authorization:** Required (JWT Bearer Token)

## 8.1 Get All Workout Programs

```
GET /api/workoutprograms
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| coachId | GUID | No | Filter by coach |
| clientId | GUID | No | Filter by client |

### Example: Get Client's Programs

```
GET /api/workoutprograms?clientId=3fa85f64-5717-4562-b3fc-2c963f66afa6
```

### Response (200 OK)

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "coachId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "coachName": "Ahmed Coach",
    "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientName": "Mohamed Client",
    "name": "Muscle Building Program",
    "startDate": "2024-12-01T00:00:00Z",
    "endDate": "2025-03-01T00:00:00Z",
    "routines": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "programId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "name": "Day 1 - Chest & Triceps",
        "dayOfWeek": 1,
        "exercises": [
          {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "routineId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
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
]
```

---

## 8.2 Get Workout Program by ID

```
GET /api/workoutprograms/{id}
```

### Response (200 OK)

Single program with full routines and exercises

---

## 8.3 Create Workout Program

```
POST /api/workoutprograms
```

### Request Body

```json
{
  "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Muscle Building Program",
  "startDate": "2024-12-01",
  "endDate": "2025-03-01"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| clientId | GUID | Yes | Target client ID |
| name | string | Yes | Program name |
| startDate | date | Yes | Program start date |
| endDate | date | No | Program end date |

### Response (201 Created)

```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

## 8.4 Add Routine to Program

```
POST /api/workoutprograms/{programId}/routines
```

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| programId | GUID | Yes | Program ID |

### Request Body

```json
{
  "name": "Day 1 - Chest & Triceps",
  "dayOfWeek": 1
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| name | string | Yes | Routine name |
| dayOfWeek | integer | Yes | Day of week (1-7) |

### Response (200 OK)

```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

## 8.5 Add Exercise to Routine

```
POST /api/workoutprograms/routines/{routineId}/exercises
```

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| routineId | GUID | Yes | Routine ID |

### Request Body

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

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| exerciseId | integer | Yes | Exercise ID |
| sets | integer | Yes | Number of sets |
| repsMin | integer | Yes | Minimum reps |
| repsMax | integer | Yes | Maximum reps |
| restSec | integer | Yes | Rest time in seconds |
| supersetGroupId | GUID | No | Superset group ID |

### Response (200 OK)

```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

## 8.6 Delete Workout Program

```
DELETE /api/workoutprograms/{id}
```

### Response (204 No Content)

---

# 9. Workout Sessions

**Base Route:** `/api/workoutsessions`
**Authorization:** Required (JWT Bearer Token)

## 9.1 Get All Workout Sessions

```
GET /api/workoutsessions
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| clientId | GUID | No | Filter by client |
| fromDate | datetime | No | Filter from date |
| toDate | datetime | No | Filter to date |

### Example

```
GET /api/workoutsessions?clientId=xxx&fromDate=2024-01-01&toDate=2024-12-31
```

### Response (200 OK)

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientName": "Mohamed Client",
    "routineId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "routineName": "Day 1 - Chest & Triceps",
    "startTime": "2024-12-06T10:00:00Z",
    "endTime": "2024-12-06T11:30:00Z",
    "notes": "Great workout!",
    "sets": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "exerciseId": 1,
        "exerciseName": "Bench Press",
        "setNumber": 1,
        "weight": 80.0,
        "reps": 10,
        "rpe": 8,
        "volumeLoad": 800.0,
        "isPR": false
      }
    ]
  }
]
```

---

## 9.2 Get Workout Session by ID

```
GET /api/workoutsessions/{id}
```

### Response (200 OK)

Single session with all sets

---

## 9.3 Start Workout Session

Starts a new workout session for a routine.

```
POST /api/workoutsessions/start
```

### Request Body

```json
{
  "routineId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| routineId | GUID | Yes | The routine to start |

### Response (201 Created)

```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

## 9.4 End Workout Session

Ends an active workout session.

```
POST /api/workoutsessions/{sessionId}/end
```

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| sessionId | GUID | Yes | Session ID |

### Request Body

```json
{
  "notes": "Great workout, felt strong today!"
}
```

### Response (204 No Content)

---

## 9.5 Log Set

Records a set during a workout session.

```
POST /api/workoutsessions/{sessionId}/sets
```

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| sessionId | GUID | Yes | Session ID |

### Request Body

```json
{
  "exerciseId": 1,
  "setNumber": 1,
  "weight": 80.0,
  "reps": 10,
  "rpe": 8
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| exerciseId | integer | Yes | Exercise ID |
| setNumber | integer | Yes | Set number (1, 2, 3...) |
| weight | double | Yes | Weight lifted (kg) |
| reps | integer | Yes | Repetitions completed |
| rpe | integer | No | Rate of Perceived Exertion (1-10) |

### Response (200 OK)

```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

# 10. Diet Plans

**Base Route:** `/api/dietplans`
**Authorization:** Required (JWT Bearer Token)

## 10.1 Get All Diet Plans

```
GET /api/dietplans
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| coachId | GUID | No | Filter by coach |
| clientId | GUID | No | Filter by client |
| status | integer | No | 0=Active, 1=Archived, 2=Draft |

### Example: Get Client's Active Diet Plan

```
GET /api/dietplans?clientId=xxx&status=0
```

### Response (200 OK)

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "coachId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "coachName": "Ahmed Coach",
    "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientName": "Mohamed Client",
    "name": "Weight Loss Plan",
    "startDate": "2024-12-01T00:00:00Z",
    "endDate": "2025-03-01T00:00:00Z",
    "status": 0,
    "targetCalories": 2000.0,
    "targetProtein": 150.0,
    "targetCarbs": 200.0,
    "targetFats": 67.0,
    "meals": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "planId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "name": "Breakfast",
        "orderIndex": 1,
        "items": [
          {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "mealId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "foodId": 1,
            "foodName": "Eggs",
            "assignedQuantity": 200.0,
            "calcCalories": 310.0,
            "calcProtein": 26.0,
            "calcCarbs": 2.0,
            "calcFats": 22.0
          }
        ]
      }
    ]
  }
]
```

---

## 10.2 Get Diet Plan by ID

```
GET /api/dietplans/{id}
```

### Response (200 OK)

Single plan with full meals and items

---

## 10.3 Create Diet Plan

```
POST /api/dietplans
```

### Request Body

```json
{
  "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Weight Loss Plan",
  "startDate": "2024-12-01",
  "endDate": "2025-03-01",
  "targetCalories": 2000.0,
  "targetProtein": 150.0,
  "targetCarbs": 200.0,
  "targetFats": 67.0
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| clientId | GUID | Yes | Target client ID |
| name | string | Yes | Plan name |
| startDate | date | Yes | Plan start date |
| endDate | date | No | Plan end date |
| targetCalories | double | No | Daily calorie target |
| targetProtein | double | No | Daily protein target (g) |
| targetCarbs | double | No | Daily carbs target (g) |
| targetFats | double | No | Daily fats target (g) |

### Response (201 Created)

```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

## 10.4 Add Meal to Diet Plan

```
POST /api/dietplans/{planId}/meals
```

### Request Body

```json
{
  "name": "Breakfast",
  "orderIndex": 1
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| name | string | Yes | Meal name |
| orderIndex | integer | Yes | Display order |

### Response (200 OK)

```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

## 10.5 Add Food Item to Meal

```
POST /api/dietplans/meals/{mealId}/items
```

### Request Body

```json
{
  "foodId": 1,
  "assignedQuantity": 200.0
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| foodId | integer | Yes | Food ID |
| assignedQuantity | double | Yes | Quantity in grams |

### Response (200 OK)

```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

**Note:** Calories, protein, carbs, and fats are calculated automatically based on food's per-100g values.

---

## 10.6 Delete Diet Plan

```
DELETE /api/dietplans/{id}
```

### Response (204 No Content)

---

# 11. Body Measurements

**Base Route:** `/api/bodymeasurements`
**Authorization:** Required (JWT Bearer Token)

## 11.1 Get All Body Measurements

```
GET /api/bodymeasurements
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| clientId | GUID | No | Filter by client |
| fromDate | datetime | No | Filter from date |
| toDate | datetime | No | Filter to date |

### Response (200 OK)

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "dateRecorded": "2024-12-06T00:00:00Z",
    "weightKg": 85.5,
    "skeletalMuscleMass": 38.2,
    "bodyFatMass": 15.3,
    "bodyFatPercent": 17.9,
    "totalBodyWater": 55.2,
    "bmr": 1850,
    "visceralFatLevel": 8,
    "inbodyImageUrl": "/uploads/images/2024/12/measurements/inbody.jpg",
    "frontPhotoUrl": "/uploads/images/2024/12/measurements/front.jpg",
    "sidePhotoUrl": "/uploads/images/2024/12/measurements/side.jpg",
    "backPhotoUrl": "/uploads/images/2024/12/measurements/back.jpg"
  }
]
```

---

## 11.2 Create Body Measurement

```
POST /api/bodymeasurements
```

**Content-Type:** `multipart/form-data`

### Form Data

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| clientId | GUID | Yes | Client ID |
| dateRecorded | date | Yes | Measurement date |
| weightKg | double | Yes | Weight in kg |
| skeletalMuscleMass | double | No | Skeletal muscle mass (kg) |
| bodyFatMass | double | No | Body fat mass (kg) |
| bodyFatPercent | double | No | Body fat percentage |
| totalBodyWater | double | No | Total body water (%) |
| bmr | integer | No | Basal Metabolic Rate |
| visceralFatLevel | integer | No | Visceral fat level |
| inbodyImage | file | No | InBody result image |
| frontPhoto | file | No | Front progress photo |
| sidePhoto | file | No | Side progress photo |
| backPhoto | file | No | Back progress photo |

### Response (200 OK)

```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

## 11.3 Delete Body Measurement

```
DELETE /api/bodymeasurements/{id}
```

### Response (204 No Content)

---

# 12. Subscriptions

**Base Route:** `/api/subscriptions`
**Authorization:** Required (JWT Bearer Token)

## 12.1 Get Subscription Plans

```
GET /api/subscriptions/plans
```

### Response (200 OK)

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Monthly",
    "price": 500.00,
    "durationMonths": 1
  },
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Quarterly",
    "price": 1200.00,
    "durationMonths": 3
  }
]
```

---

## 12.2 Create Subscription Plan

```
POST /api/subscriptions/plans
```

### Request Body

```json
{
  "name": "Monthly",
  "price": 500.00,
  "durationMonths": 1
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| name | string | Yes | Plan name |
| price | decimal | Yes | Plan price |
| durationMonths | integer | Yes | Duration in months |

### Response (200 OK)

```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

## 12.3 Get Client Subscriptions

```
GET /api/subscriptions
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| clientId | GUID | No | Filter by client |
| status | integer | No | Filter by status |

### Status Values

| Value | Status |
|-------|--------|
| 0 | Active |
| 1 | Suspended |
| 2 | Trial |
| 3 | Expired |
| 4 | Cancelled |

### Response (200 OK)

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientName": "Mohamed Client",
    "planId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "planName": "Monthly",
    "startDate": "2024-12-01T00:00:00Z",
    "endDate": "2025-01-01T00:00:00Z",
    "status": 0,
    "salesCoachId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "salesCoachName": "Ahmed Coach",
    "freezes": []
  }
]
```

---

## 12.4 Create Client Subscription

```
POST /api/subscriptions
```

### Request Body

```json
{
  "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "planId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "startDate": "2024-12-01"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| clientId | GUID | Yes | Client ID |
| planId | GUID | Yes | Subscription plan ID |
| startDate | date | Yes | Subscription start date |

**Note:** End date is calculated automatically based on plan duration.

### Response (200 OK)

```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

## 12.5 Freeze Subscription

```
POST /api/subscriptions/{subscriptionId}/freeze
```

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| subscriptionId | GUID | Yes | Subscription ID |

### Request Body

```json
{
  "startDate": "2024-12-15",
  "endDate": "2024-12-22",
  "reason": "Travel"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| startDate | date | Yes | Freeze start date |
| endDate | date | Yes | Freeze end date |
| reason | string | No | Reason for freezing |

### Response (200 OK)

```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

## 12.6 Cancel Subscription

```
POST /api/subscriptions/{subscriptionId}/cancel
```

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| subscriptionId | GUID | Yes | Subscription ID |

### Response (204 No Content)

---

# 13. Coach Clients

**Base Route:** `/api/coach-clients`
**Authorization:** Required (JWT Bearer Token)

## 13.1 Get Coach Clients

Returns trainees assigned to a coach.

```
GET /api/coach-clients
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| coachId | GUID | No | Filter by coach (Owner only) |
| isActive | boolean | No | Filter by active status (default: true) |

### Authorization Notes

- **Owner:** Can view any coach's trainees using coachId filter
- **Coach:** Sees only their own trainees

### Response (200 OK)

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "coachId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "coachName": "Ahmed Coach",
    "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientName": "Mohamed Client",
    "clientPhone": "01055555555",
    "clientEmail": "client@email.com",
    "assignedAt": "2024-12-01T00:00:00Z",
    "unassignedAt": null,
    "isActive": true,
    "notes": "Focus on weight loss",
    "hasActiveSubscription": true,
    "subscriptionEndDate": "2025-01-01T00:00:00Z",
    "workoutProgramsCount": 2,
    "dietPlansCount": 1,
    "workoutSessionsCount": 15,
    "lastSessionDate": "2024-12-05T10:00:00Z"
  }
]
```

---

## 13.2 Assign Client to Coach

```
POST /api/coach-clients
```

### Request Body

```json
{
  "coachId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "notes": "Focus on weight loss"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| coachId | GUID | No | Coach ID (null = assign to self) |
| clientId | GUID | Yes | Client to assign |
| notes | string | No | Assignment notes |

### Authorization Notes

- **Owner:** Can assign to any coach
- **Coach:** Can only assign to self (leave coachId null)

### Response (200 OK)

```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

## 13.3 Unassign Client from Coach

```
DELETE /api/coach-clients/{clientId}
```

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| clientId | GUID | Yes | Client ID to unassign |

### Authorization Notes

- **Owner:** Can unassign any client
- **Coach:** Can only unassign their own clients

### Response (204 No Content)

---

# 14. Gym Profile

**Base Route:** `/api/gymprofile`
**Authorization:** Required (JWT Bearer Token)

## 14.1 Get Gym Profile

```
GET /api/gymprofile
```

### Response (200 OK)

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "FitZone Gym",
  "subdomain": "fitzone-gym",
  "description": "Best gym in the city",
  "address": "123 Main Street, Cairo",
  "phoneNumber": "01012345678",
  "email": "info@fitzone.com",
  "logoUrl": "/uploads/images/2024/12/gym-logos/logo.png",
  "coverImageUrl": "/uploads/images/2024/12/gym-covers/cover.jpg",
  "galleryImages": [
    "/uploads/images/2024/12/gym-gallery/img1.jpg",
    "/uploads/images/2024/12/gym-gallery/img2.jpg"
  ],
  "status": "Active",
  "brandingSettings": {
    "primaryColor": "#FF5722",
    "secondaryColor": "#2196F3",
    "logoUrl": "/uploads/images/2024/12/gym-logos/logo.png"
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

## 14.2 Update Gym Profile

```
PUT /api/gymprofile
```

### Request Body

```json
{
  "name": "FitZone Gym",
  "description": "Best gym in the city",
  "address": "123 Main Street, Cairo",
  "phoneNumber": "01012345678",
  "email": "info@fitzone.com",
  "primaryColor": "#FF5722",
  "secondaryColor": "#2196F3"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| name | string | No | Gym name |
| description | string | No | Gym description |
| address | string | No | Physical address |
| phoneNumber | string | No | Contact phone |
| email | string | No | Contact email |
| primaryColor | string | No | Primary brand color (hex) |
| secondaryColor | string | No | Secondary brand color (hex) |

### Response (204 No Content)

---

## 14.3 Upload Logo

```
POST /api/gymprofile/logo
```

**Content-Type:** `multipart/form-data`

### Form Data

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| file | file | Yes | Logo image file |

### Response (200 OK)

```json
{
  "url": "/uploads/images/2024/12/gym-logos/logo.png"
}
```

---

## 14.4 Upload Cover Image

```
POST /api/gymprofile/cover
```

**Content-Type:** `multipart/form-data`

### Form Data

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| file | file | Yes | Cover image file |

### Response (200 OK)

```json
{
  "url": "/uploads/images/2024/12/gym-covers/cover.jpg"
}
```

---

## 14.5 Upload Gallery Images

```
POST /api/gymprofile/gallery
```

**Content-Type:** `multipart/form-data`

### Form Data

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| files | files | Yes | Multiple gallery images |

### Response (200 OK)

```json
{
  "urls": [
    "/uploads/images/2024/12/gym-gallery/img1.jpg",
    "/uploads/images/2024/12/gym-gallery/img2.jpg"
  ]
}
```

---

# 15. Reports

**Base Route:** `/api/reports`
**Authorization:** Required (JWT Bearer Token)

## 15.1 Dashboard Report

Overview statistics for the gym.

```
GET /api/reports/dashboard
```

### Response (200 OK)

```json
{
  "totalClients": 150,
  "activeClients": 120,
  "newClientsThisMonth": 15,
  "totalCoaches": 5,
  "activeSubscriptions": 100,
  "expiringSubscriptions": 8,
  "totalRevenueThisMonth": 50000.00,
  "totalRevenueLastMonth": 45000.00,
  "totalWorkoutsThisMonth": 450,
  "totalDietPlansActive": 80
}
```

---

## 15.2 Clients Report

Detailed client analytics.

```
GET /api/reports/clients
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| fromDate | datetime | No | Report start date |
| toDate | datetime | No | Report end date |

### Response (200 OK)

```json
{
  "totalClients": 150,
  "activeClients": 120,
  "inactiveClients": 30,
  "newClientsThisMonth": 15,
  "clientsWithActiveSubscription": 100,
  "clientsWithoutSubscription": 50,
  "topClients": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Mohamed Client",
      "phoneNumber": "01055555555",
      "totalSessions": 25,
      "totalPaid": 5000.00
    }
  ],
  "monthlyTrend": [
    {
      "month": "2024-12",
      "newClients": 15,
      "churnedClients": 3
    }
  ]
}
```

---

## 15.3 Subscriptions Report

```
GET /api/reports/subscriptions
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| fromDate | datetime | No | Report start date |
| toDate | datetime | No | Report end date |

### Response (200 OK)

```json
{
  "totalSubscriptions": 200,
  "activeSubscriptions": 100,
  "expiredSubscriptions": 80,
  "cancelledSubscriptions": 20,
  "expiringIn7Days": 8,
  "expiringIn30Days": 25,
  "totalRevenue": 500000.00,
  "revenueThisMonth": 50000.00,
  "planStatistics": [
    {
      "planId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "planName": "Monthly",
      "activeCount": 60,
      "totalSold": 150,
      "totalRevenue": 300000.00
    }
  ],
  "monthlyRevenue": [
    {
      "month": "2024-12",
      "revenue": 50000.00,
      "subscriptionCount": 100
    }
  ]
}
```

---

## 15.4 Financial Report

```
GET /api/reports/financial
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| fromDate | datetime | No | Report start date |
| toDate | datetime | No | Report end date |

### Response (200 OK)

```json
{
  "totalRevenue": 500000.00,
  "revenueThisMonth": 50000.00,
  "revenueLastMonth": 45000.00,
  "growthPercentage": 11.1,
  "averageSubscriptionValue": 500.00,
  "totalWalletBalance": 25000.00,
  "monthlyRevenue": [
    {
      "month": "2024-12",
      "revenue": 50000.00,
      "subscriptionCount": 100
    }
  ],
  "paymentMethods": [
    {
      "paymentMethod": "Cash",
      "count": 150,
      "totalAmount": 300000.00
    }
  ]
}
```

---

## 15.5 Coach Dashboard Report

```
GET /api/reports/coach/dashboard
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| coachId | GUID | No | Coach ID (Owner can view any coach) |

### Authorization Notes

- **Owner:** Can view any coach's dashboard using coachId filter
- **Coach:** Sees only their own dashboard

### Response (200 OK)

```json
{
  "totalTrainees": 25,
  "activeTrainees": 22,
  "newTraineesThisMonth": 5,
  "activeWorkoutPrograms": 20,
  "activeDietPlans": 15,
  "totalSessionsThisMonth": 150,
  "totalVolumeThisMonth": 125000.0,
  "topTraineesByProgress": [
    {
      "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "clientName": "Mohamed Client",
      "clientPhone": "01055555555",
      "sessionsCount": 20,
      "weightChange": -5.2,
      "bodyFatChange": -3.1
    }
  ],
  "topTraineesBySessions": [
    {
      "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "clientName": "Ahmed Client",
      "clientPhone": "01066666666",
      "sessionsCount": 25,
      "weightChange": -3.0,
      "bodyFatChange": -2.0
    }
  ]
}
```

---

## 15.6 Coach Trainees Report

```
GET /api/reports/coach/trainees
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| coachId | GUID | No | Coach ID (Owner can view any coach) |

### Response (200 OK)

```json
{
  "totalTrainees": 25,
  "withActiveSubscription": 20,
  "withoutSubscription": 5,
  "trainees": [
    {
      "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Mohamed Client",
      "phone": "01055555555",
      "email": "client@email.com",
      "assignedAt": "2024-12-01T00:00:00Z",
      "hasActiveSubscription": true,
      "subscriptionEndDate": "2025-01-01T00:00:00Z",
      "activeWorkoutPrograms": 2,
      "activeDietPlans": 1,
      "totalSessions": 50,
      "sessionsThisMonth": 12,
      "lastSessionDate": "2024-12-05T10:00:00Z",
      "currentWeight": 80.5,
      "weightChange": -5.2,
      "bodyFatPercent": 18.5,
      "lastMeasurementDate": "2024-12-01T00:00:00Z"
    }
  ]
}
```

---

## 15.7 Trainee Progress Report

Detailed progress report for a specific trainee.

```
GET /api/reports/coach/trainee/{clientId}
```

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| clientId | GUID | Yes | Client ID |

### Authorization Notes

- **Owner:** Can view any trainee's progress
- **Coach:** Can only view their own trainees

### Response (200 OK)

```json
{
  "clientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientName": "Mohamed Client",
  "clientPhone": "01055555555",
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
      "dateRecorded": "2024-12-01T00:00:00Z",
      "weightKg": 80.0,
      "bodyFatPercent": 18.0,
      "muscleMass": 38.0,
      "bmr": 1900
    }
  ],
  "startWeight": 85.0,
  "currentWeight": 80.0,
  "totalWeightChange": -5.0,
  "startBodyFat": 22.0,
  "currentBodyFat": 18.0,
  "totalBodyFatChange": -4.0,
  "startMuscleMass": 35.0,
  "currentMuscleMass": 38.0,
  "totalMuscleMassChange": 3.0,
  "totalSessions": 50,
  "totalVolumeLifted": 150000.0,
  "monthlySessions": [
    {
      "month": "2024-12",
      "sessionCount": 12,
      "totalVolume": 30000.0
    }
  ],
  "workoutPrograms": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Muscle Building Program",
      "startDate": "2024-01-01T00:00:00Z",
      "routinesCount": 4
    }
  ],
  "dietPlans": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Weight Loss Plan",
      "startDate": "2024-01-01T00:00:00Z",
      "targetCalories": 2000.0
    }
  ],
  "personalRecords": [
    {
      "exerciseId": 1,
      "exerciseName": "Bench Press",
      "maxWeight": 100.0,
      "reps": 5,
      "achievedAt": "2024-12-01T10:00:00Z"
    }
  ]
}
```

---

# 16. Enums Reference

## User Roles

| Value | Name | Description |
|-------|------|-------------|
| 0 | Owner | Gym owner with full access |
| 1 | Coach | Trainer with trainee management |
| 2 | Client | Gym member/client |

## Gender Types

| Value | Name |
|-------|------|
| 1 | Male |
| 2 | Female |

## Subscription Status

| Value | Name | Description |
|-------|------|-------------|
| 0 | Active | Currently active subscription |
| 1 | Suspended | Temporarily suspended |
| 2 | Trial | Trial period |
| 3 | Expired | Subscription expired |
| 4 | Cancelled | Subscription cancelled |

## Plan Status (Diet Plans)

| Value | Name | Description |
|-------|------|-------------|
| 0 | Active | Currently active plan |
| 1 | Archived | Archived/completed plan |
| 2 | Draft | Draft plan not yet active |

---

# 17. Error Handling

## Error Response Format

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "PhoneNumber": ["Phone number is required"],
    "Password": ["Password must be at least 8 characters"]
  }
}
```

## HTTP Status Codes

| Code | Description |
|------|-------------|
| 200 | OK - Request successful |
| 201 | Created - Resource created successfully |
| 204 | No Content - Request successful, no response body |
| 400 | Bad Request - Validation error |
| 401 | Unauthorized - Missing or invalid token |
| 403 | Forbidden - Insufficient permissions |
| 404 | Not Found - Resource not found |
| 500 | Internal Server Error - Server error |

## Common Validation Errors

| Field | Error | Description |
|-------|-------|-------------|
| PhoneNumber | Required | Phone number is required |
| PhoneNumber | Invalid format | Must contain only digits, +, -, spaces |
| Password | Min length | Must be at least 8 characters |
| Password | Complexity | Must contain uppercase, lowercase, and digit |
| Email | Invalid format | Must be a valid email address |
| TenantId | Required | Tenant ID is required |
| TenantId | Invalid | Must be a valid GUID |

---

# Authorization Summary

## Public Endpoints (No Token Required)

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/forget-password`
- `POST /api/auth/reset-password`

## Protected Endpoints (JWT Bearer Token Required)

All other endpoints require a valid JWT token in the Authorization header:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## Role-Based Access Control

| Endpoint Category | Owner | Coach | Client |
|-------------------|-------|-------|--------|
| Tenants | Full | Read | - |
| Users | Full | Read (own) | Read (self) |
| Clients | Full | Assigned only | Self only |
| Exercises | Full | Full | Read |
| Foods | Full | Full | Read |
| Workout Programs | Full | Assigned clients | Self only |
| Diet Plans | Full | Assigned clients | Self only |
| Body Measurements | Full | Assigned clients | Self only |
| Subscriptions | Full | Create | Self only |
| Coach Clients | Full | Own clients | - |
| Gym Profile | Full | Read | Read |
| Reports | Full | Coach reports | - |

---

*Last Updated: December 2024*
