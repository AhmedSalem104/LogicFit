using MediatR;

namespace LogicFit.Application.Features.Platform.PaymentMethods.Commands.DeletePaymentMethod;

public class DeletePaymentMethodCommand : IRequest<Unit>
{
    public Guid Id { get; set; }

    public DeletePaymentMethodCommand(Guid id) => Id = id;
}
