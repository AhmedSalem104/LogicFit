# TOP GYM → LogicFit Gym parity

This document records the implementation contract for the Gym experience. TOP GYM is
the product reference; LogicFit remains the shared multi-tenant platform and keeps its
backend authorization, Tenant database routing, and existing API contracts.

## Feature parity

| TOP GYM area | LogicFit implementation | Status |
|---|---|---|
| Login, Owner/Assistant access | Identity login, workspace selection, RBAC, workspace members | Complete |
| Dashboard and KPIs | `/owner/dashboard`, reports and operations dashboard | Complete |
| Members and profiles | `/owner/clients`, onboarding, client details, self-service client area | Complete |
| Membership plans, renewals, freeze and arrears | `/owner/subscription-plans`, `/owner/subscriptions`, invoices/payments | Complete |
| Payments, receipts and ledger | Payments, invoices, wallet/refund source-of-truth services | Complete |
| Daily visitor passes | `/owner/day-passes`, `api/day-passes`, tenant tables `DayPassTypes` and `DayPassSales` | Complete in this parity change |
| Attendance and QR/gate access | `/owner/attendance`, `/owner/gate-access`, Membership Cards | Complete |
| External trainees | Coach client assignment and client experience | Complete |
| Training programs and workout sessions | Coach builders, client execution and idempotent sessions | Complete |
| Nutrition plans and meal logs | Diet plan builder, foods, meal logs and client nutrition screens | Complete |
| Measurements and daily check-ins | Measurements, progress and readiness check-ins | Complete |
| Exercise/food library | Coach library hub and exercise/food/muscle routes | Complete |
| Expenses and finance reports | Expenses, categories, financial and operations reports | Complete; daily-pass revenue included |
| Member feedback | `/client/feedback`, `/owner/member-feedback`, `api/member-feedback` | Complete in this parity change |
| Member QR card | `/owner/membership-cards`, QR issue/revoke/scan | Complete |
| Workspace backup | Tenant backup export plus platform backup controls | Complete; storage configuration is required for runtime |
| Print/PDF/export | Shared export/PDF services and print actions | Complete |
| WhatsApp manual click-to-chat messages | Shared LogicFit message service for member onboarding, renewal/expiry/debt/freeze alerts, and daily passes; opens `wa.me` with a prefilled message and leaves Send to the operator | Complete for the manual TOP GYM flow |

## Screen flow

### Gym administration

1. Owner signs in and selects an active Gym workspace.
2. The sidebar shows Gym navigation only. `FreelanceCoach` capabilities are rejected by
   both Angular route guards and backend policies.
3. The owner registers a member from **المشتركون**. The onboarding form keeps member,
   membership, discount, payment, and review in one flow.
4. A daily visitor is registered from **الحصص اليومية**. The server resolves the active
   pass type, creates the sale and its payment in one save operation, and the UI can
   open the visitor's WhatsApp chat with the TOP GYM-style prefilled visit message.
5. The owner uses **الحضور والبوابة** for attendance and QR checks, and **التقارير**
   for collected revenue, expenses, subscriptions, and daily-pass revenue.
6. Member onboarding can open a prefilled membership message after the transaction;
   subscription rows and client rows expose the same manual click-to-chat alerts for
   expiry, freeze, debt, and renewal follow-up. No provider/API send is performed.
7. Member feedback is submitted by the authenticated client from **رأيي في الجيم**;
   staff review it from **تقييمات المشتركين** without leaving the tenant.

### Client experience

The authenticated LogicFit client area is the secure equivalent of TOP GYM's public
membership-code portal. It exposes the member's own dashboard, subscriptions,
attendance, programs, diet, measurements, progress, appointments, chat, challenges,
profile, and feedback. It does not expose another tenant's records or accept an
arbitrary client id from the browser.

## Backend contracts added for parity

- `GET/PUT /api/day-passes/pricing`
- `GET /api/day-passes`
- `GET /api/day-passes/summary`
- `POST/PUT/DELETE /api/day-passes/{id}`
- `POST /api/day-passes/{id}/void`
- `POST /api/day-passes/{id}/whatsapp-opened`
- `POST /api/member-feedback` (the authenticated client only)
- `GET /api/member-feedback` and `/summary` (member-view permission)
- `POST /api/member-feedback/{id}/review` (member-management permission)

Daily-pass endpoints require both `ManageDayPasses` and `GymExperience`; the permission
is tenant-scoped and the capability prevents FreelanceCoach access. Feedback rows are
tenant-owned and have explicit `New`, `Reviewed`, and `Resolved` states.

## Migrations

Both the dedicated Tenant migration history and the legacy compatibility context have
new migrations named `TopGymDailyPasses` and `TopGymMemberFeedback`. Production rollout
must apply the Tenant migration to every assigned workspace database before exposing the
new menu items.

## Deliberate integration boundary

TOP GYM's Smart Assistant is not copied as a fake/mock feature. LogicFit needs the chosen
AI provider, model, retention policy, and allowed data fields before it can be enabled
safely. WhatsApp is intentionally not part of this boundary: the required behavior is
manual click-to-chat with a prepared message, not provider automation or background
sending.

## Member portal parity (Issue #329)

The TOP GYM membership-code portal is now implemented as `/member-portal` in the Tenant UI.
It uses the existing opaque `MembershipCard.QrCode`, resolves the tenant before database routing,
and exposes only safe read-only member data plus tenant feedback. The admin membership-card screen
opens the portal for active cards; expired/revoked cards cannot open it. See
[TOP-GYM-MEMBER-PORTAL.md](TOP-GYM-MEMBER-PORTAL.md) for the endpoint contract, privacy boundary,
rate limit, and verification details.
