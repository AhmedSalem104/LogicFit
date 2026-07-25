using LogicFit.Domain.Common;
using LogicFit.Domain.Common.Interfaces;

namespace LogicFit.Domain.Entities;

/// <summary>Tenant-owned visual asset used by the Gym App white-label experience.</summary>
public class TenantBrandAsset : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string AssetType { get; set; } = "Gallery";
    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? DesktopImageUrl { get; set; }
    public string? TabletImageUrl { get; set; }
    public string? MobileImageUrl { get; set; }
    public string? Title { get; set; }
    public string? AltText { get; set; }
    public decimal FocalPointX { get; set; } = 0.5m;
    public decimal FocalPointY { get; set; } = 0.5m;
    public string? CropPosition { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
