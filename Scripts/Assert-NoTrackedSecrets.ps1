[CmdletBinding()]
param(
    [string] $RepositoryRoot = (Get-Location).Path
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$trackedFiles = @(git -C $root ls-files)
$violations = [System.Collections.Generic.List[string]]::new()

foreach ($relativePath in $trackedFiles) {
    $normalizedPath = $relativePath -replace '\\', '/'

    # Examples, documentation, tests, and CI-only test credentials are not production secrets.
    # Production configuration and deployment profiles are never allowed to be tracked.
    if ($normalizedPath -match '(?i)(^|/)(appsettings\.production\.json|\.env|.*\.publishsettings|.*\.pubxml\.user)$') {
        $violations.Add("tracked secret-bearing file: $normalizedPath")
        continue
    }

    if ($normalizedPath -match '(?i)(^|/)(docs/|.*\.example\.(json|yml|yaml|env)$|\.github/workflows/|LogicFit\.Tests/|.*seeddata/)|\.md$') {
        continue
    }

    $fullPath = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        continue
    }

    $content = Get-Content -LiteralPath $fullPath -Raw
    if ($content -match '(?i)-----BEGIN (?:RSA |OPENSSH |EC |DSA )?PRIVATE KEY-----') {
        $violations.Add("private key material: $normalizedPath")
        continue
    }

    $pattern = '(?im)(?:"|''|\b)(?:password|secret|api[_-]?key|connectionstring)(?:"|''|\b)\s*[:=]\s*(?:"|'')(?!(?:\$\{|\$env:|<|your[_-]|replace[_-]|change[_-]|test[_-]only|set[_-]))[^"''\r\n]{8,}(?:"|'')'
    $matches = [regex]::Matches($content, $pattern)
    foreach ($match in $matches) {
        $line = 1 + ($content.Substring(0, $match.Index) -split "`n").Count - 1
        $violations.Add("literal secret-like value: ${normalizedPath}:$line")
    }
}

if ($violations.Count -gt 0) {
    Write-Error ("Tracked secret policy failed:`n - " + ($violations -join "`n - "))
    exit 1
}

Write-Host "Tracked secret policy passed: no production secret material or literal secret-like values were found."
