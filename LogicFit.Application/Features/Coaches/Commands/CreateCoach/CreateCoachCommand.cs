using LogicFit.Application.Common.Interfaces;
using MediatR;

namespace LogicFit.Application.Features.Coaches.Commands.CreateCoach;

public class CreateCoachCommand : IRequest<Guid>, IRequireQuota
{
    public string QuotaResource => QuotaResources.Coaches;

    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? FullName { get; set; }
    public int? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
}
