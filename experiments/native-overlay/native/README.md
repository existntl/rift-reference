# Independent D3D11 renderer experiment

This DLL composites a BGRA frame directly onto a cooperating D3D11 host's
backbuffer before Present. The bundled executable owns that window/device and
explicitly loads the DLL. This directory contains no process injector or game
hook. Success here does not demonstrate League/Vanguard compatibility.

Build with `native/build.ps1 -Test` from PowerShell (or its absolute path).
The compiler defaults to the repository's ignored llvm-mingw cache. Output goes
to `build/native-overlay`. The test executable, with no arguments, checks actual
GPU pixels for straight alpha composition and transparent regions, host pipeline
restoration, buffer resizing, cached textures, cleanup/reinitialization and invalid
input. It prefers hardware, with WARP fallback only for automated tests.

For a visible own-host preview:

```powershell
& ./build/native-overlay/RiftReadyRendererHost.exe --frame frame.bgra --width 1920 --height 1080
& ./build/native-overlay/RiftReadyRendererHost.exe --session '<session-guid>'
```

Add `--fullscreen` to request DXGI fullscreen **in this test host only**. Escape
closes the host and restores windowed state. Fullscreen testing changes display
ownership temporarily and should be coordinated while the user is not playing.
It does not change League's settings or install the renderer into Rift Ready.
Use `--duration 5` for automatic exit after five seconds, and
`--capture output.bmp` to save the first composited backbuffer via GPU readback.
CPU submission mean/max are reported separately from Present and GPU execution;
the first frame includes shader initialization and upload cost.

`renderer.h` describes the exported ABI. Incoming images use straight alpha;
premultiplied input will produce dark edges. A null pixel pointer reuses the
existing texture, so a caller need not upload unchanged frames. Texture dimensions
may differ from the backbuffer, but native resolution avoids scaled text.
Maximum input dimensions are 4096 by 2160. Draw attempts the renderer lock without
waiting and returns S_FALSE when another renderer operation owns it.

The optional live stream reader uses the version 1 shared memory layout agreed
with the managed frame publisher: `Local\RiftReady.Render.<guid>` plus mutex
`Local\RiftReady.RenderLock.<guid>`. It tries the mutex without waiting; validates
magic, version, dimensions, stride, byte count, visibility and a four-second
monotonic heartbeat; and copies pixels only when the sequence changes. Invalid,
stale or unavailable frames are not drawn. Session tokens select an explicitly
created local stream and are never guessed by enumeration.

The current host draws a dark test background at vsync. Its frame count is not a
League performance measurement. Live IPC and actual fullscreen visibility require
separate testing beyond the default GPU tests.
