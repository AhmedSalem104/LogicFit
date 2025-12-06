using LogicFit.Domain.Enums;

namespace LogicFit.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, string email, Guid tenantId, UserRole role);
    string GenerateRefreshToken();
    (Guid userId, Guid tenantId)? ValidateToken(string token);
}
