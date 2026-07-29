namespace LogicFit.Application.Features.WorkspaceApplications;

/// <summary>Whitelist of payload keys that Platform Admin may request from a freelance applicant.</summary>
internal static class FreelanceWorkspaceApplicationFields
{
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "WorkspaceName", "OwnerFullName", "BrandName",
        "LogoUrl", "PhotoUrl", "CoverImageUrl", "BackgroundImageUrl",
        "PrimaryColor", "SecondaryColor", "Bio", "Specialties", "Certifications",
        "SocialLinks", "WelcomeMessage", "BookingSettings"
    };

    public static bool AreAllowed(IEnumerable<string> fields) => fields.All(Allowed.Contains);
}
