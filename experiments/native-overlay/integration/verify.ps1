param([switch]$Windowed)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$bin=Join-Path $root 'build\native-overlay'
$session=[guid]::NewGuid().ToString('N')
$artifacts=Join-Path $bin "verify-$session"
New-Item -ItemType Directory $artifacts | Out-Null
$hostPath=Join-Path $bin 'RiftReadyRendererHost.exe'
$publisherPath=Join-Path $root 'build\native-prototype\NativeOverlayBridgeTests.exe'
$attach=Join-Path $bin 'RiftReadyAttach.exe'
$publisher=Start-Process -FilePath $publisherPath -ArgumentList @('--serve',$session) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $artifacts 'publisher.log')
try {
 $deadline=(Get-Date).AddSeconds(5)
 while((Get-Date) -lt $deadline){if((Test-Path (Join-Path $artifacts 'publisher.log')) -and (Get-Content (Join-Path $artifacts 'publisher.log') -Raw) -match 'Sample frame ready'){break};Start-Sleep -Milliseconds 100}
 $arguments=@('--hook-test','--session',$session,'--duration','12','--capture','on.bmp','--capture-after-disable','off.bmp')
 if(!$Windowed){$arguments+='--fullscreen'}
 # Visible owned test window is intentional: foreground and fullscreen are under test.
 $testHost=Start-Process -FilePath $hostPath -ArgumentList $arguments -WorkingDirectory $artifacts -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $artifacts 'host.log') -RedirectStandardError (Join-Path $artifacts 'host-errors.log')
 & $attach --pid $testHost.Id --target $hostPath --session $session --dll (Join-Path $bin 'RiftReadyPresent.dll')
 if($LASTEXITCODE){throw 'Owned-host attachment failed'}
 # Reattachment must fail without another DLL initializer.
 & $attach --pid $testHost.Id --target $hostPath --session $session --dll (Join-Path $bin 'RiftReadyPresent.dll')
 if($LASTEXITCODE -eq 0){throw 'Duplicate attachment unexpectedly succeeded'}
 $deadline=(Get-Date).AddSeconds(6)
 while(!(Test-Path (Join-Path $artifacts 'on.bmp')) -and (Get-Date) -lt $deadline){Start-Sleep -Milliseconds 100}
 if(!(Test-Path (Join-Path $artifacts 'on.bmp'))){throw 'Hook-on capture missing'}
 & $attach --pid $testHost.Id --target $hostPath --session $session --disable
 if($LASTEXITCODE){throw 'Owned-host disable failed'}
 if(!$testHost.WaitForExit(15000)){throw 'Owned host did not exit on its bounded duration'}
 if($testHost.ExitCode -ne 0){throw 'Owned host failed; inspect host-errors.log'}
 $log=Get-Content (Join-Path $artifacts 'host.log') -Raw
 Write-Output $log
 if(!$Windowed -and $log -notmatch 'DXGI fullscreen state: TRUE'){throw 'Fullscreen was not confirmed'}
 & (Join-Path $root 'cache\runtime\python.exe') (Join-Path $PSScriptRoot 'verify-captures.py') $artifacts
 if($LASTEXITCODE){throw 'Hook GPU capture verification failed'}
 Write-Output "Artifacts: $artifacts"
} finally {
 # Only this script's own bounded fixture is stopped. Never touch League or another app.
 if(!$publisher.HasExited){Stop-Process -Id $publisher.Id}
}
