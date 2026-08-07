[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $PublishSettingsPath,
    [Parameter(Mandatory = $true)] [string] $ExpectedSite,
    [Parameter(Mandatory = $true)] [string] $HealthCheckUrl,
    [string] $ManagementHostOverride,
    [switch] $AllowUntrustedManagementCertificate,
    [ValidateRange(8, 60)] [int] $CaptureAttempts = 8,
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
$baselineLogRoot = Join-Path $operationRoot 'baseline-logs'
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

function Get-LogFiles([string]$RemoteLogPath, [string]$DestinationRoot) {
    if (Test-Path -LiteralPath $DestinationRoot) { Remove-Item -LiteralPath $DestinationRoot -Recurse -Force }
    New-Item -ItemType Directory -Path $DestinationRoot | Out-Null
    try { Get-RemoteFile $RemoteLogPath $DestinationRoot } catch { return @() }
    return @(Get-ChildItem -LiteralPath $DestinationRoot -File -Recurse -ErrorAction SilentlyContinue)
}

function Get-LogSnapshot([System.IO.FileInfo[]]$Files, [string]$Root) {
    $snapshot = @{}
    $rootPrefix = [IO.Path]::GetFullPath($Root).TrimEnd('\') + '\'
    foreach ($file in $Files) {
        try {
            $relativePath = $file.FullName.Substring($rootPrefix.Length)
            $snapshot[$relativePath] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        }
        catch { }
    }
    return ,$snapshot
}

function Get-ChangedLogFiles([System.IO.FileInfo[]]$Files, [string]$Root, [hashtable]$Baseline) {
    $changed = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
    $rootPrefix = [IO.Path]::GetFullPath($Root).TrimEnd('\') + '\'
    foreach ($file in $Files) {
        try {
            $relativePath = $file.FullName.Substring($rootPrefix.Length)
            $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
            if (-not $Baseline.ContainsKey($relativePath) -or $Baseline[$relativePath] -ne $hash) {
                $changed.Add($file)
            }
        }
        catch { }
    }
    return @($changed)
}

function Wait-ForLogFiles([string]$RemoteLogPath, [hashtable]$Baseline, [int]$MaxAttempts = 8) {
    $lastFiles = @()
    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        Start-Sleep -Seconds 5
        $lastFiles = @(Get-LogFiles $RemoteLogPath $logRoot)
        $changedFiles = @(Get-ChangedLogFiles $lastFiles $logRoot $Baseline)
        if ($changedFiles.Count -gt 0) {
            return [pscustomobject]@{
                Files = $changedFiles
                ChangedCount = $changedFiles.Count
                TotalCount = $lastFiles.Count
                UsedFallback = $false
            }
        }
    }
    $fallbackFiles = @($lastFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 3)
    return [pscustomobject]@{
        Files = $fallbackFiles
        ChangedCount = 0
        TotalCount = $lastFiles.Count
        UsedFallback = $true
    }
}

function Write-SafeLogCategories(
    [System.IO.FileInfo[]]$Files,
    [int]$ChangedCount,
    [int]$TotalCount,
    [bool]$UsedFallback) {
    if ($Files.Count -eq 0) {
        Write-Host "Monster stdout log files captured: $TotalCount; new or changed: $ChangedCount."
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
    $signaturePatterns = [ordered]@{
        DatabaseLogin = '(?i)login failed|cannot open database|server was not found|network-related'
        DatabaseSchema = '(?i)invalid object name|invalid column|does not exist|migrationshistory'
        DatabaseTimeout = '(?i)timeout|time-out|timed out'
        DatabasePermission = '(?i)(sql|database|object|table|schema|select|insert|update|create|alter|execute).{0,100}(permission was denied|not authorized|access is denied)|(permission was denied|not authorized|access is denied).{0,100}(sql|database|object|table|schema|select|insert|update|create|alter|execute)'
        MigrationState = '(?i)pending migration|migration.*failed|migrateasync'
        StoragePermission = '(?i)(directory|folder|file|path|disk).{0,100}(access denied|unauthorized|permission)|(access denied|unauthorized|permission).{0,100}(directory|folder|file|path|disk)'
        StoragePath = '(?i)directory not found|could not find.*path|disk.*space'
        HostingStartup = '(?i)500\.30|ANCM|failed to start|application startup|unhandled exception|fatal'
    }
    $exceptionPatterns = [ordered]@{
        SqlException = '(?i)\bSqlException\b|Microsoft\.Data\.SqlClient'
        DbUpdateException = '(?i)\bDbUpdateException\b'
        UnauthorizedAccessException = '(?i)\bUnauthorizedAccessException\b'
        DirectoryNotFoundException = '(?i)\bDirectoryNotFoundException\b'
        FileNotFoundException = '(?i)\bFileNotFoundException\b'
        TimeoutException = '(?i)\bTimeoutException\b|timed out|timeout'
        InvalidOperationException = '(?i)\bInvalidOperationException\b'
    }
    $text = ($Files | ForEach-Object {
        try { Get-Content -LiteralPath $_.FullName -Tail 2000 -ErrorAction SilentlyContinue } catch { }
    }) -join "`n"
    $categories = @($patterns.Keys | Where-Object { $text -match $patterns[$_] })
    if ($categories.Count -eq 0) { $categories = @('NoKnownRootCategory') }
    $signatures = @($signaturePatterns.Keys | Where-Object { $text -match $signaturePatterns[$_] })
    if ($signatures.Count -eq 0) { $signatures = @('NoKnownSafeSignature') }
    $exceptionTypes = @($exceptionPatterns.Keys | Where-Object { $text -match $exceptionPatterns[$_] })
    if ($exceptionTypes.Count -eq 0) { $exceptionTypes = @('NoKnownExceptionType') }
    $safeSqlDetails = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($line in ($text -split "`r?`n")) {
        $trimmed = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed)) { continue }
        if ($trimmed -match '(?i)invalid object name\s+[''\"](?<name>[^''\"]+)[''\"]') {
            [void]$safeSqlDetails.Add("InvalidObject:$($Matches['name'])")
        }
        elseif ($trimmed -match '(?i)invalid column name\s+[''\"](?<name>[^''\"]+)[''\"]') {
            [void]$safeSqlDetails.Add("InvalidColumn:$($Matches['name'])")
        }
        elseif ($trimmed -match '(?i)foreign key constraint\s+[''\"](?<name>[^''\"]+)[''\"]') {
            [void]$safeSqlDetails.Add("ForeignKey:$($Matches['name'])")
        }
        elseif ($trimmed -match '(?i)(?:unique key|duplicate key).*?constraint\s+[''\"](?<name>[^''\"]+)[''\"]') {
            [void]$safeSqlDetails.Add("UniqueConstraint:$($Matches['name'])")
        }
        elseif ($trimmed -match '(?i)null into column\s+[''\"](?<name>[^''\"]+)[''\"]') {
            [void]$safeSqlDetails.Add("NullColumn:$($Matches['name'])")
        }
        elseif ($trimmed -match "(?i)string or binary data would be truncated") {
            [void]$safeSqlDetails.Add('StringTruncation')
        }
        elseif ($trimmed -match '(?i)(?:SqlException|SqlError).*?(?:Number|ErrorNumber)\s*[:=]?\s*(?<number>-?\d+)') {
            [void]$safeSqlDetails.Add("SqlErrorNumber:$($Matches['number'])")
        }
        if ($trimmed -match '(?i)CreatePlatformWorkspaceApplication') {
            [void]$safeSqlDetails.Add('WorkspaceApplicationCreateStack')
        }
    }
    if ($safeSqlDetails.Count -eq 0) { [void]$safeSqlDetails.Add('NoKnownSafeSqlDetail') }
    Write-Host "Monster stdout log files captured: $TotalCount; new or changed: $ChangedCount."
    if ($UsedFallback) { Write-Host 'No new log file was detected; analyzed the three newest existing files as a bounded fallback.' }
    Write-Host "Safe log categories: $($categories -join ', ')."
    Write-Host "Safe log signatures: $($signatures -join ', ')."
    Write-Host "Safe exception types: $($exceptionTypes -join ', ')."
    Write-Host "Safe SQL details: $($safeSqlDetails -join ', ')."
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

    $environmentVariables = @($aspNetCore.SelectNodes('./environmentVariables/environmentVariable'))
    $hasConnectionOverride = @($environmentVariables | Where-Object {
        [string]::Equals([string]$_.GetAttribute('name'), 'ConnectionStrings__DefaultConnection', [StringComparison]::OrdinalIgnoreCase)
    }).Count -gt 0
    $hasRedisOverride = @($environmentVariables | Where-Object {
        [string]$_.GetAttribute('name') -match '^Redis(__|:)?'
    }).Count -gt 0
    $hasProcessTarget = -not [string]::IsNullOrWhiteSpace([string]$aspNetCore.GetAttribute('processPath')) -and
        -not [string]::IsNullOrWhiteSpace([string]$aspNetCore.GetAttribute('arguments'))
    Write-Host "Remote IIS connection-string environment override present: $hasConnectionOverride."
    Write-Host "Remote IIS Redis environment override present: $hasRedisOverride."
    Write-Host "Remote IIS process target metadata present: $hasProcessTarget."

    $logDirectory = Resolve-LogDirectory ([string]$aspNetCore.GetAttribute('stdoutLogFile'))
    $remoteLogPath = "$ExpectedSite/$logDirectory"
    $diagnosticStdoutPath = ".\$($logDirectory.Replace('/', '\'))\stdout"
    $baselineFiles = @(Get-LogFiles $remoteLogPath $baselineLogRoot)
    $baselineSnapshot = Get-LogSnapshot $baselineFiles $baselineLogRoot
    $aspNetCore.SetAttribute('stdoutLogEnabled', 'true')
    $aspNetCore.SetAttribute('stdoutLogFile', $diagnosticStdoutPath)
    [IO.File]::WriteAllText($diagnosticWebConfig, $webConfig.OuterXml, [Text.UTF8Encoding]::new($false))

    Write-Host 'Temporarily enabling Monster stdout logging for root-cause capture.'
    Set-RemoteFile $diagnosticWebConfig $remoteWebConfigPath
    $remoteChanged = $true
    Write-HealthState 'During temporary log capture' (Get-HealthState)
    $logCapture = Wait-ForLogFiles $remoteLogPath $baselineSnapshot $CaptureAttempts
    Write-SafeLogCategories $logCapture.Files $logCapture.ChangedCount $logCapture.TotalCount $logCapture.UsedFallback
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
