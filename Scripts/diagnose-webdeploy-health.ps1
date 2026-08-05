[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $PublishSettingsPath,
    [Parameter(Mandatory = $true)] [string] $ExpectedSite,
    [Parameter(Mandatory = $true)] [string] $ExpectedConnectionString,
    [string] $HealthCheckUrl,
    [string] $ManagementHostOverride,
    [switch] $AllowUntrustedManagementCertificate,
    [string] $MsDeployPath = "C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $PublishSettingsPath -PathType Leaf)) { throw "Publish settings file not found." }
if (-not (Test-Path -LiteralPath $MsDeployPath -PathType Leaf)) { throw "MSDeploy executable not found." }
if ($ExpectedSite -notmatch '^site[0-9]+$') { throw "Expected site must use the site12345 format." }
if ([string]::IsNullOrWhiteSpace($ExpectedConnectionString)) { throw "Protected production connection is required." }

[xml] $settings = Get-Content -LiteralPath $PublishSettingsPath -Raw -Encoding UTF8
$profile = $settings.publishData.publishProfile | Where-Object { $_.publishMethod -eq "MSDeploy" } | Select-Object -First 1
if ($null -eq $profile) { throw "The file has no MSDeploy profile." }
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
$operationRoot = Join-Path ([IO.Path]::GetTempPath()) ("logicfit-diagnosis-{0}" -f [Guid]::NewGuid().ToString("N"))
$remoteConfig = Join-Path $operationRoot "appsettings.Production.remote.json"
$remoteWebConfig = Join-Path $operationRoot "web.config.remote"
$remoteConfigPath = "$ExpectedSite/appsettings.Production.json"
$remoteWebConfigPath = "$ExpectedSite/web.config"

function Invoke-MsDeploy([string[]] $Arguments) {
    $effectiveArguments = @($Arguments)
    if ($AllowUntrustedManagementCertificate) { $effectiveArguments += '-allowUntrusted' }
    # Capture all tool output because it can contain the protected management endpoint.
    $ignoredOutput = & $MsDeployPath @effectiveArguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Read-only MSDeploy capture failed with exit code $LASTEXITCODE." }
}

function Get-RemoteFile([string] $RemotePath, [string] $LocalPath) {
    $source = "-source:contentPath=$RemotePath,ComputerName=$endpoint,UserName=$($profile.userName),Password=$($profile.userPWD),AuthType=Basic"
    $destination = "-dest:contentPath=$LocalPath"
    Invoke-MsDeploy @('-verb:sync', $source, $destination, '-retryAttempts:2', '-retryInterval:2000')
}

function Get-ConnectionIdentity([string] $ConnectionString) {
    try {
        $builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($ConnectionString)
        return [pscustomobject]@{
            DataSource = $builder.DataSource.Trim()
            InitialCatalog = $builder.InitialCatalog.Trim()
            UserId = $builder.UserID.Trim()
        }
    }
    catch {
        throw "A protected or remote database connection string is invalid."
    }
}

New-Item -ItemType Directory -Path $operationRoot | Out-Null
try {
    Get-RemoteFile $remoteConfigPath $remoteConfig
    Get-RemoteFile $remoteWebConfigPath $remoteWebConfig

    [xml]$webConfig = Get-Content -LiteralPath $remoteWebConfig -Raw
    $aspNetCore = $webConfig.SelectSingleNode('//aspNetCore')
    if ($null -eq $aspNetCore) {
        throw "Remote web.config has no aspNetCore entry."
    }
    $stdoutSetting = [string]$aspNetCore.GetAttribute('stdoutLogEnabled')
    $stdoutEnabled = $stdoutSetting.Equals('true', [StringComparison]::OrdinalIgnoreCase)
    $hostingModelConfigured = -not [string]::IsNullOrWhiteSpace([string]$aspNetCore.GetAttribute('hostingModel'))
    Write-Host "Remote IIS aspNetCore metadata is present: True."
    Write-Host "Remote IIS stdout logging enabled: $stdoutEnabled."
    Write-Host "Remote IIS hosting model is configured: $hostingModelConfigured."
    if (-not [string]::IsNullOrWhiteSpace($HealthCheckUrl)) {
        $healthUri = [Uri]$HealthCheckUrl
        if ($healthUri.Scheme -ne 'https') { throw "Diagnostic health URL must use HTTPS." }
        $profileHostMatchesHealthHost = [StringComparer]::OrdinalIgnoreCase.Equals($managementHost, $healthUri.Host)
        Write-Host "Profile management host equals configured health host: $profileHostMatchesHealthHost."
        $destinationAppUrl = [string]$profile.destinationAppUrl
        if ([string]::IsNullOrWhiteSpace($destinationAppUrl)) {
            Write-Host "Publish profile destination app URL is present: False."
        }
        else {
            try {
                $destinationUri = [Uri]$destinationAppUrl
                $destinationHostMatchesHealthHost =
                    [StringComparer]::OrdinalIgnoreCase.Equals($destinationUri.Host, $healthUri.Host)
                Write-Host "Publish profile destination app URL matches configured health host: $destinationHostMatchesHealthHost."
            }
            catch {
                Write-Host "Publish profile destination app URL is present but invalid: True."
            }
        }
    }

    $configuration = Get-Content -LiteralPath $remoteConfig -Raw | ConvertFrom-Json
    $remoteConnectionString = [string]$configuration.ConnectionStrings.DefaultConnection
    if ([string]::IsNullOrWhiteSpace($remoteConnectionString)) {
        throw "Remote production database connection is missing."
    }

    $remoteIdentity = Get-ConnectionIdentity $remoteConnectionString
    $protectedIdentity = Get-ConnectionIdentity $ExpectedConnectionString
    $targetMatches = [StringComparer]::OrdinalIgnoreCase.Equals($remoteIdentity.DataSource, $protectedIdentity.DataSource) -and
        [StringComparer]::OrdinalIgnoreCase.Equals($remoteIdentity.InitialCatalog, $protectedIdentity.InitialCatalog)
    $principalMatches = [StringComparer]::OrdinalIgnoreCase.Equals($remoteIdentity.UserId, $protectedIdentity.UserId)

    Write-Host "Remote database target matches the protected target: $targetMatches."
    Write-Host "Remote database principal matches the protected principal: $principalMatches."
    if (-not $targetMatches -or -not $principalMatches) {
        throw "Remote server configuration does not match the protected production database identity."
    }

    $remoteConnection = [System.Data.SqlClient.SqlConnection]::new($remoteConnectionString)
    try {
        $remoteConnection.Open()
        $command = $remoteConnection.CreateCommand()
        $command.CommandText = 'SELECT 1'
        $command.CommandTimeout = 15
        $result = $command.ExecuteScalar()
        if ([int]$result -ne 1) {
            throw "Unexpected read-only probe result."
        }
        Write-Host "Remote server-configuration database connectivity probe passed."
    }
    catch {
        throw "Remote server-configuration database connectivity probe failed."
    }
    finally {
        $remoteConnection.Dispose()
    }
}
finally {
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    $resolvedOperationRoot = [IO.Path]::GetFullPath($operationRoot)
    if ($resolvedOperationRoot.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedOperationRoot)) {
        Remove-Item -LiteralPath $resolvedOperationRoot -Recurse -Force
    }
}
