[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $PublishSettingsPath,
    [Parameter(Mandatory = $true)] [string] $ExpectedSite,
    [Parameter(Mandatory = $true)] [string] $ExpectedConnectionString,
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
$remoteConfigPath = "$ExpectedSite/appsettings.Production.json"

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
}
finally {
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    $resolvedOperationRoot = [IO.Path]::GetFullPath($operationRoot)
    if ($resolvedOperationRoot.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedOperationRoot)) {
        Remove-Item -LiteralPath $resolvedOperationRoot -Recurse -Force
    }
}
