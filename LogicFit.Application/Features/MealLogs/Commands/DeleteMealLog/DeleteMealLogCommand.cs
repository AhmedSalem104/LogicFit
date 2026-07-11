using MediatR;

namespace LogicFit.Application.Features.MealLogs.Commands.DeleteMealLog;

public class DeleteMealLogCommand : IRequest
{
    public Guid Id { get; set; }
}
