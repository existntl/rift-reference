$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
. (Join-Path $repo 'scripts/start-private-recommendations.ps1') -ValidateOnly
$testRoot = Join-Path $repo ('build/private-launcher-test-' + [guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $testRoot)
$fake = Join-Path $testRoot 'fake_collector.py'
$code = @'
import json, os, pathlib, sys, time
output = pathlib.Path(sys.argv[sys.argv.index('--output') + 1])
key = os.environ.get('RIOT_API_KEY', '')
(output / 'result.json').write_text(json.dumps({'hasKey': key.startswith('RGAPI-'), 'keyInArgs': key in ' '.join(sys.argv)}))
print('x' * 100000)
print('y' * 100000, file=sys.stderr)
if (output / 'wait').exists(): time.sleep(30)
'@
[IO.File]::WriteAllText($fake, $code, (New-Object Text.UTF8Encoding($false)))
$flags = [Reflection.BindingFlags]'NonPublic,Instance'
$window = New-Object PrivateRecommendationWindow((Join-Path $repo 'cache/runtime/python.exe'), $fake, $testRoot, $testRoot, 500)
function Field($name) { $window.GetType().GetField($name, $flags).GetValue($window) }
function Invoke-Private($name) { [void]$window.GetType().GetMethod($name, $flags).Invoke($window, @()) }
try {
    $window.ShowInTaskbar = $false
    $window.StartPosition = [Windows.Forms.FormStartPosition]::Manual
    $window.Location = New-Object Drawing.Point(-10000, -10000)
    $window.Show()
    $preview = New-Object Drawing.Bitmap(1920, 1080)
    $graphics = [Drawing.Graphics]::FromImage($preview)
    $graphics.Clear([Drawing.Color]::FromArgb(24, 24, 27))
    $graphics.Dispose()
    $window.DrawToBitmap($preview, (New-Object Drawing.Rectangle(660, 370, $window.Width, $window.Height)))
    $preview.Save((Join-Path $testRoot 'launcher.png'), [Drawing.Imaging.ImageFormat]::Png)
    $preview.Dispose()
    $window.Hide()
    $keyBox = Field 'key'
    if (!$keyBox.UseSystemPasswordChar) { throw 'Key field is not masked.' }
    $keyBox.Text = 'RGAPI-private-test-only-0000000000000000'
    Invoke-Private 'BeginCollection'
    $worker = Field 'worker'
    if ($keyBox.Text.Length -ne 0) { throw 'Key field retained credential.' }
    if ($worker.StartInfo.EnvironmentVariables.ContainsKey('RIOT_API_KEY')) { throw 'Parent launch metadata retained credential.' }
    if (!$worker.WaitForExit(10000)) { throw 'Output pipes blocked collector.' }
    Invoke-Private 'Poll'
    $result = Get-Content -Raw -Encoding UTF8 (Join-Path $testRoot 'result.json') | ConvertFrom-Json
    if (!$result.hasKey -or $result.keyInArgs -or $worker.ExitCode -ne 0) { throw 'Child credential handling failed.' }
    if (!(Field 'status').Text.StartsWith('Collection finished.')) { throw 'Success status missing.' }
    [IO.File]::WriteAllText((Join-Path $testRoot 'wait'), '')
    $keyBox.Text = 'RGAPI-private-test-only-0000000000000000'
    Invoke-Private 'BeginCollection'
    $worker = Field 'worker'
    Invoke-Private 'StopWorker'
    if (!$worker.WaitForExit(10000)) { throw 'Owned worker did not stop.' }
    Invoke-Private 'Poll'
    if (!(Field 'status').Text.StartsWith('Stopped.')) { throw 'Cancellation status missing.' }
    Write-Output 'Private launcher: masked input, child environment, metadata clearing, pipe draining, success and cancellation checks passed.'
}
finally {
    Invoke-Private 'StopWorker'
    $window.Dispose()
}
