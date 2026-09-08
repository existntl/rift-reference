$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..\..'))
$output = Join-Path $repo 'build\gamebar'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$source = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\WidgetPage.xaml.cs')
$offset = $source.IndexOf('    internal sealed class LiveFrame')
if ($offset -lt 0) { throw 'Cannot find exact widget parser source.' }
$parser = 'using System; using System.Text; using Windows.Data.Json; namespace RiftReady.GameBar {' + $source.Substring($offset)
$parserPath = Join-Path $output 'LiveFrameParser.cs'
$exe = Join-Path $output 'LiveFrameTests.exe'
Set-Content -LiteralPath $parserPath -Value $parser -Encoding utf8
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$metadata = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\UnionMetadata\10.0.19041.0\Windows.winmd'
& (Join-Path $framework 'csc.exe') /nologo /target:exe "/out:$exe" "/r:$metadata" "/r:$framework\System.Runtime.dll" "/r:$framework\System.Runtime.WindowsRuntime.dll" "/r:$framework\System.Runtime.InteropServices.WindowsRuntime.dll" $parserPath (Join-Path $PSScriptRoot 'LiveFrameTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Parser test compilation failed.' }
& $exe
if ($LASTEXITCODE -ne 0) { throw 'Parser tests failed.' }
