#include "pch.h"

using namespace std;

void ImportHooker::Hook(const map<string, void*>& replacementFuncs)
{
    HMODULE hExe = GetModuleHandle(nullptr);
    DetourEnumerateImportsEx(hExe, (void*)&replacementFuncs, nullptr, PatchGameImport);
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
