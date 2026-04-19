using MediatR;

namespace LogicFit.Application.Features.TaxSettings.Commands.UpdateTaxSetting;

public class UpdateTaxSettingCommand : IRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}
