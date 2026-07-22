using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Domain.Authorization;
using MediatR;

namespace LogicFit.Application.Features.Reports.Queries.GetBranchComparisonReport;

public class GetBranchComparisonReportQuery : IRequest<BranchComparisonReportDto>, IRequireFeature
{
    public string RequiredFeatureCode => FeatureCodes.AdvancedReports;

    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
