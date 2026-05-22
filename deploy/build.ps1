<#
.SYNOPSIS
    在宿主机完成预构建，产物输出到 artifacts/publish/webapi。
    构建完成后执行 docker compose up --build 即可直接打出运行时镜像。

.PARAMETER NpmRegistry
    npm 镜像源，默认使用 npmmirror.com。

.PARAMETER Configuration
    dotnet 发布配置，默认 Release。

.EXAMPLE
    .\deploy\build.ps1
    .\deploy\build.ps1 -NpmRegistry https://registry.npmjs.org
#>
[CmdletBinding()]
param(
    [string] $NpmRegistry    = 'https://registry.npmmirror.com',
    [string] $Configuration  = 'Release'
)

$ErrorActionPreference = 'Stop'

$repoRoot   = Resolve-Path "$PSScriptRoot\.."
$outDir     = Join-Path $repoRoot 'artifacts\publish\webapi'
$adminDir   = Join-Path $repoRoot 'src\HomeMesh.AdminConsole'
$webapiProj = Join-Path $repoRoot 'src\HomeMesh.WebApi\HomeMesh.WebApi.csproj'
$distSrc    = Join-Path $repoRoot 'src\HomeMesh.WebApi\dist'
$distDst    = Join-Path $outDir   'dist'

# ── 1. Build AdminConsole ────────────────────────────────────────────────────
Write-Host "`n=== [1/3] Building AdminConsole ===" -ForegroundColor Cyan
Push-Location $adminDir
try {
    npm config set registry $NpmRegistry
    npm ci
    if ($LASTEXITCODE -ne 0) { throw "npm ci failed" }
    npm run build
    if ($LASTEXITCODE -ne 0) { throw "npm run build failed" }
} finally {
    Pop-Location
}

# ── 2. Publish WebApi ────────────────────────────────────────────────────────
Write-Host "`n=== [2/3] Publishing WebApi ===" -ForegroundColor Cyan
if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }

dotnet publish $webapiProj `
    -c $Configuration `
    -o $outDir `
    -p:SkipAdminConsoleBuild=true
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# ── 3. Copy dist into publish output ────────────────────────────────────────
Write-Host "`n=== [3/3] Copying AdminConsole dist ===" -ForegroundColor Cyan
if (Test-Path $distSrc) {
    Copy-Item -Path $distSrc -Destination $distDst -Recurse -Force
    Write-Host "  dist copied → $distDst"
} else {
    throw "AdminConsole dist not found at: $distSrc"
}

Write-Host "`nPre-build complete." -ForegroundColor Green
Write-Host "Run the following to build and start the container:"
Write-Host '  docker compose -f deploy\docker\docker-compose.yml up -d --build'
