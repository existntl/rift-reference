#pragma once
#include <windows.h>
#include <dxgi.h>
#include <stdint.h>

// Exports are resolved with GetProcAddress by an explicitly cooperating host.
// Call only from the immediate-context render thread, before Present.
// pixels: tightly packed top-down BGRA, straight alpha, valid width*height*4 bytes.
// Passing nullptr reuses the previous texture with the same dimensions.
// The DLL preserves D3D11 immediate context pipeline state and does not keep
// backbuffer references. Win8+ D3D11.1 interfaces required; D3D feature level 10+.
// One device/cache per DLL; serialize multiple swapchains on the render thread.
typedef HRESULT (*RR_DrawFn)(IDXGISwapChain*,const uint8_t*,uint32_t,uint32_t);
typedef void (*RR_ShutdownFn)();
