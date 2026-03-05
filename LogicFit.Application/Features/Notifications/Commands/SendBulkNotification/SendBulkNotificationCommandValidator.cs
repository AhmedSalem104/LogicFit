using FluentValidation;

namespace LogicFit.Application.Features.Notifications.Commands.SendBulkNotification;

public class SendBulkNotificationCommandValidator : AbstractValidator<SendBulkNotificationCommand>
{
    public SendBulkNotificationCommandValidator()
    {
        RuleFor(x => x.RecipientIds)
            .NotEmpty().WithMessage("At least one recipient is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required")
            .MaximumLength(2000).WithMessage("Body must not exceed 2000 characters");
    }
}
