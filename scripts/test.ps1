param([switch]$Desktop, [switch]$Live)
. (Join-Path $PSScriptRoot 'common.ps1')
if ($Live) { $Desktop = $true }
$RunRoot = Join-Path $RepoRoot ('.build/tests/' + [guid]::NewGuid().ToString('N'))
$WorkRoot = Join-Path $RunRoot 'work'
New-Item -ItemType Directory -Path $WorkRoot -Force | Out-Null
$TestSource = Join-Path $RepoRoot 'tests/CoreTests.cs'
$Main = '/main:CoreTests'
if ($Desktop) {
    $TestSource = Join-Path $RepoRoot 'tests/DesktopTests.cs'
    $Main = '/main:QuoteTests'
    Copy-Item -LiteralPath (Join-Path $RepoRoot 'tests/instruments.json') -Destination (Join-Path $WorkRoot 'instruments.json')
}
$Exe = Join-Path $WorkRoot 'Tests.exe'
& $Compiler /nologo /target:exe /optimize+ /codepage:65001 $References $Main "/out:$Exe" $Sources $TestSource
if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
Push-Location $RunRoot
try {
    if ($Live) { & $Exe --live } else { & $Exe }
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
} finally { Pop-Location }
