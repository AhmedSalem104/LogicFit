using LogicFit.Application.Features.TaxSettings.DTOs;
using MediatR;

namespace LogicFit.Application.Features.TaxSettings.Queries.GetTaxSettings;

public class GetTaxSettingsQuery : IRequest<List<TaxSettingDto>>
{
    public bool? IsActive { get; set; }
}
