using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Identity;
using LogicFit.Application.Features.WorkspaceClientJoins.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EmailTokenGenerator = LogicFit.Application.Features.Identity.IdentityEmailActionToken;

namespace LogicFit.Application.Features.WorkspaceClientJoins.Commands.GenerateWorkspaceClientJoinCode;

public sealed class GenerateWorkspaceClientJoinCodeCommandHandler : IRequestHandler<GenerateWorkspaceClientJoinCodeCommand, WorkspaceClientJoinCodeDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public GenerateWorkspaceClientJoinCodeCommandHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
        => (_context, _tenantService, _dateTimeService) = (context, tenantService, dateTimeService);

    public async Task<WorkspaceClientJoinCodeDto> Handle(GenerateWorkspaceClientJoinCodeCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;
        var activeCodes = await _context.WorkspaceClientJoinCodes
            .Where(x => x.TenantId == tenantId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var active in activeCodes)
            active.RevokedAt = now;
        var code = EmailTokenGenerator.CreateRaw();
        var joinCode = new WorkspaceClientJoinCode
        {
            TenantId = tenantId,
            CodeHash = EmailTokenGenerator.Hash(code),
            ExpiresAt = now.AddDays(request.ValidForDays),
            AutoApproveClients = request.AutoApproveClients
        };
        _context.WorkspaceClientJoinCodes.Add(joinCode);
        _context.OutboxMessages.Add(new OutboxMessage
        {
            Type = "workspace.client_join_code.rotated",
            Payload = $"{{\"workspaceId\":\"{tenantId}\",\"joinCodeId\":\"{joinCode.Id}\"}}",
            OccurredAtUtc = now,
            IdempotencyKey = $"workspace-client-join-code:{joinCode.Id}:created"
        });
        await _context.SaveChangesAsync(cancellationToken);
        return new WorkspaceClientJoinCodeDto { Code = code, ExpiresAt = joinCode.ExpiresAt, AutoApproveClients = joinCode.AutoApproveClients };
    }
}
