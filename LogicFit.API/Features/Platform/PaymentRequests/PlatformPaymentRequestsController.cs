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
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.WebUtilities;

namespace LogicFit.API.Features.Platform.PaymentRequests;

[ApiController]
[Route("api/platform/payment-requests")]
[Authorize(Policy = Permissions.ManagePaymentRequests)]
public class PlatformPaymentRequestsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IAmazonS3? _s3;
    private readonly IConfiguration _configuration;

    public PlatformPaymentRequestsController(
        IMediator mediator,
        IApplicationDbContext context,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IServiceProvider services)
    {
        _mediator = mediator;
        _context = context;
        _environment = environment;
        _configuration = configuration;
        _s3 = services.GetService<IAmazonS3>();
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
        if (string.IsNullOrWhiteSpace(url))
            return NotFound();

        // R2 keeps payment proofs private and returns an authenticated media URL.
        // Platform operators reach this endpoint through the payment-request policy,
        // so do not route the request through the tenant-scoped MediaController.
        if (url.StartsWith("/api/media/object", StringComparison.OrdinalIgnoreCase))
        {
            if (_s3 is null || !string.Equals(_configuration["Storage:Provider"], "r2", StringComparison.OrdinalIgnoreCase))
                return NotFound();

            var query = QueryHelpers.ParseQuery(new Uri(url, UriKind.RelativeOrAbsolute).Query);
            if (!query.TryGetValue("key", out var keyValue) || string.IsNullOrWhiteSpace(keyValue))
                return NotFound();

            var key = Uri.UnescapeDataString(keyValue.ToString()).TrimStart('/');
            if (key.Contains("..", StringComparison.Ordinal) || key.Contains('\\') ||
                !key.Contains("/payment-proofs/", StringComparison.OrdinalIgnoreCase))
                return NotFound();

            try
            {
                var objectResponse = await _s3.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = _configuration["Storage:R2:Bucket"],
                    Key = key
                }, cancellationToken);
                return File(objectResponse.ResponseStream,
                    objectResponse.Headers.ContentType ?? MediaTypeNames.Application.Octet,
                    enableRangeProcessing: true);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
        }

        if (!url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
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
