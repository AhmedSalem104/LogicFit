# Gym App ↔ Platform Integration Review

## إصلاحات فعلية

1. كانت روابط إثبات الدفع تُعاد كنسبية (`/uploads/...`) بينما لوحة الإدارة المنشورة لا تحتوي rewrite لمسار `/uploads`. تمت إضافة rewrite في `LogiFit_Platform_Admin_Dashboard/vercel.json`، وتوحيد تطبيع الرابط في معاينة لوحة الإدارة.
2. معاينة الإثبات أصبحت تعرض حالة خطأ واضحة، وتفتح الرابط بالحجم الكامل، وتتعامل مع الملف المفقود.
3. تم التحقق من أن رفع الإثبات يستخدم `multipart/form-data` واسم الحقل `proof`، وأن `FileUploadService` يفرض JPG/PNG/GIF/WebP وحجم 5MB.
4. أصول الهوية البصرية أصبحت Tenant-scoped بجدول مستقل، مع حد خمس صور Gallery وصلاحية `ManageSettings`.

## مصفوفة تحقق مختصرة

| Screen | Required Permission | Package Feature | Roles | Frontend | Backend | Result |
|---|---|---|---|---|---|---|
| Gym billing/payment requests | ManageTenantBilling | TenantBilling/plan access | Owner | owner route + permission | policy + tenant context | Pass |
| Platform payment requests | ManagePaymentRequests | Platform billing | Platform owner/admin | permission-filtered route | platform policy | Pass |
| Gym settings/branding | ManageSettings | WhiteLabel | Owner | owner route + permission | policy + subscription guard | Pass |
| Client subscriptions | authenticated client | subscription access | Client | client route | TenantAccess + subscription rules | Pass |

## تدفق إثبات الدفع

`TenantBillingController` يستقبل `proof` كـ`IFormFile`، يرفعه قبل إنشاء `PaymentRequest`، ويحفظ `ProofFileUrl`. الاستعلامات في Gym App تقيد النتائج بـ`ITenantService`; استعلامات Platform تعرض الطلبات المصرح بها. بعد الموافقة ينفذ الـhandler تفعيل الاشتراك والمعاملة الذرية، وبعد الرفض يحفظ السبب ويرسل الإشعار.

## الاختبارات

- `dotnet build --no-restore`: ناجح.
- `dotnet test --no-build --no-restore`: 64 ناجحة.
- Gym App `npm run build`: ناجح.
- Platform Dashboard `npm run build`: ناجح.

## نقاط إعداد خارجية

- يجب نشر rewrite `/uploads` في Vercel للوحة الإدارة (تم تحديثه في الكود).
- يجب أن يكون مجلد `wwwroot/uploads` قابلاً للقراءة من خدمة الباك اند، مع عدم مشاركة روابط رفع غير مقصودة خارج الصلاحيات.
