# Platform/Tenant Schema Ownership Inventory

Issue: #169  
Status: design inventory; no schema, database, or migration has been changed by this document.

This inventory is based on the current `ApplicationDbContext` model. The current application
uses one shared database and EF query filters. The target uses two migration assemblies:

```text
PlatformDbContext  -> Platform database and Platform migrations
TenantDbContext    -> one independent database per Workspace and Tenant migrations
```

`Shared contract` means the identifier/event/seed contract is shared; it does not mean that a
customer row is stored in the Platform database. No cross-database foreign keys are planned.

## Platform-owned tables

| Entity | Current table/DbSet | Target owner | Notes |
|---|---|---|---|
| IdentityAccount | IdentityAccounts | Platform | Global login identity; Email + Password only. |
| IdentityEmailActionToken | IdentityEmailActionTokens | Platform | Hashed, single-use email verification/reset links. |
| IdentityWorkspaceSession | IdentityWorkspaceSessions | Platform | Will become renewable Identity Access sessions; not a Tenant JWT. |
| OtpChallenge | OtpChallenges | Remove from target | Historical only; OTP and Phone Login are prohibited. |
| Tenant | Tenants | Platform | Workspace metadata, status, identifier and lifecycle. |
| WorkspaceMembership | WorkspaceMemberships | Platform | Identity-to-Workspace membership reference and status. |
| ApplicationRequest | ApplicationRequests | Platform | Workspace applications and state machine. |
| ApplicationRequestRevision | ApplicationRequestRevisions | Platform | Immutable resubmission history. |
| ApplicationTrackingSession | ApplicationTrackingSessions | Platform | Transitional public tracking; identity flow becomes primary. |
| WorkspaceInvite | WorkspaceInvites | Platform | Single-use invitation metadata and hash. |
| WorkspaceClientJoinCode | WorkspaceClientJoinCodes | Platform | Join-code metadata and hash before membership activation. |
| Plan | Plans | Platform | SaaS plan catalog. |
| Feature | Features | Platform | SaaS feature catalog. |
| FeatureDependency | FeatureDependencies | Platform | Platform feature graph. |
| FeatureQuotaDefinition | FeatureQuotaDefinitions | Platform | Platform quota definitions. |
| PlanFeature | PlanFeatures | Platform | Plan-to-feature catalog relation. |
| TenantSubscription | TenantSubscriptions | Platform | SaaS subscription lifecycle. |
| TenantFeature | TenantFeatures | Platform | Tenant entitlement overrides. |
| SubscriptionFeatureSnapshot | SubscriptionFeatureSnapshots | Platform | Immutable entitlement snapshot. |
| TenantUsage | TenantUsages | Platform | Platform quota/usage summary. |
| TenantPaymentMethod | TenantPaymentMethods | Platform | Manual billing method configuration. |
| PaymentRequest | PaymentRequests | Platform | Application/renewal proof review and status. |
| SubscriptionPayment | SubscriptionPayments | Platform | Platform billing payment history. |
| SubscriptionInvoice | SubscriptionInvoices | Platform | Platform billing invoice history. |
| RefreshToken | RefreshTokens | Platform | Identity/Platform/Tenant refresh-session records, surface-scoped. |
| ApplicationUser (ASP.NET Identity) | AspNetUsers and related tables | Split/review | Existing legacy store; target global password authority is IdentityAccount. |
| Role | Roles/AppRoles | Platform | Canonical platform role templates. |
| Permission | Permissions | Platform | Canonical permission catalog. |
| RolePermission | RolePermissions | Platform | Canonical role-permission catalog. |
| UserRoleAssignment | UserRoleAssignments | Platform (Platform rows) | PlatformOwner/PlatformAdmin assignments only. Tenant rows are local. |
| AuditLog | AuditLogs | Platform (security summary) | Authentication, approvals, provisioning and security events. |
| OutboxMessage | OutboxMessages | Platform | Cross-database workflow events. Tenant outbox is local. |
| JobExecutionLog | JobExecutionLogs | Platform | Persistent Platform job history. Tenant job history is local. |

New Platform tables required by later issues:

| Planned entity | Purpose |
|---|---|
| TenantDatabaseMapping | Encrypted server-side Workspace-to-database mapping. |
| DatabaseResource | Monster/Local resource pool state and capabilities. |
| ProvisioningJob | Idempotent, resumable provisioning saga steps. |
| BackupJob / BackupBatch / BackupManifest | Per-database backup/export tracking. |
| RestoreJob | Conditional restore and mapping-switch audit. |
| SensitiveActionGrant | Hashed, five-minute, single-use password reauthentication grant. |

## Tenant-owned tables

Each row below is created in the selected Workspace database by the Tenant migration assembly.
The existing TenantId column remains as a defense-in-depth boundary even though the database is
already isolated.

| Entity | Current table/DbSet |
|---|---|
| User | Users |
| UserProfile | UserProfiles |
| FreelanceWorkspaceProfile | FreelanceWorkspaceProfiles |
| NutrientDefinition | NutrientDefinitions |
| Food | Foods |
| FoodMicronutrient | FoodMicronutrients |
| Recipe | Recipes |
| RecipeIngredient | RecipeIngredients |
| DietPlan | DietPlans |
| DailyMeal | DailyMeals |
| MealItem | MealItems |
| MealLog | MealLogs |
| Muscle | Muscles |
| Exercise | Exercises |
| ExerciseSecondaryMuscle | ExerciseSecondaryMuscles |
| WorkoutProgram | WorkoutPrograms |
| ProgramRoutine | ProgramRoutines |
| RoutineExercise | RoutineExercises |
| WorkoutSession | WorkoutSessions |
| SessionSet | SessionSets |
| BodyMeasurement | BodyMeasurements |
| SubscriptionPlan | SubscriptionPlans |
| ClientSubscription | ClientSubscriptions |
| SubscriptionFreeze | SubscriptionFreezes |
| CoachClient | CoachClients |
| Appointment | Appointments |
| Attendance | Attendances |
| StaffAttendance | StaffAttendances |
| ChatConversation | ChatConversations |
| ChatMessage | ChatMessages |
| Challenge | Challenges |
| ClientChallenge | ClientChallenges |
| Branch | Branches |
| BranchOperatingHours | BranchOperatingHours |
| UserBranchAccess | UserBranchAccesses |
| MembershipCard | MembershipCards |
| GateAccessLog | GateAccessLogs |
| Room | Rooms |
| Equipment | Equipment |
| MaintenanceRecord | MaintenanceRecords |
| ExpenseCategory | ExpenseCategories |
| Expense | Expenses |
| Invoice | Invoices |
| InvoiceItem | InvoiceItems |
| Payment | Payments |
| Coupon | Coupons |
| CouponUsage | CouponUsages |
| TaxSetting | TaxSettings |
| GroupClass | GroupClasses |
| ClassSchedule | ClassSchedules |
| ClassEnrollment | ClassEnrollments |
| ProductCategory | ProductCategories |
| Product | Products |
| StockItem | StockItems |
| StockMovement | StockMovements |
| Supplier | Suppliers |
| PurchaseOrder | PurchaseOrders |
| PurchaseOrderItem | PurchaseOrderItems |
| Sale | Sales |
| SaleItem | SaleItems |
| EmployeeProfile | EmployeeProfiles |
| EmployeeBranch | EmployeeBranches |
| Shift | Shifts |
| ShiftAssignment | ShiftAssignments |
| LeaveRequest | LeaveRequests |
| Commission | Commissions |
| CommissionRule | CommissionRules |
| PayrollRun | PayrollRuns |
| PayrollItem | PayrollItems |
| Notification | Notifications |
| WalletTransaction | WalletTransactions |
| TenantBrandAsset | TenantBrandAssets |
| UserRoleAssignment (Tenant rows) | UserRoleAssignments |
| AuditLog (tenant operational events) | AuditLogs |
| OutboxMessage (Tenant) | OutboxMessages |
| JobExecutionLog (Tenant jobs) | JobExecutionLogs |

## Shared contracts and split entities

| Concern | Central representation | Tenant representation |
|---|---|---|
| IdentityAccountId | IdentityAccount and Membership | Local User reference only; no FK across databases. |
| Workspace/TenantId | Tenant metadata and Mapping | Required column/query boundary on every tenant aggregate. |
| MembershipId | WorkspaceMembership status/role reference | Local user/role projection uses the stable ID. |
| Role/Permission | Canonical platform catalog | Immutable tenant seed/projection used by Tenant authorization. |
| UserRoleAssignment | Platform assignments central | Tenant assignments local to the Workspace database. |
| Food/Exercise catalog | Global seed/version contract | Tenant copy or override with stable source IDs. |
| Audit | Central security summary | Tenant operational detail. |
| Outbox | Platform saga events | Tenant-local operational events. |
| Backup metadata | Platform only | No backup secrets or storage paths in Tenant DB. |

## Migration ownership and baseline

The existing 52 source migration classes belong to the legacy shared `ApplicationDbContext`.
The production database has 53 history rows because it also contains
`20260729141315_SeedFreelanceSystemRoles`, present on the historical WIP branch but not in the
current canonical source. Published migrations are immutable and must not be edited.

The next design issue must:

1. Reconcile the missing historical migration and compare its SQL/designer/snapshot.
2. Freeze the legacy context for the existing Platform database.
3. Create a Platform migration assembly/history table containing only Platform tables.
4. Create a Tenant migration assembly/history table containing only Tenant tables.
5. Generate a clean Tenant baseline against an empty prepared database.
6. Define a data-transfer plan for any existing Tenant rows; never assume a reset.
7. Preserve the current PlatformOwner and orphan PlatformAdmin by ID and identity link.
8. Verify both baselines locally and against Monster-prepared databases before activation.
