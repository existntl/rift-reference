$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$appRoot = Join-Path $projectRoot 'build/app'
& (Join-Path $PSScriptRoot 'build.ps1') -Installer
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $compiler /nologo /target:exe "/out:$appRoot/MobileTests.exe" "/reference:$appRoot/RiftReference.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll (Join-Path $projectRoot 'tests/MobileTests.cs') (Join-Path $projectRoot 'tests/DashboardFixtures.cs')
if ($LASTEXITCODE -ne 0) { throw 'Mobile test compilation failed' }
& (Join-Path $appRoot 'MobileTests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Mobile snapshot/lifecycle tests failed' }
& $compiler /nologo /target:exe "/out:$appRoot/PostgameTests.exe" "/reference:$appRoot/RiftReference.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll (Join-Path $projectRoot 'tests/PostgameTests.cs') (Join-Path $projectRoot 'tests/DashboardFixtures.cs')
if ($LASTEXITCODE -ne 0) { throw 'Postgame test compilation failed' }
& (Join-Path $appRoot 'PostgameTests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Postgame fixture/tests failed' }
& (Join-Path $appRoot 'runtime/python.exe') (Join-Path $projectRoot 'tests/mobile_server_test.py')
if ($LASTEXITCODE -ne 0) { throw 'Mobile HTTP tests failed' }
Write-Output 'For browser QA, run tests/mobile-browser.cjs with Playwright and Edge available.'
