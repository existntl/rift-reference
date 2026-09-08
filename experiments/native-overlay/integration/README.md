# Opt-in Present adapter experiment

Build with `integration/build.ps1`. Requires the portable native toolchain and
downloads the official MinHook v1.3.4 source pinned by commit. Upstream MinHook
license remains in `cache/minhook/minhook-<commit>/LICENSE.txt`; include it with any
distributed binaries. No distribution or app installation is part of this experiment.

The loader requires all of `--pid`, `--target` (exact absolute executable path),
`--session` (32 hexadecimal GUID), and `--dll` (absolute adapter DLL path).
It verifies the process path and x64 architecture, then uses ordinary Windows
LoadLibrary loading followed by an explicit exported initializer outside DllMain.
An already loaded adapter is refused until target restart. Access denial stops the attempt. It never elevates, scans for
games automatically, changes security settings, or accesses game state.

The adapter discovers D3D11 Present through an owned dummy swap chain and uses
MinHook to intercept that entry point. A worker copies the existing sanitized BGRA
shared-memory session; Present only tries a local lock and never waits for IPC.
It hides on stale data, invisible frames, or loss of foreground process, and only
draws the swap chain whose output window exactly matches the foreground window. Unchanged
frame sequences reuse the GPU texture. Original Present always receives the
original sync interval and flags; test-only Present does not draw.

Disable using the same `--pid`, `--target`, `--session` and `--disable`.
This leaves the hook as a pass-through until target exit; the DLL is intentionally
not unloaded while another thread may be executing it.

This is a D3D11 experiment, not a tested League integration. Present1, other graphics
APIs, alternate swap-chain implementations, concurrent swap chains, device removal,
anti-cheat compatibility and crash recovery need additional validation. The renderer
may allocate GPU resources during initialization and resize; CPU IPC allocation
occurs on the worker. Do not claim zero allocation or zero performance overhead.

Initial validation targets only the repository's owned renderer host. A successful
loader exit proves initialization, not visible composition; compare GPU captures
before and after disable to verify actual drawing.

Run `integration/verify.ps1` for a bounded 12-second fullscreen owned-host test, or
pass `-Windowed`. It creates a fresh fixture/session and never targets League.
The hook-only fixture uses SEQUENTIAL presentation to preserve post-Present
readback; it does not call the renderer directly. The verification rejects a
duplicate attachment, captures the hooked output, disables drawing, then verifies
the entire captured frame returns to its original background.

On 2026-09-07 the fullscreen test confirmed DXGI fullscreen state TRUE at 1920x1080:
82,089 pixels contained overlay content while enabled and zero differed from the
background after disable. The host presented 1,723 frames over 12 seconds with
vsync. This is an owned synthetic fixture result, not a League FPS measurement.
Host CPU timing excludes Present and therefore excludes the hook's rendering cost.
