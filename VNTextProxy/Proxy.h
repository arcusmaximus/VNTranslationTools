#pragma once

#define VNTEXTPROXY_D2D1

class Proxy
{
public:
    static void Init(HMODULE hProxy);

    static inline HMODULE ProxyModuleHandle{};
    static inline HMODULE OriginalModuleHandle{};
    
    static inline decltype(D2D1ComputeMaximumScaleFactor)*  OriginalD2D1ComputeMaximumScaleFactor{};
    static inline decltype(D2D1ConvertColorSpace)*          OriginalD2D1ConvertColorSpace{};
    
    typedef HRESULT (__stdcall D2D1CreateDevice_t)(IDXGIDevice* dxgiDevice, D2D1_CREATION_PROPERTIES* creationProperties, ID2D1Device** d2dDevice);
    static inline D2D1CreateDevice_t*                       OriginalD2D1CreateDevice{};
    
    typedef HRESULT (__stdcall D2D1CreateDeviceContext_t)(IDXGISurface* dxgiSurface, D2D1_CREATION_PROPERTIES* creationProperties, ID2D1DeviceContext** d2dDeviceContext);
    static inline D2D1CreateDeviceContext_t*                OriginalD2D1CreateDeviceContext{};

    typedef HRESULT (__stdcall D2D1CreateFactory_t)(D2D1_FACTORY_TYPE factoryType, REFIID riid, const D2D1_FACTORY_OPTIONS* pFactoryOptions, void** ppIFactory);
    static inline D2D1CreateFactory_t*                      OriginalD2D1CreateFactory{};

    static inline decltype(D2D1GetGradientMeshInteriorPointsFromCoonsPatch)*    OriginalD2D1GetGradientMeshInteriorPointsFromCoonsPatch{};
    static inline decltype(D2D1InvertMatrix)*               OriginalD2D1InvertMatrix{};
    static inline decltype(D2D1IsMatrixInvertible)*         OriginalD2D1IsMatrixInvertible{};
    static inline decltype(D2D1MakeRotateMatrix)*           OriginalD2D1MakeRotateMatrix{};
    static inline decltype(D2D1MakeSkewMatrix)*             OriginalD2D1MakeSkewMatrix{};
    static inline decltype(D2D1SinCos)*                     OriginalD2D1SinCos{};
    static inline decltype(D2D1Tan)*                        OriginalD2D1Tan{};
    static inline decltype(D2D1Vec3Length)*                 OriginalD2D1Vec3Length{};
};
