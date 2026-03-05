using FluentValidation;

namespace LogicFit.Application.Features.Chat.Commands.SendMessage;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MaximumLength(4000).WithMessage("Content must not exceed 4000 characters");
    }
}
