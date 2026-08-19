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
    if (-not [string]::IsNullOrWhiteSpace($uri.AbsolutePath) -and $uri.AbsolutePath -ne '/') {
        throw 'Authenticated E2E base URL must be the API host root, not a route or frontend URL.'
    }
    return $Value.TrimEnd('/')
}

function Assert-Health {
    try {
        $response = Invoke-WebRequest -Uri "$script:apiBase/health" -UseBasicParsing -TimeoutSec 30 -ErrorAction Stop
    }
    catch {
        throw 'Authenticated E2E health gate could not reach the configured API host.'
    }

    if ($response.StatusCode -ne 200 -or $response.Content.Trim() -ne 'Healthy') {
        throw "Authenticated E2E health gate expected HTTP 200 Healthy, received HTTP $($response.StatusCode)."
    }
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
    try {
        $response = Invoke-WebRequest @request -ErrorAction Stop
        return [int]$response.StatusCode
    }
    catch {
        $response = $_.Exception.Response
        if ($null -eq $response) { throw "Authenticated E2E request $Method $Path did not return a response." }
        return [int]$response.StatusCode
    }
}

$script:apiBase = Normalize-BaseUrl $BaseUrl
Assert-Health
Require-Value 'LOGICFIT_E2E_EMAIL' $Email
Require-Value 'LOGICFIT_E2E_PASSWORD' $Password
Require-Value 'LOGICFIT_E2E_WORKSPACE_ID' $WorkspaceId

[Guid]$workspaceGuid = [Guid]::Empty
if (-not [Guid]::TryParse($WorkspaceId, [ref]$workspaceGuid)) {
    throw 'LOGICFIT_E2E_WORKSPACE_ID must be a valid GUID.'
}

$session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
$login = Invoke-ApiJson -Method POST -Path '/api/identity/login' -Body @{ email = $Email; password = $Password } -WebSession $session
if ([string]::IsNullOrWhiteSpace([string]$login.workspaceSelectionToken)) {
    throw 'Identity login did not return a workspace selection token.'
}

$activeWorkspaceIds = @($login.activeWorkspaces | ForEach-Object {
    [Guid]$activeWorkspaceId = [Guid]::Empty
    if ([Guid]::TryParse([string]$_.workspaceId, [ref]$activeWorkspaceId)) { $activeWorkspaceId }
})
if ($activeWorkspaceIds -notcontains $workspaceGuid) {
    throw 'Configured E2E workspace is not present in the authenticated identity active-workspace list.'
}

$selected = Invoke-ApiJson -Method POST -Path '/api/identity/select-workspace' -Body @{
    workspaceSelectionToken = $login.workspaceSelectionToken
    workspaceId = $workspaceGuid
} -WebSession $session
if ([string]::IsNullOrWhiteSpace([string]$selected.accessToken)) {
    throw 'Workspace selection did not return a tenant access token.'
}
[Guid]$selectedTenantGuid = [Guid]::Empty
if (-not [Guid]::TryParse([string]$selected.tenantId, [ref]$selectedTenantGuid) -or
    $selectedTenantGuid -ne $workspaceGuid) {
    throw 'Workspace selection returned a tenant context different from the configured workspace.'
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
$refreshedProfile = Invoke-ApiJson -Method GET -Path '/api/profile' -Headers @{
    Authorization = "Bearer $($refresh.accessToken)"
} -WebSession $session
if ($null -eq $refreshedProfile) {
    throw 'The access token returned by refresh failed the protected profile check.'
}

if (-not [string]::IsNullOrWhiteSpace($TenantAId) -and -not [string]::IsNullOrWhiteSpace($TenantBId)) {
    [Guid]$tenantAGuid = [Guid]::Empty
    [Guid]$tenantBGuid = [Guid]::Empty
    if (-not [Guid]::TryParse($TenantAId, [ref]$tenantAGuid) -or
        -not [Guid]::TryParse($TenantBId, [ref]$tenantBGuid) -or
        $tenantAGuid -eq $tenantBGuid) {
        throw 'Tenant A and Tenant B E2E identifiers must be different GUIDs.'
    }
    if ($tenantAGuid -ne $workspaceGuid) {
        throw 'LOGICFIT_E2E_TENANT_A_ID must match LOGICFIT_E2E_WORKSPACE_ID.'
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
    $platformPositiveStatus = Invoke-ApiStatus -Method GET -Path '/api/platform/diagnostics/version' -Headers @{
        Authorization = "Bearer $PlatformToken"
    } -WebSession $session
    if ($platformPositiveStatus -ne 200) {
        throw "Platform-token positive smoke check expected HTTP 200, received HTTP $platformPositiveStatus."
    }

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
