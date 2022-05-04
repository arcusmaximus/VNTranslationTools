#include "pch.h"

void D2DProportionalizer::Init()
{
    Proportionalizer::Init();
    ImportHooker::Hook(
        {
            { "DWriteCreateFactory", DWriteCreateFactoryHook },
            { "D3D11CreateDevice", D3D11CreateDeviceHook }
        }
    );
}

HRESULT D2DProportionalizer::DWriteCreateFactoryHook(DWRITE_FACTORY_TYPE factoryType, REFIID iid, IUnknown** ppFactory)
{
    static bool patched = false;

    HRESULT result = DWriteCreateFactory(factoryType, iid, ppFactory);
    if (result != S_OK || patched)
        return result;

    void* pFactory = *ppFactory;
    void** pVtable = *(void***)pFactory;
    MemoryUnprotector unprotect(pVtable, 0x60);
    OriginalD2DCreateTextFormat = (decltype(OriginalD2DCreateTextFormat))pVtable[15];
    pVtable[15] = D2DCreateTextFormatHook;

    patched = true;
    return S_OK;
}

HRESULT D2DProportionalizer::D3D11CreateDeviceHook(
    IDXGIAdapter* pAdapter,
    D3D_DRIVER_TYPE driverType,
    HMODULE software,
    UINT flags,
    const D3D_FEATURE_LEVEL* pFeatureLevels,
    UINT numFeatureLevels,
    UINT sdkVersion,
    ID3D11Device** ppDevice,
    D3D_FEATURE_LEVEL* pSelectedFeatureLevel,
    ID3D11DeviceContext** ppImmediateContext)
{
    static bool patched = false;

    HRESULT result = D3D11CreateDevice(pAdapter, driverType, software, flags, pFeatureLevels, numFeatureLevels, sdkVersion, ppDevice, pSelectedFeatureLevel, ppImmediateContext);
    if (result != S_OK || (flags & D3D11_CREATE_DEVICE_BGRA_SUPPORT) == 0 || patched)
        return result;

    ID3D11Device* pD3DDevice = *ppDevice;
    IDXGIDevice1* pDxgiDevice;
    pD3DDevice->QueryInterface(&pDxgiDevice);

    ID2D1Factory1* pD2DFactory;
    D2D1_FACTORY_OPTIONS options;
    options.debugLevel = D2D1_DEBUG_LEVEL_NONE;

#ifdef VNTEXTPROXY_D2D1
    Proxy::OriginalD2D1CreateFactory(D2D1_FACTORY_TYPE_SINGLE_THREADED, IID_ID2D1Factory1, &options, (void**)&pD2DFactory);
#else
    HMODULE hD2D1 = LoadLibrary(L"d2d1.dll");
    auto pD2D1CreateFactory =
        (HRESULT (__stdcall*)(D2D1_FACTORY_TYPE factoryType, REFIID riid, D2D1_FACTORY_OPTIONS* pFactoryOptions, void** ppIFactory))GetProcAddress(hD2D1, "D2D1CreateFactory");
    pD2D1CreateFactory(D2D1_FACTORY_TYPE_SINGLE_THREADED, IID_ID2D1Factory1, &options, (void**)&pD2DFactory);
#endif

    ID2D1Device* pD2DDevice;
    pD2DFactory->CreateDevice(pDxgiDevice, &pD2DDevice);

    ID2D1DeviceContext* pD2DDeviceContext;
    pD2DDevice->CreateDeviceContext(D2D1_DEVICE_CONTEXT_OPTIONS_NONE, &pD2DDeviceContext);

    void** pVtable = *(void***)pD2DDeviceContext;
    MemoryUnprotector unprotect(pVtable, 0x170);
    OriginalD2DDrawText = (decltype(OriginalD2DDrawText))pVtable[27];
    pVtable[27] = D2DDrawTextHook;

    pD2DDeviceContext->Release();
    pD2DDevice->Release();
    pD2DFactory->Release();
    pDxgiDevice->Release();

    patched = true;
    return S_OK;
}

HRESULT D2DProportionalizer::D2DCreateTextFormatHook(
    IDWriteFactory* pFactory,
    const WCHAR* pFontFamilyName,
    IDWriteFontCollection* pFontCollection,
    DWRITE_FONT_WEIGHT fontWeight,
    DWRITE_FONT_STYLE fontStyle,
    DWRITE_FONT_STRETCH fontStretch,
    FLOAT fontSize,
    const WCHAR* pLocaleName,
    IDWriteTextFormat** ppTextFormat)
{
    LastFontName = !CustomFontName.empty() ? CustomFontName.c_str() : pFontFamilyName;
    return OriginalD2DCreateTextFormat(
        pFactory,
        LastFontName.c_str(),
        pFontCollection,
        fontWeight,
        fontStyle,
        fontStretch,
        fontSize,
        pLocaleName,
        ppTextFormat
    );
}

void D2DProportionalizer::D2DDrawTextHook(
    ID2D1DeviceContext* pDeviceContext,
    const WCHAR* pString,
    UINT32 stringLength,
    IDWriteTextFormat* pTextFormat,
    const D2D1_RECT_F* pLayoutRect,
    ID2D1Brush* pDefaultFillBrush,
    D2D1_DRAW_TEXT_OPTIONS options,
    DWRITE_MEASURING_MODE measuringMode)
{
    int fontSize = (int)pTextFormat->GetFontSize();
    int x = (int)pLayoutRect->left;
    int y = (int)pLayoutRect->top;
    float width = pLayoutRect->right - pLayoutRect->left;
    float height = pLayoutRect->bottom - pLayoutRect->top;

    if (!AdaptRenderArgs(pString, stringLength, fontSize, x, y))
        return;

    if (!CustomFontName.empty())
        pTextFormat = FontManager.FetchFont(CustomFontName, fontSize, Bold, Italic, Underline)->GetDWriteTextFormat();

    D2D1_RECT_F rect;
    rect.left = x;
    rect.top = y;
    rect.right = x + width;
    rect.bottom = y + height;

    OriginalD2DDrawText(
        pDeviceContext,
        pString,
        stringLength,
        pTextFormat,
        &rect,
        pDefaultFillBrush,
        options,
        measuringMode
    );
}
