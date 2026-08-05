[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [uri]$BaseUrl,

    [Parameter(Mandatory = $true)]
    [string]$PlatformEmail,

    [Parameter(Mandatory = $true)]
    [string]$ExpectedReleaseCommit,

    [Parameter(Mandatory = $true)]
    [string]$VerifiedBackupReference,

    [Parameter(Mandatory = $true)]
    [ValidateSet('POST-DEPLOY-SMOKE-APPROVED')]
    [string]$OperatorApproval,

    [Parameter(Mandatory = $true)]
    [guid]$AllocatedResourceId,

    [Parameter(Mandatory = $true)]
    [guid]$FailedResourceId,

    [switch]$AllowMutations,

    [string]$PlatformPasswordEnvironmentVariable = 'LOGICFIT_SMOKE_PLATFORM_PASSWORD',
    [string]$AllocatedConnectionEnvironmentVariable = 'LOGICFIT_SMOKE_ALLOCATED_CONNECTION',
    [string]$FailedConnectionEnvironmentVariable = 'LOGICFIT_SMOKE_FAILED_CONNECTION',
    [string]$TestGymSubdomain = '',
    [string]$ResultPath = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:SmokeBaseUri = $BaseUrl
$script:SmokeChecks = [System.Collections.Generic.List[object]]::new()
$script:SmokeSecrets = [System.Collections.Generic.List[string]]::new()
$script:SmokeValues = [ordered]@{}
$script:SmokePassed = $false
$script:SmokeFailure = $null
$script:PlatformToken = $null
$script:TenantToken = $null

function Get-RequiredEnvironmentValue {
    param([Parameter(Mandatory = $true)][string]$Name)

    if ($Name -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') {
        throw "Environment variable name is invalid: $Name"
    }

    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrEmpty($value)) {
        throw "Required smoke secret is missing from environment variable $Name."
    }

    return $value
}

function Add-SmokeSecret {
    param([AllowEmptyString()][string]$Value)

    if (-not [string]::IsNullOrEmpty($Value) -and -not $script:SmokeSecrets.Contains($Value)) {
        $script:SmokeSecrets.Add($Value)
    }
}

function Get-JsonPropertyValue {
    param(
        [AllowNull()][object]$Object,
        [Parameter(Mandatory = $true)][string[]]$Names
    )

    if ($null -eq $Object) {
        return $null
    }

    foreach ($name in $Names) {
        $property = $Object.PSObject.Properties |
            Where-Object { $_.Name -ieq $name } |
            Select-Object -First 1
        if ($null -ne $property) {
            return $property.Value
        }
    }

    return $null
}

function Get-ResponseCode {
    param([AllowNull()][object]$Body)

    $code = Get-JsonPropertyValue $Body @('code', 'errorCode')
    if ($null -eq $code) {
        return ''
    }

    $text = [string]$code
    if ($text.Length -gt 120 -or $text -match '[\r\n]') {
        return 'UNSAFE_ERROR_CODE'
    }

    return $text
}

function Get-ResponseRequestId {
    param([AllowNull()][object]$Body)

    $requestId = Get-JsonPropertyValue $Body @('requestId')
    if ($null -eq $requestId) {
        return ''
    }

    $text = [string]$requestId
    if ($text.Length -gt 120 -or $text -match '[\r\n]') {
        return 'UNSAFE_REQUEST_ID'
    }

    return $text
}

function Get-ResponseItems {
    param([Parameter(Mandatory = $true)][object]$Body)

    $items = Get-JsonPropertyValue $Body @('items')
    if ($null -eq $items) {
        throw 'The smoke endpoint returned no paged items collection.'
    }

    return @($items)
}

function Get-DatabaseStatusName {
    param([AllowNull()][object]$Value)

    $numeric = 0
    if ([int]::TryParse([string]$Value, [ref]$numeric)) {
        $names = @{
            1 = 'Available'
            2 = 'Reserved'
            3 = 'Provisioning'
            4 = 'Assigned'
            5 = 'Maintenance'
            6 = 'RestorePending'
            7 = 'Faulted'
            8 = 'Retired'
        }
        if ($names.ContainsKey($numeric)) {
            return $names[$numeric]
        }
    }

    return [string]$Value
}

function Get-ProvisioningStatusName {
    param([AllowNull()][object]$Value)

    $numeric = 0
    if ([int]::TryParse([string]$Value, [ref]$numeric)) {
        $names = @{
            1 = 'Pending'
            2 = 'AwaitingDatabaseCapacity'
            3 = 'Provisioning'
            4 = 'Completed'
            5 = 'Failed'
        }
        if ($names.ContainsKey($numeric)) {
            return $names[$numeric]
        }
    }

    return [string]$Value
}

function Invoke-SmokeRequest {
    param(
        [Parameter(Mandatory = $true)][ValidateSet('GET', 'POST')][string]$Method,
        [Parameter(Mandatory = $true)][string]$Path,
        [AllowNull()][string]$Token,
        [AllowNull()][object]$Body,
        [hashtable]$Headers = @{},
        [int[]]$ExpectedStatus = @(200)
    )

    $requestHeaders = @{}
    foreach ($key in $Headers.Keys) {
        $requestHeaders[$key] = [string]$Headers[$key]
    }
    if (-not [string]::IsNullOrWhiteSpace($Token)) {
        $requestHeaders['Authorization'] = "Bearer $Token"
    }

    $requestUri = [Uri]::new($script:SmokeBaseUri, $Path)
    $requestParameters = @{
        Uri = $requestUri
        Method = $Method
        Headers = $requestHeaders
        UseBasicParsing = $true
        ErrorAction = 'Stop'
    }
    if ($null -ne $Body) {
        $requestParameters['Body'] = $Body | ConvertTo-Json -Depth 20 -Compress
        $requestParameters['ContentType'] = 'application/json'
    }

    $rawResponse = $null
    $responseText = ''
    $statusCode = 0
    try {
        $invokeCommand = Get-Command Invoke-WebRequest
        if ($invokeCommand.Parameters.ContainsKey('SkipHttpErrorCheck')) {
            $requestParameters['SkipHttpErrorCheck'] = $true
        }

        $rawResponse = Invoke-WebRequest @requestParameters
        $statusCode = [int]$rawResponse.StatusCode
        $responseText = [string]$rawResponse.Content
    }
    catch {
        $webResponse = $_.Exception.Response
        if ($null -eq $webResponse) {
            throw "Smoke request failed for $Method $Path without an HTTP response."
        }

        $statusCode = [int]$webResponse.StatusCode
        try {
            $reader = [IO.StreamReader]::new($webResponse.GetResponseStream())
            $responseText = $reader.ReadToEnd()
            $reader.Dispose()
        }
        catch {
            $responseText = ''
        }
    }

    foreach ($secret in $script:SmokeSecrets) {
        if (-not [string]::IsNullOrEmpty($secret) -and $responseText.Contains($secret, [StringComparison]::Ordinal)) {
            throw "Smoke response from $Method $Path contained a protected value."
        }
    }

    $bodyObject = $null
    if (-not [string]::IsNullOrWhiteSpace($responseText)) {
        try {
            $bodyObject = $responseText | ConvertFrom-Json
        }
        catch {
            $bodyObject = $null
        }
    }

    $requestId = Get-ResponseRequestId $bodyObject
    if ($null -ne $rawResponse -and $rawResponse.Headers['X-Request-Id']) {
        $requestId = [string]$rawResponse.Headers['X-Request-Id']
    }

    if ($ExpectedStatus -notcontains $statusCode) {
        $code = Get-ResponseCode $bodyObject
        $suffix = if ([string]::IsNullOrWhiteSpace($requestId)) { '' } else { ", requestId $requestId" }
        throw "Smoke request $Method $Path returned HTTP $statusCode (code '$code'$suffix)."
    }

    return [pscustomobject]@{
        StatusCode = $statusCode
        Body = $bodyObject
        BodyText = $responseText
        RequestId = $requestId
    }
}

function Add-SmokeCheck {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [hashtable]$Details = @{}
    )

    $script:SmokeChecks.Add([ordered]@{
        name = $Name
        details = $Details
    })
    Write-Host "[PASS] $Name"
}

function Assert-ResponseSucceeded {
    param([Parameter(Mandatory = $true)][object]$Response)

    $succeeded = Get-JsonPropertyValue $Response.Body @('succeeded')
    if ($succeeded -ne $true) {
        $code = Get-ResponseCode $Response.Body
        throw "Smoke operation did not succeed (code '$code', requestId '$($Response.RequestId)')."
    }
}

function Get-Resource {
    param(
        [Parameter(Mandatory = $true)][guid]$ResourceId,
        [Parameter(Mandatory = $true)][string]$Token
    )

    return (Invoke-SmokeRequest -Method GET -Path "/api/platform/database-resources/$ResourceId" -Token $Token).Body
}

function Repair-Resource {
    param(
        [Parameter(Mandatory = $true)][guid]$ResourceId,
        [Parameter(Mandatory = $true)][string]$ConnectionString,
        [Parameter(Mandatory = $true)][string]$Token,
        [Parameter(Mandatory = $true)][string]$ExpectedBefore,
        [Parameter(Mandatory = $true)][string]$ExpectedAfter,
        [Parameter(Mandatory = $true)][string]$CheckName
    )

    $before = Get-Resource -ResourceId $ResourceId -Token $Token
    $beforeStatus = Get-DatabaseStatusName (Get-JsonPropertyValue $before @('status', 'lifecycleStatus'))
    if ($beforeStatus -ne $ExpectedBefore) {
        throw "$CheckName expected resource $ResourceId to be $ExpectedBefore but found $beforeStatus."
    }

    $repair = Invoke-SmokeRequest -Method POST -Path "/api/platform/database-resources/$ResourceId/repair-connection" -Token $Token -Body @{
        connectionString = $ConnectionString
        confirm = $true
    }
    Assert-ResponseSucceeded $repair

    $after = Get-Resource -ResourceId $ResourceId -Token $Token
    $afterStatus = Get-DatabaseStatusName (Get-JsonPropertyValue $after @('status', 'lifecycleStatus'))
    $hasConnection = [bool](Get-JsonPropertyValue $after @('hasConnectionString'))
    $afterTenantId = [string](Get-JsonPropertyValue $after @('tenantId'))
    if ($afterStatus -ne $ExpectedAfter -or -not $hasConnection -or
        ($ExpectedAfter -eq 'Available' -and -not [string]::IsNullOrWhiteSpace($afterTenantId))) {
        throw "$CheckName did not leave the selected resource in the expected repaired state."
    }

    return [pscustomobject]@{
        Before = $beforeStatus
        After = $afterStatus
        HasConnectionString = $hasConnection
        TenantId = $afterTenantId
    }
}

function Assert-RepairAudit {
    param(
        [Parameter(Mandatory = $true)][guid]$ResourceId,
        [Parameter(Mandatory = $true)][string]$Token
    )

    $auditResponse = Invoke-SmokeRequest -Method GET -Path '/api/platform/audit-logs?entityName=DatabaseResource&page=1&pageSize=100' -Token $Token
    $auditItems = Get-ResponseItems $auditResponse.Body
    $resourceText = $ResourceId.ToString()
    $matching = @($auditItems | Where-Object {
        $entityId = [string](Get-JsonPropertyValue $_ @('entityId'))
        $newValues = [string](Get-JsonPropertyValue $_ @('newValues'))
        $entityId -eq $resourceText -and $newValues.Contains('DatabaseResourceConnectionRepaired', [StringComparison]::Ordinal)
    })
    if ($matching.Count -eq 0) {
        throw "No safe connection-repair audit event was found for resource $ResourceId."
    }
}

function Wait-ForProvisioningJob {
    param(
        [Parameter(Mandatory = $true)][guid]$TenantId,
        [Parameter(Mandatory = $true)][string]$Token
    )

    for ($attempt = 1; $attempt -le 12; $attempt++) {
        $jobsResponse = Invoke-SmokeRequest -Method GET -Path '/api/platform/operations/provisioning?page=1&pageSize=100' -Token $Token
        $jobs = Get-ResponseItems $jobsResponse.Body
        $job = $jobs | Where-Object {
            [string](Get-JsonPropertyValue $_ @('tenantId')) -eq $TenantId.ToString()
        } | Select-Object -First 1

        if ($null -ne $job) {
            $status = Get-ProvisioningStatusName (Get-JsonPropertyValue $job @('status'))
            if ($status -eq 'Completed') {
                return $job
            }
            if ($status -in @('Failed', 'AwaitingDatabaseCapacity')) {
                $errorCode = [string](Get-JsonPropertyValue $job @('lastErrorCode'))
                throw "Provisioning job reached $status with safe error code '$errorCode'."
            }
        }

        if ($attempt -lt 12) {
            Start-Sleep -Seconds 5
        }
    }

    throw "Provisioning job did not reach Completed within the smoke timeout."
}

try {
    if ($script:SmokeBaseUri.Scheme -ne 'https') {
        throw 'Post-deploy smoke requires an HTTPS BaseUrl.'
    }
    if (-not $AllowMutations) {
        throw 'Post-deploy smoke is protected: pass -AllowMutations only during an approved window.'
    }
    if ([string]::IsNullOrWhiteSpace($ExpectedReleaseCommit) -or
        $ExpectedReleaseCommit -notmatch '^[0-9a-fA-F]{7,40}$') {
        throw 'ExpectedReleaseCommit must be a 7-40 character hexadecimal commit prefix.'
    }
    if ([string]::IsNullOrWhiteSpace($VerifiedBackupReference) -or $VerifiedBackupReference -match '[\r\n]') {
        throw 'VerifiedBackupReference must be a non-empty single-line operator reference.'
    }
    if ($AllocatedResourceId -eq $FailedResourceId) {
        throw 'AllocatedResourceId and FailedResourceId must identify different resources.'
    }

    $platformPassword = Get-RequiredEnvironmentValue $PlatformPasswordEnvironmentVariable
    $allocatedConnection = Get-RequiredEnvironmentValue $AllocatedConnectionEnvironmentVariable
    $failedConnection = Get-RequiredEnvironmentValue $FailedConnectionEnvironmentVariable
    Add-SmokeSecret $platformPassword
    Add-SmokeSecret $allocatedConnection
    Add-SmokeSecret $failedConnection

    if ([string]::IsNullOrWhiteSpace($TestGymSubdomain)) {
        $suffix = [guid]::NewGuid().ToString('N').Substring(0, 12)
        $TestGymSubdomain = "postdeploy-$suffix"
    }
    if ($TestGymSubdomain -notmatch '^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$') {
        throw 'TestGymSubdomain must be a DNS-safe disposable subdomain.'
    }

    $versionResponse = Invoke-SmokeRequest -Method GET -Path '/api/platform/diagnostics/version' -Token $null
    $buildSha = [string](Get-JsonPropertyValue $versionResponse.Body @('buildSha'))
    $expectedPrefix = $ExpectedReleaseCommit.Substring(0, [Math]::Min(12, $ExpectedReleaseCommit.Length))
    if ([string]::IsNullOrWhiteSpace($buildSha) -or $buildSha -eq 'unknown' -or
        -not $buildSha.StartsWith($expectedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Deployed release does not match the expected commit prefix."
    }
    $script:SmokeValues.releaseCommit = $buildSha
    Add-SmokeCheck 'release-version' @{ buildSha = $buildSha }

    $healthResponse = Invoke-SmokeRequest -Method GET -Path '/health' -Token $null
    if ($healthResponse.StatusCode -ne 200 -or $healthResponse.BodyText.Trim().Trim('"') -ne 'Healthy') {
        throw 'The protected post-deploy health check did not return Healthy/200.'
    }
    Add-SmokeCheck 'health' @{ statusCode = $healthResponse.StatusCode }

    $loginResponse = Invoke-SmokeRequest -Method POST -Path '/api/platform/auth/login' -Body @{
        email = $PlatformEmail
        password = $platformPassword
    }
    $script:PlatformToken = [string](Get-JsonPropertyValue $loginResponse.Body @('accessToken'))
    if ([string]::IsNullOrWhiteSpace($script:PlatformToken)) {
        throw 'Platform login returned no access token.'
    }
    Add-SmokeSecret $script:PlatformToken
    Add-SmokeCheck 'platform-authentication'

    $resourceList = Invoke-SmokeRequest -Method GET -Path '/api/platform/database-resources?page=1&pageSize=100' -Token $script:PlatformToken
    $resourceItems = Get-ResponseItems $resourceList.Body
    $resourceJson = $resourceList.Body | ConvertTo-Json -Depth 12 -Compress
    foreach ($forbiddenProperty in @('connectionString', 'encryptedConnectionString', 'password')) {
        if ($resourceJson -match $forbiddenProperty) {
            throw "Database resource response exposed a protected property: $forbiddenProperty."
        }
    }
    $script:SmokeValues.resourceCount = $resourceItems.Count
    Add-SmokeCheck 'resource-list-and-secret-redaction' @{ resourceCount = $resourceItems.Count }

    $safeFailure = Invoke-SmokeRequest -Method POST -Path '/api/platform/tenants' -Token $script:PlatformToken -Headers @{
        'Idempotency-Key' = ('x' * 129)
    } -Body @{} -ExpectedStatus @(400)
    $safeFailureCode = Get-ResponseCode $safeFailure.Body
    if ($safeFailureCode -ne 'IDEMPOTENCY_KEY_INVALID') {
        throw "Safe failure returned unexpected error code '$safeFailureCode'."
    }
    Add-SmokeCheck 'safe-failure-contract' @{ statusCode = $safeFailure.StatusCode; code = $safeFailureCode }

    $allocatedRepair = Repair-Resource -ResourceId $AllocatedResourceId -ConnectionString $allocatedConnection -Token $script:PlatformToken -ExpectedBefore 'Assigned' -ExpectedAfter 'Assigned' -CheckName 'allocated-resource-repair'
    Assert-RepairAudit -ResourceId $AllocatedResourceId -Token $script:PlatformToken
    Add-SmokeCheck 'allocated-resource-repair' @{ before = $allocatedRepair.Before; after = $allocatedRepair.After; audit = $true }

    $failedRepair = Repair-Resource -ResourceId $FailedResourceId -ConnectionString $failedConnection -Token $script:PlatformToken -ExpectedBefore 'Faulted' -ExpectedAfter 'Available' -CheckName 'unallocated-resource-repair'
    Assert-RepairAudit -ResourceId $FailedResourceId -Token $script:PlatformToken
    Add-SmokeCheck 'unallocated-resource-repair' @{ before = $failedRepair.Before; after = $failedRepair.After; tenantId = $failedRepair.TenantId; audit = $true }

    $ownerSuffix = [guid]::NewGuid().ToString('N').Substring(0, 12)
    $ownerEmail = "logicfit-smoke-$ownerSuffix@example.invalid"
    $ownerPassword = "Smoke!$ownerSuffix-Owner9"
    Add-SmokeSecret $ownerPassword
    $gymResponse = Invoke-SmokeRequest -Method POST -Path '/api/platform/tenants' -Token $script:PlatformToken -Headers @{
        'Idempotency-Key' = "post-deploy-smoke:$ownerSuffix"
    } -Body @{
        name = "Post Deploy Smoke Gym $ownerSuffix"
        subdomain = $TestGymSubdomain
        email = $ownerEmail
        ownerEmail = $ownerEmail
        ownerPassword = $ownerPassword
        ownerFullName = 'LogicFit Post Deploy Smoke Owner'
    } -ExpectedStatus @(201)
    $gymId = [guid](Get-JsonPropertyValue $gymResponse.Body @('id'))
    $script:SmokeValues.gymId = $gymId

    $job = Wait-ForProvisioningJob -TenantId $gymId -Token $script:PlatformToken
    $jobId = [guid](Get-JsonPropertyValue $job @('id'))
    $script:SmokeValues.provisioningJobId = $jobId
    $jobResourceId = [guid](Get-JsonPropertyValue $job @('databaseResourceId'))
    if ($jobResourceId -eq [guid]::Empty) {
        throw 'Completed provisioning job did not return a database resource id.'
    }
    Add-SmokeCheck 'gym-provisioning' @{ status = 'Completed'; jobId = $jobId; databaseResourceId = $jobResourceId }

    $gymResources = Invoke-SmokeRequest -Method GET -Path "/api/platform/database-resources?tenantId=$gymId&page=1&pageSize=10" -Token $script:PlatformToken
    $gymResourceItems = Get-ResponseItems $gymResources.Body
    $gymResource = $gymResourceItems | Where-Object {
        [string](Get-JsonPropertyValue $_ @('tenantId')) -eq $gymId.ToString()
    } | Select-Object -First 1
    if ($null -eq $gymResource -or (Get-DatabaseStatusName (Get-JsonPropertyValue $gymResource @('status', 'lifecycleStatus'))) -ne 'Assigned') {
        throw 'The smoke gym does not have an Assigned database resource.'
    }

    $approveResponse = Invoke-SmokeRequest -Method POST -Path "/api/platform/tenants/$gymId/approve" -Token $script:PlatformToken -ExpectedStatus @(200)
    $approvedStatus = [string](Get-JsonPropertyValue $approveResponse.Body @('status'))
    Add-SmokeCheck 'gym-approval' @{ status = $approvedStatus }

    $identityResponse = Invoke-SmokeRequest -Method POST -Path '/api/identity/login' -Body @{
        email = $ownerEmail
        password = $ownerPassword
    }
    $selectionToken = [string](Get-JsonPropertyValue $identityResponse.Body @('workspaceSelectionToken'))
    if ([string]::IsNullOrWhiteSpace($selectionToken)) {
        throw 'Owner identity login returned no workspace selection token.'
    }
    Add-SmokeSecret $selectionToken
    $activeWorkspaces = @(Get-JsonPropertyValue $identityResponse.Body @('activeWorkspaces'))
    if (-not ($activeWorkspaces | Where-Object { [string](Get-JsonPropertyValue $_ @('workspaceId')) -eq $gymId.ToString() })) {
        throw 'Owner identity login did not return the provisioned gym workspace.'
    }

    $tenantLoginResponse = Invoke-SmokeRequest -Method POST -Path '/api/identity/select-workspace' -Body @{
        workspaceSelectionToken = $selectionToken
        workspaceId = $gymId
    }
    $script:TenantToken = [string](Get-JsonPropertyValue $tenantLoginResponse.Body @('accessToken'))
    if ([string]::IsNullOrWhiteSpace($script:TenantToken)) {
        throw 'Owner workspace selection returned no tenant access token.'
    }
    Add-SmokeSecret $script:TenantToken
    Add-SmokeCheck 'owner-login-and-workspace-selection' @{ gymId = $gymId }

    foreach ($tenantPath in @('/api/tenant/plans', '/api/tenant/my-subscription', '/api/tenant/payment-requests', '/api/Notifications/unread-count')) {
        $tenantResponse = Invoke-SmokeRequest -Method GET -Path $tenantPath -Token $script:TenantToken
        if ($tenantResponse.StatusCode -ne 200) {
            throw "Tenant regression endpoint $tenantPath did not return HTTP 200."
        }
    }
    Add-SmokeCheck 'tenant-billing-and-notification-regression'

    foreach ($platformPath in @(
        '/api/platform/subscriptions?page=1&pageSize=1',
        '/api/platform/plans?activeOnly=true&page=1&pageSize=1',
        '/api/platform/payment-requests?page=1&pageSize=1',
        '/api/platform/notifications?isRead=false&page=1&pageSize=1')) {
        $platformResponse = Invoke-SmokeRequest -Method GET -Path $platformPath -Token $script:PlatformToken
        if ($platformResponse.StatusCode -ne 200) {
            throw "Platform regression endpoint $platformPath did not return HTTP 200."
        }
    }
    Add-SmokeCheck 'platform-subscription-plan-payment-notification-regression'

    $script:SmokePassed = $true
}
catch {
    $script:SmokeFailure = $_.Exception.Message
    Write-Error "Post-deploy smoke failed: $script:SmokeFailure"
    throw
}
finally {
    if ([string]::IsNullOrWhiteSpace($ResultPath)) {
        $stamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
        $ResultPath = Join-Path (Join-Path $PSScriptRoot '..\artifacts') "post-deploy-smoke-$stamp.json"
    }
    $ResultPath = [IO.Path]::GetFullPath($ResultPath)
    $resultDirectory = Split-Path -Parent $ResultPath
    if (-not [string]::IsNullOrWhiteSpace($resultDirectory)) {
        New-Item -ItemType Directory -Path $resultDirectory -Force | Out-Null
    }

    $result = [ordered]@{
        passed = $script:SmokePassed
        generatedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
        baseUrl = $script:SmokeBaseUri.AbsoluteUri
        expectedReleaseCommit = $ExpectedReleaseCommit
        verifiedBackupReference = $VerifiedBackupReference
        operatorApproval = $OperatorApproval
        values = $script:SmokeValues
        checks = @($script:SmokeChecks)
        failure = $script:SmokeFailure
    }
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [IO.File]::WriteAllText($ResultPath, ($result | ConvertTo-Json -Depth 12), $utf8NoBom)
    Write-Host "Auditable smoke result: $ResultPath"
}
