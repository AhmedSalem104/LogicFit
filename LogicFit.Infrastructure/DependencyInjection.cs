using System.Text;
using Amazon.S3;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Infrastructure.Authorization;
using LogicFit.Infrastructure.Identity;
using LogicFit.Infrastructure.Persistence;
using LogicFit.Infrastructure.Security;
using LogicFit.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace LogicFit.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddDataProtection();

        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

          services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
          services.AddScoped<IDatabaseResourcePool, DatabaseResourcePoolService>();
          services.AddScoped<ManualMonsterProvisioningProvider>();
          services.AddScoped<LocalSqlProvisioningProvider>();
          services.AddScoped<IDatabaseProvisioningProvider>(provider =>
              configuration["DatabaseResourcePool:ProvisioningProvider"]?.Equals("LocalSql", StringComparison.OrdinalIgnoreCase) == true
                  ? provider.GetRequiredService<LocalSqlProvisioningProvider>()
                  : provider.GetRequiredService<ManualMonsterProvisioningProvider>());
        services.AddScoped<IWorkspaceProvisioningSaga, WorkspaceProvisioningSaga>();
        services.AddSingleton<IConnectionStringProtector, DataProtectionConnectionStringProtector>();
        services.AddScoped<ITenantDatabaseMappingReader, PlatformTenantDatabaseMappingReader>();
        services.AddScoped<ITenantDatabaseResolver, TenantDatabaseResolver>();
        services.AddOptions<StartupDatabaseMigrationOptions>()
            .Bind(configuration.GetSection(StartupDatabaseMigrationOptions.SectionName))
            .Validate(
                StartupDatabaseMigrationOptions.IsValid,
                "Database startup migration timeouts are outside the supported safe range.")
            .ValidateOnStart();
        services.AddScoped<StartupDatabaseMigrator>();
        var platformBootstrap = configuration
            .GetSection(PlatformOwnerBootstrapOptions.SectionName)
            .Get<PlatformOwnerBootstrapOptions>() ?? new PlatformOwnerBootstrapOptions();
        PlatformOwnerBootstrapOptions.Validate(platformBootstrap);
        services.Configure<PlatformOwnerBootstrapOptions>(
            configuration.GetSection(PlatformOwnerBootstrapOptions.SectionName));
        services.Configure<OtpOptions>(configuration.GetSection(OtpOptions.SectionName));
        services.Configure<MetaWhatsAppOptions>(configuration.GetSection(MetaWhatsAppOptions.SectionName));
        var environmentName = configuration["ASPNETCORE_ENVIRONMENT"] ?? Environments.Production;
        var otpProvider = configuration["Otp:Provider"] ?? string.Empty;
        var fixedCode = configuration["Otp:DevelopmentFixedCode"];
        var allowTemporaryFixedCode = configuration.GetValue("Otp:AllowTemporaryFixedCode", false);
        var temporaryFixedCode = configuration["Otp:TemporaryFixedCode"];
        var temporaryFixedCodeExpiresAtUtc = configuration.GetValue<DateTime?>("Otp:TemporaryFixedCodeExpiresAtUtc");
        var temporaryFixedExpiryUtc = temporaryFixedCodeExpiresAtUtc.HasValue
            ? NormalizeUtc(temporaryFixedCodeExpiresAtUtc.Value)
            : (DateTime?)null;
        if (!environmentName.Equals(Environments.Development, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(fixedCode))
            throw new InvalidOperationException("Otp:DevelopmentFixedCode is forbidden outside Development.");
        if (otpProvider.Equals("Development", StringComparison.OrdinalIgnoreCase) &&
            !environmentName.Equals(Environments.Development, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("DevelopmentOtpProvider is forbidden outside Development.");
        var hmacSecret = configuration["Otp:HmacSecret"];
        if (string.IsNullOrWhiteSpace(hmacSecret) || hmacSecret.Length < 32)
            throw new InvalidOperationException("Otp:HmacSecret must be supplied by server secrets and contain at least 32 characters.");
        if (otpProvider.Equals("Development", StringComparison.OrdinalIgnoreCase) && fixedCode != "1234")
            throw new InvalidOperationException("Development OTP must use the reviewed fixed code.");
        if (otpProvider.Equals("TemporaryFixed", StringComparison.OrdinalIgnoreCase))
        {
            if (!allowTemporaryFixedCode)
                throw new InvalidOperationException("Temporary fixed OTP requires the explicit server-only allow flag.");
            if (temporaryFixedCode != "1234")
                throw new InvalidOperationException("Temporary fixed OTP must use the explicitly reviewed temporary code.");
            var nowUtc = DateTime.UtcNow;
            if (temporaryFixedExpiryUtc is null ||
                temporaryFixedExpiryUtc.Value <= nowUtc ||
                temporaryFixedExpiryUtc.Value > nowUtc.AddDays(31))
            {
                throw new InvalidOperationException("Temporary fixed OTP requires a future UTC expiry no more than 31 days away.");
            }
        }
        else if (allowTemporaryFixedCode || !string.IsNullOrWhiteSpace(temporaryFixedCode) || temporaryFixedCodeExpiresAtUtc.HasValue)
        {
            throw new InvalidOperationException("Temporary fixed OTP settings must be removed when the provider is not TemporaryFixed.");
        }
        if (otpProvider.Equals("MetaWhatsApp", StringComparison.OrdinalIgnoreCase))
        {
            var requiredMetaSecrets = new[]
            {
                "MetaWhatsApp:AccessToken",
                "MetaWhatsApp:PhoneNumberId",
                "MetaWhatsApp:BusinessAccountId",
                "MetaWhatsApp:TemplateName",
                "MetaWhatsApp:TemplateLanguage",
                "MetaWhatsApp:GraphApiVersion"
            };
            if (requiredMetaSecrets.Any(key => string.IsNullOrWhiteSpace(configuration[key])))
                throw new InvalidOperationException("Meta WhatsApp OTP settings must be supplied by server secrets.");
        }
        services.AddHttpClient<MetaWhatsAppOtpProvider>(client => client.Timeout = TimeSpan.FromSeconds(10));
        services.AddScoped<DevelopmentOtpProvider>();
        services.AddScoped<TemporaryFixedOtpProvider>();
        services.AddScoped<IOtpSender>(provider => otpProvider.ToLowerInvariant() switch
        {
            "development" => provider.GetRequiredService<DevelopmentOtpProvider>(),
            "temporaryfixed" => provider.GetRequiredService<TemporaryFixedOtpProvider>(),
            "metawhatsapp" => provider.GetRequiredService<MetaWhatsAppOtpProvider>(),
            _ => throw new InvalidOperationException("Otp:Provider must be Development, TemporaryFixed, or MetaWhatsApp.")
        });

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
                ValidAudiences = new[] { "LogicFitUsers", "LogicFitPlatform" },
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
        services.Configure<IdentityAccessOptions>(configuration.GetSection(IdentityAccessOptions.SectionName));
        services.AddScoped<IIdentityWorkspaceAccessGuard, IdentityWorkspaceAccessGuard>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDateTimeService, DateTimeService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordResetTokenService, PasswordResetTokenService>();
        var storageProvider = configuration["Storage:Provider"] ?? "local";
        if (storageProvider.Equals("r2", StringComparison.OrdinalIgnoreCase))
        {
            var serviceUrl = configuration["Storage:R2:ServiceUrl"];
            var accessKey = configuration["Storage:R2:AccessKey"];
            var secretKey = configuration["Storage:R2:SecretKey"];
            if (string.IsNullOrWhiteSpace(serviceUrl) || string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
                throw new InvalidOperationException("Storage is configured for R2 but Storage:R2:ServiceUrl, AccessKey and SecretKey are missing.");

            services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(accessKey, secretKey, new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                AuthenticationRegion = configuration["Storage:R2:Region"] ?? "auto",
                ForcePathStyle = true
            }));
            services.AddScoped<IFileUploadService, R2FileUploadService>();
        }
        else
        {
            services.AddScoped<IFileUploadService, FileUploadService>();
        }
        services.Configure<SmtpEmailOptions>(configuration.GetSection(SmtpEmailOptions.SectionName));
        services.Configure<IdentityEmailLinkOptions>(configuration.GetSection(IdentityEmailLinkOptions.SectionName));
        services.AddSingleton<IIdentityEmailLinkFactory, IdentityEmailLinkFactory>();
        if (string.Equals(configuration["Email:Provider"], "smtp", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        else
            services.AddScoped<IEmailSender, UnconfiguredEmailSender>();
        services.AddScoped<IEmailService, LoggingEmailService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IBackupService, DatabaseBackupService>();
        services.AddSingleton<IMediaBackupService, LocalMediaBackupService>();

        if (configuration.GetValue("Backup:Enabled", false))
            services.AddHostedService<DailyBackupHostedService>();

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
            services.AddHostedService<OutboxProcessorService>();
        }

        return services;
    }

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
