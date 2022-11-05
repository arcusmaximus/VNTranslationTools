#include "pch.h"

void Proxy::Init(HMODULE hProxy)
{
    ProxyModuleHandle = hProxy;
    
    wchar_t realDllPath[MAX_PATH];
    GetSystemDirectory(realDllPath, MAX_PATH);
    wcscat_s(realDllPath, L"\\ddraw.dll");
    OriginalModuleHandle = LoadLibrary(realDllPath);
    if (OriginalModuleHandle == nullptr)
    {
        MessageBox(nullptr, L"Cannot load original ddraw.dll library", L"Proxy", MB_ICONERROR);
        ExitProcess(0);
    }

#define RESOLVE(fn) Original##fn = GetProcAddress(OriginalModuleHandle, #fn)
    RESOLVE(AcquireDDThreadLock);
    RESOLVE(CompleteCreateSysmemSurface);
    RESOLVE(D3DParseUnknownCommand);
    RESOLVE(DDGetAttachedSurfaceLcl);
    RESOLVE(DDInternalLock);
    RESOLVE(DDInternalUnlock);
    RESOLVE(DSoundHelp);
    RESOLVE(DirectDrawCreate);
    RESOLVE(DirectDrawCreateClipper);
    RESOLVE(DirectDrawCreateEx);
    RESOLVE(DirectDrawEnumerateA);
    RESOLVE(DirectDrawEnumerateExA);
    RESOLVE(DirectDrawEnumerateExW);
    RESOLVE(DirectDrawEnumerateW);
    RESOLVE(DllCanUnloadNow);
    RESOLVE(DllGetClassObject);
    RESOLVE(GetDDSurfaceLocal);
    RESOLVE(GetOLEThunkData);
    RESOLVE(GetSurfaceFromDC);
    RESOLVE(RegisterSpecialCase);
    RESOLVE(ReleaseDDThreadLock);
    RESOLVE(SetAppCompatData);
#undef RESOLVE
}

__declspec(naked) void FakeAcquireDDThreadLock()            { __asm { jmp [Proxy::OriginalAcquireDDThreadLock] } }
__declspec(naked) void FakeCompleteCreateSysmemSurface()    { __asm { jmp [Proxy::OriginalCompleteCreateSysmemSurface] } }
__declspec(naked) void FakeD3DParseUnknownCommand()         { __asm { jmp [Proxy::OriginalD3DParseUnknownCommand] } }
__declspec(naked) void FakeDDGetAttachedSurfaceLcl()        { __asm { jmp [Proxy::OriginalDDGetAttachedSurfaceLcl] } }
__declspec(naked) void FakeDDInternalLock()                 { __asm { jmp [Proxy::OriginalDDInternalLock] } }
__declspec(naked) void FakeDDInternalUnlock()               { __asm { jmp [Proxy::OriginalDDInternalUnlock] } }
__declspec(naked) void FakeDSoundHelp()                     { __asm { jmp [Proxy::OriginalDSoundHelp] } }
__declspec(naked) void FakeDirectDrawCreate()               { __asm { jmp [Proxy::OriginalDirectDrawCreate] } }
__declspec(naked) void FakeDirectDrawCreateClipper()        { __asm { jmp [Proxy::OriginalDirectDrawCreateClipper] } }
__declspec(naked) void FakeDirectDrawCreateEx()             { __asm { jmp [Proxy::OriginalDirectDrawCreateEx] } }
__declspec(naked) void FakeDirectDrawEnumerateA()           { __asm { jmp [Proxy::OriginalDirectDrawEnumerateA] } }
__declspec(naked) void FakeDirectDrawEnumerateExA()         { __asm { jmp [Proxy::OriginalDirectDrawEnumerateExA] } }
__declspec(naked) void FakeDirectDrawEnumerateExW()         { __asm { jmp [Proxy::OriginalDirectDrawEnumerateExW] } }
__declspec(naked) void FakeDirectDrawEnumerateW()           { __asm { jmp [Proxy::OriginalDirectDrawEnumerateW] } }
__declspec(naked) void FakeDllCanUnloadNow()                { __asm { jmp [Proxy::OriginalDllCanUnloadNow] } }
__declspec(naked) void FakeDllGetClassObject()              { __asm { jmp [Proxy::OriginalDllGetClassObject] } }
__declspec(naked) void FakeGetDDSurfaceLocal()              { __asm { jmp [Proxy::OriginalGetDDSurfaceLocal] } }
__declspec(naked) void FakeGetOLEThunkData()                { __asm { jmp [Proxy::OriginalGetOLEThunkData] } }
__declspec(naked) void FakeGetSurfaceFromDC()               { __asm { jmp [Proxy::OriginalGetSurfaceFromDC] } }
__declspec(naked) void FakeRegisterSpecialCase()            { __asm { jmp [Proxy::OriginalRegisterSpecialCase] } }
__declspec(naked) void FakeReleaseDDThreadLock()            { __asm { jmp [Proxy::OriginalReleaseDDThreadLock] } }
__declspec(naked) void FakeSetAppCompatData()               { __asm { jmp [Proxy::OriginalSetAppCompatData] } }