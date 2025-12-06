namespace LogicFit.Application.Features.Auth.DTOs;

public class AuthResponseDto
{
    public Guid UserId { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
