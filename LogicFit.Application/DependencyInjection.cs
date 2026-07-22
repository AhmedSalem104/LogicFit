using System.Reflection;
using FluentValidation;
using LogicFit.Application.Common.Behaviors;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
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
        services.AddScoped<ITenantSubscriptionGuard, TenantSubscriptionGuard>();
        services.AddScoped<ITenantAccessGuard, TenantAccessGuard>();
        services.AddScoped<ICommissionService, CommissionService>();
        services.AddScoped<ITenantUsageCalculator, TenantUsageCalculator>();

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
