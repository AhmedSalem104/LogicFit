[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $PublishSettingsPath,
    [Parameter(Mandatory = $true)] [string] $ContentPath,
    [string] $MsDeployPath = "C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe",
    [string] $HealthCheckUrl
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $PublishSettingsPath)) { throw "Publish settings file not found" }
if (-not (Test-Path -LiteralPath $ContentPath)) { throw "Publish content path not found" }
if (-not (Test-Path -LiteralPath $MsDeployPath)) { throw "MSDeploy executable not found" }

[xml] $settings = Get-Content -LiteralPath $PublishSettingsPath -Raw -Encoding UTF8
$profile = $settings.publishData.publishProfile | Select-Object -First 1
if ($null -eq $profile -or $profile.publishMethod -ne "MSDeploy") { throw "The publish settings file is not an MSDeploy profile" }
if ([string]::IsNullOrWhiteSpace($profile.publishUrl) -or [string]::IsNullOrWhiteSpace($profile.msdeploySite)) { throw "MSDeploy host/site is missing" }
if ([string]::IsNullOrWhiteSpace($profile.userName) -or [string]::IsNullOrWhiteSpace($profile.userPWD)) { throw "MSDeploy credentials are missing" }

$destination = "https://$($profile.publishUrl):8172/msdeploy.axd?site=$($profile.msdeploySite)"
$arguments = @(
    '-verb:sync',
    "-source:contentPath=`"$ContentPath`"",
    "-dest:auto,ComputerName=$destination,UserName=$($profile.userName),Password=$($profile.userPWD),AuthType=Basic",
    '-enableLink:AppPoolExtension',
    # Keep server-only secrets and production overrides, including appsettings.Production.json.
    '-enableRule:DoNotDeleteRule',
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
