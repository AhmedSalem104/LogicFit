using LogicFit.Application.Features.Transactions.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Transactions.Queries.GetTransactions;

public class GetTransactionsQuery : IRequest<List<TransactionDto>>
{
    public Guid? UserId { get; set; }
    public TransactionType? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
