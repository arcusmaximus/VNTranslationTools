#pragma once

class D2DProportionalizer : public Proportionalizer
{
public:
    static void Init();

private:
	static HRESULT __stdcall DWriteCreateFactoryHook(DWRITE_FACTORY_TYPE factoryType, REFIID iid, IUnknown** ppFactory);
	static HRESULT __stdcall D3D11CreateDeviceHook(
		IDXGIAdapter* pAdapter,
		D3D_DRIVER_TYPE driverType,
		HMODULE software,
		UINT flags,
		const D3D_FEATURE_LEVEL* pFeatureLevels,
		UINT numFeatureLevels,
		UINT sdkVersion,
		ID3D11Device** ppDevice,
		D3D_FEATURE_LEVEL* pSelectedFeatureLevel,
		ID3D11DeviceContext** ppImmediateContext
	);

	static HRESULT __stdcall D2DCreateTextFormatHook(
		IDWriteFactory* pFactory,
		const WCHAR* pFontFamilyName,
		IDWriteFontCollection* pFontCollection,
		DWRITE_FONT_WEIGHT fontWeight,
		DWRITE_FONT_STYLE fontStyle,
		DWRITE_FONT_STRETCH fontStretch,
		FLOAT fontSize,
		const WCHAR* pLocaleName,
		IDWriteTextFormat** ppTextFormat
	);
	static void __stdcall D2DDrawTextHook(
		ID2D1DeviceContext* pDeviceContext,
		const WCHAR* pString,
		UINT32 stringLength,
		IDWriteTextFormat* pTextFormat,
		const D2D1_RECT_F* pLayoutRect,
		ID2D1Brush* pDefaultFillBrush,
		D2D1_DRAW_TEXT_OPTIONS options,
		DWRITE_MEASURING_MODE measuringMode
	);

	static inline decltype(D2DCreateTextFormatHook)* OriginalD2DCreateTextFormat{};
	static inline decltype(D2DDrawTextHook)* OriginalD2DDrawText{};
};