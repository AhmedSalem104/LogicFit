using LogicFit.Application.Features.BodyMeasurements.DTOs;
using MediatR;

namespace LogicFit.Application.Features.BodyMeasurements.Queries.GetBodyMeasurements;

public class GetBodyMeasurementsQuery : IRequest<List<BodyMeasurementDto>>
{
    public Guid? ClientId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
