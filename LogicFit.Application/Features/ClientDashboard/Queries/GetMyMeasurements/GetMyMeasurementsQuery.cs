using LogicFit.Application.Features.ClientDashboard.DTOs;
using MediatR;

namespace LogicFit.Application.Features.ClientDashboard.Queries.GetMyMeasurements;

public class GetMyMeasurementsQuery : IRequest<List<MyBodyMeasurementDto>>
{
}
