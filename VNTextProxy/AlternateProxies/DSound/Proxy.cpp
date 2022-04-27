#include "pch.h"

void Proxy::Init()
{
    wchar_t realDllPath[MAX_PATH];
    GetSystemDirectory(realDllPath, MAX_PATH);
    wcscat_s(realDllPath, L"\\dsound.dll");
    HMODULE hDll = LoadLibrary(realDllPath);
    if (hDll == nullptr)
    {
        MessageBox(nullptr, L"Cannot load original dsound.dll library", L"Proxy", MB_ICONERROR);
        ExitProcess(0);
    }

#define RESOLVE(fn) Original##fn = GetProcAddress(hDll, #fn)
    RESOLVE(DirectSoundCaptureCreate);
    RESOLVE(DirectSoundCaptureCreate8);
    RESOLVE(DirectSoundCaptureEnumerateA);
    RESOLVE(DirectSoundCaptureEnumerateW);
    RESOLVE(DirectSoundCreate);
    RESOLVE(DirectSoundCreate8);
    RESOLVE(DirectSoundEnumerateA);
    RESOLVE(DirectSoundEnumerateW);
    RESOLVE(DirectSoundFullDuplexCreate);
    RESOLVE(DllCanUnloadNow);
    RESOLVE(DllGetClassObject);
    RESOLVE(GetDeviceID);
#undef RESOLVE
}

__declspec(naked) void FakeDirectSoundCaptureCreate()            { __asm { jmp [Proxy::OriginalDirectSoundCaptureCreate] } }
__declspec(naked) void FakeDirectSoundCaptureCreate8()    { __asm { jmp [Proxy::OriginalDirectSoundCaptureCreate8] } }
__declspec(naked) void FakeDirectSoundCaptureEnumerateA()         { __asm { jmp [Proxy::OriginalDirectSoundCaptureEnumerateA] } }
__declspec(naked) void FakeDirectSoundCaptureEnumerateW()        { __asm { jmp [Proxy::OriginalDirectSoundCaptureEnumerateW] } }
__declspec(naked) void FakeDirectSoundCreate()                 { __asm { jmp [Proxy::OriginalDirectSoundCreate] } }
__declspec(naked) void FakeDirectSoundCreate8()               { __asm { jmp [Proxy::OriginalDirectSoundCreate8] } }
__declspec(naked) void FakeDirectSoundEnumerateA()                     { __asm { jmp [Proxy::OriginalDirectSoundEnumerateA] } }
__declspec(naked) void FakeDirectSoundEnumerateW()               { __asm { jmp [Proxy::OriginalDirectSoundEnumerateW] } }
__declspec(naked) void FakeDirectSoundFullDuplexCreate()        { __asm { jmp [Proxy::OriginalDirectSoundFullDuplexCreate] } }
__declspec(naked) void FakeDllCanUnloadNow()             { __asm { jmp [Proxy::OriginalDllCanUnloadNow] } }
__declspec(naked) void FakeDllGetClassObject()           { __asm { jmp [Proxy::OriginalDllGetClassObject] } }
__declspec(naked) void FakeGetDeviceID()         { __asm { jmp [Proxy::OriginalGetDeviceID] } }
