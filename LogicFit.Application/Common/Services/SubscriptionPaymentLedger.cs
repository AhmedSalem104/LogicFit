using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;

namespace LogicFit.Application.Common.Services;

/// <summary>
/// Appends an immutable payment row for subscription money movements. The subscription amount
/// remains the current summary; this ledger is the historical source used by receipts/reports.
/// </summary>
public static class SubscriptionPaymentLedger
{
    public static Payment? Append(
        IApplicationDbContext context,
        Guid tenantId,
        ClientSubscription subscription,
        decimal amount,
        PaymentMethod method,
        Guid? receivedById,
        DateTime receivedAt,
        string operation,
        string? notes = null)
    {
        if (amount <= 0) return null;

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SubscriptionId = subscription.Id,
            BranchId = subscription.BranchId,
            ClientId = subscription.ClientId,
            Amount = amount,
            Method = method,
            ReceivedAt = receivedAt,
            ReceivedById = receivedById,
            ReceiptNumber = $"LF-{Guid.NewGuid():N}"[..15].ToUpperInvariant(),
            Notes = string.IsNullOrWhiteSpace(notes) ? operation : $"{operation}: {notes.Trim()}"
        };
        context.Payments.Add(payment);
        return payment;
    }
}
