using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;

namespace LogicFit.Application.Common.Services;

public static class ProvisioningErrorCodes
{
    public const string ApplicationOwnerNotFound = "APPLICATION_OWNER_NOT_FOUND";
    public const string DatabaseCapacityUnavailable = "DATABASE_CAPACITY_UNAVAILABLE";
    public const string DatabaseConnectionNotConfigured = "DATABASE_CONNECTION_NOT_CONFIGURED";
    public const string DatabaseMappingInvalid = "DATABASE_MAPPING_INVALID";
    public const string DatabaseResourceNotFound = "DATABASE_RESOURCE_NOT_FOUND";
    public const string GymProvisioningFailed = "TENANT_PROVISIONING_FAILED";
    public const string IdempotencyKeyReused = "IDEMPOTENCY_KEY_REUSED";
    public const string ProviderNotConfigured = "PROVIDER_NOT_CONFIGURED";
    public const string ProvisioningInProgress = "TENANT_PROVISIONING_IN_PROGRESS";
    public const string TenantDatabaseHealthCheckFailed = "TENANT_DATABASE_HEALTH_CHECK_FAILED";

    public static string Normalize(string? code)
        => code switch
        {
            ApplicationOwnerNotFound => ApplicationOwnerNotFound,
            DatabaseCapacityUnavailable => DatabaseCapacityUnavailable,
            DatabaseConnectionNotConfigured => DatabaseConnectionNotConfigured,
            DatabaseMappingInvalid => DatabaseMappingInvalid,
            DatabaseResourceNotFound => DatabaseResourceNotFound,
            GymProvisioningFailed => GymProvisioningFailed,
            IdempotencyKeyReused => IdempotencyKeyReused,
            ProviderNotConfigured => ProviderNotConfigured,
            ProvisioningInProgress => ProvisioningInProgress,
            TenantDatabaseHealthCheckFailed => TenantDatabaseHealthCheckFailed,
            _ => GymProvisioningFailed
        };
}

public static class ProvisioningOutcomeGuard
{
    public static void EnsureCompleted(WorkspaceProvisioningOutcome outcome)
    {
        if (outcome.Status == ProvisioningJobStatus.Completed)
            return;

        if (outcome.Status == ProvisioningJobStatus.AwaitingDatabaseCapacity ||
            string.Equals(outcome.ErrorCode, ProvisioningErrorCodes.DatabaseCapacityUnavailable, StringComparison.Ordinal))
        {
            throw new ProvisioningException(
                ProvisioningErrorCodes.DatabaseCapacityUnavailable,
                409,
                "No database resource is currently available. An operator must repair or add capacity before retrying this gym.",
                retryable: true,
                tenantId: outcome.TenantId,
                applicationRequestId: outcome.ApplicationRequestId,
                databaseResourceId: outcome.DatabaseResourceId);
        }

        var code = ProvisioningErrorCodes.Normalize(outcome.ErrorCode);
        if (code == ProvisioningErrorCodes.ProvisioningInProgress)
        {
            throw new ProvisioningException(
                code,
                409,
                "Gym provisioning is still running. Retry after the current attempt completes.",
                retryable: true,
                tenantId: outcome.TenantId,
                applicationRequestId: outcome.ApplicationRequestId,
                databaseResourceId: outcome.DatabaseResourceId);
        }

        throw new ProvisioningException(
            code,
            503,
            "Gym provisioning did not complete. An operator can repair the database resource and retry the provisioning job.",
            retryable: true,
            tenantId: outcome.TenantId,
            applicationRequestId: outcome.ApplicationRequestId,
            databaseResourceId: outcome.DatabaseResourceId);
    }
}
