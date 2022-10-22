#pragma once

class ImportHooker
{
public:
    static void Hook(const std::map<std::string, void*>& replacementFuncs);
    static void ApplyToModule(HMODULE hModule);

private:
    static void Init();
    static BOOL __stdcall PatchGameImport(void* pContext, DWORD nOrdinal, LPCSTR pszFunc, void** ppvFunc);

    static FARPROC __stdcall GetProcAddressHook(HMODULE hModule, LPCSTR lpProcName);

    static inline bool Initialized{};
    static inline std::map<std::string, void*> ReplacementFuncs{};
};
