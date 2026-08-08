[CmdletBinding()]
param(
    [string] $ConnectionString = $env:LOGICFIT_PRODUCTION_DB_CONNECTION
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw 'A protected production connection is required for the read-only resource-state probe.'
}

$connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try {
    $connection.Open()

    $command = $connection.CreateCommand()
    $command.CommandTimeout = 15
    $command.CommandText = @'
SELECT
    r.[Id] AS [ResourceId],
    r.[DatabaseName],
    r.[Status],
    CASE WHEN NULLIF(r.[EncryptedConnectionString], '') IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS [HasProtectedConnection],
    SUM(CASE WHEN m.[IsActive] = 1 THEN 1 ELSE 0 END) AS [ActiveMappingCount]
FROM [dbo].[DatabaseResources] AS r
LEFT JOIN [dbo].[TenantDatabaseMappings] AS m ON m.[DatabaseResourceId] = r.[Id]
WHERE r.[IsDeleted] = 0
GROUP BY r.[Id], r.[DatabaseName], r.[Status], r.[EncryptedConnectionString], r.[CreatedAt]
ORDER BY r.[CreatedAt], r.[Id];

SELECT
    (SELECT COUNT_BIG(1) FROM [dbo].[DataProtectionKeys]) AS [DataProtectionKeyCount],
    (SELECT COUNT_BIG(1) FROM [dbo].[TenantDatabaseMappings] WHERE [IsActive] = 1) AS [ActiveMappingCount],
    (SELECT COUNT_BIG(1) FROM [dbo].[DatabaseBackups]) AS [DatabaseBackupRecordCount],
    (SELECT COUNT_BIG(1) FROM [dbo].[BackupBatches]) AS [BackupBatchCount];
'@

    $reader = $command.ExecuteReader()
    try {
        $resourceCount = 0
        while ($reader.Read()) {
            $resourceCount++
            $resourceId = [Guid]$reader.GetValue(0)
            $databaseName = [string]$reader.GetValue(1)
            $status = [int]$reader.GetValue(2)
            $hasProtectedConnection = [bool]$reader.GetValue(3)
            $activeMappingCount = [long]$reader.GetValue(4)
            Write-Host "Resource $resourceId; database=$databaseName; status=$status; protected=$hasProtectedConnection; activeMappings=$activeMappingCount."
        }

        if ($reader.NextResult() -and $reader.Read()) {
            Write-Host "Resource rows: $resourceCount."
            Write-Host "Data Protection keys: $([long]$reader.GetValue(0))."
            Write-Host "Active mappings: $([long]$reader.GetValue(1))."
            Write-Host "Database backup records: $([long]$reader.GetValue(2))."
            Write-Host "Backup batch records: $([long]$reader.GetValue(3))."
        }
        else {
            throw 'The resource-state probe returned no summary row.'
        }
    }
    finally {
        $reader.Dispose()
        $command.Dispose()
    }
}
catch {
    throw "Read-only protected resource-state probe failed with $($_.Exception.GetType().Name)."
}
finally {
    $connection.Dispose()
}
