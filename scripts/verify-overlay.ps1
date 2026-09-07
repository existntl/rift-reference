param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory = Join-Path $projectRoot ('build/overlay-' + [Guid]::NewGuid().ToString('N')) }
$validationRoot = [IO.Path]::GetFullPath($OutputDirectory)
& (Join-Path $PSScriptRoot 'build.ps1') -OutputDirectory $validationRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $compiler /nologo /target:exe "/out:$validationRoot/OverlayTests.exe" "/reference:$validationRoot/RiftReference.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll (Join-Path $projectRoot 'tests/OverlayTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Overlay test compilation failed' }
& (Join-Path $validationRoot 'OverlayTests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Overlay checks failed' }
Write-Output "Overlay evidence: $validationRoot"
