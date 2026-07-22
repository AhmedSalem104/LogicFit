using System.Text;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Infrastructure.Authorization;
using LogicFit.Infrastructure.Identity;
using LogicFit.Infrastructure.Persistence;
using LogicFit.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace LogicFit.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Identity
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = false; // We handle uniqueness per tenant
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // JWT Authentication
        var jwtSecret = configuration["JwtSettings:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        var key = Encoding.UTF8.GetBytes(jwtSecret);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = configuration["JwtSettings:Issuer"] ?? "LogicFit",
                ValidateAudience = true,
                ValidAudience = configuration["JwtSettings:Audience"] ?? "LogicFitUsers",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        // Permission-based authorization: policies are synthesized per permission code
        // and evaluated against the "permission" claims embedded in the JWT at login.
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, ActiveTenantAuthorizationHandler>();
        services.AddAuthorization(options =>
        {
            // Endpoints with a plain [Authorize] (no permission policy) still enforce the gym-status rule.
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new ActiveTenantRequirement())
                .Build();
        });

        // Services
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDateTimeService, DateTimeService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordResetTokenService, PasswordResetTokenService>();
        services.AddScoped<IFileUploadService, FileUploadService>();
        services.AddScoped<IEmailService, LoggingEmailService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddSingleton<IBackupService, SqlServerBackupService>();

        // Data Seeder
        services.AddScoped<RbacSeeder>();
        services.AddScoped<PlanSeeder>();
        services.AddScoped<DataSeeder>();

        // Background Services — run in a single host only (disabled on the Platform API via config)
        // so the daily jobs don't execute twice against the shared database.
        var bgFlag = configuration["BackgroundJobs:Enabled"];
        var runBackgroundJobs = string.IsNullOrEmpty(bgFlag) || bgFlag.Equals("true", StringComparison.OrdinalIgnoreCase);
        if (runBackgroundJobs)
        {
            services.AddHostedService<SubscriptionLifecycleService>();
            services.AddHostedService<PlatformSubscriptionLifecycleService>();
        }

        return services;
    }
}
