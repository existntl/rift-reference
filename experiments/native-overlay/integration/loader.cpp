#include <windows.h>
#include <tlhelp32.h>
#include <string>
#include <cstdio>
#include <cstdlib>
static int fail(const char* reason){fprintf(stderr,"Stopped: %s (Windows error %lu). No elevation or retry attempted.\n",reason,GetLastError());return 1;}
static uintptr_t findDll(DWORD pid,const std::wstring& path){HANDLE snapshot=CreateToolhelp32Snapshot(TH32CS_SNAPMODULE,pid);MODULEENTRY32W module={};module.dwSize=sizeof(module);uintptr_t result=0;if(snapshot!=INVALID_HANDLE_VALUE){if(Module32FirstW(snapshot,&module))do{if(!_wcsicmp(module.szExePath,path.c_str())){result=(uintptr_t)module.modBaseAddr;break;}}while(Module32NextW(snapshot,&module));CloseHandle(snapshot);}return result;}
int wmain(int argc,wchar_t** argv){
 DWORD pid=0;std::wstring expected,dll,session;bool disable=false;
 for(int i=1;i<argc;++i){std::wstring arg=argv[i];if(arg==L"--disable")disable=true;else if(i+1<argc){if(arg==L"--pid")pid=wcstoul(argv[++i],nullptr,10);else if(arg==L"--target")expected=argv[++i];else if(arg==L"--dll")dll=argv[++i];else if(arg==L"--session")session=argv[++i];else return fail("unknown argument");}else return fail("missing argument");}
 if(!pid||expected.empty()||session.size()!=32||session.find_first_not_of(L"0123456789abcdefABCDEF")!=std::wstring::npos)return fail("required: --pid PID --target exact-absolute-exe-path --session GUID [--dll absolute-dll-path | --disable]");
 HANDLE process=OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION|(!disable?(PROCESS_CREATE_THREAD|PROCESS_VM_OPERATION|PROCESS_VM_WRITE|PROCESS_VM_READ):0),FALSE,pid);
 if(!process)return fail("target access denied or unavailable");
 wchar_t path[32768];DWORD count=32768;if(!QueryFullProcessImageNameW(process,0,path,&count)||_wcsicmp(expected.c_str(),path))return fail("target executable path mismatch");
 BOOL wow=FALSE;if(!IsWow64Process(process,&wow)||wow)return fail("target must be x64");
 std::wstring stopName=L"Local\\RiftReady.AttachStop."+session;
 if(disable){HANDLE stop=OpenEventW(EVENT_MODIFY_STATE,FALSE,stopName.c_str());if(!stop)return fail("no matching active session");SetEvent(stop);CloseHandle(stop);CloseHandle(process);puts("Overlay disabled; pass-through DLL remains until target exit.");return 0;}
 if(dll.size()<4||dll[1]!=L':'||GetFileAttributesW(dll.c_str())==INVALID_FILE_ATTRIBUTES)return fail("DLL must be an existing absolute local path");
 if(findDll(pid,dll))return fail("adapter already loaded; restart the owned target before attaching again");
 HMODULE inspection=LoadLibraryExW(dll.c_str(),nullptr,DONT_RESOLVE_DLL_REFERENCES);
 auto start=inspection?GetProcAddress(inspection,"RR_Start"):nullptr;
 if(!start)return fail("DLL lacks RR_Start export");
 uintptr_t startOffset=(uintptr_t)start-(uintptr_t)inspection;FreeLibrary(inspection);
 wchar_t configName[100];swprintf(configName,100,L"Local\\RiftReady.Attach.%lu",pid);
 HANDLE config=CreateFileMappingW(INVALID_HANDLE_VALUE,nullptr,PAGE_READWRITE,0,128,configName);
 if(!config||GetLastError()==ERROR_ALREADY_EXISTS)return fail("target already has an attachment session");
 auto data=(wchar_t*)MapViewOfFile(config,FILE_MAP_WRITE,0,0,128);if(!data)return fail("configuration mapping failed");memcpy(data,session.c_str(),(session.size()+1)*sizeof(wchar_t));
 HANDLE ready=CreateEventW(nullptr,TRUE,FALSE,(L"Local\\RiftReady.AttachReady."+session).c_str());
 HANDLE stop=CreateEventW(nullptr,TRUE,FALSE,stopName.c_str());if(!ready||!stop)return fail("session events failed");
 // Resolve LoadLibraryW against the corresponding loaded module in this exact target.
 auto load=GetProcAddress(GetModuleHandleW(L"kernel32.dll"),"LoadLibraryW");HMODULE owner=nullptr;
 if(!GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,(LPCWSTR)load,&owner))return fail("loader resolution failed");
 wchar_t ownerPath[MAX_PATH];GetModuleFileNameW(owner,ownerPath,MAX_PATH);const wchar_t* basename=wcsrchr(ownerPath,L'\\');basename=basename?basename+1:ownerPath;
 HANDLE snapshot=CreateToolhelp32Snapshot(TH32CS_SNAPMODULE,pid);MODULEENTRY32W module={};module.dwSize=sizeof(module);uintptr_t remoteBase=0;
 if(snapshot!=INVALID_HANDLE_VALUE){if(Module32FirstW(snapshot,&module))do{if(!_wcsicmp(module.szModule,basename)){remoteBase=(uintptr_t)module.modBaseAddr;break;}}while(Module32NextW(snapshot,&module));CloseHandle(snapshot);}
 if(!remoteBase)return fail("target loader module unavailable");
 SIZE_T bytes=(dll.size()+1)*sizeof(wchar_t);void* argument=VirtualAllocEx(process,nullptr,bytes,MEM_COMMIT|MEM_RESERVE,PAGE_READWRITE);
 if(!argument||!WriteProcessMemory(process,argument,dll.c_str(),bytes,nullptr))return fail("target denied ordinary loader arguments");
 auto entry=(LPTHREAD_START_ROUTINE)(remoteBase+((uintptr_t)load-(uintptr_t)owner));HANDLE thread=CreateRemoteThread(process,nullptr,0,entry,argument,0,nullptr);
 if(!thread){VirtualFreeEx(process,argument,0,MEM_RELEASE);return fail("target denied ordinary loader thread");}
 if(WaitForSingleObject(thread,10000)!=WAIT_OBJECT_0)return fail("loader timed out; argument retained to avoid use-after-free");
 VirtualFreeEx(process,argument,0,MEM_RELEASE);CloseHandle(thread);
 uintptr_t adapterBase=findDll(pid,dll);if(!adapterBase)return fail("adapter not loaded in verified target");
 HANDLE starter=CreateRemoteThread(process,nullptr,0,(LPTHREAD_START_ROUTINE)(adapterBase+startOffset),nullptr,0,nullptr);
 if(!starter)return fail("target denied explicit adapter initialization");CloseHandle(starter);
 if(WaitForSingleObject(ready,10000)!=WAIT_OBJECT_0){SetEvent(stop);return fail("adapter did not confirm initialization");}
 UnmapViewOfFile(data);CloseHandle(config);CloseHandle(ready);CloseHandle(stop);CloseHandle(process);
 puts("Present adapter initialized. Compatibility is confirmed only after visible render testing.");return 0;
}
