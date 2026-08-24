using LogicFit.API.Features.Reports;
using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace LogicFit.Tests;

public sealed class FollowUpAlertsContractTests
{
    [Fact]
    public void Dashboard_follow_up_contract_is_read_only_and_tenant_safe()
    {
        Assert.NotNull(typeof(FollowUpAlertsDto).GetProperty(nameof(FollowUpAlertsDto.Alerts)));
        Assert.NotNull(typeof(FollowUpAlertItemDto).GetProperty(nameof(FollowUpAlertItemDto.ClientId)));
        Assert.NotNull(typeof(FollowUpAlertItemDto).GetProperty(nameof(FollowUpAlertItemDto.Kind)));
        Assert.Null(typeof(FollowUpAlertItemDto).GetProperty("MedicalHistory"));
        Assert.Null(typeof(FollowUpAlertItemDto).GetProperty("ConnectionString"));
    }

    [Fact]
    public void Dashboard_follow_up_endpoint_requires_reports_and_member_access()
    {
        var method = typeof(ReportsController).GetMethod(nameof(ReportsController.GetFollowUpAlerts));
        Assert.NotNull(method);
        var policies = method!.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .Select(attribute => attribute.Policy)
            .ToArray();

        Assert.Contains(WorkspaceCapabilities.GymReports, policies);
        Assert.Contains(Permissions.ViewMembers, policies);
    }
}
