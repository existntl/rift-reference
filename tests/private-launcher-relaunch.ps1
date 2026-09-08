$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$builder=Join-Path $repo 'scripts/build-private-launcher.ps1'
$first=@(& $builder)
if($first.Count -ne 1 -or !(Test-Path -LiteralPath $first[0])){throw 'Build did not return exactly one executable.'}
$before=(Get-FileHash -LiteralPath $first[0]).Hash
# An open executable cannot be rewritten on Windows. Rebuilding unchanged source
# must reuse it without trying to replace it or starting an application.
$locked=[IO.File]::Open($first[0],[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
try {
    $second=@(& $builder)
    if($second.Count -ne 1 -or $second[0] -ne $first[0]){throw 'Identical build did not reuse its executable.'}
} finally {$locked.Dispose()}
if((Get-FileHash -LiteralPath $first[0]).Hash -ne $before){throw 'Reused executable changed.'}

# Mock process lookup so the test never inspects, starts, or closes real apps.
function Get-Process {
    param($Name,$ErrorAction)
    if($Name -eq 'PrivateBuilds'){return [pscustomobject]@{Path=$first[0]}}
    throw 'Repeated launch reached app/game process handling.'
}
function Start-Process {throw 'Repeated launch started an application.'}
$result=& (Join-Path $repo 'scripts/start-private-builds.ps1')
if($result -notlike 'Private collection is already open.*'){throw 'Repeated launch did not preserve the existing session.'}
Write-Output 'Private launcher relaunch: locked executable reuse and existing-session preservation passed.'
