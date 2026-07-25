using LogicFit.Application.Features.Platform.Plans.DTOs;
using LogicFit.Domain.Entities;
using Xunit;

namespace LogicFit.Tests;

public class PlatformPlanDtoMapperTests
{
    [Fact]
    public void Maps_feature_codes_and_optional_limits_after_plan_graph_is_materialized()
    {
        var plan = new Plan
        {
            Name = "Professional",
            Price = 1200m,
            PlanFeatures =
            [
                new PlanFeature { Feature = new Feature { Code = "members.manage" }, LimitValue = 500 },
                new PlanFeature { Feature = new Feature { Code = "reports.export" }, LimitValue = null }
            ]
        };

        var result = PlanDtoMapper.Map(plan);

        Assert.Equal(["members.manage", "reports.export"], result.Features);
        Assert.Equal(500, result.FeatureLimits["members.manage"]);
        Assert.Null(result.FeatureLimits["reports.export"]);
    }
}
