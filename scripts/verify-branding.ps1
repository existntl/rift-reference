$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$testRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot ('build/branding-test-' + [Guid]::NewGuid().ToString('N'))))
$allowed = [IO.Path]::GetFullPath((Join-Path $projectRoot 'build')).TrimEnd('\') + '\'
if (!$testRoot.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid test workspace' }
New-Item -ItemType Directory -Path $testRoot | Out-Null
$assembly = [Reflection.Assembly]::LoadFile((Join-Path $projectRoot 'dist/RiftReference-Setup.exe'))
$remove = $assembly.GetType('RiftReferenceSetup.Program').GetMethod('RemoveOwnedLegacyShortcut',[Reflection.BindingFlags]'NonPublic,Static')
$shell = New-Object -ComObject WScript.Shell
$path = Join-Path $testRoot 'Rift Reference.lnk'
$link = $shell.CreateShortcut($path)
$link.TargetPath = Join-Path $testRoot 'Other.exe'
$link.Save()
$remove.Invoke($null,@($testRoot,$testRoot)) | Out-Null
if (!(Test-Path -LiteralPath $path)) { throw 'Unrelated shortcut was removed' }
$link.TargetPath = Join-Path $testRoot 'RiftReference.exe'
$link.Save()
$remove.Invoke($null,@($testRoot,$testRoot)) | Out-Null
if (Test-Path -LiteralPath $path) { throw 'Owned legacy shortcut was not migrated' }
$html = [IO.File]::ReadAllText((Join-Path $projectRoot 'build/app/mobile.html'),[Text.Encoding]::UTF8)
if ($html.Contains('__RIFT_READY_LOGO__') -or !$html.Contains('data:image/png;base64,iVBOR')) { throw 'Mobile branding was not embedded' }
Write-Output 'PASS: owned legacy shortcut cleanup, unrelated shortcut preservation, embedded mobile logo.'
