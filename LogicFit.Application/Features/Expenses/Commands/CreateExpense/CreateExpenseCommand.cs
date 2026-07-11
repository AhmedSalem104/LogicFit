using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Expenses.Commands.CreateExpense;

public class CreateExpenseCommand : IRequest<Guid>, IRequireFeature
{
    public string RequiredFeatureCode => FeatureCodes.FinanceModule;

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
