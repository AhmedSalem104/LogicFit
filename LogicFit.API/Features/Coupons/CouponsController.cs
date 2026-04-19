using LogicFit.Application.Features.Coupons.Commands.CreateCoupon;
using LogicFit.Application.Features.Coupons.Commands.DeleteCoupon;
using LogicFit.Application.Features.Coupons.Commands.UpdateCoupon;
using LogicFit.Application.Features.Coupons.DTOs;
using LogicFit.Application.Features.Coupons.Queries.GetCoupons;
using LogicFit.Application.Features.Coupons.Queries.ValidateCoupon;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Coupons;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CouponsController : ControllerBase
{
    private readonly IMediator _mediator;
    public CouponsController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<List<CouponDto>>> GetCoupons([FromQuery] bool? isActive, [FromQuery] string? search)
        => Ok(await _mediator.Send(new GetCouponsQuery { IsActive = isActive, Search = search }));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateCouponCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, UpdateCouponCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteCouponCommand { Id = id });
        return NoContent();
    }

    [HttpGet("validate")]
    public async Task<ActionResult<ValidateCouponResultDto>> Validate(
        [FromQuery] string code,
        [FromQuery] decimal amount,
        [FromQuery] CouponApplicability? context)
        => Ok(await _mediator.Send(new ValidateCouponQuery { Code = code, Amount = amount, Context = context }));
}
