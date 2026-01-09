using LogicFit.Application.Features.Transactions.Commands.CreateTransaction;
using LogicFit.Application.Features.Transactions.Commands.DeleteTransaction;
using LogicFit.Application.Features.Transactions.DTOs;
using LogicFit.Application.Features.Transactions.Queries.GetTransactionById;
using LogicFit.Application.Features.Transactions.Queries.GetTransactions;
using LogicFit.Application.Features.Transactions.Queries.GetTransactionSummary;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Transactions;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all transactions with optional filters
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TransactionDto>>> GetTransactions(
        [FromQuery] Guid? userId,
        [FromQuery] TransactionType? type,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await _mediator.Send(new GetTransactionsQuery
        {
            UserId = userId,
            Type = type,
            FromDate = fromDate,
            ToDate = toDate
        });
        return Ok(result);
    }

    /// <summary>
    /// Get transaction summary with optional filters
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(TransactionSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionSummaryDto>> GetTransactionSummary(
        [FromQuery] Guid? userId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await _mediator.Send(new GetTransactionSummaryQuery
        {
            UserId = userId,
            FromDate = fromDate,
            ToDate = toDate
        });
        return Ok(result);
    }

    /// <summary>
    /// Get a specific transaction by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDto>> GetTransaction(Guid id)
    {
        var result = await _mediator.Send(new GetTransactionByIdQuery { Id = id });
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Create a new transaction
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateTransaction([FromBody] CreateTransactionCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetTransaction), new { id = result }, result);
    }

    /// <summary>
    /// Delete a transaction
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        var result = await _mediator.Send(new DeleteTransactionCommand { Id = id });
        if (!result)
            return NotFound();
        return NoContent();
    }
}
