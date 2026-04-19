using MediatR;

namespace LogicFit.Application.Features.TaxSettings.Commands.CreateTaxSetting;

public class CreateTaxSettingCommand : IRequest<Guid>
{
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}
