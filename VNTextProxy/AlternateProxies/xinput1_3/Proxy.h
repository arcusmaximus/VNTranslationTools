#pragma once

class Proxy
{
public:
    static void Init(HMODULE hProxy);

    static inline HMODULE ProxyModuleHandle{};
    static inline HMODULE OriginalModuleHandle{};

    static inline void* OriginalDllMain{};
    static inline void* OriginalXInputEnable{};
    static inline void* OriginalXInputGetBatteryInformation{};
    static inline void* OriginalXInputGetCapabilities{};
    static inline void* OriginalXInputGetDSoundAudioDeviceGuids{};
    static inline void* OriginalXInputGetKeystroke{};
    static inline void* OriginalXInputGetState{};
    static inline void* OriginalXInputSetState{};
};
