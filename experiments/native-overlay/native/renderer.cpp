#include <windows.h>
#include <d3d11_1.h>
#include <dxgi.h>
#include <d3dcompiler.h>
#include <stdint.h>
#include <mutex>

template<class T> static void release(T*& p) { if(p) p->Release(); p=nullptr; }
static std::mutex gate;
struct Renderer {
 ID3D11Device* device=nullptr; ID3D11DeviceContext1* context=nullptr;
 ID3DDeviceContextState* state=nullptr; ID3D11VertexShader* vs=nullptr;
 ID3D11PixelShader* ps=nullptr; ID3D11BlendState* blend=nullptr;
 ID3D11RasterizerState* raster=nullptr; ID3D11DepthStencilState* depth=nullptr;
 ID3D11SamplerState* sampler=nullptr; ID3D11Texture2D* texture=nullptr;
 ID3D11ShaderResourceView* srv=nullptr; UINT width=0,height=0;
 void clear() { release(srv);release(texture);release(sampler);release(depth);release(raster);release(blend);release(ps);release(vs);release(state);release(context);release(device);width=height=0; }
 // Explicit RR_Shutdown performs D3D cleanup outside the loader lock. Do not
 // invoke COM/D3D from a global destructor during process/DLL termination.
 HRESULT init(ID3D11Device* d) {
  clear(); device=d;device->AddRef(); ID3D11DeviceContext* base=nullptr;
  device->GetImmediateContext(&base);HRESULT hr=base->QueryInterface(__uuidof(ID3D11DeviceContext1),(void**)&context);release(base);if(FAILED(hr))return hr;
  ID3D11Device1* d1=nullptr;hr=device->QueryInterface(__uuidof(ID3D11Device1),(void**)&d1);if(FAILED(hr))return hr;
  D3D_FEATURE_LEVEL levels[]={D3D_FEATURE_LEVEL_11_0,D3D_FEATURE_LEVEL_10_1,D3D_FEATURE_LEVEL_10_0};
  hr=d1->CreateDeviceContextState(0,levels,3,D3D11_SDK_VERSION,__uuidof(ID3D11Device),nullptr,&state);release(d1);if(FAILED(hr))return hr;
  const char* source="Texture2D image:register(t0);SamplerState sampling:register(s0);struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};V vertex(uint id:SV_VertexID){V o;o.uv=float2((id<<1)&2,id&2);o.p=float4(o.uv*float2(2,-2)+float2(-1,1),0,1);return o;}float4 pixel(V i):SV_TARGET{return image.Sample(sampling,i.uv);}";
  ID3DBlob* code=nullptr;ID3DBlob* error=nullptr;
  hr=D3DCompile(source,lstrlenA(source),"rift-ready-overlay",nullptr,nullptr,"vertex","vs_4_0",D3DCOMPILE_OPTIMIZATION_LEVEL3,0,&code,&error);release(error);if(FAILED(hr))return hr;
  hr=device->CreateVertexShader(code->GetBufferPointer(),code->GetBufferSize(),nullptr,&vs);release(code);if(FAILED(hr))return hr;
  hr=D3DCompile(source,lstrlenA(source),"rift-ready-overlay",nullptr,nullptr,"pixel","ps_4_0",D3DCOMPILE_OPTIMIZATION_LEVEL3,0,&code,&error);release(error);if(FAILED(hr))return hr;
  hr=device->CreatePixelShader(code->GetBufferPointer(),code->GetBufferSize(),nullptr,&ps);release(code);if(FAILED(hr))return hr;
  D3D11_BLEND_DESC b={};b.RenderTarget[0].BlendEnable=TRUE;b.RenderTarget[0].SrcBlend=D3D11_BLEND_SRC_ALPHA;b.RenderTarget[0].DestBlend=D3D11_BLEND_INV_SRC_ALPHA;b.RenderTarget[0].BlendOp=D3D11_BLEND_OP_ADD;b.RenderTarget[0].SrcBlendAlpha=D3D11_BLEND_ONE;b.RenderTarget[0].DestBlendAlpha=D3D11_BLEND_INV_SRC_ALPHA;b.RenderTarget[0].BlendOpAlpha=D3D11_BLEND_OP_ADD;b.RenderTarget[0].RenderTargetWriteMask=D3D11_COLOR_WRITE_ENABLE_ALL;
  hr=device->CreateBlendState(&b,&blend);if(FAILED(hr))return hr;
  D3D11_RASTERIZER_DESC r={};r.FillMode=D3D11_FILL_SOLID;r.CullMode=D3D11_CULL_NONE;r.DepthClipEnable=TRUE;
  hr=device->CreateRasterizerState(&r,&raster);if(FAILED(hr))return hr;
  D3D11_DEPTH_STENCIL_DESC ds={};ds.DepthEnable=FALSE;ds.StencilEnable=FALSE;hr=device->CreateDepthStencilState(&ds,&depth);if(FAILED(hr))return hr;
  D3D11_SAMPLER_DESC s={};s.Filter=D3D11_FILTER_MIN_MAG_MIP_LINEAR;s.AddressU=s.AddressV=s.AddressW=D3D11_TEXTURE_ADDRESS_CLAMP;s.MaxLOD=D3D11_FLOAT32_MAX;
  return device->CreateSamplerState(&s,&sampler);
 }
 HRESULT upload(const uint8_t* pixels,UINT w,UINT h) {
  HRESULT hr;
  if(w!=width||h!=height||!texture||!srv){release(srv);release(texture);width=height=0;D3D11_TEXTURE2D_DESC t={};t.Width=w;t.Height=h;t.MipLevels=t.ArraySize=1;t.Format=DXGI_FORMAT_B8G8R8A8_UNORM;t.SampleDesc.Count=1;t.Usage=D3D11_USAGE_DYNAMIC;t.BindFlags=D3D11_BIND_SHADER_RESOURCE;t.CPUAccessFlags=D3D11_CPU_ACCESS_WRITE;
   hr=device->CreateTexture2D(&t,nullptr,&texture);if(FAILED(hr))return hr;hr=device->CreateShaderResourceView(texture,nullptr,&srv);if(FAILED(hr))return hr;width=w;height=h;}
  D3D11_MAPPED_SUBRESOURCE m={};hr=context->Map(texture,0,D3D11_MAP_WRITE_DISCARD,0,&m);if(FAILED(hr))return hr;
  for(UINT y=0;y<h;++y)memcpy((uint8_t*)m.pData+y*m.RowPitch,pixels+(size_t)y*w*4,(size_t)w*4);context->Unmap(texture,0);return S_OK;
 }
} renderer;

// Called by an explicitly cooperating host, on its render thread, before Present.
// Pixels are tightly packed, top-down BGRA with straight alpha. No process hooks.
extern "C" __declspec(dllexport) HRESULT RR_Draw(IDXGISwapChain* chain,const uint8_t* pixels,uint32_t width,uint32_t height){
 if(!chain||!width||!height||width>4096||height>2160)return E_INVALIDARG;
 std::unique_lock<std::mutex> lock(gate,std::try_to_lock);if(!lock.owns_lock())return S_FALSE;
 ID3D11Device* d=nullptr;HRESULT hr=chain->GetDevice(__uuidof(ID3D11Device),(void**)&d);if(FAILED(hr))return hr;
 if(renderer.device!=d||!renderer.sampler){hr=renderer.init(d);}release(d);if(FAILED(hr)){renderer.clear();return hr;}
 ID3D11Texture2D* buffer=nullptr;ID3D11RenderTargetView* target=nullptr;
 hr=chain->GetBuffer(0,__uuidof(ID3D11Texture2D),(void**)&buffer);if(FAILED(hr))return hr;
 D3D11_TEXTURE2D_DESC desc={};buffer->GetDesc(&desc);hr=renderer.device->CreateRenderTargetView(buffer,nullptr,&target);release(buffer);if(FAILED(hr))return hr;
 if(pixels)hr=renderer.upload(pixels,width,height);else if(!renderer.srv||renderer.width!=width||renderer.height!=height)hr=E_INVALIDARG;
 if(FAILED(hr)){release(target);return hr;}
 ID3DDeviceContextState* previous=nullptr;auto c=renderer.context;c->SwapDeviceContextState(renderer.state,&previous);
 c->OMSetRenderTargets(1,&target,nullptr);c->OMSetBlendState(renderer.blend,nullptr,0xffffffff);c->OMSetDepthStencilState(renderer.depth,0);c->RSSetState(renderer.raster);
 D3D11_VIEWPORT viewport={0,0,(float)desc.Width,(float)desc.Height,0,1};c->RSSetViewports(1,&viewport);
 c->IASetInputLayout(nullptr);c->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
 c->VSSetShader(renderer.vs,nullptr,0);c->PSSetShader(renderer.ps,nullptr,0);c->GSSetShader(nullptr,nullptr,0);c->HSSetShader(nullptr,nullptr,0);c->DSSetShader(nullptr,nullptr,0);
 c->PSSetShaderResources(0,1,&renderer.srv);c->PSSetSamplers(0,1,&renderer.sampler);c->Draw(3,0);
 // Release overlay state's backbuffer binding before saving it or resizing host buffers.
 c->OMSetRenderTargets(0,nullptr,nullptr);c->SwapDeviceContextState(previous,nullptr);release(previous);release(target);return S_OK;
}
extern "C" __declspec(dllexport) void RR_Shutdown(){std::lock_guard<std::mutex> lock(gate);renderer.clear();}
