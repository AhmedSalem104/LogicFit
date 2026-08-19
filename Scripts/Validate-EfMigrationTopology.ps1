[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",
    [string] $OutputDirectory = (Join-Path ([IO.Path]::GetTempPath()) "logicfit-migration-topology"),
    [switch] $NoBuild
)

$ErrorActionPreference = "Stop"

$definitions = @(
    [pscustomobject]@{
        Name = "application-compatibility"
        Project = "LogicFit.Infrastructure"
        Context = "LogicFit.Infrastructure.Persistence.ApplicationDbContext"
        HistoryTable = "__EFMigrationsHistory"
        ScriptName = "application-compatibility.sql"
    },
    [pscustomobject]@{
        Name = "platform"
        Project = "LogicFit.Platform.Migrations"
        Context = "LogicFit.Infrastructure.Persistence.PlatformDbContext"
        HistoryTable = "__PlatformEFMigrationsHistory"
        ScriptName = "platform.sql"
    },
    [pscustomobject]@{
        Name = "tenant"
        Project = "LogicFit.Tenant.Migrations"
        Context = "LogicFit.Infrastructure.Persistence.TenantDbContext"
        HistoryTable = "__TenantEFMigrationsHistory"
        ScriptName = "tenant.sql"
    }
)

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

function Invoke-Ef([string[]] $Arguments) {
    $output = (& dotnet @Arguments 2>&1 | Out-String)
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        throw "dotnet ef failed with exit code $exitCode. Output: $output"
    }
    return $output
}

$manifest = foreach ($definition in $definitions) {
    $commonArguments = @(
        "ef",
        "migrations",
        "list",
        "--configuration", $Configuration,
        "--project", $definition.Project,
        "--startup-project", "LogicFit.API",
        "--context", $definition.Context
    )
    if ($NoBuild) { $commonArguments += "--no-build" }

    $migrationList = Invoke-Ef $commonArguments
    $migrationIds = [regex]::Matches($migrationList, "(?m)^\s*(\d{14}_[A-Za-z0-9_]+)(?:\s+\(Pending\))?\s*$") |
        ForEach-Object { $_.Groups[1].Value }
    if ($migrationIds.Count -eq 0) {
        throw "No migrations were discovered for $($definition.Context)."
    }

    $scriptPath = Join-Path $OutputDirectory $definition.ScriptName
    $scriptArguments = @(
        "ef",
        "migrations",
        "script",
        "--idempotent",
        "--configuration", $Configuration,
        "--project", $definition.Project,
        "--startup-project", "LogicFit.API",
        "--context", $definition.Context,
        "--output", $scriptPath
    )
    if ($NoBuild) { $scriptArguments += "--no-build" }
    [void](Invoke-Ef $scriptArguments)

    if (-not (Test-Path -LiteralPath $scriptPath)) {
        throw "EF did not create the migration script for $($definition.Name)."
    }

    $sql = Get-Content -LiteralPath $scriptPath -Raw
    if ([string]::IsNullOrWhiteSpace($sql)) {
        throw "The migration script for $($definition.Name) is empty or is not an EF script."
    }

    if ($sql -notmatch [regex]::Escape($definition.HistoryTable)) {
        throw "The migration script for $($definition.Name) does not use $($definition.HistoryTable)."
    }

    foreach ($otherDefinition in $definitions | Where-Object { $_.HistoryTable -ne $definition.HistoryTable }) {
        if ($sql -match [regex]::Escape($otherDefinition.HistoryTable)) {
            throw "The $($definition.Name) migration script references the $($otherDefinition.Name) history table."
        }
    }

    $hash = (Get-FileHash -LiteralPath $scriptPath -Algorithm SHA256).Hash
    [pscustomobject]@{
        Name = $definition.Name
        Context = $definition.Context
        HistoryTable = $definition.HistoryTable
        MigrationCount = $migrationIds.Count
        LatestMigration = $migrationIds[-1]
        Script = $scriptPath
        Sha256 = $hash
    }
}

$manifestPath = Join-Path $OutputDirectory "manifest.json"
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

foreach ($entry in $manifest) {
    Write-Host "$($entry.Name): $($entry.MigrationCount) migration(s), latest $($entry.LatestMigration), SHA256 $($entry.Sha256)"
}
Write-Host "EF migration topology validation passed. Manifest: $manifestPath"
