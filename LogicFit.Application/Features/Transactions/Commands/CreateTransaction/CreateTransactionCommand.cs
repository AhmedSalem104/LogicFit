using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Transactions.Commands.CreateTransaction;

public class CreateTransactionCommand : IRequest<Guid>
{
    public Guid UserId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
}
