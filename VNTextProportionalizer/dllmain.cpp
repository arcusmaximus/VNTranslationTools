#include "pch.h"

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        Proxy::Init();

        // Uncomment for games that only work in a Japanese locale
        // (and include LoaderDll.dll and LocaleEmulator.dll from https://github.com/xupefei/Locale-Emulator/releases)
        /*
        if (GetACP() != 932)
        {
            LocaleEmulator::Relaunch();
            ExitProcess(0);
        }
        //*/

        // Uncomment one of these depending on what the game uses
        GdiProportionalizer::Init();
        //D2DProportionalizer::Init();

        EnginePatches::Init();
        break;
    	
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}
