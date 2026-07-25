using LogicFit.API.Middleware;
using LogicFit.Application;
using LogicFit.Infrastructure;
using LogicFit.Infrastructure.Persistence;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Serilog;
using LogicFit.Platform.API.Features.Auth;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers()
    .AddApplicationPart(typeof(PlatformAuthController).Assembly);
builder.Services.AddHttpContextAccessor();

builder.Services.AddRateLimiter(options =>
{
    var permitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 120);
    var windowSeconds = builder.Configuration.GetValue("RateLimiting:WindowSeconds", 60);
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User?.Identity?.IsAuthenticated == true
                ? context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
                : context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

// Short-lived distributed cache backing the tenant-access gate. In-memory today; swap to
// AddStackExchangeRedisCache for multi-instance scale (config only — no code change).
builder.Services.AddDistributedMemoryCache();
// Emits a typed TENANT_PENDING_APPROVAL body when the tenant authorization requirement fails.
builder.Services.AddSingleton<
    Microsoft.AspNetCore.Authorization.IAuthorizationMiddlewareResultHandler,
    LogicFit.API.Authorization.TenantAuthorizationResultHandler>();

// Health checks (readiness includes a DB connectivity probe).
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");

// Swagger configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LogicFit API",
        Version = "v1",
        Description = "Smart Trainer SaaS API for Gym Management"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Just paste your token (without 'Bearer' prefix).",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS — locked down by configuration. In production, list allowed origins in
// Cors:AllowedOrigins; an empty list outside Development means no cross-origin access.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? Array.Empty<string>();

        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// Seed data on startup
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();

    // Check if force reset of foods is requested (to fix identity issues)
    var resetFoods = Environment.GetEnvironmentVariable("RESET_FOODS");
    if (!string.IsNullOrEmpty(resetFoods) && resetFoods.Equals("true", StringComparison.OrdinalIgnoreCase))
    {
        await seeder.ForceResetFoodsAsync();
    }

    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LogicFit API v1");
    });
}

app.UseExceptionHandling();

app.UseHttpsRedirection();

// Ensure uploads directory exists
var uploadsPath = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
    Log.Information("Created uploads directory: {UploadsPath}", uploadsPath);
}

// Enable static files for file uploads
app.UseStaticFiles();

app.UseCors("AppCors");
app.UseRateLimiter();

app.UseAuthentication();

// Tenant must be resolved BEFORE authorization so permission checks and query filters
// see the current tenant.
app.UseTenant();

// Hard gate: block requests for suspended/expired/cancelled/archived gyms before authorization.
app.UseTenantAccessGate();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
