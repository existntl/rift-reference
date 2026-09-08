param([ValidateRange(10,600)][int]$Seconds=180)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$native=Join-Path $repo 'build/native-overlay'
$prototype=Join-Path $repo 'build/native-prototype'
$games=@(Get-Process -Name 'League of Legends' -ErrorAction SilentlyContinue)
if($games.Count -ne 1){throw 'Open one Practice Tool game in Fullscreen before starting this compatibility test.'}
$game=$games[0]
$target=$game.Path
if(!$target){
 # Process.Path requests more access than needed on protected game processes.
 # Verify identity using only Windows' limited query right; never elevate.
 Add-Type -TypeDefinition @'
using System;using System.Text;using System.Runtime.InteropServices;
public static class RiftReadyTargetPath {
 [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr OpenProcess(uint access,bool inherit,uint id);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool QueryFullProcessImageName(IntPtr p,uint flags,StringBuilder path,ref uint size);
 [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr p);
 public static string Read(uint id){var p=OpenProcess(0x1000,false,id);if(p==IntPtr.Zero)throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());try{var path=new StringBuilder(32768);uint size=32768;if(!QueryFullProcessImageName(p,0,path,ref size))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());return path.ToString();}finally{CloseHandle(p);}}
}
'@
 $target=[RiftReadyTargetPath]::Read($game.Id)
}
if(!$target -or ![IO.Path]::IsPathRooted($target)){throw 'Cannot verify the running game executable. Test stopped.'}
foreach($file in @((Join-Path $native 'RiftReadyAttach.exe'),(Join-Path $native 'RiftReadyPresent.dll'),(Join-Path $prototype 'LivePublisher.exe'))){if(!(Test-Path -LiteralPath $file)){throw 'Build the prototype using scripts/verify-native-overlay.ps1 first.'}}
$homePath=Join-Path $env:LOCALAPPDATA 'Programs/RiftReference'
$session=[Guid]::NewGuid().ToString('N')
$logDir=Join-Path $native ('test-'+$session)
New-Item -ItemType Directory -Path $logDir | Out-Null
$publisher=$null;$attached=$false
try{
 $publisher=Start-Process -FilePath (Join-Path $prototype 'LivePublisher.exe') -ArgumentList @($session,('"'+$homePath+'"'),[string]$Seconds) -WindowStyle Hidden -RedirectStandardOutput (Join-Path $logDir 'publisher.txt') -RedirectStandardError (Join-Path $logDir 'publisher-errors.txt') -PassThru
 $ready=$false
 for($attempt=0;$attempt -lt 100;$attempt++){
  if($publisher.HasExited){throw 'The isolated data producer stopped; inspect publisher-errors.txt.'}
  try{$map=[IO.MemoryMappedFiles.MemoryMappedFile]::OpenExisting('Local\RiftReady.Render.'+$session);$map.Dispose();$ready=$true;break}catch [IO.FileNotFoundException]{}
  Start-Sleep -Milliseconds 100
 }
 if(!$ready){throw 'The local frame producer did not become ready.'}
 $loader=Start-Process -FilePath (Join-Path $native 'RiftReadyAttach.exe') -ArgumentList @('--pid',[string]$game.Id,'--target',('"'+$target+'"'),'--session',$session,'--dll',('"'+(Join-Path $native 'RiftReadyPresent.dll')+'"')) -WindowStyle Hidden -RedirectStandardOutput (Join-Path $logDir 'attach.txt') -RedirectStandardError (Join-Path $logDir 'attach-errors.txt') -Wait -PassThru
 Get-Content -LiteralPath (Join-Path $logDir 'attach.txt'),(Join-Path $logDir 'attach-errors.txt')
 if($loader.ExitCode -ne 0){throw 'Ordinary loading was unsuccessful. No retry, elevation or security changes will be attempted.'}
 $attached=$true
 Write-Output "Compatibility test active for up to $Seconds seconds. Return to Practice Tool; hold Tab for the gold markers. Logs: $logDir"
 $publisher.WaitForExit()
}finally{
 if($attached -and !$game.HasExited){& (Join-Path $native 'RiftReadyAttach.exe') --pid $game.Id --target $target --session $session --disable}
 if($publisher -and !$publisher.HasExited){$publisher.Kill();$publisher.WaitForExit()}
 if($attached){Write-Output 'Test ended. The pass-through adapter remains until the game exits; the installed Rift Ready app is unchanged.'}
 else{Write-Output 'Test ended without confirmed attachment; the installed Rift Ready app is unchanged. Inspect the loader error for the stage reached.'}
}
