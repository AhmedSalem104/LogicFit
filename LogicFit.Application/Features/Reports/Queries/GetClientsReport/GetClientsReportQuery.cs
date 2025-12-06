using LogicFit.Application.Features.Reports.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Reports.Queries.GetClientsReport;

public class GetClientsReportQuery : IRequest<ClientsReportDto>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
