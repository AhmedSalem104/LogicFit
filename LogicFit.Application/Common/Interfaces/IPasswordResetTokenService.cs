namespace LogicFit.Application.Common.Interfaces;

/// <summary>Creates and verifies short-lived password-reset codes without storing the raw code.</summary>
public interface IPasswordResetTokenService
{
    string GenerateToken();
    string HashToken(string token);
    bool VerifyToken(string token, string storedHash);
    bool CanExposeToken { get; }
}
