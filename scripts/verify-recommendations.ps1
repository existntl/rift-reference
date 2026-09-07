$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$appRoot = Join-Path $projectRoot 'build/app'
& (Join-Path $projectRoot 'cache/runtime/python.exe') -m unittest discover -s (Join-Path $projectRoot 'services/recommendations') -p 'test_*.py'
if ($LASTEXITCODE -ne 0) { throw 'Collector checks failed' }
& (Join-Path $PSScriptRoot 'build.ps1')
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $compiler /nologo /target:exe "/out:$appRoot/RecommendationTests.exe" "/reference:$appRoot/RiftReference.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll (Join-Path $projectRoot 'tests/RecommendationTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Recommendation test compilation failed' }
& (Join-Path $appRoot 'RecommendationTests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Recommendation checks failed' }
$appCheck = Start-Process -FilePath (Join-Path $appRoot 'RiftReference.exe') -ArgumentList '--test' -WindowStyle Hidden -PassThru -Wait
if ($appCheck.ExitCode -ne 0) { throw 'Application checks failed' }
