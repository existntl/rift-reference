#include <windows.h>
#include <d3d11.h>
#include <dxgi.h>
#include <stdint.h>
#include <stdio.h>
#include <vector>
#include <string>
#include <fstream>
#include <iterator>
#include <stdlib.h>
#include <cmath>

template<class T> static void drop(T*& p){if(p)p->Release();p=nullptr;}
typedef HRESULT (*Draw)(IDXGISwapChain*,const uint8_t*,uint32_t,uint32_t);
typedef void (*Shutdown)();
static bool resized=false;
static LRESULT CALLBACK procedure(HWND w,UINT m,WPARAM a,LPARAM b){if(m==WM_DESTROY){PostQuitMessage(0);return 0;}if(m==WM_KEYDOWN&&a==VK_ESCAPE){DestroyWindow(w);return 0;}if(m==WM_SIZE){resized=true;return 0;}return DefWindowProcW(w,m,a,b);}
static void check(HRESULT hr,const char* operation){if(FAILED(hr)){fprintf(stderr,"FAIL %s HRESULT=0x%08lx\n",operation,(unsigned long)hr);exit(1);}}
struct Stream {
 HANDLE map=nullptr,mutex=nullptr;const uint8_t* view=nullptr;uint32_t seq=0,w=0,h=0;bool have=false;
 ~Stream(){if(view)UnmapViewOfFile(view);if(map)CloseHandle(map);if(mutex)CloseHandle(mutex);}
 bool open(const std::wstring& session){
  if(session.empty()||session.size()>64||session.find_first_not_of(L"0123456789abcdefABCDEF-")!=std::wstring::npos)return false;
  map=OpenFileMappingW(FILE_MAP_READ,FALSE,(L"Local\\RiftReady.Render."+session).c_str());mutex=OpenMutexW(SYNCHRONIZE|MUTEX_MODIFY_STATE,FALSE,(L"Local\\RiftReady.RenderLock."+session).c_str());
  if(!map||!mutex)return false;view=(const uint8_t*)MapViewOfFile(map,FILE_MAP_READ,0,0,64ull+4096ull*2160*4);return view!=nullptr;
 }
 bool read(std::vector<uint8_t>& pixels,bool& changed){
  changed=false;DWORD wait=WaitForSingleObject(mutex,0);if(wait!=WAIT_OBJECT_0&&wait!=WAIT_ABANDONED)return false;
  uint32_t header[8];memcpy(header,view,32);uint64_t heartbeat;memcpy(&heartbeat,view+32,8);uint64_t now=GetTickCount64();
  bool valid=wait==WAIT_OBJECT_0&&header[0]==0x52465252&&header[1]==1&&header[3]>0&&header[3]<=4096&&header[4]>0&&header[4]<=2160&&header[5]==header[3]*4&&header[6]==header[5]*header[4]&&header[7]==1&&heartbeat<=now&&now-heartbeat<=4000;
  if(valid&&(!have||seq!=header[2])){w=header[3];h=header[4];pixels.assign(view+64,view+64+header[6]);seq=header[2];have=true;changed=true;}
  ReleaseMutex(mutex);return valid&&have;
 }
};
int wmain(int argc,wchar_t** argv){
 bool full=false,interactive=false,hookTest=false;std::wstring file,session,capture,captureDisabled;uint32_t width=1920,height=1080;unsigned duration=0;
 for(int i=1;i<argc;i++){std::wstring a=argv[i];if(a==L"--fullscreen"){full=true;interactive=true;}else if(a==L"--hook-test"){hookTest=true;interactive=true;}else if(a==L"--frame"&&i+1<argc){file=argv[++i];interactive=true;}else if(a==L"--session"&&i+1<argc){session=argv[++i];interactive=true;}else if(a==L"--width"&&i+1<argc)width=(uint32_t)_wtoi(argv[++i]);else if(a==L"--height"&&i+1<argc)height=(uint32_t)_wtoi(argv[++i]);else if(a==L"--duration"&&i+1<argc)duration=(unsigned)_wtoi(argv[++i]);else if(a==L"--capture"&&i+1<argc)capture=argv[++i];else if(a==L"--capture-after-disable"&&i+1<argc)captureDisabled=argv[++i];else {fprintf(stderr,"Unknown argument\n");return 2;}}
 if(!width||width>4096||!height||height>2160)return 2;
 wchar_t exe[MAX_PATH];GetModuleFileNameW(nullptr,exe,MAX_PATH);std::wstring dll=exe;dll=dll.substr(0,dll.find_last_of(L"\\/")+1)+L"RiftReadyRenderer.dll";
 HMODULE library=LoadLibraryW(dll.c_str());if(!library){fprintf(stderr,"Cannot load own renderer DLL: %lu\n",GetLastError());return 1;}
 Draw draw=(Draw)GetProcAddress(library,"RR_Draw");Shutdown shutdown=(Shutdown)GetProcAddress(library,"RR_Shutdown");if(!draw||!shutdown)return 1;
 std::vector<uint8_t> pixels;Stream stream;
 if(!file.empty()){FILE* input=_wfopen(file.c_str(),L"rb");if(!input)return 2;pixels.resize((size_t)width*height*4);size_t count=fread(pixels.data(),1,pixels.size(),input);int extra=fgetc(input);fclose(input);if(count!=pixels.size()||extra!=EOF){fprintf(stderr,"Frame length mismatch\n");return 2;}}
 if(!session.empty()&&!stream.open(session)){fprintf(stderr,"Cannot open authorized frame session\n");return 2;}
 WNDCLASSW wc={};wc.lpfnWndProc=procedure;wc.hInstance=GetModuleHandleW(nullptr);wc.lpszClassName=L"RiftReadyOwnRendererTest";RegisterClassW(&wc);
 HWND window=CreateWindowW(wc.lpszClassName,L"Rift Ready independent renderer TEST HOST - Esc closes",WS_OVERLAPPEDWINDOW,CW_USEDEFAULT,CW_USEDEFAULT,960,600,nullptr,nullptr,wc.hInstance,nullptr);
 if(!window)return 1;ShowWindow(window,interactive?SW_SHOW:SW_HIDE);
 DXGI_SWAP_CHAIN_DESC sd={};sd.BufferDesc.Width=interactive?width:64;sd.BufferDesc.Height=interactive?height:64;sd.BufferDesc.Format=DXGI_FORMAT_R8G8B8A8_UNORM;sd.SampleDesc.Count=1;sd.BufferUsage=DXGI_USAGE_RENDER_TARGET_OUTPUT;sd.BufferCount=2;sd.OutputWindow=window;sd.Windowed=TRUE;sd.SwapEffect=hookTest?DXGI_SWAP_EFFECT_SEQUENTIAL:DXGI_SWAP_EFFECT_DISCARD;sd.Flags=DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH;
 IDXGISwapChain* chain=nullptr;ID3D11Device* device=nullptr;ID3D11DeviceContext* context=nullptr;D3D_FEATURE_LEVEL level;
 HRESULT hr=D3D11CreateDeviceAndSwapChain(nullptr,D3D_DRIVER_TYPE_HARDWARE,nullptr,D3D11_CREATE_DEVICE_BGRA_SUPPORT,nullptr,0,D3D11_SDK_VERSION,&sd,&chain,&device,&level,&context);
 if(FAILED(hr)&&!interactive)hr=D3D11CreateDeviceAndSwapChain(nullptr,D3D_DRIVER_TYPE_WARP,nullptr,D3D11_CREATE_DEVICE_BGRA_SUPPORT,nullptr,0,D3D11_SDK_VERSION,&sd,&chain,&device,&level,&context);check(hr,"create D3D11 device");
 if(full){check(chain->SetFullscreenState(TRUE,nullptr),"own host exclusive fullscreen");BOOL state=FALSE;check(chain->GetFullscreenState(&state,nullptr),"query fullscreen");printf("Own test host DXGI fullscreen state: %s\n",state?"TRUE":"FALSE");}
 unsigned passes=0;
 auto test=[&](UINT size){
  context->ClearState();check(chain->ResizeBuffers(2,size,size,DXGI_FORMAT_UNKNOWN,sd.Flags),"resize without renderer buffer references");
  ID3D11Texture2D* buffer=nullptr;ID3D11RenderTargetView* target=nullptr;check(chain->GetBuffer(0,__uuidof(ID3D11Texture2D),(void**)&buffer),"get buffer");check(device->CreateRenderTargetView(buffer,nullptr,&target),"target");
  float background[4]={0,0,1,1};context->ClearRenderTargetView(target,background);
  context->OMSetRenderTargets(1,&target,nullptr);context->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_LINELIST);D3D11_VIEWPORT original={3,4,12,13,0.2f,0.8f};context->RSSetViewports(1,&original);
  uint8_t frame[16]={0,0,255,128, 0,255,0,0, 0,0,255,128, 0,255,0,0};check(draw(chain,frame,2,2),"draw straight alpha frame");
  D3D11_PRIMITIVE_TOPOLOGY topology;context->IAGetPrimitiveTopology(&topology);D3D11_VIEWPORT restored={};UINT n=1;context->RSGetViewports(&n,&restored);ID3D11RenderTargetView* restoredTarget=nullptr;context->OMGetRenderTargets(1,&restoredTarget,nullptr);
  if(topology!=D3D11_PRIMITIVE_TOPOLOGY_LINELIST||memcmp(&original,&restored,sizeof original)||restoredTarget!=target){fprintf(stderr,"FAIL host pipeline restoration\n");exit(1);}drop(restoredTarget);passes++;
  D3D11_TEXTURE2D_DESC td;buffer->GetDesc(&td);td.Usage=D3D11_USAGE_STAGING;td.BindFlags=0;td.CPUAccessFlags=D3D11_CPU_ACCESS_READ;ID3D11Texture2D* staging=nullptr;check(device->CreateTexture2D(&td,nullptr,&staging),"staging");context->CopyResource(staging,buffer);D3D11_MAPPED_SUBRESOURCE mapped={};check(context->Map(staging,0,D3D11_MAP_READ,0,&mapped),"GPU readback");
  uint8_t* p=(uint8_t*)mapped.pData;uint8_t* right=p+(size-1)*4;
  if(abs((int)p[0]-128)>1||p[1]!=0||abs((int)p[2]-127)>1||p[3]!=255||right[0]!=0||right[1]!=0||right[2]!=255){fprintf(stderr,"FAIL composition pixel RGBA %u %u %u %u / %u %u %u\n",p[0],p[1],p[2],p[3],right[0],right[1],right[2]);exit(1);}context->Unmap(staging,0);passes++;
  check(draw(chain,nullptr,2,2),"cached texture draw");passes++;drop(staging);context->ClearState();drop(target);drop(buffer);
 };
 if(!interactive){test(64);test(96);shutdown();test(32);if(draw(chain,nullptr,0,2)!=E_INVALIDARG)return 1;passes++;printf("PASS %u checks: GPU alpha/transparency, host state, resize, cached draw, reinitialization, invalid input\n",passes);}
 else {
  bool running=true,captured=false,capturedDisabled=false;unsigned frames=0;uint64_t started=GetTickCount64();LARGE_INTEGER frequency;QueryPerformanceFrequency(&frequency);double cpuTotal=0,cpuMax=0;
  while(running){MSG message;while(PeekMessageW(&message,nullptr,0,0,PM_REMOVE)){if(message.message==WM_QUIT)running=false;TranslateMessage(&message);DispatchMessageW(&message);}if(!running||(duration&&GetTickCount64()-started>=duration*1000ull))break;
   if(IsIconic(window)){Sleep(50);continue;}
   if(resized&&!full){RECT r;GetClientRect(window,&r);if(r.right>0&&r.bottom>0){context->ClearState();check(chain->ResizeBuffers(2,r.right,r.bottom,DXGI_FORMAT_UNKNOWN,sd.Flags),"window resize");}resized=false;}
   ID3D11Texture2D* back=nullptr;ID3D11RenderTargetView* target=nullptr;check(chain->GetBuffer(0,__uuidof(ID3D11Texture2D),(void**)&back),"frame buffer");check(device->CreateRenderTargetView(back,nullptr,&target),"frame target");float background[]={0.015f,0.022f,0.03f,1};context->ClearRenderTargetView(target,background);drop(target);drop(back);
   LARGE_INTEGER before,after;QueryPerformanceCounter(&before);bool changed=false,visible=true;if(!session.empty()){visible=stream.read(pixels,changed);width=stream.w;height=stream.h;}else changed=frames==0;
   if(!hookTest&&visible&&!pixels.empty())check(draw(chain,changed?pixels.data():nullptr,width,height),"interactive draw");
   QueryPerformanceCounter(&after);double elapsed=(after.QuadPart-before.QuadPart)*1000.0/frequency.QuadPart;cpuTotal+=elapsed;if(elapsed>cpuMax)cpuMax=elapsed;
   if(hookTest){hr=chain->Present(1,0);if(FAILED(hr)){fprintf(stderr,"Present failed %08lx\n",(unsigned long)hr);break;}}
   std::wstring captureNow;
   if(!captured&&!capture.empty()&&(!hookTest||GetTickCount64()-started>=3000)){captureNow=capture;captured=true;}
   else if(hookTest&&!capturedDisabled&&!captureDisabled.empty()&&GetTickCount64()-started>=7000){captureNow=captureDisabled;capturedDisabled=true;}
   if(!captureNow.empty()){
    ID3D11Texture2D* source=nullptr;check(chain->GetBuffer(0,__uuidof(ID3D11Texture2D),(void**)&source),"capture buffer");D3D11_TEXTURE2D_DESC desc;source->GetDesc(&desc);desc.Usage=D3D11_USAGE_STAGING;desc.BindFlags=0;desc.CPUAccessFlags=D3D11_CPU_ACCESS_READ;ID3D11Texture2D* staging=nullptr;check(device->CreateTexture2D(&desc,nullptr,&staging),"capture staging");context->CopyResource(staging,source);D3D11_MAPPED_SUBRESOURCE mapped;check(context->Map(staging,0,D3D11_MAP_READ,0,&mapped),"capture GPU readback");
    BITMAPFILEHEADER fh={};BITMAPINFOHEADER ih={};fh.bfType=0x4d42;fh.bfOffBits=sizeof(fh)+sizeof(ih);fh.bfSize=fh.bfOffBits+desc.Width*desc.Height*4;ih.biSize=sizeof(ih);ih.biWidth=desc.Width;ih.biHeight=-(LONG)desc.Height;ih.biPlanes=1;ih.biBitCount=32;ih.biCompression=BI_RGB;
    FILE* output=_wfopen(captureNow.c_str(),L"wb");if(!output){fprintf(stderr,"Cannot write capture\n");return 2;}fwrite(&fh,sizeof fh,1,output);fwrite(&ih,sizeof ih,1,output);std::vector<uint8_t> row(desc.Width*4);for(UINT y=0;y<desc.Height;y++){const uint8_t* p=(uint8_t*)mapped.pData+y*mapped.RowPitch;for(UINT x=0;x<desc.Width;x++){row[x*4]=p[x*4+2];row[x*4+1]=p[x*4+1];row[x*4+2]=p[x*4];row[x*4+3]=255;}fwrite(row.data(),1,row.size(),output);}fclose(output);context->Unmap(staging,0);drop(staging);drop(source);
   }
   if(!hookTest){hr=chain->Present(1,0);if(FAILED(hr)){fprintf(stderr,"Present failed %08lx\n",(unsigned long)hr);break;}}frames++;
  }
  printf("Own host rendered %u frames in %llu ms (vsync enabled; not a League FPS measurement).\n",frames,(unsigned long long)(GetTickCount64()-started));
  printf("Overlay CPU submission milliseconds: mean %.4f, max %.4f (includes initialization/upload and stream copy; excludes Present and GPU time).\n",frames?cpuTotal/frames:0,cpuMax);
 }
 chain->SetFullscreenState(FALSE,nullptr);shutdown();context->ClearState();drop(context);drop(chain);drop(device);FreeLibrary(library);if(IsWindow(window))DestroyWindow(window);return 0;
}
