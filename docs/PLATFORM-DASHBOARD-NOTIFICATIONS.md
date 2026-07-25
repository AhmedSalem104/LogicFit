# Platform Dashboard and Notifications

## Dashboard metrics

`GET /api/platform/dashboard` supports `fromUtc`, `toUtc`, `tenantId`, `planId`, and `subscriptionStatus`. The response includes gym states, member count, subscription states, invoice totals, collected payments, pending requests, features, quota definitions, failed jobs, and failed outbox records.

## Gym drill-down

`GET /api/platform/dashboard/tenants` supports `search`, `status`, `planId`, `page`, and `pageSize`. Results are paged and include gym identity, status, member count, latest plan, and subscription end date. The endpoint derives tenant scope on the server and requires `ManagePlatformReports`.

## Platform notifications

```text
GET  /api/platform/notifications?search=&type=&isRead=&page=1&pageSize=20
POST /api/platform/notifications/{id}/read
POST /api/platform/notifications/read-all
```

The recipient is always taken from the authenticated user claims. Client-supplied recipient IDs are never trusted. Read operations are idempotent and persist `IsRead` and `ReadAt` in UTC.
