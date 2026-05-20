<#
.SYNOPSIS
    Publish XIVUIColorPreviewer as a self-extracting x64 exe.
.DESCRIPTION
    1. Runs dotnet publish (self-contained, trimmed, ReadyToRun)
    2. Packages the output into a single self-extracting .exe via 7-Zip
.NOTES
    Requires: .NET SDK, 7-Zip installed (default path or in PATH)
#>

param(
    [string]$Configuration = "Release",
    [string]$SevenZipPath = ""
)

$ErrorActionPreference = "Stop"

# Locate 7-Zip
if (-not $SevenZipPath) {
    $candidates = @(
        "C:\Program Files\7-Zip\7z.exe",
        "C:\Program Files (x86)\7-Zip\7z.exe",
        (Get-Command 7z -ErrorAction SilentlyContinue)?.Source
    )
    $SevenZipPath = $candidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
}

if (-not $SevenZipPath -or -not (Test-Path $SevenZipPath)) {
    Write-Error "7-Zip not found. Install it or pass -SevenZipPath parameter."
    exit 1
}

$projectDir = "$PSScriptRoot\XIVUIColorPreviewer"
$publishDir = "$projectDir\bin\publish\win-x64"
$outputExe  = "$PSScriptRoot\XIVUIColorPreviewer-x64.exe"
$sfxModule  = (Split-Path $SevenZipPath) + "\7z.sfx"

# Clean previous output
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (Test-Path $outputExe)  { Remove-Item $outputExe -Force }

# Publish
Write-Host "Publishing ($Configuration, win-x64, self-contained, trimmed)..." -ForegroundColor Cyan
dotnet publish $projectDir\XIVUIColorPreviewer.csproj `
    -c $Configuration `
    -p:Platform=x64 `
    -p:PublishProfile=win-x64 `
    -p:WindowsPackageType=None

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed."
    exit 1
}

# Verify publish output
$mainExe = Get-ChildItem "$publishDir\XIVUIColorPreviewer.exe" -ErrorAction SilentlyContinue
if (-not $mainExe) {
    Write-Error "Publish succeeded but XIVUIColorPreviewer.exe not found in $publishDir"
    exit 1
}

Write-Host "Publish complete. Size: $([math]::Round((Get-ChildItem $publishDir -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB, 1)) MB" -ForegroundColor Green

# Create SFX config
$sfxConfig = "$PSScriptRoot\sfx_config.txt"
@"
;!@Install@!UTF-8!
Title="XIVUIColorPreviewer"
RunProgram="XIVUIColorPreviewer.exe"
;!@InstallEnd@!
"@ | Set-Content $sfxConfig -Encoding UTF8

# Create 7z archive
$archivePath = "$PSScriptRoot\temp_package.7z"
if (Test-Path $archivePath) { Remove-Item $archivePath -Force }

Write-Host "Compressing..." -ForegroundColor Cyan
& $SevenZipPath a -t7z -mx=9 -mf=BCJ2 -r $archivePath "$publishDir\*" | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Error "7-Zip compression failed."
    exit 1
}

# Combine: SFX module + config + archive = self-extracting exe
Write-Host "Creating self-extracting exe..." -ForegroundColor Cyan
$sfxBytes     = [System.IO.File]::ReadAllBytes($sfxModule)
$configBytes  = [System.IO.File]::ReadAllBytes($sfxConfig)
$archiveBytes = [System.IO.File]::ReadAllBytes($archivePath)

$stream = [System.IO.File]::Create($outputExe)
$stream.Write($sfxBytes, 0, $sfxBytes.Length)
$stream.Write($configBytes, 0, $configBytes.Length)
$stream.Write($archiveBytes, 0, $archiveBytes.Length)
$stream.Close()

# Cleanup temp files
Remove-Item $sfxConfig -Force
Remove-Item $archivePath -Force

$finalSize = [math]::Round((Get-Item $outputExe).Length / 1MB, 1)
Write-Host "`nDone! Output: $outputExe ($finalSize MB)" -ForegroundColor Green
