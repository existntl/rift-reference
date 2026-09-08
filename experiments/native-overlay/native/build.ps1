param([string]$Compiler = "", [switch]$Test)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if (!$Compiler) { $Compiler = Join-Path $repo 'cache/native-toolchain/llvm-mingw-20260826-ucrt-x86_64/bin/x86_64-w64-mingw32-clang++.exe' }
if (!(Test-Path -LiteralPath $Compiler)) { throw "Native compiler missing: $Compiler" }
$output = Join-Path $repo 'build/native-overlay'
New-Item -ItemType Directory -Path $output -Force | Out-Null
& $Compiler '-std=c++17' '-O2' '-static' '-shared' (Join-Path $PSScriptRoot 'renderer.cpp') '-o' (Join-Path $output 'RiftReadyRenderer.dll') '-ld3d11' '-ldxgi' '-ldxguid' '-ld3dcompiler'
if ($LASTEXITCODE -ne 0) { throw 'Renderer build failed' }
& $Compiler '-std=c++17' '-O2' '-static' '-municode' (Join-Path $PSScriptRoot 'host.cpp') '-o' (Join-Path $output 'RiftReadyRendererHost.exe') '-ld3d11' '-ldxgi' '-ldxguid' '-luser32'
if ($LASTEXITCODE -ne 0) { throw 'Host build failed' }
if ($Test) { & (Join-Path $output 'RiftReadyRendererHost.exe'); if ($LASTEXITCODE -ne 0) { throw 'Native renderer tests failed' } }
Write-Output "Native experiment built at $output"
