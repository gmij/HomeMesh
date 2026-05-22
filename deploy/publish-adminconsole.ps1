[CmdletBinding()]
param(
    [switch]$InstallDependencies
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$adminConsolePath = Join-Path $repoRoot 'src/HomeMesh.AdminConsole'
$webApiDistPath = Join-Path $repoRoot 'src/HomeMesh.WebApi\dist'
$packageJsonPath = Join-Path $adminConsolePath 'package.json'
$distIndexPath = Join-Path $webApiDistPath 'index.html'

if (-not (Test-Path $packageJsonPath)) {
    throw "AdminConsole package.json was not found: $packageJsonPath"
}

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    throw 'npm was not found. Install Node.js and make sure npm is available on PATH.'
}

Write-Host "Repository root: $repoRoot"
Write-Host "AdminConsole: $adminConsolePath"
Write-Host "Output folder: $webApiDistPath"

Push-Location $adminConsolePath
try {
    $nodeModulesPath = Join-Path $adminConsolePath 'node_modules'
    if ($InstallDependencies -or -not (Test-Path $nodeModulesPath)) {
        Write-Host 'Installing frontend dependencies...'
        npm ci
        if ($LASTEXITCODE -ne 0) {
            throw "npm ci failed with exit code: $LASTEXITCODE"
        }
    }

    Write-Host 'Building AdminConsole...'
    npm run build
    if ($LASTEXITCODE -ne 0) {
        throw "npm run build failed with exit code: $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path $distIndexPath)) {
    throw "Build finished, but output file was not found: $distIndexPath"
}

Write-Host ''
Write-Host 'AdminConsole has been published to the WebApi dist folder.'
Write-Host "Publish result: $distIndexPath"
