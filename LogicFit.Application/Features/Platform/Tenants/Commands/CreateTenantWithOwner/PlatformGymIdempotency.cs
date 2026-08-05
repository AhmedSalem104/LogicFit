using System.Security.Cryptography;
using System.Text;
using LogicFit.Application.Features.Identity;

namespace LogicFit.Application.Features.Platform.Tenants.Commands.CreateTenantWithOwner;

public static class PlatformGymIdempotency
{
    public static string BuildScopeKey(CreateTenantWithOwnerCommand request, string? platformUserId)
    {
        var material = new StringBuilder("platform-gym-v1|");
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            Append(material, "header-user", platformUserId);
            Append(material, "header-key", request.IdempotencyKey);
        }
        else
        {
            Append(material, "name", request.Name);
            Append(material, "subdomain", request.Subdomain);
            Append(material, "email", request.Email);
            Append(material, "phone", request.PhoneNumber);
            Append(material, "owner-email", request.OwnerEmail);
            Append(material, "owner-phone", request.OwnerPhoneNumber);
            Append(material, "owner-name", request.OwnerFullName);
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material.ToString())))
            .ToLowerInvariant();
        return $"platform-gym:{hash}";
    }

    public static bool MatchesRequest(
        CreateTenantWithOwnerCommand request,
        Domain.Entities.Tenant tenant,
        Domain.Entities.IdentityAccount identity)
        => string.Equals(tenant.Name, request.Name.Trim(), StringComparison.Ordinal) &&
           string.Equals(tenant.Subdomain, Normalize(request.Subdomain), StringComparison.OrdinalIgnoreCase) &&
           string.Equals(tenant.Email, request.Email?.Trim(), StringComparison.OrdinalIgnoreCase) &&
           string.Equals(tenant.PhoneNumber, request.PhoneNumber?.Trim(), StringComparison.Ordinal) &&
           string.Equals(identity.NormalizedEmail, IdentityEmailAddress.Normalize(request.OwnerEmail), StringComparison.Ordinal) &&
           string.Equals(identity.PhoneNumber, request.OwnerPhoneNumber?.Trim(), StringComparison.Ordinal) &&
           string.Equals(identity.FullName, request.OwnerFullName.Trim(), StringComparison.Ordinal);

    private static void Append(StringBuilder material, string name, string? value)
    {
        var normalized = Normalize(value).ToUpperInvariant();
        material.Append(name).Append(':').Append(normalized.Length).Append(':').Append(normalized).Append('|');
    }

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;
}
