using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Domain.Authorization;
using MediatR;

namespace LogicFit.Application.Features.Reports.Queries.GetOperationsDashboard;

public class GetOperationsDashboardQuery : IRequest<OperationsDashboardDto>, IRequireFeature
{
    public string RequiredFeatureCode => FeatureCodes.AdvancedReports;
}
