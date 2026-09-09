$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$validationRoot = Join-Path $projectRoot ('build/ui-' + [Guid]::NewGuid().ToString('N'))
& (Join-Path $PSScriptRoot 'build.ps1') -OutputDirectory $validationRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
foreach ($test in @('LayoutTest','PracticeUiTests','RiftToggleTests','RiftComboBoxTests','LoadoutTests','PostgameTests')) {
    & $compiler /nologo /target:exe "/out:$validationRoot/$test.exe" "/reference:$validationRoot/RiftReference.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll (Join-Path $projectRoot "tests/$test.cs") (Join-Path $projectRoot 'tests/DashboardFixtures.cs')
    if ($LASTEXITCODE -ne 0) { throw "Compilation failed: $test" }
    & (Join-Path $validationRoot "$test.exe")
    if ($LASTEXITCODE -ne 0) { throw "Failed: $test" }
}
Write-Output "UI evidence: $validationRoot"
