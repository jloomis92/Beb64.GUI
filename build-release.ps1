<#
.SYNOPSIS
    One-click release builder for the WPF Base64 app:
    - dotnet publish (self-contained, single file)
    - Inno Setup compile
    - Opens the installer output folder

.PARAMETER Project
    Path to the .csproj (auto-detected if omitted)

.PARAMETER Configuration
    Build configuration. Default: Release

.PARAMETER Runtime
    Target runtime. Default: win-x64

.PARAMETER Framework
    Target framework. Default: net8.0

.PARAMETER SelfContained
    Build self-contained (true/false). Default: $true

.PARAMETER SingleFile
    Publish as single file (true/false). Default: $true

.PARAMETER InnoSetupPath
    Full path to ISCC.exe if not in default location/PATH.

.PARAMETER IssFile
    Path to your installer .iss script. Default: installer.iss in repo root
#>

[CmdletBinding()]
param(
    [string] $Project,
    [string] $Configuration = "Release",
    [string] $Runtime = "win-x64",
    [string] $Framework = "net8.0-windows",
    [bool]   $SelfContained = $true,
    [bool]   $SingleFile = $true,
    [string] $InnoSetupPath,
    [string] $IssFile = (Join-Path $PSScriptRoot "BeB64.iss")
)

# -----------------------------
# Helpers
# -----------------------------
function Fail($msg) {
    Write-Error $msg
    exit 1
}

function Get-ProjectFile {
    param([string]$explicitPath)

    if ($explicitPath) {
        if (Test-Path $explicitPath) { return (Resolve-Path $explicitPath).Path }
        Fail "Project file not found: $explicitPath"
    }

    $candidates = Get-ChildItem -Path $PSScriptRoot -Recurse -Filter *.csproj | Select-Object -First 1
    if (-not $candidates) { Fail "No .csproj found. Pass -Project <path-to-csproj>." }
    return $candidates.FullName
}

function Get-ProjectVersion {
    param([string]$csprojPath)

    [xml]$xml = Get-Content $csprojPath
    $verNode = $xml.Project.PropertyGroup.Version | Select-Object -First 1
    if ($verNode) { return $verNode.Trim() }

    # Fallback to AssemblyVersion/FileVersion if Version not present
    $asmVer = $xml.Project.PropertyGroup.AssemblyVersion | Select-Object -First 1
    if ($asmVer) { return $asmVer.Trim() }

    return "1.0.0"
}

function Find-Iscc {
    param([string]$explicitPath)

    if ($explicitPath -and (Test-Path $explicitPath)) { return (Resolve-Path $explicitPath).Path }

    # Check PATH
    $isccOnPath = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($isccOnPath) { return $isccOnPath.Source }

    # Common default install location
    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
    )
    foreach ($c in $candidates) {
        if (Test-Path $c) { return $c }
    }

    Fail "ISCC.exe (Inno Setup Compiler) not found. Install Inno Setup 6 or pass -InnoSetupPath"
}

# -----------------------------
# Main
# -----------------------------
$ErrorActionPreference = "Stop"

$projectPath = Get-ProjectFile -explicitPath $Project
$projectDir  = Split-Path $projectPath -Parent
$projectName = Split-Path $projectPath -LeafBase

$version = Get-ProjectVersion -csprojPath $projectPath
Write-Host "✔ Project:   $projectName" -ForegroundColor Cyan
Write-Host "✔ Version:   $version" -ForegroundColor Cyan

# Publish
Write-Host "`n==> dotnet publish" -ForegroundColor Yellow
$publishArgs = @(
    "publish", "`"$projectPath`"",
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained:$SelfContained",
    "/p:PublishSingleFile=$SingleFile",
    "/p:IncludeAllContentForSelfExtract=true",
    "/p:PublishTrimmed=false"
)

dotnet @publishArgs

# Compute publish directory
$publishDir = Join-Path $projectDir "bin\$Configuration\$Framework\$Runtime\publish"
if (-not (Test-Path $publishDir)) {
    Fail "Publish output not found at: $publishDir"
}
Write-Host "✔ Publish dir: $publishDir" -ForegroundColor Cyan

# Inno Setup
if (-not (Test-Path $IssFile)) {
    Fail "Installer script not found: $IssFile"
}

$iscc = Find-Iscc -explicitPath $InnoSetupPath
Write-Host "`n==> ISCC: $iscc" -ForegroundColor Yellow

# We’ll build to Output\ under the script dir; let the .iss control that via OutputDir
$issArgs = @(
    "/DMySourceDir=$publishDir",
    "/DMyAppVersion=$version"
)
& "$iscc" $issArgs "`"$IssFile`""

if ($LASTEXITCODE -ne 0) {
    Fail "ISCC failed with exit code $LASTEXITCODE"
}

# Try to locate the output folder (default in the provided .iss is 'Output' next to the script)
$outputDir = Join-Path (Split-Path $IssFile -Parent) "Output"
if (Test-Path $outputDir) {
    Write-Host "`n✔ Installer built. Opening: $outputDir" -ForegroundColor Green
    Start-Process explorer.exe $outputDir
} else {
    Write-Host "`n✔ Installer built. (Couldn't auto-detect Output dir; check ISCC log)" -ForegroundColor Green
}
