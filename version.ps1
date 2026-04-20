# Usage: .\version.ps1 -Version 1.2.0
param([Parameter(Mandatory)][string]$Version)

$parts = $Version.Split('.')
if ($parts.Count -ne 3) {
    Write-Host "Version must be in format X.Y.Z" -ForegroundColor Red
    exit 1
}

$assemblyVersion = "$Version.0"
$csproj = "MangaStudio.UI\MangaStudio.UI.csproj"

$content = Get-Content $csproj -Raw
$content = $content -replace '<AssemblyVersion>.*?</AssemblyVersion>',     "<AssemblyVersion>$assemblyVersion</AssemblyVersion>"
$content = $content -replace '<FileVersion>.*?</FileVersion>',             "<FileVersion>$assemblyVersion</FileVersion>"
$content = $content -replace '<InformationalVersion>.*?</InformationalVersion>', "<InformationalVersion>$Version</InformationalVersion>"

Set-Content $csproj $content

Write-Host "Version updated to $Version" -ForegroundColor Green
Write-Host "Run .\build.ps1 to create the release." -ForegroundColor Cyan