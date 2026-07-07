using LogicFit.Application.Features.Platform.PaymentMethods.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Platform.PaymentMethods.Commands.SavePaymentMethod;

/// <summary>Creates a payment method when Id is null, otherwise updates the existing one.</summary>
public class SavePaymentMethodCommand : IRequest<PaymentMethodDto>
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? AccountName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IBAN { get; set; }
    public string? WalletNumber { get; set; }
    public string? Instructions { get; set; }
    public string? QRImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}
