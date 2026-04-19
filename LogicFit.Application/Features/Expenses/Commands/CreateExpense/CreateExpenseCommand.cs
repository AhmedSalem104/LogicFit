using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Expenses.Commands.CreateExpense;

public class CreateExpenseCommand : IRequest<Guid>
{
    public Guid? BranchId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateTime? ExpenseDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public string? ReceiptImageUrl { get; set; }
    public string? ReferenceNumber { get; set; }
}
