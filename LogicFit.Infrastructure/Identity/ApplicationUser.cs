using LogicFit.Domain.Common.Interfaces;
using LogicFit.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace LogicFit.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>, ITenantEntity, ISoftDeletable
{
    public string FullName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal WalletBalance { get; set; } = 0;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    // Audit Fields
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
