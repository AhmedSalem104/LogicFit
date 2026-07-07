using LogicFit.Application.Features.TaxSettings.Commands.CreateTaxSetting;
using LogicFit.Application.Features.TaxSettings.Commands.DeleteTaxSetting;
using LogicFit.Application.Features.TaxSettings.Commands.UpdateTaxSetting;
using LogicFit.Application.Features.TaxSettings.DTOs;
using LogicFit.Application.Features.TaxSettings.Queries.GetTaxSettings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.TaxSettings;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.ManageSettings)]
public class TaxSettingsController : ControllerBase
{
    private readonly IMediator _mediator;
    public TaxSettingsController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<List<TaxSettingDto>>> GetSettings([FromQuery] bool? isActive)
        => Ok(await _mediator.Send(new GetTaxSettingsQuery { IsActive = isActive }));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateTaxSettingCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, UpdateTaxSettingCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteTaxSettingCommand { Id = id });
        return NoContent();
    }
}
