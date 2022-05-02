#pragma once

class Proxy
{
public:
    static void Init();

    static inline void* OriginalDllMain{};
    static inline void* OriginalXInputEnable{};
    static inline void* OriginalXInputGetBatteryInformation{};
    static inline void* OriginalXInputGetCapabilities{};
    static inline void* OriginalXInputGetDSoundAudioDeviceGuids{};
    static inline void* OriginalXInputGetKeystroke{};
    static inline void* OriginalXInputGetState{};
    static inline void* OriginalXInputSetState{};
};
