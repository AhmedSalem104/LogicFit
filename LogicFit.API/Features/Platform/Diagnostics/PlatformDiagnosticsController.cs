using System.Reflection;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Platform.Diagnostics;

[ApiController]
[Route("api/platform/diagnostics")]
[Authorize(Policy = Permissions.ManagePlatformReports)]
public sealed class PlatformDiagnosticsController(IConfiguration configuration, IHostEnvironment environment) : ControllerBase
{
    [HttpGet("version")]
    public ActionResult<PlatformVersionDiagnosticsDto> Version()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(PlatformDiagnosticsController).Assembly;
        var buildSha = configuration["Build:Sha"]
            ?? Environment.GetEnvironmentVariable("BUILD_SHA")
            ?? Environment.GetEnvironmentVariable("GITHUB_SHA");

        return Ok(new PlatformVersionDiagnosticsDto
        {
            ApiContractVersion = configuration["Api:ContractVersion"] ?? "v1",
            BuildSha = string.IsNullOrWhiteSpace(buildSha) ? "unknown" : buildSha[..Math.Min(buildSha.Length, 12)],
            AssemblyVersion = assembly.GetName().Version?.ToString() ?? "unknown",
            Environment = environment.EnvironmentName,
            Runtime = Environment.Version.ToString()
        });
    }
}

public sealed class PlatformVersionDiagnosticsDto
{
    public string ApiContractVersion { get; init; } = "v1";
    public string BuildSha { get; init; } = "unknown";
    public string AssemblyVersion { get; init; } = "unknown";
    public string Environment { get; init; } = string.Empty;
    public string Runtime { get; init; } = string.Empty;
}
