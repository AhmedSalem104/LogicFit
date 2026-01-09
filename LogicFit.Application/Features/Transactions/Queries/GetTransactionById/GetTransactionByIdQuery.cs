using LogicFit.Application.Features.Transactions.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Transactions.Queries.GetTransactionById;

public class GetTransactionByIdQuery : IRequest<TransactionDto?>
{
    public Guid Id { get; set; }
}
