using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LogicFit.Infrastructure.Identity;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(Guid userId, string email, Guid tenantId, UserRole role)
    {
        var secret = _configuration["JwtSettings:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        var issuer = _configuration["JwtSettings:Issuer"] ?? "LogicFit";
        var audience = _configuration["JwtSettings:Audience"] ?? "LogicFitUsers";
        var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "60");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim("TenantId", tenantId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public (Guid userId, Guid tenantId)? ValidateToken(string token)
    {
        var secret = _configuration["JwtSettings:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateIssuer = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"] ?? "LogicFit",
                ValidateAudience = true,
                ValidAudience = _configuration["JwtSettings:Audience"] ?? "LogicFitUsers",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = Guid.Parse(jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
            var tenantId = Guid.Parse(jwtToken.Claims.First(x => x.Type == "TenantId").Value);

            return (userId, tenantId);
        }
        catch
        {
            return null;
        }
    }
}
