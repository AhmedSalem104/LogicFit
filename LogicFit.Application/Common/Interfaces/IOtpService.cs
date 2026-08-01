using LogicFit.Application.Features.Identity.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;

namespace LogicFit.Application.Common.Interfaces;

public interface IOtpService
{
    Task<OtpChallengeDto> RequestAsync(
        string phoneNumber,
        OtpPurpose purpose,
        Guid? identityAccountId,
        string? sessionBinding,
        CancellationToken cancellationToken = default,
        bool sendToProvider = true);

    Task<OtpChallenge> VerifyAsync(
        Guid challengeId,
        string code,
        OtpPurpose purpose,
        string? sessionBinding,
        CancellationToken cancellationToken = default);
}
