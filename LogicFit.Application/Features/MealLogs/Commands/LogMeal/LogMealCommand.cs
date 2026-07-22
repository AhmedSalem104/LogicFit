using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using MediatR;

namespace LogicFit.Application.Features.MealLogs.Commands.LogMeal;

/// <summary>The signed-in client logs that they consumed a planned meal item (or an alternative food).</summary>
public class LogMealCommand : IRequest<Guid>, IRequireFeature
{
    public string RequiredFeatureCode => FeatureCodes.ClientMobileApp;

    public Guid MealItemId { get; set; }
    public double ConsumedQuantity { get; set; }
    public DateTime? ConsumedAt { get; set; }

    /// <summary>Optional: the client ate a different food than planned; macros use this food instead.</summary>
    public int? AlternativeFoodId { get; set; }
}
