[CmdletBinding()]
param(
    [string]$OutputPath
)

<##
Builds the human-readable endpoint catalog directly from the ASP.NET Core
controllers.  The catalog deliberately records the public HTTP contract, not
implementation details, so it is safe to commit and useful to frontend teams.

Run from the repository root:
    powershell -ExecutionPolicy Bypass -File .\Scripts\Export-ApiEndpointCatalog.ps1
##>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $PSScriptRoot '..\docs\API-ENDPOINT-CATALOG.md'
}
$controllerFiles = @(
    Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'LogicFit.API') -Recurse -Filter '*Controller.cs' -File
)
$sourceFiles = Get-ChildItem -LiteralPath $repositoryRoot -Recurse -Filter '*.cs' -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
$typeSourceMap = @{}
foreach ($file in $sourceFiles) {
    $fileSource = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    foreach ($typeMatch in [regex]::Matches($fileSource, '(?m)\b(?:class|record|struct)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\b')) {
        $typeName = $typeMatch.Groups['name'].Value
        if (-not $typeSourceMap.ContainsKey($typeName)) {
            $typeSourceMap[$typeName] = $fileSource
        }
    }
}

function Get-TypeProperties {
    param([string]$TypeName)

    $shortName = ($TypeName -replace '.*\.', '' -replace '<.*', '' -replace '\?', '').Trim()
    if ([string]::IsNullOrWhiteSpace($shortName) -or $shortName -match '^(Guid|String|string|Int32|int|Boolean|bool|Decimal|decimal|DateTime|DateTimeOffset|IFormFile)$') {
        return @()
    }

    # DTOs are frequently grouped in a shared file or declared as nested
    # records inside a controller. Do not rely on the file name matching the
    # type name; search the source set for the actual declaration.
    $source = if ($typeSourceMap.ContainsKey($shortName)) { $typeSourceMap[$shortName] } else { $null }
    if ($null -eq $source) { return @() }
    $properties = [System.Collections.Generic.List[string]]::new()
    foreach ($match in [regex]::Matches($source, '(?ms)(?<attributes>(?:^\s*\[[^\]]+\]\s*)*)^\s*public\s+(?:required\s+)?(?<type>[A-Za-z_][A-Za-z0-9_\.<>?,\[\]]*)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\{\s*get;')) {
        if ($match.Groups['attributes'].Value -match 'JsonIgnore') { continue }
        $properties.Add(('`{0}`: {1}' -f $match.Groups['name'].Value, $match.Groups['type'].Value))
    }

    # Positional records use constructor parameters instead of property blocks.
    if ($properties.Count -eq 0) {
        $recordMatch = [regex]::Match($source, ('(?s)record\s+' + [regex]::Escape($shortName) + '\s*\((?<args>.*?)\)'))
        if ($recordMatch.Success) {
            foreach ($argument in ($recordMatch.Groups['args'].Value -split ',')) {
                $part = $argument.Trim() -replace '\s*=.*$', ''
                if ($part -match '^(?<type>[^\s]+)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)$') {
                    $properties.Add(('`{0}`: {1}' -f $matches['name'], $matches['type']))
                }
            }
        }
    }

    return @($properties | Select-Object -Unique | Select-Object -First 24)
}

function Get-AuthorizationLabel {
    param([string]$ClassAttributes, [string]$MethodAttributes)

    $all = "$ClassAttributes`n$MethodAttributes"
    if ($MethodAttributes -match 'AllowAnonymous') { return 'Anonymous (no token required)' }

    $authorizations = @([regex]::Matches($all, '\[Authorize(?<args>\([^\]]*\))?\]'))
    if ($authorizations.Count -eq 0) { return 'Server default (not declared explicitly)' }

    $policies = [System.Collections.Generic.List[string]]::new()
    $roles = [System.Collections.Generic.List[string]]::new()
    $hasPlainAuthorization = $false
    foreach ($authorization in $authorizations) {
        $args = $authorization.Groups['args'].Value
        if ($args -match 'Policy\s*=\s*(?<policy>[^,\)]+)') {
            $policies.Add($matches['policy'].Trim())
        }
        elseif ($args -match 'Roles\s*=\s*"(?<roles>[^"]+)"') {
            $roles.Add($matches['roles'])
        }
        else {
            $hasPlainAuthorization = $true
        }
    }

    $uniquePolicies = @($policies | Select-Object -Unique)
    $uniqueRoles = @($roles | Select-Object -Unique)
    if ($uniquePolicies.Count -eq 1 -and $uniqueRoles.Count -eq 0) {
        return ('JWT + Policy: `{0}`' -f $uniquePolicies[0])
    }
    if ($uniquePolicies.Count -gt 1 -and $uniqueRoles.Count -eq 0) {
        return ('JWT + Policies: {0}' -f (($uniquePolicies | ForEach-Object { '`' + $_ + '`' }) -join ' AND '))
    }
    if ($uniqueRoles.Count -eq 1 -and $uniquePolicies.Count -eq 0) {
        return ('JWT + Roles: `{0}`' -f $uniqueRoles[0])
    }

    $requirements = [System.Collections.Generic.List[string]]::new()
    foreach ($policy in $uniquePolicies) { $requirements.Add(('Policy `{0}`' -f $policy)) }
    foreach ($role in $uniqueRoles) { $requirements.Add(('Roles `{0}`' -f $role)) }
    if ($hasPlainAuthorization -and $requirements.Count -eq 0) { return 'JWT required' }
    return ('JWT + {0}' -f ($requirements -join ' AND '))
}

function Get-ResponseTypes {
    param([string]$Attributes, [string]$ReturnType)

    $responses = [System.Collections.Generic.List[string]]::new()
    foreach ($match in [regex]::Matches($Attributes, '\[ProducesResponseType(?:\((?<args>[^\]]*)\))?\]')) {
        $args = $match.Groups['args'].Value.Trim()
        if ($args) { $responses.Add($args) }
    }
    if ($responses.Count -eq 0) { $responses.Add($ReturnType) }
    return @($responses | Select-Object -Unique)
}

function Get-ResponseSchema {
    param([string]$Declaration)

    $normalized = $Declaration.Trim()
    $normalized = $normalized -replace 'typeof\((?<type>[^\)]+)\)', '$1'
    $normalized = $normalized -replace 'StatusCodes\.[A-Za-z0-9_]+', ''
    $normalized = $normalized.Trim(' ', ',')

    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return 'No response body declared.'
    }

    $typeName = $normalized
    while ($typeName -match '^(?:Task|ActionResult)\s*<(?<inner>.*)>$') {
        $typeName = $matches['inner'].Trim()
    }

    $candidates = [System.Collections.Generic.List[string]]::new()
    $candidates.Add($typeName)
    if ($typeName -match '<(?<inner>[^<>]+)>') { $candidates.Add($matches['inner'].Trim()) }

    foreach ($candidate in $candidates) {
        $properties = @(Get-TypeProperties -TypeName $candidate)
        if ($properties.Count -gt 0) {
            return ('`{0}` with fields: {{ {1} }}' -f $normalized, ($properties -join '; '))
        }
    }

    if ($normalized -match 'NoContent|void|204') { return 'No response body (HTTP 204).' }
    if ($normalized -match 'File|IActionResult|ActionResult') { return ('`{0}`; body is action-specific or a file/blob.' -f $normalized) }
    return ('`{0}`; concrete properties are not declared in a discoverable DTO.' -f $normalized)
}

function Get-ControllerPurpose {
    param([string]$Controller)

    $purposeMap = [ordered]@{
        'PlatformAuth|Identity|Auth' = 'Identity, login, and session issuance/rotation.'
        'WorkspaceApplication|FreelanceTeamApplication' = 'Gym and FreelanceCoach applications, review, and provisioning.'
        'PlatformTenant|Tenants' = 'Workspace lifecycle, isolation, status, and owner membership.'
        'PlatformDatabaseResource|DatabaseResource|TenantDatabase' = 'Database resource allocation, connectivity, migrations, and mapping.'
        'PlatformBackup|TenantBackup|Restore' = 'Backup creation, checksum verification, retry, and controlled restore.'
        'PlatformPayment|Payment|Invoice|Subscription|Billing' = 'Payments, invoices, subscriptions, and financial transitions.'
        'PlatformPlan|Feature|Quota|Dependency' = 'SaaS product configuration: plans, features, quotas, and dependencies.'
        'PlatformDashboard|PlatformReport|Report' = 'Operational and financial indicators and reports.'
        'PlatformAdministrator|PlatformRole|PlatformAudit|PlatformAlert|PlatformNotification' = 'Governance, permissions, audit history, and alerts.'
        'Operations|Maintenance|Job|Outbox' = 'Background jobs, Outbox messages, and operational monitoring.'
        'Coach|Workout|Diet|Exercise|Food|Muscle|BodyMeasurement|MealLog' = 'Training, nutrition, measurements, and content libraries.'
        'ClientDashboard|Client|CoachClient|WorkspaceClient' = 'Client management, trainee portal, and coach relationships.'
        'Attendance|GateAccess|Appointment|Class|Schedule' = 'Attendance, appointments, classes, and scheduling.'
        'Branch|Room|Equipment|Supplier|Product|Stock|Sale|Expense|Payroll|Employee|Leave|Commission' = 'Gym operations, facilities, finance, inventory, and staff.'
        'Notification|Chat|Challenge' = 'Communication, notifications, and challenges.'
    }

    foreach ($key in $purposeMap.Keys) {
        if ($Controller -match $key) { return $purposeMap[$key] }
    }

    return ('LogicFit API module `{0}`.' -f ($Controller -replace 'Controller$', ''))
}

function Get-OperationProfile {
    param([string]$Method, [string]$Action, [string]$Controller)

    $combined = "$Action $Controller"
    if ($Method -in @('GET', 'HEAD')) {
        return [PSCustomObject]@{
            Kind = 'Read / Query'
            Importance = 'Reads the authoritative state or data with tenant isolation and authorization.'
            Benefit = 'Gives the UI and operators reliable information for decisions without changing server state.'
            Safety = 'Read-only; handle 401/403/404/429 and show explicit loading, empty, or error states.'
        }
    }

    if ($combined -match 'approve|reject|transition|activate|suspend|archive|restore|retry|provision|migrat|status|reset|logout|assign|join|read-all|check-out|complete|start|end') {
        return [PSCustomObject]@{
            Kind = 'Workflow / Lifecycle Command'
            Importance = 'Moves an entity through a sensitive business state or executes a workflow command instead of generic CRUD.'
            Benefit = 'Preserves lifecycle consistency, auditability, isolation, and idempotency for approval, provisioning, or suspension.'
            Safety = 'Validate current state, RowVersion/idempotency, and authorization; return 409 for state conflicts.'
        }
    }

    if ($Method -eq 'DELETE') {
        return [PSCustomObject]@{
            Kind = 'Delete / Remove'
            Importance = 'Removes a configuration record or relationship that the domain explicitly allows to be deleted.'
            Benefit = 'Cleans non-historical configuration without deleting immutable financial or operational history.'
            Safety = 'Enforce authorization, isolation, and duplicate prevention; use lifecycle or reversal for historical records.'
        }
    }

    if ($Method -in @('PUT', 'PATCH')) {
        return [PSCustomObject]@{
            Kind = 'Update / Patch'
            Importance = 'Updates an existing entity while preserving authorization and optimistic concurrency rules.'
            Benefit = 'Corrects or configures data without creating duplicates or breaking existing relationships.'
            Safety = 'Validate input, isolation, and RowVersion where required; return 400 for validation and 409 for conflicts.'
        }
    }

    return [PSCustomObject]@{
        Kind = 'Create / Command'
        Importance = 'Creates an entity or executes a command inside a defined business module.'
        Benefit = 'Turns user input into an audited server operation and links required entities transactionally where needed.'
        Safety = 'Use validation, unique constraints, and idempotency for commands that may be retried.'
    }
}

function Get-CommonFailureContract {
    param([string]$Method, [string]$Authorization)

    $codes = [System.Collections.Generic.List[string]]::new()
    if ($Authorization -notmatch 'Anonymous') { $codes.Add('401: missing or expired session') }
    $codes.Add('403: insufficient permission or workspace scope')
    $codes.Add('400: invalid input or rejected business rule')
    $codes.Add('404: resource missing or outside the visible scope')
    if ($Method -ne 'GET' -and $Method -ne 'HEAD') { $codes.Add('409: state, RowVersion, or duplicate conflict') }
    $codes.Add('429: rate limit exceeded')
    $codes.Add('500: unexpected server error; inspect state before retrying a mutation')
    return ($codes -join ' · ')
}

function Get-InputDescription {
    param([string]$Parameters)

    $items = [System.Collections.Generic.List[string]]::new()
    $bodyMatches = [regex]::Matches($Parameters, '\[FromBody\]\s*(?<type>[A-Za-z_][A-Za-z0-9_\.<>?,\[\]]*)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)')
    foreach ($body in $bodyMatches) {
        $schema = @(Get-TypeProperties -TypeName $body.Groups['type'].Value)
        $detail = if ($schema.Count -gt 0) { ' { ' + ($schema -join '; ') + ' }' } else { '' }
        $items.Add(('Body `{0}`: `{1}`{2}' -f $body.Groups['name'].Value, $body.Groups['type'].Value, $detail))
    }

    $queryMatches = [regex]::Matches($Parameters, '\[FromQuery\]\s*(?<type>[A-Za-z_][A-Za-z0-9_\.<>?,\[\]]*)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)')
    foreach ($query in $queryMatches) {
        $items.Add(('Query `{0}`: `{1}`' -f $query.Groups['name'].Value, $query.Groups['type'].Value))
    }

    $formMatches = [regex]::Matches($Parameters, '\[FromForm\]\s*(?<type>[A-Za-z_][A-Za-z0-9_\.<>?,\[\]]*)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)')
    foreach ($form in $formMatches) {
        $items.Add(('Form `{0}`: `{1}`' -f $form.Groups['name'].Value, $form.Groups['type'].Value))
    }

    $routeMatches = [regex]::Matches($Parameters, '\[FromRoute\]\s*(?<type>[A-Za-z_][A-Za-z0-9_\.<>?,\[\]]*)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)')
    foreach ($route in $routeMatches) {
        $items.Add(('Path `{0}`: `{1}`' -f $route.Groups['name'].Value, $route.Groups['type'].Value))
    }

    # Preserve the raw handler signature as well. This captures conventional
    # route values (for example `Guid id`) which are intentionally not always
    # annotated with [FromRoute] in ASP.NET Core controllers.
    if (-not [string]::IsNullOrWhiteSpace($Parameters)) {
        $clean = ($Parameters -replace '\s+', ' ').Trim()
        $clean = $clean -replace '(?:(?<=^)|(?<=,\s))CancellationToken\s+[A-Za-z_][A-Za-z0-9_]*(?:\s*=\s*[^,]+)?\s*,?\s*', ''
        $clean = $clean.Trim(' ', ',')
        if ($clean) { $items.Add(('Handler signature: `{0}`' -f $clean)) }
    }

    if ($items.Count -eq 0) { return 'No request input.' }
    return ($items -join '<br>')
}

$entries = [System.Collections.Generic.List[object]]::new()
foreach ($file in $controllerFiles) {
    $source = Get-Content -LiteralPath $file.FullName -Raw
    $classMatch = [regex]::Match($source, 'public\s+(?:sealed\s+)?class\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)')
    if (-not $classMatch.Success) { continue }

    $controllerName = $classMatch.Groups['name'].Value
    $controllerToken = $controllerName -replace 'Controller$', ''
    $classPrelude = $source.Substring(0, $classMatch.Index)
    $classAttributes = ($classPrelude -split '(?m)^\s*namespace\s+' | Select-Object -Last 1)
    $routeMatch = [regex]::Matches($classPrelude, '\[Route\("(?<route>[^"]+)"\)\]') | Select-Object -Last 1
    if ($null -eq $routeMatch) { continue }
    $baseRoute = $routeMatch.Groups['route'].Value -replace '\[controller\]', $controllerToken
    $surface = if ($file.FullName -match 'LogicFit\.API\\Features\\Platform\\') { 'Platform API' } else { 'Tenant API' }

    foreach ($httpMatch in [regex]::Matches($source, '\[(?<verb>Http(?<method>Get|Post|Put|Delete|Patch|Head|Options))(?<args>\([^\]]*\))?\]')) {
        $tail = $source.Substring($httpMatch.Index + $httpMatch.Length)
        $signatureMatch = [regex]::Match($tail, '(?s)^(?<attributes>.*?)\bpublic\s+(?:async\s+)?(?<return>Task(?:<.*?>)?|IActionResult|ActionResult(?:<.*?>)?)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\((?<parameters>.*?)\)\s*(?:\{|=>)')
        if (-not $signatureMatch.Success) { continue }

        $routeSuffix = ''
        $literalRoute = [regex]::Match($httpMatch.Groups['args'].Value, '"(?<route>[^"]*)"')
        if ($literalRoute.Success) { $routeSuffix = $literalRoute.Groups['route'].Value }

        # ASP.NET Core treats an action template that starts with `/` as an
        # absolute route and ignores the controller prefix.  Keep the catalog
        # aligned with runtime routing instead of producing paths such as
        # `/api/freelance/team/applications/api/freelance/team/invites`.
        $routePath = if ($routeSuffix.StartsWith('/')) {
            $routeSuffix.Trim('/')
        } elseif ([string]::IsNullOrWhiteSpace($routeSuffix)) {
            $baseRoute.Trim('/')
        } else {
            ($baseRoute.Trim('/') + '/' + $routeSuffix.Trim('/')).Trim('/')
        }
        $route = ('/{0}' -f $routePath)

        $methodAttributes = $signatureMatch.Groups['attributes'].Value
        $returnType = $signatureMatch.Groups['return'].Value
        $responses = @(Get-ResponseTypes -Attributes $methodAttributes -ReturnType $returnType)
        $profile = Get-OperationProfile -Method $httpMatch.Groups['method'].Value.ToUpperInvariant() -Action $signatureMatch.Groups['name'].Value -Controller $controllerName
        $entries.Add([PSCustomObject]@{
            Surface = $surface
            Controller = $controllerName
            Method = $httpMatch.Groups['method'].Value.ToUpperInvariant()
            Route = $route
            Action = $signatureMatch.Groups['name'].Value
            Authorization = Get-AuthorizationLabel -ClassAttributes $classAttributes -MethodAttributes $methodAttributes
            Inputs = Get-InputDescription -Parameters $signatureMatch.Groups['parameters'].Value
            Responses = $responses -join '<br>'
            ResponseSchema = (($responses | ForEach-Object { Get-ResponseSchema -Declaration $_ }) -join '<br>')
            Purpose = Get-ControllerPurpose -Controller $controllerName
            OperationKind = $profile.Kind
            Importance = $profile.Importance
            Benefit = $profile.Benefit
            Safety = $profile.Safety
            FailureContract = Get-CommonFailureContract -Method $httpMatch.Groups['method'].Value.ToUpperInvariant() -Authorization (Get-AuthorizationLabel -ClassAttributes $classAttributes -MethodAttributes $methodAttributes)
        })
    }
}

$ordered = $entries | Sort-Object Surface, Route, Method
$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('# Complete API Endpoint Catalog')
$lines.Add('')
$lines.Add('> **Source of truth:** this document is generated from the API controllers by `Scripts/Export-ApiEndpointCatalog.ps1`. Do not edit endpoint rows manually; change the controller, rerun the script, and include the refreshed catalog in the same Pull Request.')
$lines.Add('')
$lines.Add(('Generated: `{0:yyyy-MM-dd HH:mm} UTC`  |  Total endpoints: **{1}**' -f (Get-Date).ToUniversalTime(), $ordered.Count))
$lines.Add('')
$lines.Add('## Contract rules')
$lines.Add('')
$lines.Add('- **Tenant API** routes normally start with `/api/...`; tenant identity is derived from the JWT and tenant middleware. A frontend-supplied `TenantId` is never a security boundary.')
$lines.Add('- **Platform API** routes start with `/api/platform/...` and require a Platform JWT and permission unless the entry explicitly says anonymous.')
$lines.Add('- Common outcomes: `400` validation, `401` missing/expired token, `403` insufficient permission, `404` resource missing, `409` conflict/duplicate, `429` rate limited, and `500` unexpected server error.')
$lines.Add('- Paginated Platform collections normally return `{ items, totalCount, page, pageSize, totalPages, hasPreviousPage, hasNextPage }`. Pages are one-based and page size is capped at 100.')
$lines.Add('')

foreach ($surfaceGroup in ($ordered | Group-Object Surface)) {
    $lines.Add(('## {0}' -f $surfaceGroup.Name))
    $lines.Add('')
    foreach ($controllerGroup in ($surfaceGroup.Group | Group-Object Controller | Sort-Object Name)) {
        $lines.Add(('### {0}' -f ($controllerGroup.Name -replace 'Controller$', '')))
        $lines.Add('')
        foreach ($endpoint in ($controllerGroup.Group | Sort-Object Route, Method)) {
            $lines.Add(('#### `{0} {1}` - `{2}`' -f $endpoint.Method, $endpoint.Route, $endpoint.Action))
            $lines.Add('')
            $lines.Add(('- **Access:** {0}' -f $endpoint.Authorization))
            $lines.Add(('- **Business purpose:** {0}' -f $endpoint.Purpose))
            $lines.Add(('- **Operation profile:** `{0}`' -f $endpoint.OperationKind))
            $lines.Add(('- **Why it matters:** {0}' -f $endpoint.Importance))
            $lines.Add(('- **Business benefit:** {0}' -f $endpoint.Benefit))
            $lines.Add(('- **Inputs:** {0}' -f $endpoint.Inputs))
            $lines.Add(('- **Declared response:** {0}' -f $endpoint.Responses))
            $lines.Add(('- **Response schema:** {0}' -f $endpoint.ResponseSchema))
            $lines.Add(('- **Failure contract:** {0}' -f $endpoint.FailureContract))
            $lines.Add(('- **Safety/side effects:** {0}' -f $endpoint.Safety))
            $lines.Add('')
        }
    }
}

while ($lines.Count -gt 0 -and [string]::IsNullOrWhiteSpace($lines[$lines.Count - 1])) {
    $lines.RemoveAt($lines.Count - 1)
}

$candidateOutput = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
} else {
    Join-Path $repositoryRoot $OutputPath
}
$resolvedOutput = [System.IO.Path]::GetFullPath($candidateOutput)
$outputDirectory = Split-Path -Parent $resolvedOutput
if (-not (Test-Path -LiteralPath $outputDirectory)) { New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null }
while ($lines.Count -gt 0 -and [string]::IsNullOrWhiteSpace($lines[$lines.Count - 1])) {
    $lines.RemoveAt($lines.Count - 1)
}
[System.IO.File]::WriteAllLines($resolvedOutput, $lines, [System.Text.UTF8Encoding]::new($false))
Write-Host "Generated $($ordered.Count) endpoint entries: $resolvedOutput"
