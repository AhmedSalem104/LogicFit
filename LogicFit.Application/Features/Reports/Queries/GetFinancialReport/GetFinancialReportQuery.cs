using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Domain.Authorization;
using MediatR;

namespace LogicFit.Application.Features.Reports.Queries.GetFinancialReport;

public class GetFinancialReportQuery : IRequest<FinancialReportDto>, IRequireFeature
{
    public string RequiredFeatureCode => FeatureCodes.AdvancedReports;

    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
