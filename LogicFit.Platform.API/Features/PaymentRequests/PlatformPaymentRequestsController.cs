using LogicFit.Application.Features.Platform.PaymentRequests.Commands.ApprovePaymentRequest;
using LogicFit.Application.Features.Platform.PaymentRequests.Commands.RejectPaymentRequest;
using LogicFit.Application.Features.Platform.PaymentRequests.DTOs;
using LogicFit.Application.Features.Platform.PaymentRequests.Queries.GetPaymentRequests;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogicFit.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using System.Net.Mime;

namespace LogicFit.Platform.API.Features.PaymentRequests;

[ApiController]
[Route("api/platform/payment-requests")]
[Authorize(Policy = Permissions.ManagePaymentRequests)]
public class PlatformPaymentRequestsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public PlatformPaymentRequestsController(IMediator mediator, IApplicationDbContext context, IWebHostEnvironment environment)
    {
        _mediator = mediator;
        _context = context;
        _environment = environment;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] PaymentRequestStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetPaymentRequestsQuery { Status = status, Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(PaymentRequestDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentRequestDto>> Approve(Guid id)
    {
        var result = await _mediator.Send(new ApprovePaymentRequestCommand(id));
        return Ok(result);
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(PaymentRequestDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentRequestDto>> Reject(Guid id, [FromBody] RejectPaymentRequestCommand command)
    {
        command.PaymentRequestId = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Streams a payment proof only to an authorized platform operator.</summary>
    [HttpGet("{id:guid}/proof")]
    public async Task<IActionResult> Proof(Guid id, CancellationToken cancellationToken)
    {
        var url = await _context.PaymentRequests.AsNoTracking()
            .Where(p => p.Id == id && !p.IsDeleted)
            .Select(p => p.ProofFileUrl)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return NotFound();

        var root = Path.GetFullPath(Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads"));
        var relative = url["/uploads/".Length..].Replace('/', Path.DirectorySeparatorChar);
        var filePath = Path.GetFullPath(Path.Combine(root, relative));
        if (!filePath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(filePath))
            return NotFound();

        var contentType = MediaTypeNames.Application.Octet;
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension == ".jpg" || extension == ".jpeg") contentType = MediaTypeNames.Image.Jpeg;
        else if (extension == ".png") contentType = MediaTypeNames.Image.Png;
        else if (extension == ".webp") contentType = "image/webp";
        return PhysicalFile(filePath, contentType, enableRangeProcessing: true);
    }
}
