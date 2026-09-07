$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$appRoot=Join-Path $projectRoot ('build/home-'+[Guid]::NewGuid().ToString('N'))
& (Join-Path $PSScriptRoot 'build.ps1') -OutputDirectory $appRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
foreach($test in @('HomeDataTests','RankHistoryTests','HomeRender')){
 & $compiler /nologo "/out:$appRoot/$test.exe" "/reference:$appRoot/RiftReference.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll (Join-Path $projectRoot "tests/$test.cs")
 if($LASTEXITCODE -ne 0){throw 'Home test compilation failed'}
 & (Join-Path $appRoot "$test.exe")
 if($LASTEXITCODE -ne 0){throw 'Home checks failed'}
}
$check=Start-Process -FilePath (Join-Path $appRoot 'RiftReference.exe') -ArgumentList '--test' -WindowStyle Hidden -Wait -PassThru
if($check.ExitCode -ne 0){throw 'App checks failed'}
Write-Output "Home evidence: $appRoot"
