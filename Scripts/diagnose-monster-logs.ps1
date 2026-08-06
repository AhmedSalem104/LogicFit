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

$healthUri = [Uri]$HealthCheckUrl
if ($healthUri.Scheme -ne 'https') { throw "Diagnostic health URL must use HTTPS." }

[xml]$settings = Get-Content -LiteralPath $PublishSettingsPath -Raw -Encoding UTF8
$profile = $settings.publishData.publishProfile |
    Where-Object { $_.publishMethod -eq 'MSDeploy' } |
    Select-Object -First 1
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
$operationRoot = Join-Path ([IO.Path]::GetTempPath()) ("logicfit-monster-log-{0}" -f [Guid]::NewGuid().ToString('N'))
$remoteWebConfig = Join-Path $operationRoot 'web.config.original'
$diagnosticWebConfig = Join-Path $operationRoot 'web.config.diagnostic'
$logRoot = Join-Path $operationRoot 'captured-logs'
$remoteWebConfigPath = "$ExpectedSite/web.config"

function Invoke-MsDeploy([string[]]$Arguments) {
    $effectiveArguments = @($Arguments)
    if ($AllowUntrustedManagementCertificate) { $effectiveArguments += '-allowUntrusted' }
    # Never emit tool output: it may contain the protected management endpoint.
    $ignoredOutput = & $MsDeployPath @effectiveArguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "MSDeploy operation failed with exit code $LASTEXITCODE." }
}

function Get-RemoteFile([string]$RemotePath, [string]$LocalPath) {
    $source = "-source:contentPath=$RemotePath,ComputerName=$endpoint,UserName=$($profile.userName),Password=$($profile.userPWD),AuthType=Basic"
    $destination = "-dest:contentPath=$LocalPath"
    Invoke-MsDeploy @('-verb:sync', $source, $destination, '-retryAttempts:2', '-retryInterval:2000')
}

function Set-RemoteFile([string]$LocalPath, [string]$RemotePath) {
    $source = "-source:contentPath=$LocalPath"
    $destination = "-dest:contentPath=$RemotePath,ComputerName=$endpoint,UserName=$($profile.userName),Password=$($profile.userPWD),AuthType=Basic"
    Invoke-MsDeploy @('-verb:sync', $source, $destination, '-enableRule:DoNotDeleteRule', '-retryAttempts:2', '-retryInterval:2000')
}

function Get-HealthState {
    try {
        $response = Invoke-WebRequest -Uri $HealthCheckUrl -UseBasicParsing -TimeoutSec 20
        return [pscustomobject]@{
            Status = [int]$response.StatusCode
            Healthy = $response.StatusCode -eq 200 -and $response.Content.Trim() -eq 'Healthy'
        }
    }
    catch {
        $status = $null
        if ($null -ne $_.Exception.Response) { $status = $_.Exception.Response.StatusCode.value__ }
        return [pscustomobject]@{ Status = if ($null -eq $status) { 'error' } else { [int]$status }; Healthy = $false }
    }
}

function Write-HealthState([string]$Label, $State) {
    Write-Host "$Label health status: HTTP $($State.Status), Healthy=$($State.Healthy)."
}

function Resolve-LogDirectory([string]$StdoutLogFile) {
    $normalized = ([string]$StdoutLogFile).Trim().Replace('\', '/')
    if ([string]::IsNullOrWhiteSpace($normalized) -or
        [IO.Path]::IsPathRooted($normalized) -or
        $normalized -match '(^|/)\.\.(/|$)') {
        return 'logs'
    }
    $normalized = $normalized -replace '^\./', ''
    $separator = $normalized.LastIndexOf('/')
    if ($separator -le 0) { return 'logs' }
    return $normalized.Substring(0, $separator).Trim('/')
}

function Get-LogFiles([string]$RemoteLogPath) {
    if (Test-Path -LiteralPath $logRoot) { Remove-Item -LiteralPath $logRoot -Recurse -Force }
    New-Item -ItemType Directory -Path $logRoot | Out-Null
    try { Get-RemoteFile $RemoteLogPath $logRoot } catch { return @() }
    return @(Get-ChildItem -LiteralPath $logRoot -File -Recurse -ErrorAction SilentlyContinue)
}

function Wait-ForLogFiles([string]$RemoteLogPath) {
    for ($attempt = 1; $attempt -le 8; $attempt++) {
        Start-Sleep -Seconds 5
        $files = Get-LogFiles $RemoteLogPath
        if ($files.Count -gt 0) { return $files }
    }
    return @()
}

function Write-SafeLogCategories([System.IO.FileInfo[]]$Files) {
    if ($Files.Count -eq 0) {
        Write-Host 'Monster stdout log files captured: 0.'
        Write-Host 'Safe log categories: NoLogFileCaptured.'
        return
    }

    $patterns = [ordered]@{
        Database = '(?i)SqlException|Microsoft\.Data\.SqlClient|Cannot open database|Login failed|database connection'
        Redis = '(?i)Redis|ConnectionMultiplexer|StackExchange'
        Migration = '(?i)migration|DbUpdate|MigrateAsync|pending migration'
        Configuration = '(?i)configuration|missing.*setting|not configured|options validation'
        Storage = '(?i)storage|directory|permission|access denied|R2|S3'
        Hosting = '(?i)500\.30|ANCM|aspnetcore|IIS|startup|Unhandled Exception|fatal'
    }
    $text = ($Files | ForEach-Object {
        try { Get-Content -LiteralPath $_.FullName -Tail 2000 -ErrorAction SilentlyContinue } catch { }
    }) -join "`n"
    $categories = @($patterns.Keys | Where-Object { $text -match $patterns[$_] })
    if ($categories.Count -eq 0) { $categories = @('NoKnownRootCategory') }
    Write-Host "Monster stdout log files captured: $($Files.Count)."
    Write-Host "Safe log categories: $($categories -join ', ')."
}

New-Item -ItemType Directory -Path $operationRoot | Out-Null
try {
$remoteChanged = $false
$diagnosticFailure = $false
$healthAfterRollback = $null
try {
    Get-RemoteFile $remoteWebConfigPath $remoteWebConfig
    [xml]$webConfig = Get-Content -LiteralPath $remoteWebConfig -Raw
    $aspNetCore = $webConfig.SelectSingleNode('//aspNetCore')
    if ($null -eq $aspNetCore) { throw 'Remote web.config has no aspNetCore entry.' }

    $logDirectory = Resolve-LogDirectory ([string]$aspNetCore.GetAttribute('stdoutLogFile'))
    $remoteLogPath = "$ExpectedSite/$logDirectory"
    $diagnosticStdoutPath = ".\$($logDirectory.Replace('/', '\'))\stdout"
    $aspNetCore.SetAttribute('stdoutLogEnabled', 'true')
    $aspNetCore.SetAttribute('stdoutLogFile', $diagnosticStdoutPath)
    [IO.File]::WriteAllText($diagnosticWebConfig, $webConfig.OuterXml, [Text.UTF8Encoding]::new($false))

    Write-Host 'Temporarily enabling Monster stdout logging for root-cause capture.'
    Set-RemoteFile $diagnosticWebConfig $remoteWebConfigPath
    $remoteChanged = $true
    Write-HealthState 'During temporary log capture' (Get-HealthState)
    $files = Wait-ForLogFiles $remoteLogPath
    Write-SafeLogCategories $files
}
catch {
    $diagnosticFailure = $true
}
finally {
    if ($remoteChanged) {
        try {
            Set-RemoteFile $remoteWebConfig $remoteWebConfigPath
            Write-Host 'Original Monster web.config restored.'
        }
        catch {
            Write-Warning 'Monster web.config rollback failed and needs operator intervention.'
            $diagnosticFailure = $true
        }
    }
    Start-Sleep -Seconds 5
    $healthAfterRollback = Get-HealthState
    Write-HealthState 'After stdout rollback' $healthAfterRollback
}

if ($diagnosticFailure) { throw 'Monster log diagnostic or web.config rollback failed.' }
if (-not $healthAfterRollback.Healthy) { throw 'Health gate remains failed after Monster log diagnostic rollback.' }
}

finally {
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    $resolvedOperationRoot = [IO.Path]::GetFullPath($operationRoot)
    if ($resolvedOperationRoot.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedOperationRoot)) {
        Remove-Item -LiteralPath $resolvedOperationRoot -Recurse -Force
    }
}
