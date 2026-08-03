[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $PublishSettingsPath,
    [Parameter(Mandatory = $true)] [string] $ExpectedSite,
    [Parameter(Mandatory = $true)] [string] $HealthCheckUrl,
    [string] $ManagementHostOverride,
    [switch] $AllowUntrustedManagementCertificate,
    [string] $MsDeployPath = "C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $PublishSettingsPath -PathType Leaf)) { throw "Publish settings file not found." }
if (-not (Test-Path -LiteralPath $MsDeployPath -PathType Leaf)) { throw "MSDeploy executable not found." }
if ($ExpectedSite -notmatch '^site[0-9]+$') { throw "Expected site must use the site12345 format." }

$healthUri = [Uri] $HealthCheckUrl
if ($healthUri.Scheme -ne "https") { throw "Recovery health URL must use HTTPS." }

$jwtSecret = $env:LOGICFIT_JWT_SECRET
$passwordResetSecret = $env:LOGICFIT_PASSWORD_RESET_SECRET
if ([string]::IsNullOrWhiteSpace($jwtSecret) -or $jwtSecret.Length -lt 32) {
    throw "LOGICFIT_JWT_SECRET must be supplied by the protected environment."
}
if ([string]::IsNullOrWhiteSpace($passwordResetSecret) -or $passwordResetSecret.Length -lt 32) {
    throw "LOGICFIT_PASSWORD_RESET_SECRET must be supplied by the protected environment."
}

[xml] $settings = Get-Content -LiteralPath $PublishSettingsPath -Raw -Encoding UTF8
$profile = $settings.publishData.publishProfile | Select-Object -First 1
if ($null -eq $profile -or $profile.publishMethod -ne "MSDeploy") { throw "The file is not an MSDeploy profile." }
if ($profile.msdeploySite -ne $ExpectedSite) {
    throw "Protected profile site '$($profile.msdeploySite)' does not match expected site '$ExpectedSite'."
}
if ([string]::IsNullOrWhiteSpace($profile.userName) -or [string]::IsNullOrWhiteSpace($profile.userPWD)) {
    throw "MSDeploy credentials are missing."
}

$managementHost = if ([string]::IsNullOrWhiteSpace($ManagementHostOverride)) {
    $profile.publishUrl
} else {
    $ManagementHostOverride
}
if ($managementHost -notmatch '^[A-Za-z0-9.-]+$') { throw "Management host is invalid." }

$endpoint = "https://${managementHost}:8172/msdeploy.axd?site=$ExpectedSite"
$operationRoot = Join-Path ([IO.Path]::GetTempPath()) ("logicfit-recovery-{0}" -f [Guid]::NewGuid().ToString("N"))
$remoteConfig = Join-Path $operationRoot "appsettings.Production.original.json"
$remoteWebConfig = Join-Path $operationRoot "web.config.original"
$recoveryConfig = Join-Path $operationRoot "appsettings.Production.recovery.json"
$restartWebConfig = Join-Path $operationRoot "web.config.restart"
$remoteConfigPath = "$ExpectedSite/appsettings.Production.json"
$remoteWebConfigPath = "$ExpectedSite/web.config"

function Invoke-MsDeploy([string[]] $Arguments) {
    $effectiveArguments = @($Arguments)
    if ($AllowUntrustedManagementCertificate) { $effectiveArguments += '-allowUntrusted' }
    # Never print the argument list: the destination contains the protected profile password.
    & $MsDeployPath @effectiveArguments
    if ($LASTEXITCODE -ne 0) { throw "MSDeploy failed with exit code $LASTEXITCODE." }
}

function Get-RemoteFile([string] $RemotePath, [string] $LocalPath) {
    $source = "-source:contentPath=$RemotePath,ComputerName=$endpoint,UserName=$($profile.userName),Password=$($profile.userPWD),AuthType=Basic"
    $destination = "-dest:contentPath=$LocalPath"
    Invoke-MsDeploy @('-verb:sync', $source, $destination, '-retryAttempts:2', '-retryInterval:2000')
}

function Set-RemoteFile([string] $LocalPath, [string] $RemotePath) {
    $source = "-source:contentPath=$LocalPath"
    $destination = "-dest:contentPath=$RemotePath,ComputerName=$endpoint,UserName=$($profile.userName),Password=$($profile.userPWD),AuthType=Basic"
    Invoke-MsDeploy @('-verb:sync', $source, $destination, '-enableRule:DoNotDeleteRule', '-retryAttempts:2', '-retryInterval:2000')
}

function Test-RecoveryHealth {
    try {
        $response = Invoke-WebRequest -Uri $HealthCheckUrl -UseBasicParsing -TimeoutSec 20
        return $response.StatusCode -eq 200 -and $response.Content.Trim() -eq "Healthy"
    } catch {
        return $false
    }
}

function Wait-RecoveryHealth {
    for ($attempt = 1; $attempt -le 10; $attempt++) {
        Start-Sleep -Seconds 4
        if (Test-RecoveryHealth) {
            Write-Host "Recovery health check passed on attempt $attempt."
            return $true
        }
    }
    return $false
}

New-Item -ItemType Directory -Path $operationRoot | Out-Null
$remoteChanged = $false
try {
    Get-RemoteFile $remoteConfigPath $remoteConfig
    Get-RemoteFile $remoteWebConfigPath $remoteWebConfig

    $configuration = Get-Content -LiteralPath $remoteConfig -Raw | ConvertFrom-Json
    if ($null -eq $configuration.JwtSettings) { throw "Remote production configuration has no JwtSettings section." }
    if ($null -eq $configuration.ConnectionStrings -or
        [string]::IsNullOrWhiteSpace($configuration.ConnectionStrings.DefaultConnection)) {
        throw "Remote production database connection is missing."
    }

    $configuration.JwtSettings.Secret = $jwtSecret

    if ($null -eq $configuration.PasswordReset) {
        $configuration | Add-Member -NotePropertyName PasswordReset -NotePropertyValue ([pscustomobject]@{})
    }
    $configuration.PasswordReset | Add-Member -NotePropertyName Secret -NotePropertyValue $passwordResetSecret -Force

    if ($null -eq $configuration.Serilog) {
        $configuration | Add-Member -NotePropertyName Serilog -NotePropertyValue ([pscustomobject]@{})
    }
    if ($configuration.Serilog.MinimumLevel -is [string]) {
        $defaultLevel = $configuration.Serilog.MinimumLevel
        $configuration.Serilog.MinimumLevel = [pscustomobject]@{ Default = $defaultLevel }
    } elseif ($null -eq $configuration.Serilog.MinimumLevel) {
        $configuration.Serilog | Add-Member -NotePropertyName MinimumLevel -NotePropertyValue ([pscustomobject]@{})
    }
    if ($null -eq $configuration.Serilog.MinimumLevel.Override) {
        $configuration.Serilog.MinimumLevel | Add-Member -NotePropertyName Override -NotePropertyValue ([pscustomobject]@{})
    }
    $configuration.Serilog.MinimumLevel.Override | Add-Member `
        -NotePropertyName 'LogicFit.Application.Common.Behaviors.UnhandledExceptionBehavior' `
        -NotePropertyValue "Fatal" `
        -Force

    $serialized = $configuration | ConvertTo-Json -Depth 20
    [IO.File]::WriteAllText($recoveryConfig, $serialized, [Text.UTF8Encoding]::new($false))

    $webConfigText = Get-Content -LiteralPath $remoteWebConfig -Raw
    if ($webConfigText -notmatch '</configuration>') { throw "Remote web.config is invalid." }
    $restartMarker = "<!-- LogicFit Issue 137 recovery restart $([Guid]::NewGuid().ToString('N')) -->"
    $restartText = $webConfigText.Replace('</configuration>', "$restartMarker`r`n</configuration>")
    [IO.File]::WriteAllText($restartWebConfig, $restartText, [Text.UTF8Encoding]::new($false))

    Write-Host "Captured protected configuration SHA256: $((Get-FileHash $remoteConfig -Algorithm SHA256).Hash)"
    Write-Host "Applying recovery configuration to verified site $ExpectedSite."
    Set-RemoteFile $recoveryConfig $remoteConfigPath
    $remoteChanged = $true
    Set-RemoteFile $restartWebConfig $remoteWebConfigPath

    if (-not (Wait-RecoveryHealth)) { throw "Recovery health check did not pass." }

    # Remove the diagnostic restart marker and verify one more application recycle.
    Set-RemoteFile $remoteWebConfig $remoteWebConfigPath
    if (-not (Wait-RecoveryHealth)) { throw "Health failed after restoring the original web.config." }

    $remoteChanged = $false
    Write-Host "Production startup recovery completed without changing the database or application binary."
}
catch {
    if ($remoteChanged) {
        Write-Warning "Recovery failed; restoring the captured production configuration and web.config."
        try { Set-RemoteFile $remoteConfig $remoteConfigPath } catch { Write-Warning "Production configuration rollback failed and needs operator intervention." }
        try { Set-RemoteFile $remoteWebConfig $remoteWebConfigPath } catch { Write-Warning "web.config rollback failed and needs operator intervention." }
    }
    throw
}
finally {
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    $resolvedOperationRoot = [IO.Path]::GetFullPath($operationRoot)
    if ($resolvedOperationRoot.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedOperationRoot)) {
        Remove-Item -LiteralPath $resolvedOperationRoot -Recurse -Force
    }
}
