using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Transactions.DTOs;

public class TransactionDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public TransactionType Type { get; set; }
    public string TypeName => Type.ToString();
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

public class TransactionSummaryDto
{
    public decimal TotalDeposits { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public decimal TotalPayments { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal NetBalance { get; set; }
    public int TransactionCount { get; set; }
}
