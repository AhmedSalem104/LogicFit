using MediatR;

namespace LogicFit.Application.Features.TaxSettings.Commands.DeleteTaxSetting;

public class DeleteTaxSettingCommand : IRequest
{
    public Guid Id { get; set; }
}
