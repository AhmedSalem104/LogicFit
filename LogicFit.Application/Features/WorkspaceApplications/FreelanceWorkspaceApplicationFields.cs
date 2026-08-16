namespace LogicFit.Application.Features.WorkspaceApplications;

/// <summary>Whitelist of shared workspace payload keys plus the special payment-proof completion action.</summary>
internal static class FreelanceWorkspaceApplicationFields
{
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "WorkspaceName", "OwnerFullName", "BrandName",
        "LogoUrl", "PhotoUrl", "CoverImageUrl", "BackgroundImageUrl",
        "PrimaryColor", "SecondaryColor", "Bio", "Specialties", "Certifications",
        "SocialLinks", "WelcomeMessage", "BookingSettings", "PaymentProof"
    };

    public static bool AreAllowed(IEnumerable<string> fields) => fields.All(Allowed.Contains);
}
