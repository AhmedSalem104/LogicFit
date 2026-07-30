namespace LogicFit.Application.Common.Interfaces;

/// <summary>Creates frontend-only URLs for email actions without logging their raw tokens.</summary>
public interface IIdentityEmailLinkFactory
{
    bool IsConfigured { get; }
    string CreateEmailVerificationLink(string rawToken);
    string CreatePasswordResetLink(string rawToken);
}
