#include <windows.h>
#include <d3d11.h>
#include <dxgi.h>
#include <stdint.h>
#include <vector>
#include <string>
#include <mutex>
#include <atomic>
#include "MinHook.h"
extern "C" HRESULT RR_Draw(IDXGISwapChain*,const uint8_t*,uint32_t,uint32_t);
typedef HRESULT (STDMETHODCALLTYPE *PresentFn)(IDXGISwapChain*,UINT,UINT);
static PresentFn original=nullptr;
static std::mutex frameGate;
static std::vector<uint8_t> frame;
static uint32_t width=0,height=0,sequence=0;
static std::atomic<bool> enabled(false);
static uint64_t freshUntil=0;
static uint32_t uploaded=0;static bool haveUpload=false;
struct Handle { HANDLE value; Handle(HANDLE h):value(h){} ~Handle(){if(value)CloseHandle(value);} operator HANDLE()const{return value;} };
struct View { const void* value; View(const void* p):value(p){} ~View(){if(value)UnmapViewOfFile(value);} };
static HRESULT STDMETHODCALLTYPE present(IDXGISwapChain* chain,UINT sync,UINT flags){
 // Never wait on IPC or another thread. Original Present always receives unchanged arguments.
 if(enabled.load()&&!(flags&DXGI_PRESENT_TEST)&&frameGate.try_lock()) {
  HWND foreground=GetForegroundWindow();DWORD foregroundPid=0;GetWindowThreadProcessId(foreground,&foregroundPid);
  DXGI_SWAP_CHAIN_DESC description={};
  if(foregroundPid==GetCurrentProcessId()&&SUCCEEDED(chain->GetDesc(&description))&&description.OutputWindow==foreground&&!frame.empty()&&GetTickCount64()<=freshUntil){
   HRESULT drawn=RR_Draw(chain,(!haveUpload||uploaded!=sequence)?frame.data():nullptr,width,height);
   if(drawn==S_OK){uploaded=sequence;haveUpload=true;}else if(FAILED(drawn)){haveUpload=false;}
  }
  frameGate.unlock();
 }
 return original(chain,sync,flags);
}
static DWORD WINAPI worker(void*) {
 wchar_t name[100];swprintf(name,100,L"Local\\RiftReady.Attach.%lu",GetCurrentProcessId());
 Handle configuration=OpenFileMappingW(FILE_MAP_READ,FALSE,name);
 if(!configuration)return 1;
 auto config=(const wchar_t*)MapViewOfFile(configuration,FILE_MAP_READ,0,0,128);
 if(!config)return 2;
 View configView(config);std::wstring session(config,wcsnlen(config,64));
 if(session.size()!=32||session.find_first_not_of(L"0123456789abcdefABCDEF")!=std::wstring::npos)return 3;
 Handle ready=OpenEventW(EVENT_MODIFY_STATE,FALSE,(L"Local\\RiftReady.AttachReady."+session).c_str());
 Handle stop=OpenEventW(SYNCHRONIZE,FALSE,(L"Local\\RiftReady.AttachStop."+session).c_str());
 Handle map=OpenFileMappingW(FILE_MAP_READ,FALSE,(L"Local\\RiftReady.Render."+session).c_str());
 Handle mutex=OpenMutexW(SYNCHRONIZE|MUTEX_MODIFY_STATE,FALSE,(L"Local\\RiftReady.RenderLock."+session).c_str());
 auto view=map?(const uint8_t*)MapViewOfFile(map,FILE_MAP_READ,0,0,64ull+4096ull*2160*4):nullptr;
 View frameView(view);
 if(!ready||!stop||!view||!mutex)return 4;
 // Discover the ordinary D3D11 Present entry point using an owned hidden window.
 HWND window=CreateWindowExW(0,L"STATIC",L"Rift Ready hook discovery",WS_OVERLAPPED,0,0,32,32,nullptr,nullptr,GetModuleHandleW(nullptr),nullptr);
 DXGI_SWAP_CHAIN_DESC desc={};desc.BufferDesc.Width=32;desc.BufferDesc.Height=32;desc.BufferDesc.Format=DXGI_FORMAT_R8G8B8A8_UNORM;desc.SampleDesc.Count=1;desc.BufferUsage=DXGI_USAGE_RENDER_TARGET_OUTPUT;desc.BufferCount=1;desc.OutputWindow=window;desc.Windowed=TRUE;desc.SwapEffect=DXGI_SWAP_EFFECT_DISCARD;
 IDXGISwapChain* chain=nullptr;ID3D11Device* device=nullptr;ID3D11DeviceContext* context=nullptr;
 HRESULT hr=D3D11CreateDeviceAndSwapChain(nullptr,D3D_DRIVER_TYPE_HARDWARE,nullptr,0,nullptr,0,D3D11_SDK_VERSION,&desc,&chain,&device,nullptr,&context);
 if(FAILED(hr)){DestroyWindow(window);return 5;}
 void* address=(*(void***)chain)[8];
 bool hooked=MH_Initialize()==MH_OK&&MH_CreateHook(address,(void*)&present,(void**)&original)==MH_OK&&MH_EnableHook(address)==MH_OK;
 context->Release();device->Release();chain->Release();DestroyWindow(window);
 if(!hooked)return 6;
 enabled.store(true);SetEvent(ready);
 while(WaitForSingleObject(stop,33)==WAIT_TIMEOUT){
  DWORD wait=WaitForSingleObject(mutex,0);
  if(wait!=WAIT_OBJECT_0&&wait!=WAIT_ABANDONED)continue;
  uint32_t header[8];uint64_t heartbeat;memcpy(header,view,32);memcpy(&heartbeat,view+32,8);uint64_t now=GetTickCount64();
  bool valid=wait==WAIT_OBJECT_0&&header[0]==0x52465252&&header[1]==1&&header[3]>0&&header[3]<=4096&&header[4]>0&&header[4]<=2160&&header[5]==header[3]*4&&header[6]==header[5]*header[4]&&header[7]==1&&heartbeat<=now&&now-heartbeat<=4000;
  {std::lock_guard<std::mutex> lock(frameGate);
   if(valid){if(frame.empty()||sequence!=header[2]){frame.assign(view+64,view+64+header[6]);width=header[3];height=header[4];sequence=header[2];}freshUntil=heartbeat+4000;}
   else freshUntil=0;
  }
  ReleaseMutex(mutex);
 }
 enabled.store(false);
 // Leave pass-through hook resident until process exit: no unsafe live DLL unload.
 return 0;
}
extern "C" __declspec(dllexport) DWORD WINAPI RR_Start(void* argument){try{return worker(argument);}catch(...){enabled.store(false);return 7;}}
BOOL WINAPI DllMain(HINSTANCE instance,DWORD reason,LPVOID){
 if(reason==DLL_PROCESS_ATTACH)DisableThreadLibraryCalls(instance);
 return TRUE;
}
