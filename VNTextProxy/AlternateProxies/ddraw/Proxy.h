#pragma once

class Proxy
{
public:
    static void Init(HMODULE hProxy);

    static inline HMODULE ProxyModuleHandle{};
    static inline HMODULE OriginalModuleHandle{};

    static inline void* OriginalAcquireDDThreadLock{};
    static inline void* OriginalCompleteCreateSysmemSurface{};
    static inline void* OriginalD3DParseUnknownCommand{};
    static inline void* OriginalDDGetAttachedSurfaceLcl{};
    static inline void* OriginalDDInternalLock{};
    static inline void* OriginalDDInternalUnlock{};
    static inline void* OriginalDSoundHelp{};
    static inline void* OriginalDirectDrawCreate{};
    static inline void* OriginalDirectDrawCreateClipper{};
    static inline void* OriginalDirectDrawCreateEx{};
    static inline void* OriginalDirectDrawEnumerateA{};
    static inline void* OriginalDirectDrawEnumerateExA{};
    static inline void* OriginalDirectDrawEnumerateExW{};
    static inline void* OriginalDirectDrawEnumerateW{};
    static inline void* OriginalDllCanUnloadNow{};
    static inline void* OriginalDllGetClassObject{};
    static inline void* OriginalGetDDSurfaceLocal{};
    static inline void* OriginalGetOLEThunkData{};
    static inline void* OriginalGetSurfaceFromDC{};
    static inline void* OriginalRegisterSpecialCase{};
    static inline void* OriginalReleaseDDThreadLock{};
    static inline void* OriginalSetAppCompatData{};
};
