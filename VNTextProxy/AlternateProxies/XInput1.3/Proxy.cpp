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

__declspec(naked) void FakelDllMain()                                         { __asm { jmp [Proxy::OriginalDllMain] } }
__declspec(naked) void FakeXInputEnable()                                    { __asm { jmp [Proxy::OriginalXInputEnable] } }
__declspec(naked) void FakeXInputGetBatteryInformation()                     { __asm { jmp [Proxy::OriginalXInputGetBatteryInformation] } }
__declspec(naked) void FakeXInputGetCapabilities()                           { __asm { jmp [Proxy::OriginalXInputGetCapabilities] } }
__declspec(naked) void FakeXInputGetDSoundAudioDeviceGuids()                 { __asm { jmp [Proxy::OriginalXInputGetDSoundAudioDeviceGuids] } }
__declspec(naked) void FakeInputGetKeystroke()                              { __asm { jmp [Proxy::OriginalXInputGetKeystroke] } }
__declspec(naked) void FakeXInputGetState()                                  { __asm { jmp [Proxy::OriginalXInputGetState] } }
__declspec(naked) void FakeXInputSetState()                                  { __asm { jmp [Proxy::OriginalXInputSetState] } }