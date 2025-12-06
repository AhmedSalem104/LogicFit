using LogicFit.Application.Features.Reports.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Reports.Queries.GetTraineeProgressReport;

public class GetTraineeProgressReportQuery : IRequest<TraineeProgressReportDto>
{
    public Guid ClientId { get; set; }
}
