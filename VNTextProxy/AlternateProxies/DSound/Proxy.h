#pragma once

class Proxy
{
public:
    static void Init();

    static inline void* OriginalDirectSoundCaptureCreate{};
    static inline void* OriginalDirectSoundCaptureCreate8{};
    static inline void* OriginalDirectSoundCaptureEnumerateA{};
    static inline void* OriginalDirectSoundCaptureEnumerateW{};
    static inline void* OriginalDirectSoundCreate{};
    static inline void* OriginalDirectSoundCreate8{};
    static inline void* OriginalDirectSoundEnumerateA{};
    static inline void* OriginalDirectSoundEnumerateW{};
    static inline void* OriginalDirectSoundFullDuplexCreate{};
    static inline void* OriginalDllCanUnloadNow{};
    static inline void* OriginalDllGetClassObject{};
    static inline void* OriginalGetDeviceID{};
};
