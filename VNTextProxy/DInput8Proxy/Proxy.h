#pragma once

class Proxy
{
public:
    static void Init();

    static inline void* OriginalDirectInput8Create{};
    static inline void* OriginalDllCanUnloadNow{};
    static inline void* OriginalDllGetClassObject{};
    static inline void* OriginalDllRegisterServer{};
    static inline void* OriginalDllUnregisterServer{};
    static inline void* OriginalGetdfDIJoystick{};
};
