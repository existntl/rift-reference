param([switch]$SkipSmokeTest)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$cache = Join-Path $repo 'cache\native-toolchain'
$version = '20260826'
$name = "llvm-mingw-$version-ucrt-x86_64"
$expected = 'ae601f4e0f72bbdf441ad2df8bb16f037e2e9251559ea6b37b4057aef39c06c3'
$url = "https://github.com/mstorsjo/llvm-mingw/releases/download/$version/$name.zip"
New-Item -ItemType Directory -Path $cache -Force | Out-Null
$archive = Join-Path $cache "$name.zip"
if (!(Test-Path -LiteralPath $archive)) {
    # Portable upstream distribution; no PATH or registry changes.
    Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $archive
}
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expected) {
    throw "Native toolchain archive SHA256 does not match the upstream release digest."
}
$compiler = Join-Path $cache "$name\bin\x86_64-w64-mingw32-clang++.exe"
if (!(Test-Path -LiteralPath $compiler)) {
    Expand-Archive -LiteralPath $archive -DestinationPath $cache -Force
}
if (!$SkipSmokeTest) {
    $source = Join-Path $cache 'd3d11-smoke.cpp'
    $executable = Join-Path $cache 'd3d11-smoke.exe'
    @'
#include <windows.h>
#include <d3d11.h>
#include <dxgi.h>
#include <cstdio>
int main() {
    ID3D11Device* device = nullptr;
    ID3D11DeviceContext* context = nullptr;
    D3D_FEATURE_LEVEL level;
    HRESULT result = D3D11CreateDevice(nullptr, D3D_DRIVER_TYPE_WARP, nullptr, 0,
        nullptr, 0, D3D11_SDK_VERSION, &device, &level, &context);
    if (FAILED(result)) { std::printf("D3D11 WARP creation failed: %08lx\n", (unsigned long)result); return 1; }
    IDXGIDevice* dxgi = nullptr;
    result = device->QueryInterface(__uuidof(IDXGIDevice), reinterpret_cast<void**>(&dxgi));
    if (dxgi) dxgi->Release();
    context->Release();
    device->Release();
    if (FAILED(result)) return 2;
    std::printf("D3D11/DXGI smoke passed (WARP feature level %04x).\n", (unsigned)level);
    return 0;
}
'@ | Set-Content -LiteralPath $source -Encoding UTF8
    & $compiler -std=c++17 -O2 -static $source -o $executable -ld3d11 -ldxgi -ldxguid
    if ($LASTEXITCODE -ne 0) { throw 'Native smoke compilation failed.' }
    & $executable
    if ($LASTEXITCODE -ne 0) { throw 'Native smoke execution failed.' }
}
Write-Output "Compiler: $compiler"
