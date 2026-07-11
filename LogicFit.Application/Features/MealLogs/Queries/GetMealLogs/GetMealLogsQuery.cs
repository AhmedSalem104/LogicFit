using LogicFit.Application.Features.MealLogs.DTOs;
using MediatR;

namespace LogicFit.Application.Features.MealLogs.Queries.GetMealLogs;

/// <summary>The signed-in client's logged meals for a given day (defaults to today).</summary>
public class GetMealLogsQuery : IRequest<List<MealLogDto>>
{
    public DateTime? Date { get; set; }
}
