[CmdletBinding()]
param(
    [string] $ConnectionString = $env:LOGICFIT_PRODUCTION_DB_CONNECTION
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw 'A protected production connection is required for the read-only migration-state probe.'
}

$migrationDirectory = Join-Path $PSScriptRoot '..\LogicFit.Infrastructure\Persistence\Migrations'
$compiledMigrations = @(
    Get-ChildItem -LiteralPath $migrationDirectory -Filter '*.cs' -File |
        Where-Object { $_.Name -notlike '*.Designer.cs' } |
        ForEach-Object {
            $match = [regex]::Match($_.BaseName, '^(?<id>\d{14})_(?<name>.+)$')
            if ($match.Success) { $match.Value }
        } |
        Sort-Object -Unique
)

if ($compiledMigrations.Count -eq 0) {
    throw 'No compiled application migrations were found.'
}

$appliedMigrations = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = 'SELECT [MigrationId] FROM [dbo].[__EFMigrationsHistory]'
    $command.CommandTimeout = 15
    $reader = $command.ExecuteReader()
    try {
        while ($reader.Read()) {
            $migrationId = [string]$reader.GetValue(0)
            if (-not [string]::IsNullOrWhiteSpace($migrationId)) {
                [void]$appliedMigrations.Add($migrationId.Trim())
            }
        }
    }
    finally {
        $reader.Dispose()
        $command.Dispose()
    }
}
catch {
    throw "Read-only application migration history probe failed with $($_.Exception.GetType().Name)."
}
finally {
    $connection.Dispose()
}

$pendingMigrations = @($compiledMigrations | Where-Object { -not $appliedMigrations.Contains($_) })
$unmatchedAppliedMigrations = @($appliedMigrations | Where-Object { $compiledMigrations -notcontains $_ })

Write-Host "Compiled application migrations: $($compiledMigrations.Count)."
Write-Host "Applied application migration rows: $($appliedMigrations.Count)."
Write-Host "Pending compiled application migrations: $($pendingMigrations.Count)."
if ($pendingMigrations.Count -gt 0) {
    Write-Host "Pending migration ids: $($pendingMigrations -join ', ')."
}
Write-Host "Applied history rows not present in the current artifact: $($unmatchedAppliedMigrations.Count)."
