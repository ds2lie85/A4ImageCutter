[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$compilerCandidates = @(
    'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe',
    'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe'
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } |
    Select-Object -First 1
if (-not $compiler) {
    throw 'Windows .NET Framework C# compiler (csc.exe) was not found. / Windows .NET Framework C# 컴파일러를 찾지 못했습니다.'
}

$distDirectory = Join-Path $projectRoot 'dist'
New-Item -ItemType Directory -Force -Path $distDirectory | Out-Null
$outputPath = Join-Path $distDirectory 'A4ImageCutter.exe'
$sources = @(
    (Join-Path $projectRoot 'Program.cs'),
    (Join-Path $projectRoot 'MainForm.cs')
)

& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /warn:4 `
    ('/out:' + $outputPath) /r:System.Windows.Forms.dll /r:System.Drawing.dll `
    $sources
if ($LASTEXITCODE -ne 0) {
    throw 'Build failed. / 빌드에 실패했습니다.'
}

Write-Host "Build complete / 빌드 완료: $outputPath"
