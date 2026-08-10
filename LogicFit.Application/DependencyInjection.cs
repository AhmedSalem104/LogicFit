using System.Reflection;
using FluentValidation;
using LogicFit.Application.Common.Behaviors;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Identity;
using LogicFit.Application.Features.Platform.Auth;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LogicFit.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IRbacService, RbacService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IdentityEmailActionService>();
        services.AddScoped<LegacyIdentityMigrationService>();
        services.AddScoped<ITenantSubscriptionGuard, TenantSubscriptionGuard>();
        services.AddScoped<ITenantAccessGuard, TenantAccessGuard>();
        services.AddScoped<ICommissionService, CommissionService>();
        services.AddScoped<ICoachPlanAccessService, CoachPlanAccessService>();
        services.AddScoped<ITenantUsageCalculator, TenantUsageCalculator>();
        services.AddScoped<IWorkspaceMembershipQuotaService, WorkspaceMembershipQuotaService>();
        services.AddScoped<IIdentityWorkspaceSessionIssuer, IdentityWorkspaceSessionIssuer>();
        services.AddScoped<IPlatformSessionIssuer, PlatformSessionIssuer>();
        services.AddScoped<IPlatformTenantLifecycleService, PlatformTenantLifecycleService>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(SubscriptionGuardBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        });

        return services;
    }
}
