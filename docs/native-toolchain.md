# Portable native overlay toolchain

Run `scripts/bootstrap-native-toolchain.ps1` to download and verify the pinned
LLVM-MinGW 20260826 UCRT x64 toolchain into the ignored `cache/native-toolchain`
directory. The bootstrap does not modify PATH, install a service, or change the
registry. It needs roughly 2 GB of free space for the archive and extracted tools.

Upstream release: https://github.com/mstorsjo/llvm-mingw/releases/tag/20260826

Archive: `llvm-mingw-20260826-ucrt-x86_64.zip` (190721391 bytes)

SHA256 from GitHub's official release asset metadata:
`ae601f4e0f72bbdf441ad2df8bb16f037e2e9251559ea6b37b4057aef39c06c3`

Compiler, relative to repository root:
`cache/native-toolchain/llvm-mingw-20260826-ucrt-x86_64/bin/x86_64-w64-mingw32-clang++.exe`

The bootstrap compiles and runs an owned console smoke executable against D3D11
and DXGI, creates a WARP software device, queries IDXGIDevice, and releases both.
This passed on the development computer on 2026-09-07 (feature level 0xb000).
It proves compiler/header/library/runtime availability; it does not test fullscreen
presentation, another process, League, or overlay compatibility.

Use `-static` when building portable experiments to avoid requiring LLVM runtime
DLLs beside the executable. D3D11, DXGI and other Windows system DLLs still load
from Windows normally. Add the appropriate import libraries to each build.
