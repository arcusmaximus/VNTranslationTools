#include "pch.h"

void Proxy::Init()
{
    wchar_t realDllPath[MAX_PATH];
    GetSystemDirectory(realDllPath, MAX_PATH);
    wcscat_s(realDllPath, L"\\xinput1_3.dll");
    HMODULE hDll = LoadLibrary(realDllPath);
    if (hDll == nullptr)
    {
        MessageBox(nullptr, L"Cannot load original xinput1_3.dll library", L"Proxy", MB_ICONERROR);
        ExitProcess(0);
    }

#define RESOLVE(fn) Original##fn = GetProcAddress(hDll, #fn)
    RESOLVE(DllMain);
    RESOLVE(XInputEnable);
    RESOLVE(XInputGetBatteryInformation);
    RESOLVE(XInputGetCapabilities);
    RESOLVE(XInputGetDSoundAudioDeviceGuids);
    RESOLVE(XInputGetKeystroke);
    RESOLVE(XInputGetState);
    RESOLVE(XInputSetState);
#undef RESOLVE
}

__declspec(naked) void OriginalDllMain()                                         { __asm { jmp [Proxy::FakeDllMain] } }
__declspec(naked) void OriginalXInputEnable()                                    { __asm { jmp [Proxy::FakeXInputEnable] } }
__declspec(naked) void OriginalXInputGetBatteryInformation()                     { __asm { jmp [Proxy::FakeXInputGetBatteryInformation] } }
__declspec(naked) void OriginalXInputGetCapabilities()                           { __asm { jmp [Proxy::FakeXInputGetCapabilities] } }
__declspec(naked) void OriginalXInputGetDSoundAudioDeviceGuids()                 { __asm { jmp [Proxy::FakeXInputGetDSoundAudioDeviceGuids] } }
__declspec(naked) void OriginalXInputGetKeystroke()                              { __asm { jmp [Proxy::FakeXInputGetKeystroke] } }
__declspec(naked) void OriginalXInputGetState()                                  { __asm { jmp [Proxy::FakeXInputGetState] } }
__declspec(naked) void OriginalXInputSetState()                                  { __asm { jmp [Proxy::FakeXInputSetState] } }