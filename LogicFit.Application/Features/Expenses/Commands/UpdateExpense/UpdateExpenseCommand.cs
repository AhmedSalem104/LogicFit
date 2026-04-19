using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Expenses.Commands.UpdateExpense;

public class UpdateExpenseCommand : IRequest
{
    public Guid Id { get; set; }
    public Guid? BranchId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public string? ReceiptImageUrl { get; set; }
    public string? ReferenceNumber { get; set; }
}
