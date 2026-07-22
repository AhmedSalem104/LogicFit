using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.ClientDashboard.DTOs;
using LogicFit.Domain.Authorization;
using MediatR;

namespace LogicFit.Application.Features.ClientDashboard.Queries.GetMyDashboard;

public class GetMyDashboardQuery : IRequest<ClientDashboardDto>, IRequireFeature
{
    public string RequiredFeatureCode => FeatureCodes.ClientMobileApp;
}
