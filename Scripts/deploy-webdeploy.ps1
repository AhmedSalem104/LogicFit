[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $PublishSettingsPath,
    [Parameter(Mandatory = $true)] [string] $ContentPath,
    [string] $MsDeployPath = "C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe",
    [string] $HealthCheckUrl,
    [switch] $ApplyMigrations,
    [string] $VerifiedBackupReference,
    [string] $MigrationScriptPath,
    [switch] $ApproveDestructiveMigrationReview,
    [string] $MigrationConnectionEnvironmentVariable = "LOGICFIT_PRODUCTION_DB_CONNECTION",
    [string] $MigrationProject = (Join-Path $PSScriptRoot "..\LogicFit.Infrastructure\LogicFit.Infrastructure.csproj"),
    [string] $StartupProject = (Join-Path $PSScriptRoot "..\LogicFit.API\LogicFit.API.csproj"),
    [ValidateSet("Debug", "Release")] [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $PublishSettingsPath)) { throw "Publish settings file not found" }
if (-not (Test-Path -LiteralPath $ContentPath)) { throw "Publish content path not found" }
if (-not (Test-Path -LiteralPath $MsDeployPath)) { throw "MSDeploy executable not found" }

if ($ApplyMigrations) {
    if ([string]::IsNullOrWhiteSpace($VerifiedBackupReference)) {
        throw "-ApplyMigrations requires -VerifiedBackupReference for a backup verified before deployment."
    }

    if ($VerifiedBackupReference.Contains("`r") -or $VerifiedBackupReference.Contains("`n")) {
        throw "Verified backup reference must be a single line."
    }

    if ([string]::IsNullOrWhiteSpace($HealthCheckUrl)) {
        throw "-ApplyMigrations requires -HealthCheckUrl so the rollout can be verified."
    }

    if ([string]::IsNullOrWhiteSpace($MigrationScriptPath)) {
        throw "-ApplyMigrations requires -MigrationScriptPath for the reviewed idempotent SQL."
    }

    if ($MigrationConnectionEnvironmentVariable -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') {
        throw "Migration connection environment variable name is invalid."
    }

    $migrationConnectionString = [Environment]::GetEnvironmentVariable($MigrationConnectionEnvironmentVariable)
    if ([string]::IsNullOrWhiteSpace($migrationConnectionString)) {
        throw "The protected migration connection environment variable is missing."
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
    # Migrations run as an explicit deployment operation, never from the IIS startup path.
    # The protected connection is copied into the explicit EF operator variable only for the
    # lifetime of this process and is never printed. ApplicationDbContextFactory otherwise
    # remains isolated on its local design-time database.
    $hadAspNetCoreEnvironment = Test-Path Env:ASPNETCORE_ENVIRONMENT
    $previousAspNetCoreEnvironment = $env:ASPNETCORE_ENVIRONMENT
    $hadEfConnectionEnvironment = Test-Path Env:LOGICFIT_EF_CONNECTION_STRING
    $previousEfConnectionEnvironment = $env:LOGICFIT_EF_CONNECTION_STRING

    try {
        $env:ASPNETCORE_ENVIRONMENT = "Production"
        $env:LOGICFIT_EF_CONNECTION_STRING = $migrationConnectionString

        & dotnet ef --version *> $null
        if ($LASTEXITCODE -ne 0) { throw "dotnet-ef 8.x must be installed before applying migrations." }

        if (-not (Test-Path -LiteralPath $MigrationScriptPath)) { throw "Reviewed migration script was not found" }
        if ((Get-Item -LiteralPath $MigrationScriptPath).Length -le 0) { throw "Reviewed migration script is empty" }

        $migrationScript = Get-Content -LiteralPath $MigrationScriptPath -Raw
        if ($migrationScript -notmatch '__EFMigrationsHistory') {
            throw "Migration script is not an EF idempotent migration script."
        }

        $containsDestructiveSql = $migrationScript -match '(?im)\b(DROP\s+(TABLE|COLUMN|DATABASE)|TRUNCATE\s+TABLE|DELETE\s+FROM\s+(?!\[?__EFMigrationsHistory\]?))'
        if ($containsDestructiveSql -and -not $ApproveDestructiveMigrationReview) {
            throw "Migration script contains destructive SQL. Review it and pass -ApproveDestructiveMigrationReview explicitly."
        }

        $scriptHash = (Get-FileHash -LiteralPath $MigrationScriptPath -Algorithm SHA256).Hash
        Write-Host "Applying reviewed idempotent migration plan (SHA256: $scriptHash)."

        & dotnet ef database update --project $MigrationProject --startup-project $StartupProject --configuration $Configuration --context LogicFit.Infrastructure.Persistence.ApplicationDbContext --no-build
        if ($LASTEXITCODE -ne 0) { throw "Database migration failed with exit code $LASTEXITCODE" }

        $migrationListOutput = & dotnet ef migrations list --project $MigrationProject --startup-project $StartupProject --configuration $Configuration --context LogicFit.Infrastructure.Persistence.ApplicationDbContext --no-build 2>&1
        if ($LASTEXITCODE -ne 0) { throw "Post-migration history verification failed with exit code $LASTEXITCODE" }
        if (($migrationListOutput -join "`n") -match '(?i)\(Pending\)') {
            throw "Database migration verification found pending migrations."
        }

        Write-Host "Database migrations completed and no pending EF migrations remain."
    }
    finally {
        if ($hadAspNetCoreEnvironment) { $env:ASPNETCORE_ENVIRONMENT = $previousAspNetCoreEnvironment }
        else { Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue }

        if ($hadEfConnectionEnvironment) { $env:LOGICFIT_EF_CONNECTION_STRING = $previousEfConnectionEnvironment }
        else { Remove-Item Env:LOGICFIT_EF_CONNECTION_STRING -ErrorAction SilentlyContinue }
    }
}

$destination = "https://$($profile.publishUrl):8172/msdeploy.axd?site=$($profile.msdeploySite)"
$arguments = @(
    '-verb:sync',
    "-source:contentPath=`"$ContentPath`"",
    # MonsterASP delegates only the site's contentPath to the site account. Using
    # dest:auto makes Web Deploy probe linked IIS providers that the account cannot access.
    "-dest:contentPath=$($profile.msdeploySite),ComputerName=$destination,UserName=$($profile.userName),Password=$($profile.userPWD),AuthType=Basic,includeAcls=False",
    '-disableLink:AppPoolExtension',
    '-disableLink:ContentExtension',
    '-disableLink:CertificateExtension',
    # Release the running ASP.NET Core files while syncing, then remove the
    # temporary app_offline.htm after the deployment completes.
    '-enableRule:AppOffline',
    # Keep server-only secrets and production overrides, including appsettings.Production.json.
    '-enableRule:DoNotDeleteRule',
    # DoNotDeleteRule only prevents deletion; this skip also prevents an artifact-local file
    # from overwriting the protected server configuration when a developer has one locally.
    '-skip:objectName=filePath,absolutePath=appsettings\.Production\.json$',
    # The database is authoritative for Data Protection keys, but preserve the App_Data mirror
    # so rollback/recovery never removes a key ring from the deployed site.
    '-skip:objectName=dirPath,absolutePath=App_Data\\DataProtection-Keys$',
    '-skip:objectName=filePath,absolutePath=App_Data\\DataProtection-Keys\\.*$',
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
