using LogicFit.API.Features.ClientFeedback;
using LogicFit.Domain.Entities;
using LogicFit.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace LogicFit.Tests;

public sealed class MemberFeedbackFeatureContractTests
{
    [Fact]
    public void Feedback_is_a_tenant_entity_with_explicit_client_and_review_fields()
    {
        Assert.True(typeof(ClientFeedback).IsAssignableTo(typeof(LogicFit.Domain.Common.Interfaces.ITenantEntity)));
        Assert.NotNull(typeof(ClientFeedback).GetProperty(nameof(ClientFeedback.ClientId)));
        Assert.NotNull(typeof(ClientFeedback).GetProperty(nameof(ClientFeedback.Status)));
        Assert.NotNull(typeof(ClientFeedback).GetProperty(nameof(ClientFeedback.ReviewedById)));
    }

    [Fact]
    public void Feedback_controller_separates_client_submission_from_staff_review()
    {
        var methods = typeof(ClientFeedbackController).GetMethods();
        var submit = methods.Single(x => x.Name == "Submit");
        var list = methods.Single(x => x.Name == "List");
        var review = methods.Single(x => x.Name == "Review");

        Assert.Contains(submit.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(), x => x.Policy is null);
        Assert.Contains(list.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(), x => x.Policy == LogicFit.Domain.Authorization.Permissions.ViewMembers);
        Assert.Contains(list.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(), x => x.Policy == LogicFit.Domain.Authorization.WorkspaceCapabilities.GymExperience);
        Assert.Contains(review.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(), x => x.Policy == LogicFit.Domain.Authorization.Permissions.ManageMembers);
        Assert.Contains(review.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(), x => x.Policy == LogicFit.Domain.Authorization.WorkspaceCapabilities.GymExperience);
    }

    [Fact]
    public void Tenant_context_owns_feedback_table()
    {
        Assert.Contains(typeof(ClientFeedback), DbContextOwnership.TenantEntities);
    }
}
