# Tenant Branding / White-label

## Current architecture

LogicFit already had `Tenant.BrandingSettings` as a JSON value object and the anonymous endpoint `GET /api/branding/{identifier}`. This implementation extends that single source instead of introducing a competing branding model.

## Resolution and isolation

The public query resolves only by normalized `Subdomain` or `CustomDomain` and returns safe public branding data. Authenticated updates resolve the tenant through `ITenantService`; the request cannot select another tenant with a body/query TenantId. Existing tenant filters, `ManageSettings`, and the WhiteLabel subscription guard remain in force.

## Supported branding surface

Branding now supports alternate logos, icon/favicon, login and dashboard imagery, up to five gallery URLs, application colors and foreground/hover variants, surfaces/sidebar/header/text/input/status colors, font family, border radius, theme mode, invoice/support metadata, and custom CSS.

The existing `GalleryImagesJson` remains backward compatible; the public response limits it to five active image URLs until a dedicated asset table is introduced with an approved storage contract.

## Compatibility

All new properties are nullable JSON fields, so existing tenants and database migrations remain valid. Existing `GET /api/branding/{identifier}` and Gym Profile update contracts remain compatible; clients may send only the fields they support.

## Brand assets API

- `GET /api/branding/{identifier}` returns active public assets (maximum five gallery assets).
- `POST /api/GymProfile/assets` accepts an image plus `assetType`, `title`, and `altText`; it is protected by `ManageSettings` and limits active gallery assets to five per tenant.
- `DELETE /api/GymProfile/assets/{id}` removes only an asset belonging to the authenticated tenant.

## Verification

`dotnet build --no-restore` passed with zero errors. Existing nullable warnings are unrelated to branding.
