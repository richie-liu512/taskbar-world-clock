$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Source = Join-Path $ProjectRoot "src\TaskbarWorldClock.cs"
$OutDir = Join-Path $ProjectRoot "dist"
$OutExe = Join-Path $OutDir "TaskbarWorldClock.exe"
$Csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path -LiteralPath $Csc)) {
    $Csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

if (-not (Test-Path -LiteralPath $Csc)) {
    throw "csc.exe not found. Install .NET Framework developer tools."
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

& $Csc `
    /nologo `
    /codepage:65001 `
    /target:winexe `
    /platform:anycpu `
    /out:$OutExe `
    /reference:System.Windows.Forms.dll `
    /reference:System.Drawing.dll `
    /reference:System.Xml.dll `
    $Source

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host $OutExe
