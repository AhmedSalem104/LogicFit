[CmdletBinding()]
param(
    [string] $ConnectionString = $env:LOGICFIT_PRODUCTION_DB_CONNECTION
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw 'A protected production connection is required for the read-only seeder-state probe.'
}

$connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)

function Invoke-SafeCount([string] $id, [string] $sql) {
    try {
        $command = $connection.CreateCommand()
        $command.CommandText = $sql
        $command.CommandTimeout = 15
        $value = $command.ExecuteScalar()
        $command.Dispose()
        Write-Host "Seeder state $id=$value."
    }
    catch {
        Write-Host "Seeder state $id=QueryFailed:$($_.Exception.GetType().Name)."
    }
}

try {
    $connection.Open()

    Invoke-SafeCount 'DatabaseResourceRows' 'SELECT COUNT_BIG(*) FROM [dbo].[DatabaseResources]'
    Invoke-SafeCount 'DatabaseResourceDuplicateKeys' @'
SELECT COUNT_BIG(*)
FROM (
    SELECT [Provider], [DatabaseName]
    FROM [dbo].[DatabaseResources]
    GROUP BY [Provider], [DatabaseName]
    HAVING COUNT_BIG(*) > 1
) AS duplicates
'@
    Invoke-SafeCount 'PlatformOwnerRows' @'
SELECT COUNT_BIG(*)
FROM [dbo].[DomainUsers]
WHERE [TenantId] = ''00000000-0000-0000-0000-0000000000A1''
  AND [Role] = 8
  AND [IsDeleted] = 0
'@
    Invoke-SafeCount 'PlatformOwnerDuplicateRows' @'
SELECT COUNT_BIG(*)
FROM (
    SELECT [TenantId]
    FROM [dbo].[DomainUsers]
    WHERE [TenantId] = ''00000000-0000-0000-0000-0000000000A1''
      AND [Role] = 8
      AND [IsDeleted] = 0
    GROUP BY [TenantId]
    HAVING COUNT_BIG(*) > 1
) AS duplicates
'@
    Invoke-SafeCount 'MuscleDuplicateNames' @'
SELECT COUNT_BIG(*)
FROM (
    SELECT [Name]
    FROM [dbo].[Muscles]
    WHERE [IsDeleted] = 0
    GROUP BY [Name]
    HAVING COUNT_BIG(*) > 1
) AS duplicates
'@
    Invoke-SafeCount 'GlobalExerciseDuplicateNames' @'
SELECT COUNT_BIG(*)
FROM (
    SELECT [Name]
    FROM [dbo].[Exercises]
    WHERE [TenantId] IS NULL AND [IsDeleted] = 0
    GROUP BY [Name]
    HAVING COUNT_BIG(*) > 1
) AS duplicates
'@
    Invoke-SafeCount 'GlobalFoodDuplicateNames' @'
SELECT COUNT_BIG(*)
FROM (
    SELECT [Name]
    FROM [dbo].[Foods]
    WHERE [TenantId] IS NULL AND [IsDeleted] = 0
    GROUP BY [Name]
    HAVING COUNT_BIG(*) > 1
) AS duplicates
'@
    Invoke-SafeCount 'PermissionDuplicateCodes' @'
SELECT COUNT_BIG(*)
FROM (
    SELECT [Code]
    FROM [dbo].[Permissions]
    GROUP BY [Code]
    HAVING COUNT_BIG(*) > 1
) AS duplicates
'@
    Invoke-SafeCount 'SystemRoleDuplicateNames' @'
SELECT COUNT_BIG(*)
FROM (
    SELECT [Name]
    FROM [dbo].[Roles]
    WHERE [TenantId] IS NULL AND [IsDeleted] = 0
    GROUP BY [Name]
    HAVING COUNT_BIG(*) > 1
) AS duplicates
'@
    Invoke-SafeCount 'FeatureDuplicateCodes' @'
SELECT COUNT_BIG(*)
FROM (
    SELECT [Code]
    FROM [dbo].[Features]
    GROUP BY [Code]
    HAVING COUNT_BIG(*) > 1
) AS duplicates
'@
    Invoke-SafeCount 'PlanDuplicateNames' @'
SELECT COUNT_BIG(*)
FROM (
    SELECT [Name]
    FROM [dbo].[Plans]
    WHERE [IsDeleted] = 0
    GROUP BY [Name]
    HAVING COUNT_BIG(*) > 1
) AS duplicates
'@
    Invoke-SafeCount 'ApplicationMigrationRows' 'SELECT COUNT_BIG(*) FROM [dbo].[__EFMigrationsHistory]'
}
finally {
    $connection.Dispose()
}
