# MangaStudio — release build script
# Usage: .\build.ps1
# Output: publish\win-x64\

$ErrorActionPreference = "Stop"

$solutionDir = $PSScriptRoot
$publishDir  = Join-Path $solutionDir "publish\win-x64"
$uiProject   = Join-Path $solutionDir "MangaStudio.UI\MangaStudio.UI.csproj"

Write-Host ""
Write-Host "=== MangaStudio Release Build ===" -ForegroundColor Cyan
Write-Host ""

# ── Step 1: Clean previous publish output ────────────────────────────────────
if (Test-Path $publishDir) {
    Write-Host "Cleaning previous publish output..." -ForegroundColor Yellow
    Remove-Item $publishDir -Recurse -Force
}

# ── Step 2: Run tests ─────────────────────────────────────────────────────────
Write-Host "Running tests..." -ForegroundColor Yellow
dotnet test "$solutionDir\MangaStudio.Tests\MangaStudio.Tests.csproj" `
    --configuration Release `
    --no-build `
    --logger "console;verbosity=minimal"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests failed — aborting build." -ForegroundColor Red
    exit 1
}

Write-Host "All tests passed." -ForegroundColor Green

# ── Step 3: Publish ───────────────────────────────────────────────────────────
Write-Host "Publishing..." -ForegroundColor Yellow
dotnet publish $uiProject `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:PublishReadyToRun=true `
    /p:IncludeNativeLibrariesForSelfExtract=false `
    /p:PublishTrimmed=false `
    --output $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed." -ForegroundColor Red
    exit 1
}

# ── Step 4: Create logs folder so the app finds it on first run ───────────────
New-Item -ItemType Directory -Force -Path (Join-Path $publishDir "logs") | Out-Null

# ── Step 5: Report output ─────────────────────────────────────────────────────
Write-Host ""
Write-Host "=== Build complete ===" -ForegroundColor Green
Write-Host "Output: $publishDir" -ForegroundColor Cyan
Write-Host ""

$files = Get-ChildItem $publishDir -File | Sort-Object Length -Descending
foreach ($f in $files) {
    $size = if ($f.Length -gt 1MB) {
        "{0:N1} MB" -f ($f.Length / 1MB)
    } else {
        "{0:N0} KB" -f ($f.Length / 1KB)
    }
    Write-Host ("  {0,-40} {1,10}" -f $f.Name, $size)
}

Write-Host ""

# ── Step 6: Create distributable ZIP ─────────────────────────────────────────
$version    = "1.0.0"
$zipName    = "MangaStudio-$version-win-x64.zip"
$zipPath    = Join-Path $solutionDir "publish\$zipName"

Write-Host "Creating $zipName..." -ForegroundColor Yellow

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath

$zipSize = "{0:N1} MB" -f ((Get-Item $zipPath).Length / 1MB)
Write-Host "ZIP created: $zipPath ($zipSize)" -ForegroundColor Green
Write-Host ""