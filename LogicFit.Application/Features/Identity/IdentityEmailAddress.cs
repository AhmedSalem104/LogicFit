namespace LogicFit.Application.Features.Identity;

/// <summary>Canonical representation used by the globally unique identity email index.</summary>
public static class IdentityEmailAddress
{
    public static string Normalize(string email) => email.Trim().ToUpperInvariant();
}
