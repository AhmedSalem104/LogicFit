[CmdletBinding()]
param(
    [string] $ConnectionString = $env:LOGICFIT_PRODUCTION_DB_CONNECTION,
    [string] $WorkspaceIdentifier = $env:DIAGNOSE_WORKSPACE_IDENTIFIER
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw 'A protected production connection is required for the read-only workspace state probe.'
}
if ([string]::IsNullOrWhiteSpace($WorkspaceIdentifier)) {
    throw 'A workspace identifier is required for the read-only workspace state probe.'
}

$normalizedIdentifier = $WorkspaceIdentifier.Trim().ToLowerInvariant()
$connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandTimeout = 15
    $command.CommandText = @'
SELECT
    (SELECT COUNT_BIG(1) FROM [dbo].[Tenants] WHERE [Subdomain] = @identifier AND [IsDeleted] = 0) AS [TenantCount],
    (SELECT COUNT_BIG(1) FROM [dbo].[ApplicationRequests] WHERE [ReservedWorkspaceIdentifier] = @identifier) AS [ApplicationCount],
    (SELECT COUNT_BIG(1) FROM [dbo].[ApplicationRequests] WHERE [ReservedWorkspaceIdentifier] = @identifier AND [Status] IN (1, 2, 3, 4)) AS [OpenApplicationCount],
    (SELECT COUNT_BIG(1)
       FROM [dbo].[ApplicationRequests] AS [a]
       INNER JOIN [dbo].[Tenants] AS [t] ON [t].[Id] = [a].[ProvisionedWorkspaceId]
      WHERE [a].[ReservedWorkspaceIdentifier] = @identifier) AS [ProvisionedTenantLinkCount]
'@
    [void]$command.Parameters.Add('@identifier', [System.Data.SqlDbType]::NVarChar, 100)
    $command.Parameters['@identifier'].Value = $normalizedIdentifier
    $reader = $command.ExecuteReader()
    try {
        if (-not $reader.Read()) {
            throw 'The read-only workspace state probe returned no row.'
        }
        Write-Host "Workspace identifier probe completed for a redacted identifier." 
        Write-Host "Tenant rows: $($reader.GetInt64(0))."
        Write-Host "Application rows: $($reader.GetInt64(1))."
        Write-Host "Open application rows: $($reader.GetInt64(2))."
        Write-Host "Provisioned tenant links: $($reader.GetInt64(3))."
    }
    finally {
        $reader.Dispose()
        $command.Dispose()
    }
}
catch {
    throw "Read-only workspace state probe failed with $($_.Exception.GetType().Name)."
}
finally {
    $connection.Dispose()
}
