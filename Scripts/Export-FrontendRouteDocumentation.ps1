[CmdletBinding()]
param(
    [string]$TenantPath,
    [string]$AdminPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$backendRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectsRoot = Split-Path -Parent $backendRoot
if ([string]::IsNullOrWhiteSpace($TenantPath)) { $TenantPath = Join-Path $projectsRoot 'LogicFit_Angular' }
if ([string]::IsNullOrWhiteSpace($AdminPath)) { $AdminPath = Join-Path $projectsRoot 'LogiFit_Platform_Admin_Dashboard' }

function Join-RoutePath {
    param([string]$Prefix, [string]$Path)

    $parts = @(@($Prefix, $Path) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim('/') })
    if ($parts.Count -eq 0) { return '/' }
    return '/' + ($parts -join '/')
}

function Read-RouteDeclarations {
    param(
        [string]$FilePath,
        [string]$Prefix,
        [string[]]$AllowedPaths
    )

    if (-not (Test-Path -LiteralPath $FilePath)) { return @() }
    $source = [System.IO.File]::ReadAllText($FilePath, [System.Text.Encoding]::UTF8)
    $items = [System.Collections.Generic.List[object]]::new()

    # Angular route declarations are formatted in several valid ways. Match a
    # path with the metadata belonging to the same declaration, while stopping
    # before the next path declaration. This keeps the generated documentation
    # stable when formatting changes between one-line and multi-line objects.
    $pattern = "(?s)path:\s*'(?<path>[^']*)'(?<body>(?:(?!path:\s*').){0,1800}?)loadComponent:\s*\(\)\s*=>\s*import\('(?<component>[^']+)'\)"
    foreach ($match in [regex]::Matches($source, $pattern)) {
        $path = $match.Groups['path'].Value
        if ([string]::IsNullOrWhiteSpace($path)) { continue }
        if ($AllowedPaths -and $path -notin $AllowedPaths) { continue }
        $body = $match.Groups['body'].Value
        $titleMatch = [regex]::Match($body, "title:\s*'(?<title>[^']*)'")
        $guardMatch = [regex]::Match($body, 'canActivate:\s*\[(?<guards>[^\]]+)\]')
        $guards = if ($guardMatch.Success) { $guardMatch.Groups['guards'].Value.Trim() } else { '' }
        $items.Add([PSCustomObject]@{
            Route = Join-RoutePath -Prefix $Prefix -Path $path
            Guard = if ($guards) { $guards } else { 'Inherited route guard' }
            Component = $match.Groups['component'].Value
            Title = if ($titleMatch.Success) { $titleMatch.Groups['title'].Value } else { '' }
        })
    }
    return @($items)
}

function Get-ScreenPurpose {
    param([string]$Route)

    switch -Regex ($Route) {
        'identity|auth|register|login|password|invite|join' { return 'Identity, onboarding, access, and application tracking.' }
        'dashboard' { return 'Live indicators and the next operational decision.' }
        'application|workspace' { return 'Workspace creation, membership, or activation workflow.' }
        'tenant|gym|profile|branding' { return 'Workspace profile, lifecycle, or personal settings.' }
        'coach|trainee|client' { return 'Relationship management and role-scoped client work.' }
        'workout|program|diet|meal|exercise|food|muscle|measurement|progress' { return 'Training, nutrition, measurements, and progress.' }
        'subscription|payment|invoice|expense|coupon|tax|payroll|commission' { return 'Commercial, billing, finance, or payroll operations.' }
        'attendance|gate|appointment|class|schedule|shift|leave' { return 'Attendance, scheduling, classes, or staff time.' }
        'branch|room|equipment|product|stock|supplier|pos|maintenance' { return 'Facilities, inventory, sales, or maintenance.' }
        'report|operation|audit|alert|notification|chat|challenge' { return 'Monitoring, communication, reporting, or governance.' }
        default { return 'Role-scoped LogicFit workspace screen.' }
    }
}

function Get-ApiFamily {
    param([string]$Route)

    switch -Regex ($Route) {
        'identity|auth|register|invite|join|application' { return '/api/identity, /api/workspace-applications, /api/workspace-invites' }
        'dashboard|report' { return '/api/reports and dashboard endpoints' }
        'workout|program|exercise|muscle' { return '/api/workoutprograms, /api/workoutsessions, /api/exercises, /api/muscles' }
        'diet|meal|food' { return '/api/dietplans, /api/meal-logs, /api/foods' }
        'measurement|progress' { return '/api/bodymeasurements and client progress endpoints' }
        'client|trainee|coach|team' { return '/api/clients, /api/coach-clients, /api/workspace members' }
        'subscription|payment|invoice|expense|coupon|tax|payroll|commission' { return '/api/subscriptions, /api/payments, /api/invoices, finance endpoints' }
        'attendance|gate|appointment|class|schedule|shift|leave' { return 'attendance, appointments, classes, and HR endpoints' }
        'branch|room|equipment|product|stock|supplier|pos|maintenance' { return 'facilities, inventory, POS, and maintenance endpoints' }
        'backup|database-resource' { return '/api/platform/backups and /api/platform/database-resources' }
        'plan|feature|quota|dependency' { return '/api/platform/plans and /api/platform/features' }
        default { return 'See the generated API endpoint catalog for the component service.' }
    }
}

function Get-RouteTable {
    param([object[]]$Routes)

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add('| Route | Guard / permission source | Component source | Purpose and benefit | Primary API family |')
    $lines.Add('|---|---|---|---|---|')
    foreach ($route in ($Routes | Sort-Object Route -Unique)) {
        $purpose = Get-ScreenPurpose -Route $route.Route
        $apiFamily = Get-ApiFamily -Route $route.Route
        $lines.Add(('| `{0}` | `{1}` | `{2}` | {3} | `{4}` |' -f $route.Route, ($route.Guard -replace '\|', '\|'), ($route.Component -replace '\|', '\|'), $purpose, $apiFamily))
    }
    return ($lines -join [Environment]::NewLine)
}

function Replace-GeneratedSection {
    param([string]$FilePath, [string]$Table)

    $content = [System.IO.File]::ReadAllText($FilePath, [System.Text.Encoding]::UTF8)
    $start = '<!-- GENERATED ROUTES START -->'
    $end = '<!-- GENERATED ROUTES END -->'
    $startIndex = $content.IndexOf($start, [StringComparison]::Ordinal)
    $endIndex = $content.IndexOf($end, [StringComparison]::Ordinal)
    if ($startIndex -lt 0 -or $endIndex -lt 0 -or $endIndex -lt $startIndex) {
        throw "Generated route markers are missing in $FilePath"
    }
    $prefix = $content.Substring(0, $startIndex + $start.Length)
    $suffix = $content.Substring($endIndex)
    [System.IO.File]::WriteAllText($FilePath, ($prefix + [Environment]::NewLine + $Table + [Environment]::NewLine + $suffix), [System.Text.UTF8Encoding]::new($false))
}

$tenantRoutes = [System.Collections.Generic.List[object]]::new()
$tenantApp = Join-Path $TenantPath 'src\app\app.routes.ts'
$identityPaths = @('login', 'register', 'verify-email', 'reset-password', 'application-status', 'accept-invite', 'join-client')
$tenantRoutes.AddRange(@(Read-RouteDeclarations -FilePath $tenantApp -Prefix 'identity' -AllowedPaths $identityPaths))
$tenantRoutes.AddRange(@(Read-RouteDeclarations -FilePath $tenantApp -Prefix '' -AllowedPaths @('gym-unavailable')))
$tenantRoutes.AddRange(@(Read-RouteDeclarations -FilePath (Join-Path $TenantPath 'src\app\features\auth\auth.routes.ts') -Prefix 'auth'))
$tenantRoutes.AddRange(@(Read-RouteDeclarations -FilePath (Join-Path $TenantPath 'src\app\features\owner\owner.routes.ts') -Prefix 'owner'))
$tenantRoutes.AddRange(@(Read-RouteDeclarations -FilePath (Join-Path $TenantPath 'src\app\features\coach\coach.routes.ts') -Prefix 'coach'))
$tenantRoutes.AddRange(@(Read-RouteDeclarations -FilePath (Join-Path $TenantPath 'src\app\features\client\client.routes.ts') -Prefix 'client'))

$adminRoutes = @(Read-RouteDeclarations -FilePath (Join-Path $AdminPath 'src\app\app.routes.ts') -Prefix '') |
    Where-Object { $_.Route -notin @('/auth/login', '/') -and $_.Component -ne 'Route redirect or shell' }

Replace-GeneratedSection -FilePath (Join-Path $TenantPath 'docs\COMPLETE-SCREEN-DOCUMENTATION.md') -Table (Get-RouteTable -Routes $tenantRoutes)
Replace-GeneratedSection -FilePath (Join-Path $AdminPath 'docs\COMPLETE-PLATFORM-ADMIN-DOCUMENTATION.md') -Table (Get-RouteTable -Routes $adminRoutes)

Copy-Item -LiteralPath (Join-Path $backendRoot 'docs\API-ENDPOINT-CATALOG.md') -Destination (Join-Path $TenantPath 'docs\API-ENDPOINT-CATALOG.md') -Force
Copy-Item -LiteralPath (Join-Path $backendRoot 'docs\API-ENDPOINT-CATALOG.md') -Destination (Join-Path $AdminPath 'docs\API-ENDPOINT-CATALOG.md') -Force

Write-Host "Generated Tenant routes: $($tenantRoutes.Count)"
Write-Host "Generated Platform Admin routes: $($adminRoutes.Count)"
