using MediatR;

namespace LogicFit.Application.Features.Platform.Dashboard;

public class GetPlatformDashboardQuery : IRequest<PlatformDashboardDto>
{
}

public class PlatformDashboardDto
{
    public int TotalGyms { get; set; }
    public int ActiveGyms { get; set; }
    public int TrialGyms { get; set; }
    public int PendingApprovalGyms { get; set; }
    public int SuspendedGyms { get; set; }
    public int TotalMembers { get; set; }
}
