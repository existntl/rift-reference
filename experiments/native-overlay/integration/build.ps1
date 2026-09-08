$ErrorActionPreference='Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$compiler=Join-Path $root 'cache\native-toolchain\llvm-mingw-20260826-ucrt-x86_64\bin\x86_64-w64-mingw32-clang++.exe'
$cc=Join-Path (Split-Path $compiler) 'x86_64-w64-mingw32-clang.exe'
$minhook=& (Join-Path $root 'scripts\bootstrap-minhook.ps1')
$output=Join-Path $root 'build\native-overlay'
New-Item -ItemType Directory -Force $output | Out-Null
Copy-Item -LiteralPath (Join-Path $minhook 'LICENSE.txt') -Destination (Join-Path $output 'MinHook-LICENSE.txt') -Force
$objects=@()
foreach($source in @('buffer.c','hook.c','trampoline.c','hde\hde64.c')){
 $object=Join-Path $output ((Split-Path $source -Leaf)+'.o')
 & $cc -O2 -c (Join-Path $minhook "src\$source") -o $object
 if($LASTEXITCODE){throw 'MinHook compilation failed'}
 $objects+=$object
}
& $compiler -std=c++17 -O2 -static -shared (Join-Path $PSScriptRoot 'adapter.cpp') (Join-Path $PSScriptRoot '..\native\renderer.cpp') @objects '-I' (Join-Path $minhook 'include') -o (Join-Path $output 'RiftReadyPresent.dll') -ld3d11 -ldxgi -ldxguid -ld3dcompiler -luser32
if($LASTEXITCODE){throw 'Adapter compilation failed'}
& $compiler -std=c++17 -O2 -static -municode (Join-Path $PSScriptRoot 'loader.cpp') -o (Join-Path $output 'RiftReadyAttach.exe')
if($LASTEXITCODE){throw 'Loader compilation failed'}
Write-Output $output
