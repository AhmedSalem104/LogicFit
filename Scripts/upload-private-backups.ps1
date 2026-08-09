[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $PublishSettingsPath,
    [Parameter(Mandatory = $true)] [string] $ContentPath,
    [string] $ManagementHostOverride,
    [switch] $AllowUntrustedManagementCertificate,
    [string] $MsDeployPath = "C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $PublishSettingsPath -PathType Leaf)) { throw "Publish settings file not found." }
if (-not (Test-Path -LiteralPath $ContentPath -PathType Container)) { throw "Backup content path not found." }
if (-not (Test-Path -LiteralPath $MsDeployPath -PathType Leaf)) { throw "MSDeploy executable not found." }

$resolvedContentPath = (Resolve-Path -LiteralPath $ContentPath).Path
$privateBackupPath = Join-Path $resolvedContentPath "App_Data\PrivateBackups"
$backupFiles = @(Get-ChildItem -LiteralPath $privateBackupPath -File -ErrorAction Stop)
if ($backupFiles.Count -eq 0) { throw "No private backup files were produced." }
if ($backupFiles | Where-Object { $_.Extension -notin ".bacpac", ".json" }) {
    throw "The protected upload folder contains an unexpected file type."
}

[xml] $settings = Get-Content -LiteralPath $PublishSettingsPath -Raw -Encoding UTF8
$profile = $settings.publishData.publishProfile | Select-Object -First 1
if ($null -eq $profile -or $profile.publishMethod -ne "MSDeploy") { throw "The publish settings file is not an MSDeploy profile." }
if ([string]::IsNullOrWhiteSpace($profile.publishUrl) -or [string]::IsNullOrWhiteSpace($profile.msdeploySite)) {
    throw "MSDeploy host/site is missing."
}
if ([string]::IsNullOrWhiteSpace($profile.userName) -or [string]::IsNullOrWhiteSpace($profile.userPWD)) {
    throw "MSDeploy credentials are missing."
}

$managementHost = if ([string]::IsNullOrWhiteSpace($ManagementHostOverride)) {
    [string]$profile.publishUrl
} else {
    $ManagementHostOverride.Trim()
}
if ($managementHost -notmatch '^[A-Za-z0-9.-]+$') { throw "Management host is invalid." }

$destination = "https://${managementHost}:8172/msdeploy.axd?site=$($profile.msdeploySite)"
$arguments = @(
    '-verb:sync',
    "-source:contentPath=`"$resolvedContentPath`"",
    "-dest:auto,ComputerName=$destination,UserName=$($profile.userName),Password=$($profile.userPWD),AuthType=Basic",
    '-enableRule:DoNotDeleteRule',
    '-retryAttempts:3',
    '-retryInterval:5000'
)
if ($AllowUntrustedManagementCertificate) { $arguments += '-allowUntrusted' }

# Do not log the argument list: it contains the protected publish password.
$toolOutput = @(& $MsDeployPath @arguments 2>&1)
if ($LASTEXITCODE -ne 0) {
    $outputText = ($toolOutput | ForEach-Object { [string]$_ }) -join "`n"
    $category = if ($outputText -match '(?i)certificate|trust|ssl|tls') {
        'ManagementCertificate'
    } elseif ($outputText -match '(?i)unauthori|forbidden|access denied|401|403') {
        'ManagementAuthorization'
    } elseif ($outputText -match '(?i)not found|cannot resolve|name resolution|network|timeout|unreachable') {
        'ManagementEndpoint'
    } elseif ($outputText -match '(?i)permission|locked|read-only|denied|path') {
        'RemoteStoragePath'
    } else {
        'MsDeployOperation'
    }
    throw "Private backup upload failed with exit code $LASTEXITCODE (category $category)."
}

Write-Host "Protected private backup upload completed for $($backupFiles.Count) file(s)."
