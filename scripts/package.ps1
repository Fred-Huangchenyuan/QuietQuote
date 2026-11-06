. (Join-Path $PSScriptRoot 'common.ps1')
& (Join-Path $PSScriptRoot 'build.ps1')
& (Join-Path $PSScriptRoot 'test.ps1')
$Stage = Join-Path $RepoRoot ('.build/package/' + [guid]::NewGuid().ToString('N'))
$Dist = Join-Path $RepoRoot 'dist'
New-Item -ItemType Directory -Path $Stage,$Dist -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $RepoRoot '.build/app/QuietQuote.exe') -Destination $Stage
foreach ($name in @('LICENSE','README.md','README.zh-CN.md','CONTRIBUTING.md','CHANGELOG.md','version.txt')) {
    Copy-Item -LiteralPath (Join-Path $RepoRoot $name) -Destination $Stage
}
Copy-Item -LiteralPath (Join-Path $RepoRoot 'docs') -Destination (Join-Path $Stage 'docs') -Recurse
$Zip = Join-Path $Dist "QuietQuote-v$Version-windows.zip"
Compress-Archive -Path (Join-Path $Stage '*') -DestinationPath $Zip -Force
$Hash = (Get-FileHash -LiteralPath $Zip -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath (Join-Path $Dist 'SHA256SUMS.txt') -Value "$Hash  QuietQuote-v$Version-windows.zip" -Encoding Ascii
Write-Host "Release assets: $Zip and dist/SHA256SUMS.txt"
