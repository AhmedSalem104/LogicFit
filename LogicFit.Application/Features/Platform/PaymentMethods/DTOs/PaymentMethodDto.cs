namespace LogicFit.Application.Features.Platform.PaymentMethods.DTOs;

public class PaymentMethodDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? AccountName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IBAN { get; set; }
    public string? WalletNumber { get; set; }
    public string? Instructions { get; set; }
    public string? QRImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}
