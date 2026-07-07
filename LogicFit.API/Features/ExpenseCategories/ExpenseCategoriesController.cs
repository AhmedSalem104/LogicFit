using LogicFit.Application.Features.ExpenseCategories.Commands.CreateExpenseCategory;
using LogicFit.Application.Features.ExpenseCategories.Commands.DeleteExpenseCategory;
using LogicFit.Application.Features.ExpenseCategories.Commands.UpdateExpenseCategory;
using LogicFit.Application.Features.ExpenseCategories.DTOs;
using LogicFit.Application.Features.ExpenseCategories.Queries.GetExpenseCategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.ExpenseCategories;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.ManageFinance)]
public class ExpenseCategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ExpenseCategoriesController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<List<ExpenseCategoryDto>>> GetCategories([FromQuery] bool? isActive)
        => Ok(await _mediator.Send(new GetExpenseCategoriesQuery { IsActive = isActive }));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateExpenseCategoryCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, UpdateExpenseCategoryCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteExpenseCategoryCommand { Id = id });
        return NoContent();
    }
}
