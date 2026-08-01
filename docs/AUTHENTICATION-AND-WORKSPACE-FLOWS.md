# المصادقة وتدفقات مساحات العمل

> حالة المرجع: تم إصدار Issue #118 إلى فروع الإنتاج بتاريخ 2026-08-01، وما زال النشر والتحقق الفعلي يتطلبان تطبيق الـMigration وإعداد أسرار الخادم عبر مسار النشر المحمي. يضيف Issue #127 مزود اختبار مستضاف مؤقتًا ومحدد الصلاحية حتى يتوفر مزود الإرسال الخارجي.

LogicFit ينتقل تدريجيًا من حساب محلي داخل جيم إلى **هوية عالمية أولًا ثم اختيار مساحة العمل**. لذلك يوجد تدفقان مدعومان حاليًا: التدفق التقليدي المتوافق، وتدفق الهوية الجديد. لا يجوز حذف الأول قبل نقل كل الواجهات والبيانات إليه.

> **Released – Issue #118; deployment verification pending:** Email + Password remains supported, and Phone + OTP is added as a complete identity sign-in and recovery path. Passkey/WebAuthn is removed from runtime code, APIs, permissions, and both frontends.

## الكيانات وحدود الأمان

| الكيان | الغرض | لا يعني |
|---|---|---|
| `IdentityAccount` | هوية عالمية ببريد فريد مطبّع وكلمة مرور؛ قد ترتبط بأكثر من مساحة. يمكن ربط هاتف E.164 فريد ومؤكد واستخدامه مع OTP. | منح صلاحية أو Tenant context بمفردها |
| `User` / `DomainUser` | حساب محلي داخل `TenantId`، profile، دور وحالة كلمة مرور | أن المستخدم يستطيع الدخول إلى مساحة أخرى لها الاسم نفسه |
| `WorkspaceMembership` | رابط الهوية بالحساب المحلي والمساحة، والدور وحالة الاعتماد | بديلًا عن RBAC أو اشتراك المساحة |
| `ApplicationRequest` | طلب إنشاء مساحة أو انضمام عضو، بحالات ومراجعة و`RowVersion` | جلسة مستخدم عادية |
| `ApplicationRequestRevision` | لقطة تدقيق لكل إعادة تقديم/تعديل مطلوب | تعديل البيانات بلا أثر تدقيقي |
| `IdentityWorkspaceSession` | token اختيار مساحة قصير العمر (10 دقائق) محفوظ كـhash | JWT أو refresh token |
| `ApplicationTrackingSession` | token متابعة طلب قصير العمر، بلا refresh token | دخول إلى بيانات المساحة أو بيانات العملاء |
| `OtpChallenge` | تحدٍ مركزي لغرض واحد وهاتف E.164، يخزن HMAC+salt وحالة التسليم والمحاولات والصلاحية و`RowVersion` | جلسة مستخدم أو دليلًا على نجاح التحقق |
| `OtpStepUpSession` | إثبات قصير (5 دقائق) مشتق من OTP ناجح ومربوط بالهوية والجلسة والغرض | صلاحية دائمة أو بديلًا عن RBAC |

العزل بـ`TenantId` وملكية المورد وعضوية المساحة حدود أمنية. لا تعتمد واجهة المستخدم أو إخفاء زر كبديل عن فحص API.

## تدفق الدخول المتوافق مع الجيم (Legacy)

هذا ما يزال مدعومًا لحماية جميع حسابات الجيم الحالية.

```text
واجهة الجيم
  -> POST /api/auth/login { phoneNumber, password, subdomain | tenantId }
  -> حل المساحة: subdomain أولًا أو tenantId صريح
  -> فحص حالة المساحة
  -> فحص User المحلي وكلمة المرور والحالة
  -> جسر التوافق: إنشاء/ربط IdentityAccount وWorkspaceMembership عند الإمكان
  -> تحميل الأدوار والصلاحيات
  -> إصدار tenant JWT ووضع refresh token داخل Cookie آمنة HttpOnly
  -> توجيه الواجهة حسب الدور والصلاحيات
```

النقاط المهمة:

- يدخل الحساب في مساحة واحدة محددة؛ لا تظهر له قائمة مساحات في هذا endpoint.
- الجسر لا يكسر دخولًا قديمًا إذا وُجدت هوية عالمية متعارضة بكلمة مرور مختلفة؛ يبقى الحساب محليًا إلى أن يحل دمج الهوية بتدفق مستقل.
- قبل إصدار token يفحص حارس المساحة الحظر الكامل. في `PendingApproval` يمكن الدخول فقط إلى الأسطح التي يسمح بها حارس الوصول (فوترة/تهيئة)، وليس التشغيل العادي.

## تدفق الهوية أولًا ثم اختيار مساحة (المستهدف)

```text
شاشة الدخول الموحدة (أحد المسارين)
  -> POST /api/identity/login { email, password }
  أو
  -> POST /api/identity/phone-login/request { phoneNumber(E.164), sessionBinding }
  -> POST /api/identity/phone-login/verify { challengeId, code, sessionBinding }
  <- WorkspaceSelectionToken (10 دقائق)
     + activeWorkspaces[]
     + pendingApplications[]
     + requiresWorkspaceSelection

إذا كانت هناك عضوية نشطة:
  المستخدم يختار مساحة (أو تكمل الواجهة تلقائيًا عند عضوية واحدة)
  -> POST /api/identity/select-workspace { workspaceSelectionToken, workspaceId }
  -> فحص Membership.Active + User.Active + حالة المساحة
  <- tenant JWT + roles + permissions + TenantId
  + Set-Cookie للـrefresh token؛ لا يظهر في JSON ولا يستطيع JavaScript قراءته

إذا كانت هناك طلبات معلقة:
  يمكن استعراض حالتها بالتوازي مع الدخول لمساحة نشطة.
```

### Email verification and password recovery (Issue #113, unreleased)

```text
POST /api/identity/register { fullName, email, password, phoneNumber? }
  -> creates an unverified IdentityAccount and emails a 30-minute one-time link
POST /api/identity/verify-email { token }
  -> atomically consumes the hashed token and enables identity-first sign-in

POST /api/identity/password-reset { email }
  -> accepted response without revealing account existence; sends a one-time link when eligible
POST /api/identity/password-reset/confirm { token, newPassword }
  -> atomically consumes the link, updates the global password and linked local passwords,
     and revokes all local refresh tokens plus identity workspace-selection sessions
```

The raw 256-bit email token is placed in the **frontend URL fragment**, is stored only as a SHA-256 hash, and is never included in application or audit logs. Verification and reset endpoints are anonymous, but registration and reset requests are rate-limited. `NormalizedEmail` keeps its global unique index. Email + Password remains available while a separately verified, unique E.164 phone enables Phone + OTP.

## نظام OTP المركزي (Issues #118 و#127)

الأغراض المسجلة هي `PhoneVerification`, `PasswordlessLogin`, `PlatformAdminLogin`,
`SensitiveActionStepUp`, `PasswordReset`, `ChangePhone`, و`InviteAcceptance`.
لا يقبل الخادم كودًا بلا `challengeId` صحيح، ولا يخزن الكود الصريح. لكل تحدٍ salt مستقل
وHMAC-SHA256، والمقارنة ثابتة زمنيًا، والاستهلاك ذري ومحمي بـSQL `rowversion`.

القواعد الافتراضية:

- الصلاحية 5 دقائق، حد المحاولات 5، ومدة انتظار إعادة الإرسال 60 ثانية.
- إصدار كود جديد يبطل التحدي المعلق السابق لنفس الهاتف والغرض.
- الهاتف يُطبّع ويُرفض ما لم يطابق E.164.
- توجد حدود حسب IP/الجهاز في middleware، وبحسب الهاتف/التحدي/اليوم في قاعدة البيانات.
- لا يعاد OTP في response ولا يسجل في application/audit logs.
- `DevelopmentOtpProvider` لا يعمل إلا عندما تكون البيئة `Development` ويستخدم `1234`
  داخل تحدٍ حقيقي. اختيار المزود خارج Development أو وجود fixed code في Staging/Production
  يوقف Startup فورًا.
- `TemporaryFixedOtpProvider` استثناء مؤقت للاختبار المستضاف فقط. لا يعمل إلا مع تفعيل صريح
  من أسرار الخادم، والكود `1234` بالضبط، وتاريخ انتهاء مستقبلي لا يتجاوز 31 يومًا. بعد انتهاء
  التاريخ يرفض الخادم إصدار تحديات جديدة. لا يتجاوز هذا المزود `challengeId` أو الـHash أو حدود
  المحاولات والإرسال أو الاستهلاك الذري، ولا يعيد الكود في الاستجابة.
- `MetaWhatsAppOtpProvider` ينفذ نفس `IOtpSender` ويستخدم WhatsApp Authentication Template.
  يخزن `ProviderMessageId` ويدعم حالات `Queued`, `Sent`, `Delivered`, `Failed` وWebhook
  موقعًا. نجاح الإرسال لا ينشئ جلسة؛ الجلسة لا تصدر إلا بعد تحقق الكود داخليًا.

### دخول Platform Admin والتحقق الإضافي

```text
POST /api/platform/auth/login { email, password, sessionBinding }
  -> يفحص الحساب والبريد والهاتف المؤكدين
  -> ينشئ PlatformAdminLogin challenge ولا يصدر جلسة
POST /api/platform/auth/otp/verify { challengeId, code, sessionBinding }
  -> يستهلك التحدي مرة واحدة
  -> يصدر Platform JWT ويضع refresh token في HttpOnly cookie
```

العمليات الحساسة في tenants/plans/roles/workspace applications تتطلب
`POST /api/identity/step-up/request` ثم `/step-up/verify`. ترسل الواجهة الإثبات القصير
في `X-LogicFit-OTP-Step-Up` وربط الجلسة في `X-Session-Id`. لا يتجاوز الإثبات صلاحيات
المسؤول الأصلية، ولا يعمل لهوية أو جلسة أخرى.

### Refresh session transport

الـAccess Token قصير العمر ويبقى عقد Bearer الحالي. أما Refresh Token فلا يظهر في
`AuthResponseDto` ولا في localStorage: يكتبه الخادم في Cookie تبدأ بـ`__Host-` بخصائص
`HttpOnly; Secure; SameSite=None; Path=/`. `/refresh` يقرأ الـCookie فقط، يدورها،
ويكتشف إعادة استخدام النسخة القديمة فيبطل عائلة جلسات المستخدم. Reset/Change Password
وتغيير الهاتف المؤكد يبطلان الجلسات القديمة.

الاستجابة تعيد `activeWorkspaces` و`pendingApplications` معًا. وجود طلب معلّق لا يمنع المستخدم من دخول مساحة أخرى يملك فيها `WorkspaceMembership.Active`.

`requiresWorkspaceSelection` يساوي `true` عندما لا يوجد بالضبط workspace واحدة نشطة. على الواجهة أن تعرض اختيار المساحة بصورة صريحة عند تعدد المساحات أو غيابها، وألا تفترض أن أول عنصر هو الصحيح.

## إنشاء مساحة مدرب حر ومتابعة الطلب

```text
زائر/مدرب مستقل
  -> POST /api/workspace-applications/freelance
       { email, phoneNumber?, password, workspaceName, workspaceIdentifier,
         ownerFullName, branding/profile/booking fields }
  -> إنشاء/استخدام IdentityAccount
  -> إنشاء ApplicationRequest: FreelanceWorkspaceCreation / Submitted
  -> إنشاء ApplicationRequestRevision وجلسة متابعة قصيرة العمر
  <- 201 { applicationId, status, trackingToken, expiresAt }

متابعة الطلب في المتصفح
  X-Application-Tracking-Token: trackingToken
  -> GET /api/workspace-applications/tracking

إذا طلبت الإدارة معلومات:
  -> PATCH /api/workspace-applications/tracking/fields
       (الحقول المدرجة فقط في RequestedFields)
  -> POST /api/workspace-applications/tracking/resubmit
  -> إنشاء revision جديدة ثم Submitted
```

طلب مساحة المدرب الحر يحمل هوية مستقلة: الاسم التجاري/اسم المدرب، logo/photo، صور الغلاف والخلفية، الألوان، bio، specialties، certifications، social links، welcome message، booking settings و`workspaceIdentifier`. بعد الاعتماد تصبح هذه هوية المساحة التي يعمل تحتها كل من Freelance Owner وFreelance Coach وFreelance Assistant وClients؛ المدرب المنضم إلى مساحة شخص آخر لا يحصل تلقائيًا على مساحة أو علامة مستقلة.

جلسة المتابعة قصيرة العمر ومن دون refresh token عمدًا. عند انتهاءها أو إغلاق المتصفح تكون طريقة الاستعادة هي:

```text
POST /api/identity/login
  -> الحصول على WorkspaceSelectionToken
  -> POST /api/identity/application-tracking-sessions
       { workspaceSelectionToken }
  <- tracking sessions جديدة للطلبات النشطة
  -> العودة إلى شاشة حالة الطلب باستخدام token الجديد
```

لا تمنح هذه الجلسة أي JWT للمساحة ولا تكشف بيانات صحية أو تدريبية؛ تعيد فقط ما يلزم للمتابعة ومراجعة المعلومات المطلوبة.

## دورة حالات الطلب والمراجعة

| الحالة | من يصل إليها | الانتقالات المسموحة |
|---|---|---|
| `Draft` | طلب جديد قبل الإرسال | `Submitted`، `Cancelled` |
| `Submitted` | مقدم الطلب بعد الإرسال/إعادة التقديم | `UnderReview`، `Cancelled`، `Expired` |
| `UnderReview` | مسؤول منصة بدأ المراجعة | `NeedsMoreInformation`، `Approved`، `Rejected`، `Expired` |
| `NeedsMoreInformation` | الإدارة طلبت حقولًا محددة | `Submitted`، `Cancelled`، `Expired` |
| `Approved` | قرار نهائي ناجح | نهائية |
| `Rejected` | قرار رفض نهائي | نهائية؛ إعادة المحاولة تكون طلبًا جديدًا مربوطًا بـ`PreviousApplicationId` |
| `Cancelled` أو `Expired` | إلغاء أو انتهاء | نهائية |

كل قرار مراجعة يحمل `RowVersion`: الواجهة ترسل النسخة التي قرأتها، ويؤدي التعديل المتزامن أو التكرار إلى `409 Conflict` بدل الموافقة مرتين. يسجل النظام المراجع والوقت والسبب، ويستخدم Outbox للإشعارات. الرفض يبطل جلسات متابعة الطلب.

### واجهة منصة المراجعة

| العملية | endpoint | الجسم/الشرط |
|---|---|---|
| القائمة | `GET /api/platform/workspace-applications` | filter بـ`applicationType` و`status` وpagination |
| بدء المراجعة | `POST /{id}/start-review` | `rowVersion` |
| طلب معلومات | `POST /{id}/request-information` | `rowVersion`، `message`، `requestedFields` المسموح بها |
| اعتماد مساحة حرة | `POST /{id}/approve-freelance` | `rowVersion`؛ للنوع `FreelanceWorkspaceCreation` فقط |
| اعتماد عضوية | `POST /{id}/approve-membership` | `rowVersion`؛ لطلبات الفريق/العميل فقط |
| رفض | `POST /{id}/reject` | `rowVersion` و`reason` |

المسار الكامل لكل عملية في الجدول هو تحت `/api/platform/workspace-applications`. يتطلب `ManageTenants`. واجهة المراجعة تعرض الحد الأدنى اللازم لاتخاذ القرار، ولا تعرض بيانات صحة أو تدريب العملاء.

## اعتماد مساحة المدرب الحر

```text
UnderReview + RowVersion صحيح
  -> تحقق idempotent من ApplicationType والحالة ووجود FreelanceOwner system role
  -> حجز WorkspaceIdentifier
  -> إنشاء/إعادة استخدام Tenant بحالة Provisioning
  -> إنشاء User محلي للمالك + WorkspaceMembership.Active + FreelanceOwner role
  -> حفظ branding وFreelanceWorkspaceProfile
  -> Active
```

إذا حدث فشل قاعدة بيانات أثناء التجهيز تسجل المساحة `ProvisioningFailed` ليستطيع المشغل إعادة المحاولة بأمان؛ لا يجوز لإنعاش الطلب أو ضغط الزر مرتين إنشاء مساحتين. تتطلب الموافقة تطبيق migration التي تزرع `FreelanceOwner` و`FreelanceCoach` و`FreelanceAssistant` وخرائط الصلاحيات قبل الاعتماد.

## انضمام فريق أو عميل إلى مساحة حرة

```text
FreelanceOwner يرشح هوية موجودة
  -> POST /api/freelance/team/applications
       { identity identifier, requestedRole: FreelanceCoach | FreelanceAssistant | Client }
  -> CoachMembership | AssistantMembership | ClientMembership / Submitted
  -> Platform Admin يراجع ويعتمد باستخدام RowVersion
  -> يعيد فحص حد الخطة لحظة الاعتماد
  -> ينشئ User محليًا + Role + WorkspaceMembership.Active
```

يُفحص حد الباقة مرتين: عند التقديم وعند الموافقة. إذا امتلأت المساحة بينهما لا تصبح العضوية نشطة ويعاد خطأ `PLAN_MEMBER_LIMIT_REACHED` أو `PLAN_CLIENT_LIMIT_REACHED`. لا توجد دعوة أو عضوية نشطة تلقائية بمجرد ترشيح المالك.

## طبقات الحراسة والوصول بعد تسجيل الدخول

ترتيب القرار إلزامي: **WorkspaceStatus → MembershipStatus → SubscriptionStatus → roles/permissions**.

| الشرط | النتيجة |
|---|---|
| `WorkspaceMembership` ليست `Active` أو الحساب المحلي غير نشط | لا يصدر token لاختيار تلك المساحة |
| `WorkspaceStatus` = `Suspended` أو `Archived` | حظر تشغيلي كامل |
| `Provisioning` أو `ProvisioningFailed` | حظر مؤقت/تشغيلي كامل بالكود المناسب |
| اشتراك `None` أو `PendingPayment` | billing فقط؛ هذا الوضع الافتراضي لمساحة مدرب حر جديدة بلا اشتراك |
| `Trial` أو `Active` أو `PastDue` أو `GracePeriod` | وصول تشغيلي عادي ضمن حدود الخطة |
| `Cancelled` ووقت التشغيل قبل `EndDate` | وصول كامل حتى نهاية الدورة المدفوعة مع إيقاف التجديد التلقائي |
| `Expired` أو `Cancelled` عند/بعد `EndDate` أو subscription `Suspended` | قراءة فقط مع فوترة وتجديد حيث تسمح السياسة |
| جيم قديم بلا سجل اشتراك SaaS | يحافظ على الوصول القديم مؤقتًا لتوافق الترحيل |

### الحارس الموحد المنفذ أثناء الترحيل

كل session خاصة بمساحة تمر الآن بالحارس نفسه عند `login` و`refresh` و`select-workspace` وكل طلب tenant مصادق عليه. القرار يمنع فورًا الهوية غير النشطة، العضوية غير النشطة، أو الحساب المحلي غير النشط قبل حارس المساحة والاشتراك والصلاحيات.

الحساب المحلي الذي لم يرتبط بعد بـ`IdentityAccount` لا يعامل كعضوية مكتملة؛ يعمل فقط بوضع توافق مرحلي واضح. الإعداد `Authentication__IdentityAccess__AllowUnlinkedLegacySessions` قيمته الافتراضية `true` لحماية المستخدمين الحاليين من القطع. لا يجوز تحويله إلى `false` قبل تنفيذ ربط الحساب القديم المثبت بالبريد والتحقق من القياسات والسجلات؛ عندها يعيد الحارس `IDENTITY_MIGRATION_REQUIRED` بدل إصدار أو قبول session جديدة.

بعد تجاوز الحارس، لا يزال JWT يحمل `TenantId` وroles وpermissions و`PermissionsVersion`. كل endpoint محمي يطبق policy/permission وفحوص ملكية المورد؛ لا يعتمد على اختيار الواجهة لمسار أو شاشة.

## endpoints المرجعية للمصادقة

| الغرض | endpoint |
|---|---|
| تسجيل مساحة/حساب تقليدي | `POST /api/auth/register` |
| دخول تقليدي | `POST /api/auth/login` |
| refresh/logout-all/reset/change password | `/api/auth/refresh`، `/logout-all`، `/forget-password`، `/reset-password`، `/change-password` |
| تسجيل هوية عالمية | `POST /api/identity/register` |
| دخول هوية عالمي | `POST /api/identity/login` |
| طلب/تحقق دخول الهاتف | `POST /api/identity/phone-login/request`، `POST /api/identity/phone-login/verify` |
| تحقق/تغيير الهاتف | `POST /api/identity/phone/request`، `POST /api/identity/phone/verify` |
| استعادة كلمة المرور بالهاتف | `POST /api/identity/phone/password-reset/request`، `POST /api/identity/phone/password-reset/confirm` |
| OTP للعملية الحساسة | `POST /api/identity/step-up/request`، `POST /api/identity/step-up/verify` |
| دخول إدارة المنصة | `POST /api/platform/auth/login` ثم `POST /api/platform/auth/otp/verify` |
| اختيار مساحة | `POST /api/identity/select-workspace` |
| استعادة جلسة متابعة | `POST /api/identity/application-tracking-sessions` |
| إنشاء طلب مدرب حر | `POST /api/workspace-applications/freelance` |
| متابعة/تعديل/إعادة تقديم | `GET /tracking`، `PATCH /tracking/fields`، `POST /tracking/resubmit` تحت `/api/workspace-applications` |
| ترشيح فريق المدرب الحر | `POST /api/freelance/team/applications` |

الـroutes وسياسات التفويض والعقود الدقيقة مولدة في [API-ENDPOINT-CATALOG.md](API-ENDPOINT-CATALOG.md). أي تغيير في هذا التدفق يجب أن يحدّث هذا الملف، [FEATURE-CATALOG.md](FEATURE-CATALOG.md)، ووثائق الواجهات المتأثرة في Pull Request نفسه.
