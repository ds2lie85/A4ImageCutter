[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
& (Join-Path $projectRoot 'build.ps1')

$releaseDirectory = Join-Path $projectRoot 'release'
$packageName = "A4ImageCutter-v$Version-win"
$packageDirectory = Join-Path $releaseDirectory $packageName
$zipPath = Join-Path $releaseDirectory ($packageName + '.zip')

New-Item -ItemType Directory -Force -Path $releaseDirectory | Out-Null
if (Test-Path -LiteralPath $packageDirectory) {
    Remove-Item -LiteralPath $packageDirectory -Recurse -Force
}
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}
New-Item -ItemType Directory -Path $packageDirectory | Out-Null

Copy-Item -LiteralPath (Join-Path $projectRoot 'dist\A4ImageCutter.exe') `
    -Destination (Join-Path $packageDirectory 'A4ImageCutter.exe')
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') `
    -Destination (Join-Path $packageDirectory 'README.md')
Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') `
    -Destination (Join-Path $packageDirectory 'LICENSE')
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\RELEASE_NOTES_1.0.0.md') `
    -Destination (Join-Path $packageDirectory 'RELEASE_NOTES.md')

$executable = Join-Path $packageDirectory 'A4ImageCutter.exe'
$hash = (Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath (Join-Path $packageDirectory 'SHA256SUMS.txt') `
    -Value "$hash  A4ImageCutter.exe" -Encoding ASCII

Compress-Archive -LiteralPath $packageDirectory -DestinationPath $zipPath `
    -CompressionLevel Optimal
Write-Host "Package complete / 패키지 생성 완료: $zipPath"
