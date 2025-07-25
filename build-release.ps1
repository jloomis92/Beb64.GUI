<#
.SYNOPSIS
    One-click release builder for the WPF Base64 app:
    - dotnet publish (self-contained, single file)
    - Inno Setup compile
    - Opens the installer output folder
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

function To-FileVersion([string]$v) {
    $parts = $v.Split('.')
    switch ($parts.Length) {
        1 { return "$v.0.0.0" }
        2 { return "$v.0.0"   }
        3 { return "$v.0"     }
        default { return $v } # already 4-part
    }
}

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

    $asmVer = $xml.Project.PropertyGroup.AssemblyVersion | Select-Object -First 1
    if ($asmVer) { return $asmVer.Trim() }

    return "1.0.0"
}

function Find-Iscc {
    param([string]$explicitPath)

    if ($explicitPath -and (Test-Path $explicitPath)) { return (Resolve-Path $explicitPath).Path }

    $isccOnPath = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($isccOnPath) { return $isccOnPath.Source }

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
$fileVersion = To-FileVersion $version
Write-Host "✔ Version:     $version"     -ForegroundColor Cyan
Write-Host "✔ FileVersion: $fileVersion" -ForegroundColor Cyan
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
    "/p:PublishTrimmed=false",
    "/p:Version=$version",
    "/p:FileVersion=$fileVersion",
    "/p:InformationalVersion=$version"
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

$issArgs = @(
    "/DMySourceDir=$publishDir",
    "/DMyAppVersion=$version"                # <-- pass version into the installer
)

& "$iscc" $issArgs "`"$IssFile`""
if ($LASTEXITCODE -ne 0) {
    Fail "ISCC failed with exit code $LASTEXITCODE"
}

# Open output folder
$outputDir = Join-Path (Split-Path $IssFile -Parent) "Output"
if (Test-Path $outputDir) {
    Write-Host "`n✔ Installer built. Opening: $outputDir" -ForegroundColor Green
    Start-Process explorer.exe $outputDir
} else {
    Write-Host "`n✔ Installer built. (Couldn't auto-detect Output dir; check ISCC log)" -ForegroundColor Green
}
