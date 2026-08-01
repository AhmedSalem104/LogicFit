[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $PublishSettingsPath,
    [Parameter(Mandatory = $true)] [string] $ContentPath,
    [string] $MsDeployPath = "C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe",
    [switch] $ApplyMigrations,
    [string] $VerifiedBackupPath,
    [string] $HealthCheckUrl,
    [string] $MigrationProject = (Join-Path $PSScriptRoot "..\LogicFit.Infrastructure\LogicFit.Infrastructure.csproj"),
    [string] $StartupProject = (Join-Path $PSScriptRoot "..\LogicFit.API\LogicFit.API.csproj"),
    [ValidateSet("Debug", "Release")] [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $PublishSettingsPath)) { throw "Publish settings file not found" }
if (-not (Test-Path -LiteralPath $ContentPath)) { throw "Publish content path not found" }
if (-not (Test-Path -LiteralPath $MsDeployPath)) { throw "MSDeploy executable not found" }

if ($ApplyMigrations) {
    if ([string]::IsNullOrWhiteSpace($VerifiedBackupPath) -or -not (Test-Path -LiteralPath $VerifiedBackupPath)) {
        throw "-ApplyMigrations requires a non-empty -VerifiedBackupPath created before deployment."
    }

    if ((Get-Item -LiteralPath $VerifiedBackupPath).Length -le 0) {
        throw "The verified backup file is empty."
    }

    if ([string]::IsNullOrWhiteSpace($HealthCheckUrl)) {
        throw "-ApplyMigrations requires -HealthCheckUrl so the rollout can be verified."
    }

    if (-not (Test-Path -LiteralPath $MigrationProject)) { throw "Migration project not found" }
    if (-not (Test-Path -LiteralPath $StartupProject)) { throw "Migration startup project not found" }
}

[xml] $settings = Get-Content -LiteralPath $PublishSettingsPath -Raw -Encoding UTF8
$profile = $settings.publishData.publishProfile | Select-Object -First 1
if ($null -eq $profile -or $profile.publishMethod -ne "MSDeploy") { throw "The publish settings file is not an MSDeploy profile" }
if ([string]::IsNullOrWhiteSpace($profile.publishUrl) -or [string]::IsNullOrWhiteSpace($profile.msdeploySite)) { throw "MSDeploy host/site is missing" }
if ([string]::IsNullOrWhiteSpace($profile.userName) -or [string]::IsNullOrWhiteSpace($profile.userPWD)) { throw "MSDeploy credentials are missing" }

if ($ApplyMigrations) {
    # Keep migrations out of ASP.NET startup. The deploy operator applies them as a
    # separate, reviewed step, so IIS cannot restart-loop while a migration is running.
    $previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
    $migrationScriptPath = Join-Path ([System.IO.Path]::GetTempPath()) ("logicfit-migrations-{0}.sql" -f [Guid]::NewGuid().ToString("N"))

    try {
        $env:ASPNETCORE_ENVIRONMENT = "Production"

        & dotnet ef migrations script --idempotent --project $MigrationProject --startup-project $StartupProject --configuration $Configuration --no-build --output $migrationScriptPath
        if ($LASTEXITCODE -ne 0) { throw "Idempotent migration script generation failed with exit code $LASTEXITCODE" }

        $migrationScript = Get-Content -LiteralPath $migrationScriptPath -Raw
        if ($migrationScript -match '(?im)^\s*(DROP\s+(TABLE|COLUMN)|DELETE\s+FROM\s+\[(?!__EFMigrationsHistory\]))') {
            throw "Generated migration script contains an unsafe destructive statement. Review it before deployment."
        }

        & dotnet ef database update --project $MigrationProject --startup-project $StartupProject --configuration $Configuration --no-build
        if ($LASTEXITCODE -ne 0) { throw "Database migration failed with exit code $LASTEXITCODE" }

        Write-Host "Database migrations completed before WebDeploy. Reviewed script: $migrationScriptPath"
    }
    finally {
        $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    }
}

$destination = "https://$($profile.publishUrl):8172/msdeploy.axd?site=$($profile.msdeploySite)"
$arguments = @(
    '-verb:sync',
    "-source:contentPath=`"$ContentPath`"",
    "-dest:auto,ComputerName=$destination,UserName=$($profile.userName),Password=$($profile.userPWD),AuthType=Basic",
    '-enableLink:AppPoolExtension',
    '-retryAttempts:3',
    '-retryInterval:5000'
)

# Do not log the argument list: it contains the publish password.
& $MsDeployPath @arguments
if ($LASTEXITCODE -ne 0) { throw "MSDeploy failed with exit code $LASTEXITCODE" }

if (-not [string]::IsNullOrWhiteSpace($HealthCheckUrl)) {
    $healthResponse = Invoke-WebRequest -Uri $HealthCheckUrl -UseBasicParsing -TimeoutSec 30
    if ($healthResponse.StatusCode -lt 200 -or $healthResponse.StatusCode -ge 300) {
        throw "Deployment completed but the health check returned HTTP $($healthResponse.StatusCode)."
    }

    Write-Host "Health check passed: HTTP $($healthResponse.StatusCode)"
}
