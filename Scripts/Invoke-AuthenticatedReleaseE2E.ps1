[CmdletBinding()]
param(
    [ValidateSet('Smoke', 'Release')]
    [string] $Mode = 'Release',
    [string] $BaseUrl = $env:LOGICFIT_E2E_BASE_URL,
    [string] $Email = $env:LOGICFIT_E2E_EMAIL,
    [string] $Password = $env:LOGICFIT_E2E_PASSWORD,
    [string] $WorkspaceId = $env:LOGICFIT_E2E_WORKSPACE_ID,
    [string] $TenantAId = $env:LOGICFIT_E2E_TENANT_A_ID,
    [string] $TenantBId = $env:LOGICFIT_E2E_TENANT_B_ID,
    [string] $PlatformToken = $env:LOGICFIT_E2E_PLATFORM_TOKEN
)

$ErrorActionPreference = 'Stop'

function Require-Value([string] $Name, [string] $Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "Authenticated E2E configuration is missing: $Name"
    }
}

function Normalize-BaseUrl([string] $Value) {
    Require-Value 'LOGICFIT_E2E_BASE_URL' $Value
    $uri = [Uri]$Value
    if ($uri.Scheme -ne 'https' -and $uri.Host -notin @('localhost', '127.0.0.1')) {
        throw 'Authenticated E2E base URL must use HTTPS outside localhost.'
    }
    return $Value.TrimEnd('/')
}

function Invoke-ApiJson {
    param(
        [ValidateSet('GET', 'POST', 'PATCH', 'DELETE')]
        [string] $Method,
        [string] $Path,
        [hashtable] $Headers,
        [object] $Body,
        [Microsoft.PowerShell.Commands.WebRequestSession] $WebSession
    )

    $request = @{
        Method = $Method
        Uri = "$script:apiBase$Path"
        Headers = $Headers
        WebSession = $WebSession
        TimeoutSec = 30
        ErrorAction = 'Stop'
    }
    if ($null -ne $Body) {
        $request.ContentType = 'application/json'
        $request.Body = ($Body | ConvertTo-Json -Depth 8 -Compress)
    }

    try {
        return Invoke-RestMethod @request
    }
    catch {
        $status = $_.Exception.Response.StatusCode.value__
        if ($null -eq $status) { $status = 'transport' }
        throw "Authenticated E2E request $Method $Path failed with HTTP $status."
    }
}

function Invoke-ApiStatus {
    param(
        [string] $Method,
        [string] $Path,
        [hashtable] $Headers,
        [Microsoft.PowerShell.Commands.WebRequestSession] $WebSession
    )

    $request = @{
        Method = $Method
        Uri = "$script:apiBase$Path"
        Headers = $Headers
        WebSession = $WebSession
        TimeoutSec = 30
        ErrorAction = 'SilentlyContinue'
    }
    $response = Invoke-WebRequest @request
    if ($null -eq $response) {
        $last = $Error[0]
        $status = $last.Exception.Response.StatusCode.value__
        if ($null -eq $status) { throw "Authenticated E2E request $Method $Path did not return a response." }
        return [int]$status
    }
    return [int]$response.StatusCode
}

$script:apiBase = Normalize-BaseUrl $BaseUrl
Require-Value 'LOGICFIT_E2E_EMAIL' $Email
Require-Value 'LOGICFIT_E2E_PASSWORD' $Password
Require-Value 'LOGICFIT_E2E_WORKSPACE_ID' $WorkspaceId

if (-not [Guid]::TryParse($WorkspaceId, [ref]$workspaceGuid)) {
    throw 'LOGICFIT_E2E_WORKSPACE_ID must be a valid GUID.'
}

$session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
$login = Invoke-ApiJson -Method POST -Path '/api/identity/login' -Body @{ email = $Email; password = $Password } -WebSession $session
if ([string]::IsNullOrWhiteSpace([string]$login.workspaceSelectionToken)) {
    throw 'Identity login did not return a workspace selection token.'
}

$selected = Invoke-ApiJson -Method POST -Path '/api/identity/select-workspace' -Body @{
    workspaceSelectionToken = $login.workspaceSelectionToken
    workspaceId = $workspaceGuid
} -WebSession $session
if ([string]::IsNullOrWhiteSpace([string]$selected.accessToken)) {
    throw 'Workspace selection did not return a tenant access token.'
}

$tenantHeaders = @{ Authorization = "Bearer $($selected.accessToken)" }
$profile = Invoke-ApiJson -Method GET -Path '/api/profile' -Headers $tenantHeaders -WebSession $session
if ($null -eq $profile) {
    throw 'Authenticated profile smoke check returned an empty response.'
}

$refresh = Invoke-ApiJson -Method POST -Path '/api/auth/refresh' -Headers @{} -WebSession $session
if ([string]::IsNullOrWhiteSpace([string]$refresh.accessToken)) {
    throw 'Refresh-token smoke check did not return a tenant access token.'
}

if (-not [string]::IsNullOrWhiteSpace($TenantAId) -and -not [string]::IsNullOrWhiteSpace($TenantBId)) {
    if (-not [Guid]::TryParse($TenantAId, [ref]$tenantAGuid) -or
        -not [Guid]::TryParse($TenantBId, [ref]$tenantBGuid) -or
        $tenantAGuid -eq $tenantBGuid) {
        throw 'Tenant A and Tenant B E2E identifiers must be different GUIDs.'
    }

    $crossTenantStatus = Invoke-ApiStatus -Method GET -Path '/api/clients' -Headers @{
        Authorization = "Bearer $($selected.accessToken)"
        'X-Tenant-Id' = $tenantBId
    } -WebSession $session
    if ($crossTenantStatus -ne 403) {
        throw "Tenant isolation smoke check expected HTTP 403, received HTTP $crossTenantStatus."
    }
}
elseif ($Mode -eq 'Release') {
    throw 'Release mode requires LOGICFIT_E2E_TENANT_A_ID and LOGICFIT_E2E_TENANT_B_ID.'
}

if (-not [string]::IsNullOrWhiteSpace($PlatformToken)) {
    $platformStatus = Invoke-ApiStatus -Method GET -Path '/api/clients' -Headers @{
        Authorization = "Bearer $PlatformToken"
    } -WebSession $session
    if ($platformStatus -ne 403) {
        throw "Platform-token boundary smoke check expected HTTP 403, received HTTP $platformStatus."
    }
}
elseif ($Mode -eq 'Release') {
    throw 'Release mode requires LOGICFIT_E2E_PLATFORM_TOKEN.'
}

Write-Host "Authenticated E2E smoke passed for the configured workspace. No credentials or tokens were printed."
