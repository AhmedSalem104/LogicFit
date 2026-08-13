using System.Text.Json.Serialization;
using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Auth.DTOs;

public class AuthResponseDto
{
    public Guid UserId { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FullName { get; set; }
    public string Role { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = new List<string>();
    public IReadOnlyList<string> Permissions { get; set; } = new List<string>();
    public Guid TenantId { get; set; }
    public WorkspaceType? WorkspaceType { get; set; }
    public IReadOnlyList<string> Capabilities { get; set; } = new List<string>();
    public string AccessToken { get; set; } = string.Empty;
    /// <summary>
    /// Transport-only value consumed by the API controller to create the HttpOnly cookie.
    /// It is deliberately excluded from every JSON response.
    /// </summary>
    [JsonIgnore]
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool MustChangePassword { get; set; }
}
