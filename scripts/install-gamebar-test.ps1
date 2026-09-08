param([string]$PackagePath)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
[xml]$manifest=Get-Content -LiteralPath (Join-Path $repo 'experiments/gamebar/Widget/Package.appxmanifest')
$version=$manifest.Package.Identity.Version
$folder=Join-Path $repo "build/gamebar/packages/RiftReady.GameBar_${version}_x64_Debug_Test"
if(!$PackagePath){$PackagePath=Join-Path $folder "RiftReady.GameBar_${version}_x64_Debug.msix"}
$PackagePath=(Resolve-Path -LiteralPath $PackagePath).Path
$packageRoot=[IO.Path]::GetFullPath((Join-Path $repo 'build/gamebar/packages'))+[IO.Path]::DirectorySeparatorChar
if(!$PackagePath.StartsWith($packageRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Select a built widget package inside build/gamebar/packages.'}
$signature=Get-AuthenticodeSignature -LiteralPath $PackagePath
if($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Thumbprint -ne 'DAF444B739DC4F9A8651CCEA68D7A6ED3F0CAA65'){throw 'Widget package must have the existing trusted Rift Ready development signature.'}
$dependencies=@(Get-ChildItem -LiteralPath (Join-Path (Split-Path $PackagePath -Parent) 'Dependencies/x64') -Filter *.appx | ForEach-Object FullName)
Add-AppxPackage -Path $PackagePath -DependencyPath $dependencies -ForceApplicationShutdown
Get-AppxPackage RiftReady.GameBarPrototype | Select-Object Name,PackageFullName,Status
