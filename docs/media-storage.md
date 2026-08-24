# LogicFit media storage

All API image/video upload endpoints depend on `IFileUploadService`. The default provider remains the local `wwwroot/uploads` provider. Cloudflare R2 is enabled only when `Storage:Provider` is set to `r2`, so existing deployments remain backwards compatible.

## Free/no-account mode

For a deployment that does not have an object-storage account, explicitly set:

```text
Storage__Provider=local
```

No Cloudflare, Google or AWS credentials are required in this mode. Files are stored below `wwwroot/uploads` using the existing validation, random names and feature-level tenant ownership rules. Keep a backup of this directory and use a persistent disk; ephemeral containers or a multi-instance deployment should use R2 instead.

When `Backup:Enabled=true`, the daily backup job now creates both the database BACPAC and a `media-YYYYMMDD-HHmmss.zip` archive of `wwwroot/uploads` in the configured private `Backup:StorageDirectory`. The media archive uses the same retention period and is written atomically through a temporary file, so partial archives are not treated as backups.

Google Drive is intentionally not used as the default provider: its API requires a Google account/project and OAuth or service-account credentials, and Drive is not an object-storage/CDN endpoint for authenticated `<img>` requests. The R2 provider remains fully available and can be enabled later by changing only the provider setting and adding its credentials.

## Production configuration

Set these values as hosting environment variables (never commit the access key or secret):

```text
Storage__Provider=r2
Storage__R2__ServiceUrl=https://<account-id>.r2.cloudflarestorage.com
Storage__R2__Region=auto
Storage__R2__Bucket=logicfit-media
Storage__R2__AccessKey=<R2 access key id>
Storage__R2__SecretKey=<R2 secret access key>
Storage__R2__PublicBaseUrl=https://media.example.com   # optional; use a custom R2 domain
```

Without `PublicBaseUrl`, uploads return `/api/media/object?key=...`. That endpoint requires a JWT and only streams keys under the authenticated user's `TenantId` prefix. Platform users can access platform-scoped objects. Keys containing traversal segments are rejected.

With `PublicBaseUrl`, only assets intentionally returned as public URLs (branding, logos and other non-sensitive images) use the CDN domain. Payment receipts, identity documents and other sensitive files should keep the authenticated API URL.

Local sensitive documents are retained under `wwwroot/uploads/documents` for the configured
retention period, but the API blocks that path before `UseStaticFiles`. Payment proofs are therefore
retrieved only through their authenticated feature endpoint (`/api/platform/payment-requests/{id}/proof`
for Platform review, with an optional retained version); a direct public `/uploads/documents/...`
request returns `404`. Retention is separate from presentation: replacing a proof adds a version and
does not delete the previous file or its audit metadata.

## Existing image fields

Gym profile, branding assets, profile pictures, exercise images, body-measurement photos and payment proofs already call `IFileUploadService`; switching the provider therefore moves new uploads without changing their feature controllers. Existing `/uploads/...` records remain readable during migration. A storage migration job should copy those objects to the tenant-scoped R2 prefix and update their URL fields after a backup.
