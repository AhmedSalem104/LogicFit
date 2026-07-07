using LogicFit.Application.Common.Interfaces;
using MediatR;

namespace LogicFit.Application.Features.Branches.Commands.CreateBranch;

public class CreateBranchCommand : IRequest<Guid>, IRequireQuota
{
    public string QuotaResource => QuotaResources.Branches;

    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public int? Capacity { get; set; }
    public TimeSpan? OpenTime { get; set; }
    public TimeSpan? CloseTime { get; set; }
    public Guid? ManagerId { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
}
