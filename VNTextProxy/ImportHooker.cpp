#include "pch.h"

using namespace std;

void ImportHooker::Hook(const map<string, void*>& replacementFuncs)
{
    Init();

    for (auto pair : replacementFuncs)
    {
        ReplacementFuncs.insert(pair);
    }

    HMODULE hExe = GetModuleHandle(nullptr);
    DetourEnumerateImportsEx(hExe, (void*)&replacementFuncs, nullptr, PatchGameImport);
}

void ImportHooker::ApplyToModule(HMODULE hModule)
{
    DetourEnumerateImportsEx(hModule, (void*)&ReplacementFuncs, nullptr, PatchGameImport);
}

void ImportHooker::Init()
{
    if (Initialized)
        return;

    Initialized = true;
    Hook(
        {
            { "GetProcAddress", GetProcAddressHook }
        }
    );
}

BOOL ImportHooker::PatchGameImport(void* pContext, DWORD nOrdinal, LPCSTR pszFunc, void** ppvFunc)
{
    if (pszFunc == nullptr || ppvFunc == nullptr)
        return true;

    map<string, void*>* pReplacementFuncs = (map<string, void*>*)pContext;
    auto it = pReplacementFuncs->find(pszFunc);
    if (it != pReplacementFuncs->end())
    {
        MemoryUnprotector unprotect(ppvFunc, 4);
        *ppvFunc = it->second;
    }

    return true;
}

FARPROC ImportHooker::GetProcAddressHook(HMODULE hModule, LPCSTR lpProcName)
{
    auto it = ReplacementFuncs.find(lpProcName);
    if (it != ReplacementFuncs.end())
        return (FARPROC)it->second;

    return GetProcAddress(hModule, lpProcName);
}
