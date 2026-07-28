using LogicFit.Application.Features.Clients.Commands.CreateClient;
using LogicFit.Application.Features.MembershipCards.Commands.IssueMembershipCard;
using LogicFit.Application.Features.Subscriptions.Commands.CreateClientSubscription;
using LogicFit.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Clients.Commands.OnboardClient;

public sealed class OnboardClientCommandHandler : IRequestHandler<OnboardClientCommand, OnboardClientResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IMediator _mediator;

    public OnboardClientCommandHandler(IApplicationDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<OnboardClientResult> Handle(OnboardClientCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.BeginTransactionAsync(cancellationToken);
        try
        {
            var clientId = await _mediator.Send(new CreateClientCommand
            {
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                Password = request.Password,
                FullName = request.FullName,
                Gender = request.Gender,
                BirthDate = request.BirthDate,
                CoachId = request.CoachId
            }, cancellationToken);

            Guid? subscriptionId = null;
            Guid? cardId = null;
            if (request.Membership is not null)
            {
                subscriptionId = await _mediator.Send(new CreateClientSubscriptionCommand
                {
                    ClientId = clientId,
                    PlanId = request.Membership.PlanId,
                    StartDate = request.Membership.StartDate,
                    PaymentMethod = request.Membership.PaymentMethod,
                    AmountPaid = request.Membership.AmountPaid,
                    Discount = request.Membership.Discount,
                    Notes = request.Membership.Notes,
                    PayFromWallet = request.Membership.PayFromWallet
                }, cancellationToken);

                if (request.Membership.IssueCard)
                {
                    cardId = await _mediator.Send(new IssueMembershipCardCommand
                    {
                        ClientId = clientId,
                        ExpiresAt = null
                    }, cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return new OnboardClientResult { ClientId = clientId, SubscriptionId = subscriptionId, MembershipCardId = cardId };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
