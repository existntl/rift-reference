param([string]$InstallerPath, [string]$BaselineInstallerPath)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (!$InstallerPath) { $InstallerPath = Join-Path $projectRoot 'dist/RiftReference-Setup.exe' }
$InstallerPath = [IO.Path]::GetFullPath($InstallerPath)
$testRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot ('build/installer-test-' + [Guid]::NewGuid().ToString('N'))))
$allowedRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'build')).TrimEnd('\') + '\'
if (!$testRoot.StartsWith($allowedRoot,[StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid test target' }
New-Item -ItemType Directory -Path $testRoot | Out-Null
$target = Join-Path $testRoot 'installed'
function Invoke-Installer([string]$mode,[string]$working,[string]$executable=$InstallerPath) {
    $process = Start-Process -FilePath $executable -ArgumentList @($mode,('"'+$target+'"')) -WorkingDirectory $working -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0) { throw "Installer failed: $mode ($($process.ExitCode))" }
}
if ($BaselineInstallerPath) { Invoke-Installer '--extract-test' $testRoot ([IO.Path]::GetFullPath($BaselineInstallerPath)) }
else { Invoke-Installer '--extract-test' $testRoot }
$settings = '{"SettingsVersion":1,"AudioVolume":37,"EnemiesLeft":false}'
[IO.File]::WriteAllText((Join-Path $target 'preferences.json'),$settings,[Text.Encoding]::UTF8)
[IO.File]::WriteAllText((Join-Path $target 'preferences.json.bak'),$settings,[Text.Encoding]::UTF8)
$reviews = '[{"Id":"upgrade-check","NextGame":"Keep an exit"}]'
[IO.File]::WriteAllText((Join-Path $target 'reviews.json'),$reviews,[Text.Encoding]::UTF8)
[IO.File]::WriteAllText((Join-Path $target 'reviews.json.bak'),$reviews,[Text.Encoding]::UTF8)
$overlay = '{"Enabled":true,"Gold":false,"Buffs":true,"Purchase":true,"Target":3031,"Champion":"Vayne"}'
$rankHistory = '{"format":"rift-rank-history-1","accounts":{}}'
foreach ($name in @('rank-history.json','rank-history.json.bak')) { [IO.File]::WriteAllText((Join-Path $target $name),$rankHistory,[Text.Encoding]::UTF8) }
foreach ($name in @('overlay.json','overlay.json.bak')) { [IO.File]::WriteAllText((Join-Path $target $name),$overlay,[Text.Encoding]::UTF8) }
Invoke-Installer '--extract-test' $target
$original = (Get-FileHash -LiteralPath (Join-Path $target 'RiftReference.exe')).Hash
foreach ($name in @('rank-history.json','rank-history.json.bak')) { if ([IO.File]::ReadAllText((Join-Path $target $name)) -ne $rankHistory) { throw "Upgrade lost $name" } }
if ($original -ne (Get-FileHash -LiteralPath (Join-Path $projectRoot 'build/app/RiftReference.exe')).Hash) { throw 'Upgrade did not install the current app' }
foreach ($name in @('overlay.json','overlay.json.bak')) { if ([IO.File]::ReadAllText((Join-Path $target $name)) -ne $overlay) { throw "Upgrade lost $name" } }
if ([IO.File]::ReadAllText((Join-Path $target 'preferences.json')) -ne $settings) { throw 'Upgrade lost preferences' }
if ([IO.File]::ReadAllText((Join-Path $target 'preferences.json.bak')) -ne $settings) { throw 'Upgrade lost preferences recovery copy' }
foreach ($name in @('reviews.json','reviews.json.bak')) { if ([IO.File]::ReadAllText((Join-Path $target $name)) -ne $reviews) { throw "Upgrade lost $name" } }
Invoke-Installer '--rollback-test' $target
foreach ($name in @('rank-history.json','rank-history.json.bak')) { if ([IO.File]::ReadAllText((Join-Path $target $name)) -ne $rankHistory) { throw "Rollback lost $name" } }
foreach ($name in @('overlay.json','overlay.json.bak')) { if ([IO.File]::ReadAllText((Join-Path $target $name)) -ne $overlay) { throw "Rollback lost $name" } }
if ([IO.File]::ReadAllText((Join-Path $target 'preferences.json')) -ne $settings) { throw 'Rollback lost preferences' }
if ([IO.File]::ReadAllText((Join-Path $target 'preferences.json.bak')) -ne $settings) { throw 'Rollback lost preferences recovery copy' }
foreach ($name in @('reviews.json','reviews.json.bak')) { if ([IO.File]::ReadAllText((Join-Path $target $name)) -ne $reviews) { throw "Rollback lost $name" } }
if ((Get-FileHash -LiteralPath (Join-Path $target 'RiftReference.exe')).Hash -ne $original) { throw 'Rollback changed app' }
Write-Output "PASS: upgrade from installation working directory; preferences, reviews, recovery copy and executable preserved on rollback. Backups retained in $testRoot"
