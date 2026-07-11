using LogicFit.Application.Features.MealLogs.DTOs;
using MediatR;

namespace LogicFit.Application.Features.MealLogs.Queries.GetNutritionSummary;

/// <summary>The signed-in client's consumed macros for a day vs their active diet-plan targets.</summary>
public class GetNutritionSummaryQuery : IRequest<NutritionSummaryDto>
{
    public DateTime? Date { get; set; }
}
