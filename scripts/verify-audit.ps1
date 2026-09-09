$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$auditRoot = Join-Path $projectRoot ('build/audit-' + [Guid]::NewGuid().ToString('N'))
& (Join-Path $PSScriptRoot 'build.ps1') -OutputDirectory $auditRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
Copy-Item -LiteralPath (Join-Path $projectRoot 'tests/update-race-helper.py') -Destination $auditRoot
foreach ($test in @('AuditRegressionTests','UpdateRaceTests','AuditPerformanceTests','MobileTests','RecommendationTests','OverlayTests')) {
    & $compiler /nologo "/out:$auditRoot/$test.exe" "/reference:$auditRoot/RiftReference.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll (Join-Path $projectRoot "tests/$test.cs") (Join-Path $projectRoot 'tests/DashboardFixtures.cs')
    if ($LASTEXITCODE -ne 0) { throw "Compile failed: $test" }
    & (Join-Path $auditRoot "$test.exe")
    if ($LASTEXITCODE -ne 0) { throw "Checks failed: $test" }
}
foreach ($test in @('transport_test.py','mobile_server_test.py')) {
    & (Join-Path $auditRoot 'runtime/python.exe') (Join-Path $projectRoot "tests/$test")
    if ($LASTEXITCODE -ne 0) { throw "Checks failed: $test" }
}
& (Join-Path $auditRoot 'runtime/python.exe') -m unittest discover -s (Join-Path $projectRoot 'services/recommendations') -p 'test_*.py'
if ($LASTEXITCODE -ne 0) { throw 'Collector checks failed' }
Write-Output "Audit evidence: $auditRoot"
