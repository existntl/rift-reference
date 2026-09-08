param([string]$AppAssembly,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if(!$AppAssembly){$AppAssembly=Join-Path $repo 'build/app/RiftReference.exe'}
if(!$OutputDirectory){$OutputDirectory=Join-Path $repo 'build/gamebar/bridge'}
$AppAssembly=[IO.Path]::GetFullPath($AppAssembly)
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if(!(Test-Path -LiteralPath $AppAssembly)){throw 'Build the native app first or supply -AppAssembly.'}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$runtime=Join-Path $env:WINDIR 'Microsoft.NET/assembly/GAC_MSIL/System.Runtime/v4.0_4.0.0.0__b03f5f7f11d50a3a/System.Runtime.dll'
$metadata='C:\Program Files (x86)\Windows Kits\10\UnionMetadata\10.0.19041.0\Windows.winmd'
if(!(Test-Path -LiteralPath $metadata)){throw 'Windows SDK 19041 metadata is required.'}
$arguments=@('/nologo','/target:winexe',('/out:'+ (Join-Path $OutputDirectory 'RiftReady.GameBarBridge.exe')),('/reference:'+$AppAssembly),'/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll','/reference:System.Web.Extensions.dll',('/reference:'+(Join-Path $framework 'System.Runtime.WindowsRuntime.dll')),('/reference:'+$runtime),('/reference:'+$metadata),(Join-Path $repo 'experiments/gamebar/Bridge/LiveProducer.cs'))
& (Join-Path $framework 'csc.exe') @arguments
if($LASTEXITCODE -ne 0){throw 'Game Bar bridge compilation failed.'}
Copy-Item -LiteralPath $AppAssembly -Destination (Join-Path $OutputDirectory 'RiftReference.exe') -Force
$config='<?xml version="1.0" encoding="utf-8"?><configuration><startup><supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8" /></startup></configuration>'
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'RiftReady.GameBarBridge.exe.config'),$config,[Text.Encoding]::UTF8)
Write-Output (Join-Path $OutputDirectory 'RiftReady.GameBarBridge.exe')
