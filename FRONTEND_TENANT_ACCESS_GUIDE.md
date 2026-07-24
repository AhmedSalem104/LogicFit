<div align="center">

# LogicFit — دليل الفرونت: بوابات حالة الجيم/الاشتراك
### Tenant Access Gates + Typed Error Codes

</div>

> تغيير في **الـ Tenant API** (`https://logicfit.runasp.net`): كل الطلبات المحمية أصبحت تفحص **حالة الجيم والاشتراك**. الأخطاء دلوقتى بترجع **كود مُصنّف (`code`)** بدل 403 عام. هذا الدليل يشرح للفرونت إزاي يتعامل معاها.

---

## 1) ما الذي تغيّر ولماذا
قبل كده: أي مستخدم تابع لجيم **موقوف/منتهي الاشتراك** كان يقدر يسجّل دخول ويستخدم كل النظام. دلوقتى فيه **بوابتان**:
- **عند تسجيل الدخول**: لا يُصدَر توكن لجيم محظور (موقوف/منتهٍ/ملغى/مؤرشف).
- **مع كل طلب محمي**: حتى لو معاك توكن صدر **قبل** الإيقاف، الطلب هيتحجب (خلال ≤30 ثانية من الإيقاف).

**النتيجة على الفرونت**: لازم تتعامل مع أكواد الأخطاء دي **في مكانين**: (أ) شاشة الدخول، (ب) **interceptor عام** لكل الطلبات.

---

## 2) شكل الخطأ الجديد (Error Contract)
كل ردود الأخطاء بقت بالشكل ده (اتضاف حقل **`code`**):
```json
{
  "statusCode": 403,
  "code": "TENANT_SUSPENDED",
  "message": "...",
  "errors": null
}
```
- **`code`**: هو **العقد** — اعتمد عليه في المنطق (switch).
- **`message`**: نص احتياطي للعرض (قد يكون تقني — الأفضل تعرض رسالتك حسب الـ code).
- **`code = null`** لباقي الأخطاء العادية (validation/notfound...) — مفيش code إلا للحالات دي.

---

## 3) جدول أكواد حالة الجيم (Tenant Access Codes)

| `code` | HTTP | متى يحدث | ماذا يعرض/يفعل الفرونت |
|--------|:----:|---------|------------------------|
| `TENANT_SUSPENDED` | 403 | الجيم موقوف يدوياً من الإدارة | شاشة «تم إيقاف الجيم — تواصل مع الدعم». **Logout**. |
| `TENANT_SUSPENDED_NONPAYMENT` | 402 | موقوف بسبب عدم السداد | شاشة «الاشتراك متوقف لعدم السداد» + زر تجديد/دفع. **Logout** أو توجيه للدفع. |
| `TENANT_SUBSCRIPTION_EXPIRED` | 402 | انتهت مدة الاشتراك | شاشة «انتهى الاشتراك» + تجديد. |
| `TENANT_SUBSCRIPTION_CANCELLED` | 402 | اشتراك/جيم ملغى | شاشة «الاشتراك ملغى» + تواصل/تجديد. |
| `TENANT_SUBSCRIPTION_SUSPENDED` | 402 | اشتراك موقوف | مثل EXPIRED. |
| `TENANT_ARCHIVED` | 403 | الجيم مؤرشف | «الجيم غير متاح». **Logout**. |
| `TENANT_NOT_FOUND` | 404 | الجيم غير موجود/محذوف | «كود الجيم غير صحيح» (عند الدخول). |
| `TENANT_PENDING_APPROVAL` | 403 | الجيم مُسجّل لكن **بانتظار الموافقة** | **حالة خاصة** — راجع القسم 5. |

> **قاعدة عامة**: أي كود يبدأ بـ `TENANT_` (ما عدا `PENDING_APPROVAL`) = الجيم **محظور بالكامل** → اعرض شاشة الحالة واعمل logout (أو وجّه للدفع لو 402).

---

## 4) أين تتعامل معها؟

### أ) عند تسجيل الدخول (`POST /api/auth/login`)
لو الجيم محظور، الرد هيبقى خطأ بكود مُصنّف بدل التوكن:
```
POST /api/auth/login { subdomain, phoneNumber, password }
 → 402/403/404 { code: "TENANT_SUBSCRIPTION_EXPIRED", ... }
```
اعرض رسالة الحالة المناسبة حسب الـ `code` بدل «بيانات دخول خاطئة».

### ب) مع كل طلب محمي — **Interceptor عام (إلزامي)**
لأن التوكن ممكن يكون صدر قبل الإيقاف، **أي** طلب محمي ممكن يرجّع كود حالة الجيم فجأة. اعمل interceptor:
```js
// axios response interceptor
api.interceptors.response.use(r => r, (error) => {
  const code = error.response?.data?.code;
  if (code && code.startsWith("TENANT_")) {
    if (code === "TENANT_PENDING_APPROVAL") {
      router.push("/onboarding/billing");            // القسم 5
    } else {
      store.setTenantBlock(code);                    // شاشة الحالة + الرسالة المناسبة
      auth.logout();                                 // امسح التوكن
      router.push("/gym-unavailable?reason=" + code);
    }
    return Promise.reject(error);
  }
  // ... باقي المعالجة (401 refresh, 400 validation, ...)
  return Promise.reject(error);
});
```

> ⏱️ **ملاحظة الكاش**: حالة الجيم مُخزّنة مؤقتاً ~30 ثانية على السيرفر، فالإيقاف يسري خلال ≤30ث. مايحتاجش تعامل خاص من الفرونت — بس اعرف إن المستخدم قد يكمّل ثوانٍ قليلة بعد الإيقاف.

---

## 5) حالة خاصة: `TENANT_PENDING_APPROVAL` (الجيم الجديد)
الجيم اللي لسه **بانتظار الموافقة** (PendingApproval):
- **يقدر يسجّل دخول** (التوكن يُصدَر عادي).
- لكن **كل الطلبات محصورة في الفوترة/الأونبوردنج فقط**. أي endpoint آخر يرجّع `403 TENANT_PENDING_APPROVAL`.

**الـ endpoints المسموحة أثناء PendingApproval** (كلها تحت `/api/tenant`):
```
GET  /api/tenant/plans                 (الباقات المتاحة)
GET  /api/tenant/my-subscription       (اشتراك الجيم)
GET  /api/tenant/usage
GET  /api/tenant/invoices
POST /api/tenant/subscription/select-plan
POST /api/tenant/subscription/upgrade
POST /api/tenant/subscription/renew
GET  /api/tenant/payment-methods       (طرق الدفع)
POST /api/tenant/payment-requests      (رفع إثبات الدفع)
GET  /api/tenant/payment-requests      (طلبات الدفع)
```

**سلوك الفرونت المطلوب**:
1. بعد الدخول، اقرأ حالة الجيم. **إزاي تعرف إن الجيم PendingApproval؟** أبسط طريقة: بعد الدخول نادِ `GET /api/tenant/my-subscription` (مسموح) — أو اعتمد على أول `403 TENANT_PENDING_APPROVAL` من الـ interceptor.
2. لو PendingApproval → **وجّه المستخدم لشاشة onboarding/billing** (اختيار باقة → رفع إثبات دفع) وأخفِ باقي شاشات النظام.
3. بعد موافقة الإدارة (الجيم يصبح Active)، كل الـ endpoints تشتغل تلقائياً — مافيش تغيير مطلوب من الفرونت غير إعادة المحاولة.

> 💡 **توصية**: بعد أي دخول ناجح، استدعِ `GET /api/tenant/my-subscription`؛ لو الحالة تدل على PendingApproval، فعّل «وضع الأونبوردنج». هذا أنظف من انتظار أول 403.

---

## 6) منطق مقترح (Pseudo)
```js
function handleTenantCode(code) {
  switch (code) {
    case "TENANT_PENDING_APPROVAL":
      return goToOnboarding();                 // billing-only mode
    case "TENANT_SUSPENDED_NONPAYMENT":
    case "TENANT_SUBSCRIPTION_EXPIRED":
    case "TENANT_SUBSCRIPTION_CANCELLED":
    case "TENANT_SUBSCRIPTION_SUSPENDED":
      return showBillingBlock(code);           // 402 → شاشة تجديد/دفع
    case "TENANT_SUSPENDED":
    case "TENANT_ARCHIVED":
      return showBlockedAndLogout(code);        // 403 → شاشة إيقاف + دعم
    case "TENANT_NOT_FOUND":
      return showLoginError("كود الجيم غير صحيح");
  }
}
```

---

## 7) ملخّص التغييرات المطلوبة من الفرونت
- [ ] اقرأ حقل **`code`** في كل ردود الأخطاء (`{ statusCode, code, message, errors }`).
- [ ] **شاشة الدخول**: اعرض رسالة الحالة حسب `code` (بدل خطأ عام).
- [ ] **Interceptor عام**: أي `code` يبدأ بـ `TENANT_` → عالجه (logout/شاشة حالة/توجيه دفع).
- [ ] **وضع الأونبوردنج** لـ PendingApproval: احصر الواجهة في شاشات الفوترة (endpoints القسم 5).
- [ ] فرّق بين **402** (مشكلة دفع → زر تجديد/دفع) و**403** (موقوف/مؤرشف → دعم/logout).

---

<div align="center"><sub>LogicFit — Tenant Access Gates · دليل فرونت</sub></div>
