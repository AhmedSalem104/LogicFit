# مرجع LogicFit التشغيلي والتقني

هذا المجلد هو المصدر المرجعي المكتوب للمشروع. الغرض منه أن يستطيع أي مالك منتج أو
مشغّل أو مطور فهم النظام وتشغيله وتعديله بدون الاعتماد على ذاكرة محادثة أو شخص واحد.
الشفرة وقرارات الـDomain هي الحقيقة التنفيذية؛ وعند اختلاف التوثيق عنها تُصحَّح
الوثيقة في نفس الـPull Request الذي يغيّر السلوك.

## خريطة القراءة

| الوثيقة | لمن؟ | ماذا تجيب؟ |
|---|---|---|
| [حالة المشروع](LOGICFIT-PROJECT-STATUS.md) | الفريق كله | ما الذي نُفذ الآن، العقود، الاختبارات، النشر والمخاطر المعروفة؟ |
| [المنتج والتدفقات](PRODUCT-AND-FLOWS.md) | مالك المنتج والتشغيل | ما هي فكرة الـSaaS؟ كيف تسجل صالة؟ كيف يعمل الدفع والاشتراك والميزات؟ |
| [المستخدمون والصلاحيات](USERS-AND-PERMISSIONS.md) | الإدارة والأمن | من يدخل النظام، ماذا يستطيع أن يفعل، وما حدود العزل؟ |
| [مرجع لوحة المنصة](PLATFORM-ADMIN-GUIDE.md) | Platform Owner/Admin | شرح كل شاشة وزر رئيسي وإجراء في لوحة الإدارة المركزية. |
| [مرجع API](API-REFERENCE.md) | الواجهات وBackend | المسارات، المصادقة، الترقيم، أخطاء HTTP، وحدود العمليات. |
| [كتالوج API الكامل](API-ENDPOINT-CATALOG.md) | Frontend / Backend / QA | كل endpoint مولّد من الـControllers: method، route، الصلاحية، المدخلات، والاستجابة المعلنة. |
| [البيانات والـSaaS](SAAS-DOMAIN-AND-DATA.md) | Backend/DBA | الكيانات، Snapshots، الفواتير، Outbox، الحالات والثوابت. |
| [التشغيل والنشر](OPERATIONS-AND-DEPLOYMENT.md) | DevOps/Support | الإعدادات، النسخ الاحتياطي، المراقبة، CI/CD، الاستعادة وRollback. |
| [دليل واجهة الصالة](TENANT-APPLICATION-GUIDE.md) | فريق الواجهة والدعم | مسارات وتجربة مالك الصالة والمدرب والمتدرب وموظفيها. |

توثيق لوحة المنصة المستقل موجود أيضاً في مشروع الواجهة نفسه:
[README](../../LogiFit_Platform_Admin_Dashboard/README.md)،
[كتالوج الشاشات](../../LogiFit_Platform_Admin_Dashboard/docs/SCREEN-CATALOG.md)،
[الربط والمعمارية](../../LogiFit_Platform_Admin_Dashboard/docs/ARCHITECTURE-AND-INTEGRATION.md)،
[نظام التصميم](../../LogiFit_Platform_Admin_Dashboard/docs/STYLE-GUIDE.md) و
[مساحة عمل الإدارة](../../LogiFit_Platform_Admin_Dashboard/docs/ADMIN-WORKSPACE.md).

## حدود المستودعات

```text
LogicFit/                              Backend .NET + Domain + Platform API + migrations + tests
LogicFit_Angular/                      واجهة الصالات: Owner / Coach / Client / Back-office
LogiFit_Platform_Admin_Dashboard/      واجهة الإدارة المركزية للـSaaS
LogicFit_LandingPage/                  واجهة التسويق العامة
```

## قاعدة صيانة التوثيق

أي تغيير في واحد من الآتي يحدّث هذه الوثائق في نفس المهمة: Endpoint، DTO، قاعدة
بيانات أو Migration، صلاحية، حالة اشتراك/دفع، شاشة، تصميم عام، إعداد تشغيل أو خطة
استعادة. لا توثّق الأسرار، كلمات المرور، Connection Strings أو ملفات نشر تحتويها.

### كتالوج الـAPI الذي لا ينقص

ملف [API-ENDPOINT-CATALOG.md](API-ENDPOINT-CATALOG.md) ليس قائمة يدوية: يُنشأ من
جميع Controllers في Tenant API وPlatform API بواسطة:

```powershell
.\Scripts\Export-ApiEndpointCatalog.ps1
```

## Canonical feature and authentication references

The existing guides remain useful by audience. The following two references are the required starting point when changing the product:

| Reference | Purpose |
|---|---|
| [Feature catalog](FEATURE-CATALOG.md) | Complete registry of the Platform, workspace, finance, fitness, HR, inventory, communication, identity, and frontend feature families, including the implementation source and responsible roles. |
| [Authentication and workspace flows](AUTHENTICATION-AND-WORKSPACE-FLOWS.md) | The current legacy and identity-first login flows, workspace selection, freelance applications, review, approval, team membership, session recovery, and access gates. |
| [Monster capability assessment](MONSTER-CAPABILITY-ASSESSMENT.md) | Current hosting limits, provider guards, and evidence required before native backup/restore or tenant activation. |

Keep those references synchronized with the source in the same Pull Request. `API-ENDPOINT-CATALOG.md` remains the generated authority for the exact HTTP contract; it complements, rather than replaces, the user-flow documentation.

لذلك أي تعديل في route أو method أو policy أو request/response يجب أن يتبعه تشغيل
السكربت ومراجعة التغيّر الناتج قبل الـPull Request. هذا يمنع فقد endpoint من
التوثيق، ولا يلغي الحاجة إلى توثيق قرار الـDomain أو تدفق المستخدم في الملفات
المتخصصة أعلاه.
