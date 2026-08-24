# TOP GYM member portal parity

This is the LogicFit implementation of TOP GYM's membership-code portal. It keeps the
shared multi-tenant platform and uses the existing tenant-owned `MembershipCards` table;
it does not introduce a second member database or a second authentication system.

## Member journey

1. Gym staff issues a membership card from `/owner/membership-cards`.
2. The Backend generates an opaque random `QrCode` and keeps the active card tenant-scoped.
3. The staff member clicks **فتح بوابة العضو** on an active card. Angular opens
   `/member-portal?workspace={tenant-identifier}&code={opaque-card-code}` in a new tab.
4. The public screen calls `POST /api/member-portal/lookup?workspace=...` with
   `{ "membershipCode": "..." }`.
5. Tenant middleware resolves the workspace before database routing. The card code is then
   checked against the active, non-revoked, non-expired card in that tenant database.
6. The report shows only safe read-only information: member contact, current/past memberships,
   payment ledger, attendance, freeze periods, and an aggregate financial summary.
7. The member can submit a rating and note through
   `POST /api/member-portal/feedback?workspace=...`. The server derives `ClientId` from the
   card; the browser cannot submit an arbitrary client id or email.

## API boundary

| Endpoint | Access | Purpose | Important failures |
|---|---|---|---|
| `POST /api/member-portal/lookup?workspace=...` | Anonymous + `member-public-portal` rate limit | Read the member report for an active card | `400` missing workspace, `404` invalid card/workspace, `503` missing tenant database |
| `POST /api/member-portal/feedback?workspace=...` | Anonymous + `member-public-portal` rate limit | Create tenant feedback for the card owner | `400` invalid rating/type/message, `404` invalid card/workspace, `503` missing tenant database |

`workspace` may be a subdomain, custom domain, or the tenant GUID used by an admin-generated
link. A GUID is only a routing hint; it is never sufficient without the opaque membership code.
The server never returns `QrCode`, connection strings, database names, tenant database mappings,
medical history, training plans, meal plans, measurements, tokens, or internal tenant ids in the
portal report.

The endpoint is limited to `Gym` workspaces. `FreelanceCoach` keeps its authenticated coaching
experience and cannot expose the gym membership portal.

## Admin and UX behavior

- Expired, revoked, inactive, or invalid cards produce one generic message to avoid card/account
  enumeration.
- Loading, empty, error, retry/new-search, and success states are explicit; there is no blank page.
- The report is responsive at mobile and desktop widths, supports print, and keeps tables inside a
  horizontal scroll container on narrow screens.
- The card code is not echoed in the report. It remains in memory only for the feedback request.
- Feedback is stored in the existing `ClientFeedback` table and is reviewed from the existing
  owner feedback screen.

## Assistant boundary

The current TOP-style assistant is the existing local, permission-filtered Help Center/Help Local
surface. It answers from local screen metadata and opens allowed routes/forms; it does not execute
mutations silently and does not send platform data to an external LLM. Enabling an external AI
assistant remains a separate product/security decision requiring a provider, model, retention, and
allowed-data policy.

## Verification

- Backend `MemberPortalFeatureContractTests`: portal anonymity/rate limit, safe response surface,
  tenant-owned tables, and stable membership-code request contract.
- Angular `member-portal.service.spec.ts`: lookup and feedback requests are workspace + code based,
  with no email/client-id binding.
- Backend API build passed.
- Angular build passed.
- Angular ChromeHeadless suite passed (`56/56` in the current workspace).

No database migration is required because the portal reuses the already-migrated membership card
and feedback entities. Assigned tenant databases must still be on the existing current tenant
migration level before the owner exposes membership cards.
