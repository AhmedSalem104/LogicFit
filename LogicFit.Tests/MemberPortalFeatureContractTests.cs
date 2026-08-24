using LogicFit.API.Features.MemberPortal;
using LogicFit.Domain.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace LogicFit.Tests;

public sealed class MemberPortalFeatureContractTests
{
    [Fact]
    public void Portal_is_anonymous_but_rate_limited_and_exposes_only_read_models()
    {
        var controller = typeof(MemberPortalController);

        Assert.Contains(controller.GetCustomAttributes(typeof(AllowAnonymousAttribute), true), _ => true);
        Assert.Contains(controller.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true)
            .Cast<EnableRateLimitingAttribute>(), x => x.PolicyName == "member-public-portal");
        Assert.Null(typeof(MemberPortalReport).GetProperty("QrCode"));
        Assert.Null(typeof(MemberPortalReport).GetProperty("TenantId"));
        Assert.Null(typeof(MemberPortalClient).GetProperty("Id"));
        Assert.Null(typeof(MemberPortalMembership).GetProperty("Id"));
        Assert.Null(typeof(MemberPortalClient).GetProperty("MedicalHistory"));
    }

    [Fact]
    public void Portal_uses_the_existing_tenant_card_and_feedback_tables()
    {
        Assert.True(typeof(MembershipCard).IsAssignableTo(typeof(ITenantEntity)));
        Assert.True(typeof(ClientFeedback).IsAssignableTo(typeof(ITenantEntity)));
        Assert.Contains(typeof(MembershipCard), DbContextOwnership.TenantEntities);
        Assert.Contains(typeof(ClientFeedback), DbContextOwnership.TenantEntities);
    }

    [Fact]
    public void Portal_contract_has_lookup_and_feedback_actions_with_stable_request_ids()
    {
        var methods = typeof(MemberPortalController).GetMethods();

        Assert.Contains(methods, method => method.Name == "Lookup");
        Assert.Contains(methods, method => method.Name == "SubmitFeedback");
        Assert.Equal(nameof(MemberPortalFeedbackRequest.MembershipCode),
            typeof(MemberPortalFeedbackRequest).GetProperty(nameof(MemberPortalFeedbackRequest.MembershipCode))?.Name);
        Assert.Equal(nameof(MemberPortalLookupRequest.MembershipCode),
            typeof(MemberPortalLookupRequest).GetProperty(nameof(MemberPortalLookupRequest.MembershipCode))?.Name);
    }
}
