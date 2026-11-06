$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $PSScriptRoot
$Compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $Compiler)) { $Compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe' }
if (-not (Test-Path -LiteralPath $Compiler)) { throw 'Windows .NET Framework compiler not found. Install .NET Framework 4.8.' }
$Version = (Get-Content -LiteralPath (Join-Path $RepoRoot 'version.txt') -Raw).Trim()
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'version.txt must contain MAJOR.MINOR.PATCH.' }
$Sources = @(Get-ChildItem -LiteralPath (Join-Path $RepoRoot 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
$References = @('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Net.Http.dll','/r:System.Web.Extensions.dll')
