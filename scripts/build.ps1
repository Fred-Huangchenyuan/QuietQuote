. (Join-Path $PSScriptRoot 'common.ps1')
$BuildRoot = Join-Path $RepoRoot '.build/app'
New-Item -ItemType Directory -Path $BuildRoot -Force | Out-Null
$AssemblyInfo = Join-Path $BuildRoot 'AssemblyInfo.cs'
$Info = @"
using System.Reflection;
[assembly: AssemblyTitle("QuietQuote")]
[assembly: AssemblyProduct("QuietQuote")]
[assembly: AssemblyDescription("A lightweight multi-window OKX and Binance desktop ticker")]
[assembly: AssemblyVersion("$Version.0")]
[assembly: AssemblyFileVersion("$Version.0")]
[assembly: AssemblyInformationalVersion("$Version")]
"@
Set-Content -LiteralPath $AssemblyInfo -Value $Info -Encoding UTF8
$Exe = Join-Path $BuildRoot 'QuietQuote.exe'
$IconFile = Join-Path $RepoRoot 'assets/QuietQuote.ico'
& $Compiler /nologo /target:winexe /optimize+ /platform:anycpu /codepage:65001 $References "/win32icon:$IconFile" "/out:$Exe" $Sources $AssemblyInfo
if ($LASTEXITCODE -ne 0) { throw 'Build failed. Close a running development build if its file is locked.' }
Write-Host "Built $Exe (v$Version)"
