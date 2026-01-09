using LogicFit.Application.Features.Transactions.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Transactions.Queries.GetTransactionSummary;

public class GetTransactionSummaryQuery : IRequest<TransactionSummaryDto>
{
    public Guid? UserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
