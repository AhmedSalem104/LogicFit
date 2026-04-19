# LogicFit - Gym Management API (Phases 1-9)

> **دليل شامل لجميع الـ APIs الجديدة الخاصة بإدارة الجيم للفرونت إند**
> **آخر تحديث:** 2026-04-19
> **الحالة:** Build Successful | All endpoints authenticated

---

## 📋 جدول المحتويات

| # | المجموعة | Base Path |
|---|----------|-----------|
| 1 | [Branches (الفروع)](#1-branches-الفروع) | `/api/Branches` |
| 2 | [Membership Cards (بطاقات العضوية)](#2-membership-cards-بطاقات-العضوية) | `/api/MembershipCards` |
| 3 | [Gate Access (البوابة الإلكترونية)](#3-gate-access-البوابة-الإلكترونية) | `/api/GateAccess` |
| 4 | [Rooms (القاعات)](#4-rooms-القاعات) | `/api/Rooms` |
| 5 | [Equipment (الأجهزة)](#5-equipment-الأجهزة) | `/api/Equipment` |
| 6 | [Maintenance (الصيانة)](#6-maintenance-الصيانة) | `/api/Maintenance` |
| 7 | [Expense Categories (فئات المصروفات)](#7-expense-categories) | `/api/ExpenseCategories` |
| 8 | [Expenses (المصروفات)](#8-expenses-المصروفات) | `/api/Expenses` |
| 9 | [Invoices (الفواتير)](#9-invoices-الفواتير) | `/api/Invoices` |
| 10 | [Payments (المدفوعات)](#10-payments-المدفوعات) | `/api/Payments` |
| 11 | [Coupons (الكوبونات)](#11-coupons-الكوبونات) | `/api/Coupons` |
| 12 | [Tax Settings (الضرائب)](#12-tax-settings-الضرائب) | `/api/TaxSettings` |
| 13 | [Group Classes (الحصص الجماعية)](#13-group-classes-الحصص-الجماعية) | `/api/GroupClasses` |
| 14 | [Class Schedules & Enrollments](#14-class-schedules--enrollments) | `/api/ClassSchedules` |
| 15 | [Product Categories](#15-product-categories) | `/api/ProductCategories` |
| 16 | [Products (المنتجات)](#16-products-المنتجات) | `/api/Products` |
| 17 | [Stock (المخزون)](#17-stock-المخزون) | `/api/Stock` |
| 18 | [Suppliers (الموردين)](#18-suppliers-الموردين) | `/api/Suppliers` |
| 19 | [Sales / POS](#19-sales--pos-نقطة-البيع) | `/api/Sales` |
| 20 | [Employees (الموظفين)](#20-employees-الموظفين) | `/api/Employees` |
| 21 | [Shifts (الورديات)](#21-shifts-الورديات) | `/api/Shifts` |
| 22 | [Leaves (الإجازات)](#22-leaves-الإجازات) | `/api/Leaves` |
| 23 | [Commissions (العمولات)](#23-commissions-العمولات) | `/api/Commissions` |
| 24 | [Payroll (الرواتب)](#24-payroll-الرواتب) | `/api/Payroll` |
| 25 | [Reports (التقارير الجديدة)](#25-reports-التقارير-الجديدة) | `/api/Reports` |
| — | [Enums المرجعية](#enums-المرجعية) | — |

---

## 🔐 إعدادات عامة

### Base URL
```
https://<host>/api
```

### Headers المطلوبة لكل Request
```http
Authorization: Bearer <JWT_TOKEN>
X-Tenant-Id: <TENANT_GUID>
Content-Type: application/json
```

### User Roles (الأدوار)
```
Owner=1, Coach=2, Client=3, Manager=4, Receptionist=5, Accountant=6, Trainer=7
```

### HTTP Status Codes
- `200 OK` — عملية ناجحة مع بيانات
- `204 No Content` — تحديث/حذف ناجح
- `400 Bad Request` — خطأ في البيانات (DomainException)
- `401 Unauthorized` — توكن غير صحيح
- `404 Not Found` — المورد غير موجود
- `409 Conflict` — تعارض (مثل SKU مكرر)

### صيغة الخطأ (Error Response)
```json
{
  "type": "...",
  "title": "Domain error",
  "status": 400,
  "detail": "Insufficient stock. Available: 5"
}
```

---

## 1. Branches (الفروع)

إدارة فروع الجيم. كل جيم يمكن أن يكون له أكثر من فرع.

### `GET /api/Branches` — قائمة الفروع
**Query:** `isActive`, `searchTerm`
**Response 200:**
```json
[{
  "id": "guid",
  "tenantId": "guid",
  "name": "الفرع الرئيسي",
  "code": "MAIN",
  "address": "القاهرة",
  "city": "Cairo",
  "phoneNumber": "+201234567890",
  "email": "main@gym.com",
  "latitude": 30.0444,
  "longitude": 31.2357,
  "isActive": true,
  "isDefault": true,
  "capacity": 200,
  "openTime": "06:00:00",
  "closeTime": "23:00:00",
  "managerId": "guid?",
  "managerName": "string?",
  "logoUrl": "url?",
  "coverImageUrl": "url?",
  "operatingHours": [
    { "id": "guid", "dayOfWeek": 1, "openTime": "06:00:00", "closeTime": "23:00:00", "isClosed": false }
  ],
  "activeClientsCount": 150,
  "todayCheckInsCount": 45
}]
```

### `GET /api/Branches/{id}` — فرع واحد
### `POST /api/Branches` — إنشاء فرع
**Body:** كامل حقول BranchDto (بدون id/tenantId).
**Response 200:** `"guid"` (id الفرع).

### `PUT /api/Branches/{id}` — تعديل فرع → `204`
### `DELETE /api/Branches/{id}` — حذف (ممنوع للفرع الافتراضي أو لو له اشتراكات) → `204`

### `PUT /api/Branches/{id}/operating-hours` — تحديد ساعات العمل اليومية
**Body:**
```json
{
  "hours": [
    { "dayOfWeek": 0, "openTime": "08:00:00", "closeTime": "22:00:00", "isClosed": false },
    { "dayOfWeek": 5, "openTime": "00:00:00", "closeTime": "00:00:00", "isClosed": true }
  ]
}
```
> **DayOfWeek:** 0=Sunday, 1=Monday, ... 6=Saturday

---

## 2. Membership Cards (بطاقات العضوية)

بطاقة دخول القاعة (QR) لكل عميل.

### `GET /api/MembershipCards`
**Query:** `clientId`, `isActive`
**Response 200:**
```json
[{
  "id": "guid",
  "clientId": "guid",
  "clientName": "user@email.com",
  "cardNumber": "GYM-20260419-A1B2C3",
  "qrCode": "abc123def456...",
  "isActive": true,
  "issuedAt": "2026-04-19T10:00:00Z",
  "expiresAt": "2027-04-19T10:00:00Z",
  "revokedAt": null,
  "revokedReason": null,
  "isExpired": false
}]
```

### `POST /api/MembershipCards/issue` — إصدار بطاقة جديدة
**Body:**
```json
{ "clientId": "guid", "expiresAt": "2027-04-19T00:00:00Z", "cardNumber": null }
```
> لو `cardNumber` فارغ، يتم توليده تلقائياً. البطاقات السابقة تُلغى تلقائياً.

### `POST /api/MembershipCards/{id}/revoke`
**Body:** `{ "reason": "Lost card" }` → `204`

---

## 3. Gate Access (البوابة الإلكترونية)

### `POST /api/GateAccess/check-in-qr` — تسجيل دخول بالـ QR
**Body:** `{ "qrCode": "abc123...", "branchId": "guid?" }`

**Response 200:**
```json
{
  "granted": true,
  "message": "Access granted",
  "attendanceId": "guid",
  "clientId": "guid",
  "clientName": "user@email.com",
  "branchId": "guid",
  "denyReason": 0
}
```

**حالات الرفض (granted=false):** `denyReason` قيم ممكنة: `NoActiveSubscription=1, SessionsPerWeekExceeded=2, BranchCapacityFull=3, OutsideOperatingHours=4, SubscriptionFrozen=5, SubscriptionExpired=6, AlreadyCheckedIn=7, CardInactive=8, CardExpired=9, BranchAccessDenied=10, ClientNotFound=11, BranchInactive=12`

### `GET /api/GateAccess/logs` — سجل دخول البوابة
**Query:** `clientId, branchId, result (1=Granted/2=Denied), fromDate, toDate, take (default 200)`

---

## 4. Rooms (القاعات)

### `GET /api/Rooms`
**Query:** `branchId, type, isActive`

### `POST /api/Rooms`
**Body:**
```json
{
  "branchId": "guid",
  "name": "قاعة الكارديو",
  "type": 1,
  "capacity": 30,
  "description": "string?",
  "imageUrl": "url?",
  "isActive": true
}
```
> **RoomType:** `Cardio=1, Weights=2, FreeWeights=3, Studio=4, Cycling=5, Crossfit=6, Pool=7, Boxing=8, Stretching=9, LockerRoom=10, Reception=11, Other=99`

### `PUT /api/Rooms/{id}`, `DELETE /api/Rooms/{id}` → `204`

---

## 5. Equipment (الأجهزة)

### `GET /api/Equipment`
**Query:** `branchId, roomId, status, searchTerm`

**Response:**
```json
[{
  "id": "guid",
  "branchId": "guid",
  "branchName": "string",
  "roomId": "guid?",
  "roomName": "string?",
  "name": "Treadmill Pro",
  "serialNumber": "SN12345",
  "brand": "TechnoGym",
  "model": "Run Excite 1000",
  "category": "Cardio",
  "purchaseDate": "2024-01-15",
  "purchasePrice": 15000,
  "status": 1,
  "statusName": "Active",
  "warrantyUntil": "2026-01-15",
  "imageUrl": "url?",
  "notes": "string?",
  "openMaintenanceCount": 0
}]
```
> **EquipmentStatus:** `Active=1, UnderMaintenance=2, OutOfService=3, Retired=4`

### `POST /api/Equipment`, `PUT /api/Equipment/{id}`
### `PUT /api/Equipment/{id}/status` — تغيير الحالة
**Body:** `{ "status": 2, "notes": "reason" }` → `204`

### `DELETE /api/Equipment/{id}` → `204`

---

## 6. Maintenance (الصيانة)

### `GET /api/Maintenance`
**Query:** `equipmentId, status, fromDate, toDate`

**Response:**
```json
[{
  "id": "guid",
  "equipmentId": "guid",
  "equipmentName": "string",
  "issueDate": "2026-04-19T00:00:00Z",
  "resolvedDate": null,
  "cost": 500,
  "description": "Motor replacement",
  "technicianName": "Ahmed",
  "technicianContact": "+201...",
  "status": 1,
  "statusName": "Pending",
  "resolutionNotes": null,
  "durationDays": null
}]
```
> **MaintenanceStatus:** `Pending=1, InProgress=2, Completed=3, Cancelled=4`

### `POST /api/Maintenance` — فتح بلاغ صيانة
**Body:**
```json
{
  "equipmentId": "guid",
  "issueDate": "2026-04-19T00:00:00Z",
  "description": "المشكلة",
  "technicianName": "string?",
  "technicianContact": "string?",
  "cost": 500,
  "putEquipmentUnderMaintenance": true
}
```
> لو `putEquipmentUnderMaintenance=true` → الجهاز ينتقل تلقائياً لـ UnderMaintenance.

### `POST /api/Maintenance/{id}/resolve` — إنهاء الصيانة
**Body:**
```json
{ "resolutionNotes": "done", "finalCost": 600, "reactivateEquipment": true }
```
> `reactivateEquipment=true` يُعيد الجهاز إلى Active تلقائياً.

---

## 7. Expense Categories

### `GET /api/ExpenseCategories` `?isActive=true`
### `POST /api/ExpenseCategories`
**Body:**
```json
{
  "name": "Rent",
  "description": "string?",
  "parentCategoryId": "guid?",
  "isActive": true
}
```
### `PUT /api/ExpenseCategories/{id}`, `DELETE /api/ExpenseCategories/{id}`

---

## 8. Expenses (المصروفات)

### `GET /api/Expenses`
**Query:** `branchId, categoryId, fromDate, toDate, searchTerm`

### `POST /api/Expenses`
**Body:**
```json
{
  "branchId": "guid?",
  "categoryId": "guid",
  "amount": 5000,
  "expenseDate": "2026-04-19T00:00:00Z",
  "description": "إيجار شهر إبريل",
  "vendorName": "مؤجر العقار",
  "paymentMethod": 1,
  "receiptImageUrl": "url?",
  "referenceNumber": "string?"
}
```
> **PaymentMethod:** `Cash=1, Card=2, Bank=3, Wallet=4`

### `PUT /api/Expenses/{id}`, `DELETE /api/Expenses/{id}`

---

## 9. Invoices (الفواتير)

### `GET /api/Invoices`
**Query:** `clientId, branchId, status, fromDate, toDate`

### `GET /api/Invoices/{id}` — مع Items + Payments

**Response Example:**
```json
{
  "id": "guid",
  "invoiceNumber": "INV-2026-000042",
  "clientId": "guid?",
  "clientName": "string?",
  "branchId": "guid?",
  "branchName": "string?",
  "issueDate": "2026-04-19T...",
  "dueDate": "2026-05-19T...",
  "subtotal": 1000,
  "taxAmount": 150,
  "discountAmount": 100,
  "total": 1050,
  "amountPaid": 500,
  "remainingAmount": 550,
  "status": 3,
  "statusName": "PartiallyPaid",
  "couponId": "guid?",
  "couponCode": "string?",
  "notes": "string?",
  "pdfUrl": "url?",
  "items": [{
    "id": "guid",
    "itemType": 1,
    "itemTypeName": "Subscription",
    "referenceId": "guid?",
    "description": "Monthly Gold Plan",
    "quantity": 1,
    "unitPrice": 1000,
    "taxRate": 15,
    "discountAmount": 100,
    "lineTotal": 1035
  }],
  "payments": [{
    "id": "guid",
    "amount": 500,
    "method": 1,
    "receivedAt": "...",
    "receiptNumber": "string"
  }]
}
```
> **InvoiceStatus:** `Draft=1, Issued=2, PartiallyPaid=3, Paid=4, Overdue=5, Cancelled=6`
> **InvoiceItemType:** `Subscription=1, Product=2, Class=3, PersonalTraining=4, Manual=5, Other=99`

### `POST /api/Invoices` — إنشاء فاتورة
**Body:**
```json
{
  "clientId": "guid?",
  "branchId": "guid?",
  "issueDate": null,
  "dueDate": "2026-05-19",
  "couponId": "guid?",
  "notes": "string?",
  "issueImmediately": true,
  "items": [
    {
      "itemType": 5,
      "referenceId": null,
      "description": "Personal training session",
      "quantity": 1,
      "unitPrice": 500,
      "taxRate": 15,
      "discountAmount": 0
    }
  ]
}
```
> الحساب تلقائي: `Subtotal, Tax, Discount, Total`. لو `issueImmediately=true` → Status=Issued. رقم الفاتورة يُولَّد تلقائياً (INV-YYYY-NNNNNN).

### `POST /api/Invoices/{id}/issue` — من Draft إلى Issued → `204`
### `POST /api/Invoices/{id}/cancel` — إلغاء
**Body:** `{ "reason": "..." }` → `204`

---

## 10. Payments (المدفوعات)

### `GET /api/Payments`
**Query:** `clientId, branchId, invoiceId, subscriptionId, method, fromDate, toDate`

### `POST /api/Payments` — تسجيل دفعة
**Body:**
```json
{
  "invoiceId": "guid?",
  "subscriptionId": "guid?",
  "clientId": "guid?",
  "branchId": "guid?",
  "amount": 500,
  "method": 1,
  "receivedAt": null,
  "receiptNumber": "string?",
  "notes": "string?",
  "referenceNumber": "string?"
}
```
> **تلقائياً:** يحدّث `Invoice.AmountPaid` + يغيّر Status إلى PartiallyPaid/Paid. ويحدّث `Subscription.AmountPaid`.

---

## 11. Coupons (الكوبونات)

### `GET /api/Coupons` `?isActive=true&search=SUMMER`

### `POST /api/Coupons`
**Body:**
```json
{
  "code": "SUMMER2026",
  "description": "Summer 25% off",
  "discountType": 1,
  "discountValue": 25,
  "minimumAmount": 500,
  "maxDiscountAmount": 200,
  "maxUses": 100,
  "maxUsesPerUser": 1,
  "startDate": "2026-06-01",
  "endDate": "2026-08-31",
  "applicableTo": 1,
  "isActive": true
}
```
> **DiscountType:** `Percentage=1, Fixed=2`
> **CouponApplicability:** `All=1, Subscriptions=2, Products=3, Classes=4, PersonalTraining=5`

### `PUT /api/Coupons/{id}`, `DELETE /api/Coupons/{id}` (ممنوع لو مستخدم)

### `GET /api/Coupons/validate` — التحقق من صلاحية الكود
**Query:** `code=SUMMER2026&amount=1000&context=2`

**Response:**
```json
{
  "isValid": true,
  "errorMessage": null,
  "estimatedDiscount": 200,
  "coupon": { ... }
}
```

---

## 12. Tax Settings (الضرائب)

### `GET /api/TaxSettings` `?isActive=true`
### `POST /api/TaxSettings`
```json
{ "name": "VAT", "rate": 15, "isDefault": true, "isActive": true, "description": "string?" }
```
### `PUT /api/TaxSettings/{id}`, `DELETE /api/TaxSettings/{id}`

---

## 13. Group Classes (الحصص الجماعية)

### `GET /api/GroupClasses`
**Query:** `branchId, isActive, category`

### `POST /api/GroupClasses`
```json
{
  "branchId": "guid?",
  "name": "Yoga Beginners",
  "description": "string?",
  "category": "Yoga",
  "durationMinutes": 60,
  "capacity": 20,
  "color": "#FF5733",
  "imageUrl": "url?",
  "price": 100,
  "isActive": true
}
```

### `PUT /api/GroupClasses/{id}`, `DELETE /api/GroupClasses/{id}`

---

## 14. Class Schedules & Enrollments

### `GET /api/ClassSchedules`
**Query:** `groupClassId, coachId, roomId, branchId, fromDate, toDate, includeCancelled`

**Response:**
```json
[{
  "id": "guid",
  "groupClassId": "guid",
  "groupClassName": "Yoga Beginners",
  "color": "#FF5733",
  "coachId": "guid?",
  "coachName": "coach@email.com",
  "roomId": "guid?",
  "roomName": "Studio A",
  "startTime": "2026-04-20T18:00:00Z",
  "endTime": "2026-04-20T19:00:00Z",
  "recurrencePattern": 2,
  "recurrenceDaysOfWeek": "Mon,Wed,Fri",
  "recurrenceEndDate": "2026-06-30",
  "overrideCapacity": null,
  "effectiveCapacity": 20,
  "bookedCount": 15,
  "waitlistCount": 3,
  "isFull": false,
  "isCancelled": false,
  "cancellationReason": null
}]
```
> **RecurrencePattern:** `None=0, Daily=1, Weekly=2, Monthly=3`

### `POST /api/ClassSchedules` — إنشاء جدولة
```json
{
  "groupClassId": "guid",
  "coachId": "guid?",
  "roomId": "guid?",
  "startTime": "2026-04-20T18:00:00Z",
  "endTime": "2026-04-20T19:00:00Z",
  "recurrencePattern": 2,
  "recurrenceDaysOfWeek": "Mon,Wed,Fri",
  "recurrenceEndDate": "2026-06-30",
  "overrideCapacity": null
}
```
> تلقائياً يتحقق من تعارض الـ Room والـ Coach في نفس الوقت.

### `POST /api/ClassSchedules/{id}/cancel`
**Body:** `{ "reason": "..." }` → كل المسجلين يُلغى حجزهم + إشعارات تلقائية.

### `GET /api/ClassSchedules/{id}/enrollments` `?includeCancelled=false`

### `POST /api/ClassSchedules/{id}/book` — حجز عميل
**Body:** `{ "clientId": "guid" }`
**Response:**
```json
{
  "id": "guid",
  "scheduleId": "guid",
  "clientId": "guid",
  "enrolledAt": "2026-04-19T...",
  "status": 1,
  "statusName": "Booked",
  "waitlistPosition": null
}
```
> لو الحصة ممتلئة → `status=5 (Waitlist)` + `waitlistPosition=1`.

### `POST /api/ClassSchedules/enrollments/{enrollmentId}/cancel`
**Body:** `{ "reason": "..." }`
> لو كان Booked → يُرقَّى أول Waitlist تلقائياً + إشعار له.

### `POST /api/ClassSchedules/enrollments/{enrollmentId}/attended` → `204`
> **ClassEnrollmentStatus:** `Booked=1, Attended=2, Cancelled=3, NoShow=4, Waitlist=5`

---

## 15. Product Categories

### `GET /api/ProductCategories` `?isActive=true`
### `POST /api/ProductCategories`
```json
{ "name": "Supplements", "description": "string?", "parentCategoryId": "guid?", "imageUrl": "url?", "isActive": true }
```
### `PUT /api/ProductCategories/{id}`, `DELETE /api/ProductCategories/{id}`

---

## 16. Products (المنتجات)

### `GET /api/Products`
**Query:** `categoryId, isActive, searchTerm, lowStockOnly, branchId`

**Response:**
```json
[{
  "id": "guid",
  "categoryId": "guid?",
  "categoryName": "Supplements",
  "name": "Whey Protein 2kg",
  "description": "string?",
  "sku": "WHY-2KG-001",
  "barcode": "6223001234567",
  "costPrice": 800,
  "sellingPrice": 1200,
  "taxRate": 15,
  "unit": "kg",
  "imageUrl": "url?",
  "isActive": true,
  "minStockLevel": 10,
  "trackStock": true,
  "totalStock": 45,
  "isLowStock": false
}]
```

### `POST /api/Products`
```json
{
  "categoryId": "guid?",
  "name": "Whey Protein 2kg",
  "description": "...",
  "sku": "WHY-2KG-001",
  "barcode": "6223001234567",
  "costPrice": 800,
  "sellingPrice": 1200,
  "taxRate": 15,
  "unit": "kg",
  "imageUrl": "url?",
  "isActive": true,
  "minStockLevel": 10,
  "trackStock": true
}
```
> **SKU و Barcode يجب أن يكونا فريدين.**

### `PUT /api/Products/{id}`, `DELETE /api/Products/{id}` (ممنوع لو له مبيعات)

---

## 17. Stock (المخزون)

### `GET /api/Stock`
**Query:** `branchId, productId, lowStockOnly`

**Response:**
```json
[{
  "id": "guid",
  "productId": "guid",
  "productName": "Whey Protein 2kg",
  "sku": "WHY-2KG-001",
  "branchId": "guid",
  "branchName": "الفرع الرئيسي",
  "quantity": 45,
  "minStockLevel": 10,
  "isLowStock": false,
  "lastMovementAt": "2026-04-18T..."
}]
```

### `GET /api/Stock/movements`
**Query:** `productId, branchId, type, fromDate, toDate`
> **StockMovementType:** `In=1, Out=2, Adjustment=3, Transfer=4`

### `POST /api/Stock/adjust` — تعديل يدوي
```json
{
  "productId": "guid",
  "branchId": "guid",
  "type": 1,
  "quantity": 20,
  "reason": "Received shipment"
}
```
> `type=1 (In)` يضيف، `type=2 (Out)` يطرح، `type=3 (Adjustment)` يضع القيمة كـ absolute.

### `POST /api/Stock/transfer` — نقل بين فرعين
```json
{
  "productId": "guid",
  "fromBranchId": "guid",
  "toBranchId": "guid",
  "quantity": 10,
  "reason": "Branch A low on stock"
}
```
> يُنشئ حركتين تلقائياً (Out من المصدر + In للهدف).

---

## 18. Suppliers (الموردين)

### `GET /api/Suppliers` `?isActive=true&searchTerm=...`
### `POST /api/Suppliers`
```json
{
  "name": "مورد المكملات",
  "contactPerson": "خالد",
  "phone": "+20...",
  "email": "...",
  "address": "...",
  "taxNumber": "...",
  "notes": "...",
  "isActive": true
}
```
### `PUT /api/Suppliers/{id}`, `DELETE /api/Suppliers/{id}` (ممنوع لو له PurchaseOrders)

---

## 19. Sales / POS (نقطة البيع)

### `GET /api/Sales`
**Query:** `branchId, clientId, cashierId, paymentMethod, fromDate, toDate`

### `POST /api/Sales/checkout` — **عملية البيع الكاملة** ⭐
```json
{
  "branchId": "guid",
  "clientId": "guid?",
  "paymentMethod": 1,
  "couponId": "guid?",
  "extraDiscount": 0,
  "notes": "string?",
  "items": [
    {
      "productId": "guid",
      "quantity": 2,
      "unitPriceOverride": null,
      "discountAmount": 0
    }
  ]
}
```

**Response 200:** `"guid"` (Sale Id)

**ما يحدث تلقائياً عند الـ checkout:**
1. ✅ التحقق من المخزون لكل منتج (لو `trackStock=true`)
2. ✅ حساب Subtotal + Tax + Discount + Coupon
3. ✅ إنشاء Sale + SaleItems + **رقم بيع متسلسل `SALE-2026-000001`**
4. ✅ خصم المخزون من الفرع + تسجيل StockMovements (Out)
5. ✅ إنشاء Invoice تلقائياً (Status=Paid) + InvoiceItems
6. ✅ إنشاء Payment
7. ✅ تسجيل CouponUsage

---

## 20. Employees (الموظفين)

### `GET /api/Employees`
**Query:** `branchId, department, isActive, searchTerm`

**Response:**
```json
[{
  "id": "guid",
  "userId": "guid",
  "email": "employee@gym.com",
  "phoneNumber": "+20...",
  "role": 4,
  "employeeCode": "EMP001",
  "jobTitle": "Receptionist",
  "department": "Front Office",
  "joinDate": "2024-01-15",
  "terminationDate": null,
  "baseSalary": 5000,
  "salaryType": 1,
  "hourlyRate": null,
  "bankAccount": "...",
  "bankName": "...",
  "nationalId": "...",
  "emergencyContactName": "...",
  "emergencyContactPhone": "...",
  "qualifications": "...",
  "branchIds": ["guid1", "guid2"],
  "isActive": true
}]
```
> **SalaryType:** `Monthly=1, Hourly=2, Daily=3`

### `POST /api/Employees`
```json
{
  "userId": "guid",
  "employeeCode": "EMP001",
  "jobTitle": "Receptionist",
  "department": "Front Office",
  "joinDate": "2024-01-15",
  "baseSalary": 5000,
  "salaryType": 1,
  "hourlyRate": null,
  "bankAccount": "...",
  "bankName": "...",
  "nationalId": "...",
  "emergencyContactName": "...",
  "emergencyContactPhone": "...",
  "qualifications": "...",
  "branchIds": ["guid"]
}
```

### `PUT /api/Employees/{id}`
### `POST /api/Employees/{id}/terminate`
**Body:** `{ "terminationDate": null, "reason": "..." }`

---

## 21. Shifts (الورديات)

### `GET /api/Shifts` `?branchId=&isActive=`
### `POST /api/Shifts`
```json
{
  "branchId": "guid?",
  "name": "Morning Shift",
  "startTime": "06:00:00",
  "endTime": "14:00:00",
  "color": "#4CAF50",
  "isActive": true
}
```

### `POST /api/Shifts/assign` — تعيين موظف لوردية يوم محدد
```json
{
  "shiftId": "guid",
  "employeeId": "guid",
  "date": "2026-04-20",
  "notes": "string?"
}
```

### `GET /api/Shifts/assignments`
**Query:** `employeeId, shiftId, fromDate, toDate`

---

## 22. Leaves (الإجازات)

### `GET /api/Leaves`
**Query:** `employeeId, status, leaveType, fromDate, toDate`

**Response:**
```json
[{
  "id": "guid",
  "employeeId": "guid",
  "employeeName": "string",
  "fromDate": "2026-05-01",
  "toDate": "2026-05-07",
  "durationDays": 7,
  "leaveType": 1,
  "leaveTypeName": "Annual",
  "reason": "...",
  "status": 1,
  "statusName": "Pending",
  "reviewedById": null,
  "reviewedByName": null,
  "reviewedAt": null,
  "reviewNotes": null
}]
```
> **LeaveType:** `Annual=1, Sick=2, Unpaid=3, Maternity=4, Emergency=5, Other=99`
> **LeaveStatus:** `Pending=1, Approved=2, Rejected=3, Cancelled=4`

### `POST /api/Leaves`
```json
{ "employeeId": "guid", "fromDate": "2026-05-01", "toDate": "2026-05-07", "leaveType": 1, "reason": "Vacation" }
```

### `POST /api/Leaves/{id}/review` — موافقة/رفض
```json
{ "decision": 2, "notes": "Approved" }
```
> `decision` يقبل فقط `Approved=2` أو `Rejected=3`.

---

## 23. Commissions (العمولات)

### `GET /api/Commissions`
**Query:** `employeeId, status, sourceType, fromDate, toDate`

**Response:**
```json
[{
  "id": "guid",
  "employeeId": "guid",
  "employeeName": "coach@email.com",
  "sourceType": 1,
  "sourceTypeName": "SubscriptionSale",
  "referenceId": "guid?",
  "amount": 150,
  "sourceAmount": 1000,
  "earnedDate": "2026-04-15T...",
  "status": 1,
  "statusName": "Pending",
  "payrollItemId": null,
  "description": "..."
}]
```
> **CommissionSourceType:** `SubscriptionSale=1, ProductSale=2, PersonalTraining=3, Manual=99`
> **CommissionStatus:** `Pending=1, Approved=2, Paid=3, Cancelled=4`

### `GET /api/Commissions/rules`
### `POST /api/Commissions/rules` — قاعدة عمولة
```json
{
  "employeeId": "guid?",
  "role": 2,
  "sourceType": 1,
  "type": 1,
  "value": 10,
  "minAmount": null,
  "isActive": true
}
```
> **CommissionRuleType:** `Percentage=1, Fixed=2`
> إما `employeeId` (لموظف معين) أو `role` (لكل الأدوار).

---

## 24. Payroll (الرواتب)

### `GET /api/Payroll`
**Query:** `year, month, branchId, status`

**Response:**
```json
[{
  "id": "guid",
  "branchId": "guid?",
  "branchName": "string?",
  "month": 4,
  "year": 2026,
  "status": 1,
  "statusName": "Draft",
  "totalAmount": 50000,
  "approvedAt": null,
  "paidAt": null,
  "notes": "string?",
  "itemsCount": 10,
  "items": [{
    "id": "guid",
    "employeeId": "guid",
    "employeeName": "...",
    "baseSalary": 5000,
    "commissionTotal": 500,
    "bonus": 200,
    "deductions": 100,
    "netSalary": 5600,
    "paidAt": null,
    "notes": null
  }]
}]
```
> **PayrollStatus:** `Draft=1, Approved=2, Paid=3, Cancelled=4`

### `POST /api/Payroll/generate` — ⭐ توليد كشف رواتب شهري
```json
{ "month": 4, "year": 2026, "branchId": "guid?" }
```
> تلقائياً: يُنشئ PayrollRun + PayrollItems لكل موظف نشط + يجمع العمولات المعلقة + يربطها بالـ Items.

### `PUT /api/Payroll/items/{id}` — تعديل Bonus/Deductions (فقط للـ Draft)
```json
{ "bonus": 500, "deductions": 100, "notes": "string?" }
```
> يُعيد حساب NetSalary و TotalAmount للـ PayrollRun.

### `POST /api/Payroll/{id}/approve` → Draft → Approved
### `POST /api/Payroll/{id}/pay` → Approved → Paid + العمولات تتحول إلى Paid + كل PayrollItem.PaidAt يتحدّث.

---

## 25. Reports (التقارير الجديدة)

جميع التقارير بحاجة `fromDate` و `toDate` (إفتراضي الشهر الحالي لو لم يُحدد).

### `GET /api/Reports/operations-dashboard` — ⭐ Dashboard رئيسي شامل
**Response:**
```json
{
  "activeMembers": 1500,
  "todayCheckIns": 200,
  "currentlyInsideCount": 45,
  "expiringSubscriptionsIn7Days": 30,
  "expiredSubscriptions": 5,
  "monthRevenue": 150000,
  "monthExpenses": 50000,
  "monthNetProfit": 100000,
  "todayRevenue": 5000,
  "todayExpenses": 200,
  "lowStockProductsCount": 8,
  "equipmentUnderMaintenanceCount": 2,
  "pendingLeaveRequestsCount": 3,
  "unpaidInvoicesCount": 12,
  "unpaidInvoicesTotal": 25000,
  "branchKpis": [{
    "branchId": "guid",
    "branchName": "الرئيسي",
    "capacity": 200,
    "currentlyInside": 45,
    "todayCheckIns": 150,
    "activeMembers": 1000,
    "capacityUsagePercent": 22.5
  }]
}
```

### `GET /api/Reports/expenses`
**Query:** `fromDate, toDate, branchId`
**Response:** `{ totalExpenses, expensesCount, byCategory[], byBranch[], byMonth[] }`

### `GET /api/Reports/pos-sales`
**Query:** `fromDate, toDate, branchId, topProductsCount=10`
**Response:** `{ totalRevenue, salesCount, itemsSold, topProducts[], byCashier[], byBranch[], byPaymentMethod[] }`

### `GET /api/Reports/stock-valuation` `?branchId=`
**Response:** `{ totalCostValue, totalRetailValue, potentialProfit, productsCount, lowStockCount, products[] }`

### `GET /api/Reports/payroll-summary` `?year=2026&month=4`
**Response:** `{ totalBaseSalaries, totalCommissions, totalBonuses, totalDeductions, totalNetSalaries, employeesPaid, pendingCommissionsCount, pendingCommissionsAmount, byBranch[] }`

### `GET /api/Reports/class-attendance`
**Query:** `fromDate, toDate, branchId`
**Response:** `{ totalSchedulesHeld, totalBookings, totalAttended, attendanceRatePercent, averageFillRatePercent, topClasses[], coachStats[] }`

### `GET /api/Reports/equipment-utilization` `?branchId=`
**Response:** `{ totalEquipment, activeCount, underMaintenanceCount, totalMaintenanceCost, mostCostlyEquipment[], byBranch[] }`

### `GET /api/Reports/branch-comparison`
**Query:** `fromDate, toDate`
**Response:** مصفوفة الفروع مع KPIs كاملة (revenue + expenses + net profit + check-ins + classes + employees).

### `GET /api/Reports/commissions`
**Query:** `fromDate, toDate, employeeId`
**Response:** `{ totalEarned, totalPaid, totalPending, commissionsCount, byEmployee[], bySource[] }`

---

## Enums المرجعية

### UserRole
```json
{ "Owner": 1, "Coach": 2, "Client": 3, "Manager": 4, "Receptionist": 5, "Accountant": 6, "Trainer": 7 }
```

### PaymentMethod
```json
{ "Cash": 1, "Card": 2, "Bank": 3, "Wallet": 4 }
```

### SubscriptionStatus
```json
{ "Pending": 1, "Active": 2, "Expired": 3, "Frozen": 4, "Cancelled": 5 }
```

### InvoiceStatus
```json
{ "Draft": 1, "Issued": 2, "PartiallyPaid": 3, "Paid": 4, "Overdue": 5, "Cancelled": 6 }
```

### InvoiceItemType
```json
{ "Subscription": 1, "Product": 2, "Class": 3, "PersonalTraining": 4, "Manual": 5, "Other": 99 }
```

### DiscountType / CouponApplicability
```json
{ "Percentage": 1, "Fixed": 2 }
{ "All": 1, "Subscriptions": 2, "Products": 3, "Classes": 4, "PersonalTraining": 5 }
```

### RoomType
```json
{ "Cardio": 1, "Weights": 2, "FreeWeights": 3, "Studio": 4, "Cycling": 5, "Crossfit": 6, "Pool": 7, "Boxing": 8, "Stretching": 9, "LockerRoom": 10, "Reception": 11, "Other": 99 }
```

### EquipmentStatus / MaintenanceStatus
```json
{ "Active": 1, "UnderMaintenance": 2, "OutOfService": 3, "Retired": 4 }
{ "Pending": 1, "InProgress": 2, "Completed": 3, "Cancelled": 4 }
```

### StockMovementType / PurchaseOrderStatus
```json
{ "In": 1, "Out": 2, "Adjustment": 3, "Transfer": 4 }
{ "Draft": 1, "Submitted": 2, "Received": 3, "PartiallyReceived": 4, "Cancelled": 5 }
```

### ClassEnrollmentStatus / RecurrencePattern
```json
{ "Booked": 1, "Attended": 2, "Cancelled": 3, "NoShow": 4, "Waitlist": 5 }
{ "None": 0, "Daily": 1, "Weekly": 2, "Monthly": 3 }
```

### SalaryType / LeaveType / LeaveStatus
```json
{ "Monthly": 1, "Hourly": 2, "Daily": 3 }
{ "Annual": 1, "Sick": 2, "Unpaid": 3, "Maternity": 4, "Emergency": 5, "Other": 99 }
{ "Pending": 1, "Approved": 2, "Rejected": 3, "Cancelled": 4 }
```

### Commission
```json
{ "SourceType": { "SubscriptionSale": 1, "ProductSale": 2, "PersonalTraining": 3, "Manual": 99 } }
{ "Status": { "Pending": 1, "Approved": 2, "Paid": 3, "Cancelled": 4 } }
{ "RuleType": { "Percentage": 1, "Fixed": 2 } }
```

### PayrollStatus
```json
{ "Draft": 1, "Approved": 2, "Paid": 3, "Cancelled": 4 }
```

### GateAccess
```json
{ "Result": { "Granted": 1, "Denied": 2 } }
{ "Method": { "Manual": 1, "Qr": 2, "Card": 3, "Face": 4, "Fingerprint": 5 } }
{ "DenyReason": { "None": 0, "NoActiveSubscription": 1, "SessionsPerWeekExceeded": 2, "BranchCapacityFull": 3, "OutsideOperatingHours": 4, "SubscriptionFrozen": 5, "SubscriptionExpired": 6, "AlreadyCheckedIn": 7, "CardInactive": 8, "CardExpired": 9, "BranchAccessDenied": 10, "ClientNotFound": 11, "BranchInactive": 12 } }
```

---

## ملاحظات تنفيذية للفرونت

### 1. Multi-Branch Context
- كل الـ entities التشغيلية الجديدة تحتوي `BranchId` (Optional).
- لو لم يُحدَّد BranchId في CheckIn/Sale/etc. → يستخدم الفرع الافتراضي تلقائياً.
- يُنصح بإضافة `BranchSelector` في الـ UI للاختيار السريع.

### 2. القوائم الأساسية قبل بدء العمل
عند تثبيت الجيم الأول مرة، أنشئ بهذا الترتيب:
1. `POST /api/Branches` (فرع افتراضي — `isDefault: true`)
2. `POST /api/TaxSettings` (ضريبة افتراضية مثل VAT 15%)
3. `POST /api/ExpenseCategories` (إيجار، مرافق، رواتب، تسويق، صيانة)
4. `POST /api/ProductCategories` (مكملات، ملابس، إلخ)
5. `POST /api/Rooms` لكل فرع
6. `POST /api/Shifts` (صباحي، مسائي)

### 3. معالجة الأخطاء
```ts
try {
  await axios.post('/api/Sales/checkout', payload);
} catch (err) {
  const detail = err.response?.data?.detail; // "Insufficient stock. Available: 5"
  showToast(detail || 'خطأ غير متوقع');
}
```

### 4. Polling / Real-Time
- الـ Dashboard الرئيسي (`operations-dashboard`) مناسب للـ polling كل 30-60 ثانية.
- `GET /api/Stock?lowStockOnly=true` مناسب للتنبيهات.
- `GET /api/Payments?fromDate=today` للـ cashier display.

### 5. صلاحيات Role-Based (اقتراح للـ UI)
| Role | Features متاحة |
|------|----------------|
| Owner | كل شيء |
| Manager | Branches (فرعه), Employees, Reports, Payroll approval |
| Accountant | Invoices, Payments, Expenses, Reports |
| Receptionist | CheckIn, Sales, Clients, MembershipCards |
| Coach/Trainer | GroupClasses, Appointments, Commissions (الخاصة به) |
| Client | Profile, Subscriptions, Class Booking |

---

## Migration Files في الـ Backend

| Migration | Phase |
|-----------|-------|
| `AddBranchesPhase1` | الفروع + BranchId على الـ entities التشغيلية |
| `AddAccessControlPhase2` | MembershipCard + GateAccessLog |
| `AddEquipmentRoomsPhase3` | Rooms + Equipment + Maintenance |
| `AddFinancePhase5` | Invoices + Payments + Expenses + Coupons + Tax |
| `AddGroupClassesPhase4` | GroupClass + Schedule + Enrollment |
| `AddPosShopPhase6` | Products + Stock + Sales + Suppliers |
| `AddHrPayrollPhase7` | Employees + Shifts + Leaves + Commissions + Payroll |

بعد سحب آخر تحديث، شغّل:
```bash
dotnet ef database update --project LogicFit.Infrastructure --startup-project LogicFit.API
```

---

## إحصائيات التوسعة

- ✅ **50+ Entity جديد**
- ✅ **25+ Controller جديد**
- ✅ **~150+ Endpoint جديد**
- ✅ **7 Migrations**
- ✅ **~40 Enum جديد**
- ✅ **9 تقارير جديدة + Operations Dashboard**
- ✅ **Build: 0 Errors**

---

للاستفسار عن أي endpoint غير موثّق هنا، راجع Swagger UI على `/swagger` بعد تشغيل الـ API محلياً.
