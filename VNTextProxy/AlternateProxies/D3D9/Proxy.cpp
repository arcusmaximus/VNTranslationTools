#include "pch.h"

void Proxy::Init()
{
    wchar_t realDllPath[MAX_PATH];
    GetSystemDirectory(realDllPath, MAX_PATH);
    wcscat_s(realDllPath, L"\\d3d9.dll");
    HMODULE hDll = LoadLibrary(realDllPath);
    if (hDll == nullptr)
    {
        MessageBox(nullptr, L"Cannot load original d3d9.dll library", L"Proxy", MB_ICONERROR);
        ExitProcess(0);
    }

#define RESOLVE(fn) Original##fn = reinterpret_cast<decltype(Original##fn)>(GetProcAddress(hDll, #fn))
    RESOLVE(Direct3DCreate9);
#undef RESOLVE
}

__declspec(naked) void FakeDirect3DCreate9() { __asm { jmp [Proxy::OriginalDirect3DCreate9] } }
