param([switch]$Apply)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$appSource=Join-Path $repo 'build/app/RiftReference.exe'
$installer=Join-Path $repo 'dist/RiftReference-Setup.exe'
$target=[IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'Programs/RiftReference'))
$app=Join-Path $target 'RiftReference.exe'
$feed=Join-Path $repo 'services/recommendations/output/recommendations.json'
[xml]$manifest=Get-Content -LiteralPath (Join-Path $repo 'experiments/gamebar/Widget/Package.appxmanifest')
$version=$manifest.Package.Identity.Version
$package=Join-Path $repo "build/gamebar/packages/RiftReady.GameBar_${version}_x64_Debug_Test/RiftReady.GameBar_${version}_x64_Debug.msix"
foreach($path in @($appSource,$installer,$package,(Join-Path $target 'installation.json'))){if(!(Test-Path -LiteralPath $path)){throw "Missing required installation input: $path"}}
if((Get-Item -LiteralPath $target).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Linked installation targets are not supported.'}
$certificate=Get-Item -LiteralPath 'Cert:\CurrentUser\My\DAF444B739DC4F9A8651CCEA68D7A6ED3F0CAA65'
$trusted=Get-Item -LiteralPath 'Cert:\LocalMachine\TrustedPeople\DAF444B739DC4F9A8651CCEA68D7A6ED3F0CAA65'
if(!$certificate.HasPrivateKey -or $certificate.NotAfter -lt (Get-Date) -or $trusted.Subject -ne 'CN=RiftReady.Development'){throw 'The existing local widget certificate is unavailable; no trust changes were made.'}
Write-Output "Ready to install built main app at $target and widget $version, retaining the installer recovery folder and existing preferences. Game Bar will be enabled in overlay settings."
if(!$Apply){Write-Output 'Preview only. Run with -Apply after reviewing and validating both builds.';return}
$signtool=Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/bin/10.0.19041.0/x64/signtool.exe'
& $signtool sign /fd SHA256 /s My /sha1 $certificate.Thumbprint $package
if($LASTEXITCODE -ne 0){throw 'Widget package signing failed.'}
& (Join-Path $PSScriptRoot 'install-gamebar-test.ps1') -PackagePath $package
foreach($process in @(Get-Process -Name RiftReference -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $app})){
 [void]$process.CloseMainWindow()
 if(!$process.WaitForExit(10000)){throw 'Rift Ready did not close cleanly. Installation stopped; close its windows and retry.'}
}
$preserved=@{}
foreach($name in @('preferences.json','reviews.json','reviews.json.bak','overlay.json','overlay.json.bak','rank-history.json','rank-history.json.bak')){
 $path=Join-Path $target $name
 if(Test-Path -LiteralPath $path){$preserved[$name]=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash}
}
# Existing installer stages the complete payload, retains its recovery folder, and rolls back failed moves.
$install=Start-Process -FilePath $installer -ArgumentList @('--extract-test',('"'+$target+'"')) -WorkingDirectory $repo -WindowStyle Hidden -PassThru -Wait
if($install.ExitCode -ne 0){throw "Main app installer failed ($($install.ExitCode)); inspect its preserved recovery folder."}
if((Get-FileHash -LiteralPath $app).Hash -ne (Get-FileHash -LiteralPath $appSource).Hash){throw 'Installed main executable does not match validated build.'}
foreach($name in $preserved.Keys){if((Get-FileHash -LiteralPath (Join-Path $target $name)).Hash -ne $preserved[$name]){throw "Installer changed preserved data: $name"}}
$settingsPath=Join-Path $target 'overlay.json'
if(Test-Path -LiteralPath $settingsPath){
 Copy-Item -LiteralPath $settingsPath -Destination ($settingsPath+'.before-gamebar-'+(Get-Date -Format 'yyyyMMddHHmmss'))
 $settings=Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
}else{$settings=New-Object PSObject}
$settings | Add-Member -NotePropertyName GameBar -NotePropertyValue $true -Force
$settings | Add-Member -NotePropertyName Enabled -NotePropertyValue $true -Force
[IO.File]::WriteAllText($settingsPath,($settings | ConvertTo-Json -Depth 20),[Text.Encoding]::UTF8)
$launch=New-Object Diagnostics.ProcessStartInfo
$launch.FileName=$app;$launch.WorkingDirectory=$target;$launch.UseShellExecute=$false
if(Test-Path -LiteralPath $feed){$launch.EnvironmentVariables['RIFT_RECOMMENDATIONS_FILE']=$feed}
[void][Diagnostics.Process]::Start($launch)
Write-Output 'Installed main app and Game Bar widget. Preserved user-data hashes verified before enabling Game Bar; private recommendation feed retained on relaunch when present.'
