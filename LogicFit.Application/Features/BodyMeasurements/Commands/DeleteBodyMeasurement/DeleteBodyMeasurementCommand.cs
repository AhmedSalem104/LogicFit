using MediatR;

namespace LogicFit.Application.Features.BodyMeasurements.Commands.DeleteBodyMeasurement;

public class DeleteBodyMeasurementCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
