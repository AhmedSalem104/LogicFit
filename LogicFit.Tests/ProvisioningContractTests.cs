using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Platform.Tenants.Commands.CreateTenantWithOwner;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using Xunit;

namespace LogicFit.Tests;

public sealed class ProvisioningContractTests
{
    [Fact]
    public void Capacity_outcome_is_a_retryable_conflict_with_stable_identifiers()
    {
        var tenantId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var outcome = new WorkspaceProvisioningOutcome(
            tenantId,
            applicationId,
            ProvisioningJobStatus.AwaitingDatabaseCapacity,
            resourceId,
            ProvisioningErrorCodes.DatabaseCapacityUnavailable);

        var exception = Assert.Throws<ProvisioningException>(
            () => ProvisioningOutcomeGuard.EnsureCompleted(outcome));

        Assert.Equal(ProvisioningErrorCodes.DatabaseCapacityUnavailable, exception.Code);
        Assert.Equal(409, exception.StatusCode);
        Assert.True(exception.Retryable);
        Assert.Equal(tenantId, exception.TenantId);
        Assert.Equal(applicationId, exception.ApplicationRequestId);
        Assert.Equal(resourceId, exception.DatabaseResourceId);
        Assert.Contains("No database resource", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Provider_failure_is_sanitized_to_a_retryable_service_error()
    {
        var outcome = new WorkspaceProvisioningOutcome(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ProvisioningJobStatus.Failed,
            null,
            "Microsoft.Data.SqlClient.SqlException: Password=should-not-cross-api");

        var exception = Assert.Throws<ProvisioningException>(
            () => ProvisioningOutcomeGuard.EnsureCompleted(outcome));

        Assert.Equal(ProvisioningErrorCodes.GymProvisioningFailed, exception.Code);
        Assert.Equal(503, exception.StatusCode);
        Assert.True(exception.Retryable);
        Assert.DoesNotContain("Password=", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SqlException", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Natural_request_scope_ignores_password_changes_and_explicit_keys_are_hashed()
    {
        var first = CreateRequest();
        var second = CreateRequest();
        second.OwnerPassword = "A-different-password-123";

        var naturalFirst = PlatformGymIdempotency.BuildScopeKey(first, "platform-user");
        var naturalSecond = PlatformGymIdempotency.BuildScopeKey(second, "another-platform-user");
        Assert.Equal(naturalFirst, naturalSecond);

        first.IdempotencyKey = "smoke-key-42";
        var explicitScope = PlatformGymIdempotency.BuildScopeKey(first, "platform-user");
        Assert.StartsWith("platform-gym:", explicitScope, StringComparison.Ordinal);
        Assert.DoesNotContain("smoke-key-42", explicitScope, StringComparison.Ordinal);
        Assert.NotEqual(naturalFirst, explicitScope);
    }

    [Fact]
    public void Explicit_key_reuse_requires_the_same_request_shape()
    {
        var request = CreateRequest();
        var tenant = new Tenant
        {
            Name = request.Name,
            Subdomain = request.Subdomain,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };
        var identity = new IdentityAccount
        {
            NormalizedEmail = request.OwnerEmail.ToUpperInvariant(),
            PhoneNumber = request.OwnerPhoneNumber,
            FullName = request.OwnerFullName
        };

        Assert.True(PlatformGymIdempotency.MatchesRequest(request, tenant, identity));

        request.Name = "A different gym";
        Assert.False(PlatformGymIdempotency.MatchesRequest(request, tenant, identity));
    }

    [Fact]
    public void Create_command_validator_rejects_invalid_owner_and_subdomain_input()
    {
        var result = new CreateTenantWithOwnerCommandValidator().Validate(new CreateTenantWithOwnerCommand
        {
            Name = string.Empty,
            Subdomain = "Not A Subdomain",
            OwnerEmail = "not-an-email",
            OwnerPassword = "short",
            OwnerFullName = string.Empty
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTenantWithOwnerCommand.Name));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTenantWithOwnerCommand.Subdomain));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTenantWithOwnerCommand.OwnerEmail));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTenantWithOwnerCommand.OwnerPassword));
    }

    [Fact]
    public void Provisioning_api_contract_exposes_safe_retry_metadata()
    {
        var middleware = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.API",
            "Middleware",
            "ExceptionHandlingMiddleware.cs"));
        var controller = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "LogicFit.API",
            "Features",
            "Platform",
            "Tenants",
            "PlatformTenantsController.cs"));

        Assert.Contains("X-Request-Id", middleware);
        Assert.Contains("retryable = provisioning.Retryable", middleware);
        Assert.Contains("retryEndpoint", middleware);
        Assert.Contains("[FromHeader(Name = \"Idempotency-Key\")]", controller);
        Assert.Contains("IDEMPOTENCY_KEY_INVALID", controller);
    }

    [Fact]
    public void Idempotency_migrations_cover_both_runtime_contexts_and_gym_scope()
    {
        var applicationMigration = Directory.GetFiles(
                Path.Combine(RepositoryRoot, "LogicFit.Infrastructure", "Persistence", "Migrations"),
                "*_ExpandGymProvisioningIdempotencyScope.cs")
            .Single();
        var platformMigration = Directory.GetFiles(
                Path.Combine(RepositoryRoot, "LogicFit.Platform.Migrations", "Migrations"),
                "*_ExpandGymProvisioningIdempotencyScope.cs")
            .Single();

        foreach (var path in new[] { applicationMigration, platformMigration })
        {
            var source = File.ReadAllText(path);
            Assert.Contains("IX_ApplicationRequests_TargetScopeKey_ApplicationType", source);
            Assert.Contains("[ApplicationType] = 1", source);
            Assert.Contains("[Status] IN (1, 2, 3, 4, 5)", source);
        }
    }

    private static string RepositoryRoot => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static CreateTenantWithOwnerCommand CreateRequest() => new()
    {
        Name = "North Star Gym",
        Subdomain = "north-star-gym",
        Email = "gym@example.invalid",
        PhoneNumber = "+201000000000",
        OwnerEmail = "owner@example.invalid",
        OwnerPhoneNumber = "+201111111111",
        OwnerPassword = "A-safe-password-123",
        OwnerFullName = "Gym Owner"
    };
}
