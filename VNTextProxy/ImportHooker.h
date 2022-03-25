#pragma once

class ImportHooker
{
public:
    static void Hook(const std::map<std::string, void*>& replacementFuncs);

private:
    static BOOL __stdcall PatchGameImport(void* pContext, DWORD nOrdinal, LPCSTR pszFunc, void** ppvFunc);
};
