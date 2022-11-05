#include "pch.h"

void Proxy::Init(HMODULE hProxy)
{
    ProxyModuleHandle = hProxy;
    
    wchar_t realDllPath[MAX_PATH];
    GetSystemDirectory(realDllPath, MAX_PATH);
    wcscat_s(realDllPath, L"\\dinput8.dll");
    OriginalModuleHandle = LoadLibrary(realDllPath);
    if (OriginalModuleHandle == nullptr)
    {
        MessageBox(nullptr, L"Cannot load original dinput8.dll library", L"Proxy", MB_ICONERROR);
        ExitProcess(0);
    }

#define RESOLVE(fn) Original##fn = GetProcAddress(OriginalModuleHandle, #fn)
    RESOLVE(DirectInput8Create);
    RESOLVE(DllCanUnloadNow);
    RESOLVE(DllGetClassObject);
    RESOLVE(DllRegisterServer);
    RESOLVE(DllUnregisterServer);
    RESOLVE(GetdfDIJoystick);
#undef RESOLVE
}

__declspec(naked) void FakeDirectInput8Create()     { __asm { jmp [Proxy::OriginalDirectInput8Create] } }
__declspec(naked) void FakeDllCanUnloadNow()        { __asm { jmp [Proxy::OriginalDllCanUnloadNow] } }
__declspec(naked) void FakeDllGetClassObject()      { __asm { jmp [Proxy::OriginalDllGetClassObject] } }
__declspec(naked) void FakeDllRegisterServer()      { __asm { jmp [Proxy::OriginalDllRegisterServer] } }
__declspec(naked) void FakeDllUnregisterServer()    { __asm { jmp [Proxy::OriginalDllUnregisterServer] } }
__declspec(naked) void FakeGetdfDIJoystick()        { __asm { jmp [Proxy::OriginalGetdfDIJoystick] } }
