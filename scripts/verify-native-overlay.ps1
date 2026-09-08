$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo 'build/native-prototype'
& (Join-Path $PSScriptRoot 'build.ps1') -OutputDirectory $output
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
foreach($test in @('NativeOverlayBridgeTests','NativeOverlayLifecycleTests','OverlayTests')){
 & $compiler /nologo "/out:$output/$test.exe" "/reference:$output/RiftReference.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll (Join-Path $repo "tests/$test.cs")
 if($LASTEXITCODE -ne 0){throw "$test compilation failed"}
 & (Join-Path $output "$test.exe")
 if($LASTEXITCODE -ne 0){throw "$test failed"}
}
& $compiler /nologo "/out:$output/LivePublisher.exe" "/reference:$output/RiftReference.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll (Join-Path $repo 'experiments/native-overlay/LivePublisher.cs')
if($LASTEXITCODE -ne 0){throw 'Live publisher build failed'}
$check=Start-Process -FilePath (Join-Path $output 'RiftReference.exe') -ArgumentList '--test' -WindowStyle Hidden -Wait -PassThru
if($check.ExitCode -ne 0){throw 'Application checks failed'}
& (Join-Path $repo 'experiments/native-overlay/native/build.ps1') -Test
& (Join-Path $repo 'experiments/native-overlay/integration/build.ps1')
Write-Output 'Native prototype and bridge checks passed. League integration requires a separate live compatibility test.'
