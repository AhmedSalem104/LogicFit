[CmdletBinding()]
param(
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\docs\API-ENDPOINT-CATALOG.md')
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
$controllerFiles = @(
    Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'LogicFit.API') -Recurse -Filter '*Controller.cs' -File
)
$sourceFiles = Get-ChildItem -LiteralPath $repositoryRoot -Recurse -Filter '*.cs' -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

function Get-TypeProperties {
    param([string]$TypeName)

    $shortName = ($TypeName -replace '.*\.', '' -replace '<.*', '' -replace '\?', '').Trim()
    if ([string]::IsNullOrWhiteSpace($shortName) -or $shortName -match '^(Guid|String|string|Int32|int|Boolean|bool|Decimal|decimal|DateTime|DateTimeOffset|IFormFile)$') {
        return @()
    }

    $candidate = $sourceFiles | Where-Object { $_.BaseName -eq $shortName } | Select-Object -First 1
    if ($null -eq $candidate) { return @() }

    $source = Get-Content -LiteralPath $candidate.FullName -Raw
    $properties = [System.Collections.Generic.List[string]]::new()
    foreach ($match in [regex]::Matches($source, '(?m)^\s*public\s+(?:required\s+)?(?<type>[A-Za-z_][A-Za-z0-9_\.<>?,\[\]]*)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\{\s*get;')) {
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

    $authorization = [regex]::Matches($all, '\[Authorize(?<args>\([^\]]*\))?\]') | Select-Object -Last 1
    if ($null -eq $authorization) { return 'Server default (not declared explicitly)' }

    $args = $authorization.Groups['args'].Value
    if ($args -match 'Policy\s*=\s*(?<policy>[^,\)]+)') {
        return ('JWT + Policy: `{0}`' -f $matches['policy'].Trim())
    }
    if ($args -match 'Roles\s*=\s*"(?<roles>[^"]+)"') {
        return ('JWT + Roles: `{0}`' -f $matches['roles'])
    }
    return 'JWT required'
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
        $route = ('/{0}' -f (($baseRoute.Trim('/') + '/' + $routeSuffix.Trim('/')).TrimEnd('/')))
        if ($route -eq '/') { $route = '/' + $baseRoute.Trim('/') }

        $methodAttributes = $signatureMatch.Groups['attributes'].Value
        $returnType = $signatureMatch.Groups['return'].Value
        $entries.Add([PSCustomObject]@{
            Surface = $surface
            Controller = $controllerName
            Method = $httpMatch.Groups['method'].Value.ToUpperInvariant()
            Route = $route
            Action = $signatureMatch.Groups['name'].Value
            Authorization = Get-AuthorizationLabel -ClassAttributes $classAttributes -MethodAttributes $methodAttributes
            Inputs = Get-InputDescription -Parameters $signatureMatch.Groups['parameters'].Value
            Responses = (Get-ResponseTypes -Attributes $methodAttributes -ReturnType $returnType) -join '<br>'
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
            $lines.Add(('- **Inputs:** {0}' -f $endpoint.Inputs))
            $lines.Add(('- **Declared response:** {0}' -f $endpoint.Responses))
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
