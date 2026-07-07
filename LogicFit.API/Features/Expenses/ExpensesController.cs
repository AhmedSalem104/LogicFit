using LogicFit.Application.Features.Expenses.Commands.CreateExpense;
using LogicFit.Application.Features.Expenses.Commands.DeleteExpense;
using LogicFit.Application.Features.Expenses.Commands.UpdateExpense;
using LogicFit.Application.Features.Expenses.DTOs;
using LogicFit.Application.Features.Expenses.Queries.GetExpenses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Expenses;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.ManageFinance)]
public class ExpensesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ExpensesController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<List<ExpenseDto>>> GetExpenses(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? categoryId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? searchTerm)
        => Ok(await _mediator.Send(new GetExpensesQuery
        {
            BranchId = branchId,
            CategoryId = categoryId,
            FromDate = fromDate,
            ToDate = toDate,
            SearchTerm = searchTerm
        }));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateExpenseCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, UpdateExpenseCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteExpenseCommand { Id = id });
        return NoContent();
    }
}
