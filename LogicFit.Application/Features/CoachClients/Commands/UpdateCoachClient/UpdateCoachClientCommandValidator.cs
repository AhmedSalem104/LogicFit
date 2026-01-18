using FluentValidation;

namespace LogicFit.Application.Features.CoachClients.Commands.UpdateCoachClient;

public class UpdateCoachClientCommandValidator : AbstractValidator<UpdateCoachClientCommand>
{
    public UpdateCoachClientCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("CoachClient ID is required");
    }
}
