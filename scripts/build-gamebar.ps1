param([ValidateSet('Debug','Release')][string]$Configuration='Debug',[string]$AppAssembly)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
if(!(Test-Path -LiteralPath $vswhere)){throw 'Install the Microsoft UWP build tools with scripts/bootstrap-gamebar-tools.ps1 first.'}
$msbuild=& $vswhere -latest -products '*' -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if(!$msbuild){throw 'MSBuild is not installed yet.'}
$project=Join-Path $repo 'experiments/gamebar/Widget/RiftReady.GameBar.csproj'
& (Join-Path $PSScriptRoot 'build-gamebar-bridge.ps1') -AppAssembly $AppAssembly
$output=Join-Path $repo 'build/gamebar'
New-Item -ItemType Directory -Path $output -Force | Out-Null
& $msbuild $project /restore /m /v:minimal "/p:Configuration=$Configuration" /p:Platform=x64 /p:AppxPackageSigningEnabled=false /p:AppxBundle=Never /p:GenerateAppxPackageOnBuild=true /p:UapAppxPackageBuildMode=SideloadOnly /p:RestoreSources=https://api.nuget.org/v3/index.json
if($LASTEXITCODE -ne 0){throw 'Game Bar widget build failed.'}
Write-Output "Unsigned widget packages are under $output. This command does not install the widget or alter certificate trust."
