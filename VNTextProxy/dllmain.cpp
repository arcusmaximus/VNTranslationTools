#include "pch.h"

void* OriginalEntryPoint;

void Initialize();

__declspec(naked) void EntryPointHook()
{
    __asm
    {
        call Initialize
        jmp OriginalEntryPoint
    }
}

void Initialize()
{
    if (OriginalEntryPoint != nullptr)
        DetourDetach(&OriginalEntryPoint, &EntryPointHook);

    // Uncomment for games that only work in a Japanese locale
    // (and include LoaderDll.dll and LocaleEmulator.dll from https://github.com/xupefei/Locale-Emulator/releases)
    /*
    if (GetACP() != 932)
    {
        if (LocaleEmulator::Relaunch())
            ExitProcess(0);
    }
    //*/

    CompilerHelper::Init();
    Win32AToWAdapter::Init();
    SjisTunnelEncoding::PatchGameLookupTable();

    GdiProportionalizer::Init();
    D2DProportionalizer::Init();

    EnginePatches::Init();

    SetCurrentDirectoryW(Path::GetModuleFolderPath(nullptr).c_str());
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        Proxy::Init(hModule);

#if _DEBUG
        Initialize();
#else
        OriginalEntryPoint = DetourGetEntryPoint(nullptr);
        DetourTransactionBegin();
        DetourAttach(&OriginalEntryPoint, EntryPointHook);
        DetourTransactionCommit();
#endif
        break;
    	
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}
