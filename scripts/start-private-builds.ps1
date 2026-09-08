param([ValidateRange(150,5000)][int]$Budget=1500)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$app=[IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'Programs/RiftReference/RiftReference.exe'))
$feed=[IO.Path]::GetFullPath((Join-Path $repo 'services/recommendations/output/recommendations.json'))
if(!(Test-Path -LiteralPath $app)){throw 'Install Rift Ready before starting private testing.'}
# Leave an existing private session and its collector untouched on repeated launch.
# Include the earlier private-desktop launcher while it finishes an existing run.
$buildRoot=[IO.Path]::GetFullPath((Join-Path $repo 'build'))+[IO.Path]::DirectorySeparatorChar
$existing=@(Get-Process -Name PrivateBuilds -ErrorAction SilentlyContinue | Where-Object {$_.Path -and $_.Path.StartsWith($buildRoot,[StringComparison]::OrdinalIgnoreCase)})
if($existing.Count -gt 0){Write-Output 'Private collection is already open. Use the existing Rift Ready private collection window.';return}
$privateLauncher=& (Join-Path $PSScriptRoot 'build-private-launcher.ps1')
if(Get-Process -Name 'League of Legends' -ErrorAction SilentlyContinue){throw 'Finish the active game before restarting Rift Ready for private testing.'}
foreach($process in @(Get-Process -Name RiftReference -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $app})){
    [void]$process.CloseMainWindow()
    if(!$process.WaitForExit(10000)){throw 'Close Rift Ready before starting the private session.'}
}
$launch=New-Object Diagnostics.ProcessStartInfo
$launch.FileName=$app
$launch.WorkingDirectory=Split-Path $app -Parent
$launch.UseShellExecute=$false
$launch.EnvironmentVariables['RIFT_RECOMMENDATIONS_FILE']=$feed
[void][Diagnostics.Process]::Start($launch)
# Only the separate masked collector receives the development key.
Start-Process -FilePath $privateLauncher -ArgumentList ('"'+$repo+'" '+$Budget) -WindowStyle Normal
