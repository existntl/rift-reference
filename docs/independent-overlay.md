# Independent fullscreen overlay prototype

## Latest League test: ordinary loading blocked

Practice Tool live test on2026-09-07 verified the exact game executable with limited
query rights. The normal loader stopped at target-module enumeration with Windows
error5 (Access denied): `target loader module unavailable`. This occurs before remote
allocation, argument writing, LoadLibrary or adapter initialization. No overlay DLL
was loaded by this attempt. The owned producer was stopped; installed app unchanged.
The protection responsible was not independently identified. Do not claim Vanguard
specifically generated this error, or retry with elevated/evasive access. This loading
path is blocked on the tested League setup, despite the successful owned-host tests.

The user chose this route after the Overwolf proposal was submitted. No Overwolf
runtime or account credentials are used by this prototype. Installed Rift Ready
0.12.13 and the public release are unchanged.

## Implemented

- `src/NativeOverlayBridge.cs`: current-user-only named mapping and mutex, random
  session names, bounded top-down straight-alpha BGRA frames, sequence and heartbeat.
  Existing panel painters preserve the saved layout, chroma transparency and opacity.
- `GameOverlay`: explicit experimental environment opt-in; publishes changed samples,
  settings, viewport and scoreboard-key state. Unchanged polls update only heartbeat.
  Stale/unfocused/empty layouts hide the frame. No API responses or identifiers are
  transferred; the transport contains rendered pixels and fixed metadata only.
- `experiments/native-overlay/LivePublisher.cs`: isolated local API reader, existing
  overlay settings, no desktop overlay windows. Test duration is bounded to 10 minutes.
- Native D3D11 renderer: alpha-composited texture before Present, isolated context
  state, cached uploads, nonblocking drawing, no retained backbuffer references.
- MinHook Present adapter: explicit ordinary Windows DLL loading into an exact
  requested x64 process/path; a worker reads pixels outside the render callback.
  Checks foreground/swap-chain window, freshness and visibility. Preserves original
  Present synchronization/flags. No game state reads or input automation.
- Stop disables drawing and leaves a pass-through adapter resident until game exit.
  Reattaching requires restarting that target. Do not unload executing hook code.

## Verified

13 bridge checks, 26 lifecycle checks, 102 existing overlay checks, application
self-checks, and 10 GPU renderer checks pass. GPU readback verifies straight-alpha
composition, transparent pixels, host pipeline restoration, resizing and reinitialization.
An owned 1920x1080 host reported DXGI fullscreen TRUE and rendered 567 frames in
4000ms. Its CPU submission mean was 0.0354ms, max13.7599ms including initial setup;
these are not League FPS or GPU cost measurements. Output: build/native-overlay.

The complete ordinary DLL-loading/Present-hook path also passed in a fresh owned
fullscreen host (SEQUENTIAL swap chain for preserved post-Present readback): DXGI
fullscreen TRUE at1920x1080;82,089 pixels differed from the background with the hook
enabled, zero after disable. Duplicate attachment was rejected. Reproduce with
`experiments/native-overlay/integration/verify.ps1`. No League attachment occurred.

## Build and test

Use Windows PowerShell, not PowerShell Core, for the .NET Framework build scripts:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/bootstrap-native-toolchain.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/verify-native-overlay.ps1
```

The portable LLVM-MinGW compiler and pinned upstream MinHook source are cached in
ignored directories. Include the MinHook license if distributing binaries.

After the owned-host hook test passes, open Practice Tool in Fullscreen and run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/start-native-overlay-test.ps1 -Seconds 180
```

The launcher verifies one running League process, starts an isolated producer, and
attempts ordinary loading once. It does not replace the installed app, change
preferences, alter League files, install a driver, elevate or change security settings.
If loading is denied, stop and record the exact Windows error. Initialization success
does not prove drawing: verify panel visibility, Tab, focus transitions and frame times.

## Remaining release requirements

Actual League/Vanguard compatibility, alternate Present implementations, device-loss
and multi-swap-chain stress, patch changes, crash recovery, reliable user controls and
same-scene overlay-off/on GPU frame-time testing remain unresolved. This is a test
prototype, not an automatic startup component or a production fullscreen promise.

Microsoft documents [Present](https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgiswapchain-present),
[ResizeBuffers](https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgiswapchain-resizebuffers),
and [DLL lifecycle constraints](https://learn.microsoft.com/en-us/windows/win32/dlls/dynamic-link-library-best-practices).
[Riot's FAQ](https://www.riotgames.com/en/DevRel/vanguard-faq) describes API-based overlays
and states there is no Vanguard allowlist; it does not certify this custom renderer.
