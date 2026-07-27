# Production database migrations

Creating and pushing an EF Core migration does not change the production database. The
schema changes must be applied against the same database used by the deployed API.

## Monster ASP one-time rollout

The API supports a controlled, opt-in startup migration for hosts where the deployment
pipeline cannot run SQL directly:

1. Configure the site environment variable `Database__ApplyMigrationsOnStartup` to
   `true` (the double underscore maps to `Database:ApplyMigrationsOnStartup`).
2. Restart the site once and check `https://<host>/health` and the application log.
3. Confirm that the pending migrations completed successfully.
4. Set the variable back to `false` (or remove it) and restart again.

This setting is intentionally disabled by default. Do not leave it enabled on a
multi-instance deployment, because two instances must not attempt schema changes at the
same time. The API always applies migrations before `DataSeeder`, so seeders can safely
query columns introduced by the migration (including `Permissions.DisplayNameAr` and
`Roles.NameAr`).

## Preferred release process

For future releases, generate and review an idempotent script in CI and execute it as a
single migration job using a restricted database credential before deploying the API:

```powershell
dotnet ef migrations script --idempotent `
  --project LogicFit.Infrastructure `
  --startup-project LogicFit.API `
  --context ApplicationDbContext
```

Never paste `GO` separators into SQL clients that do not support batch commands. Run
each batch separately or use the startup switch above. Take a database backup before
production schema changes.
