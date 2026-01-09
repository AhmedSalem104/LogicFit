using MediatR;

namespace LogicFit.Application.Features.Transactions.Commands.DeleteTransaction;

public class DeleteTransactionCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
