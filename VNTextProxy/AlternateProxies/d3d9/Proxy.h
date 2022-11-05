#pragma once

class Proxy
{
public:
    static void Init(HMODULE hProxy);

    static inline HMODULE ProxyModuleHandle{};
    static inline HMODULE OriginalModuleHandle{};

    static inline void* OriginalD3DPERF_BeginEvent{};
    static inline void* OriginalD3DPERF_EndEvent{};
    static inline void* OriginalD3DPERF_GetStatus{};
    static inline void* OriginalD3DPERF_QueryRepeatFrame{};
    static inline void* OriginalD3DPERF_SetMarker{};
    static inline void* OriginalD3DPERF_SetOptions{};
    static inline void* OriginalD3DPERF_SetRegion{};
    static inline void* OriginalDebugSetLevel{};
    static inline void* OriginalDebugSetMute{};
    static inline void* OriginalDirect3D9EnableMaximizedWindowedModeShim{};
    static inline void* OriginalDirect3DCreate9{};
    static inline void* OriginalDirect3DCreate9Ex{};
    static inline void* OriginalDirect3DCreate9On12{};
    static inline void* OriginalDirect3DCreate9On12Ex{};
    static inline void* OriginalDirect3DShaderValidatorCreate9{};
    static inline void* OriginalPSGPError{};
    static inline void* OriginalPSGPSampleTexture{};
};
