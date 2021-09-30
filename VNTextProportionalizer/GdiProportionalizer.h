#pragma once

class GdiProportionalizer : public Proportionalizer
{
public:
    static void Init();

private:
	static HFONT __stdcall CreateFontAHook(int cHeight, int cWidth, int cEscapement, int cOrientation, int cWeight, DWORD bItalic,
		DWORD bUnderline, DWORD bStrikeOut, DWORD iCharSet, DWORD iOutPrecision, DWORD iClipPrecision,
		DWORD iQuality, DWORD iPitchAndFamily, LPCSTR pszFaceName);
	static HGDIOBJ __stdcall SelectObjectHook(HDC hdc, HGDIOBJ obj);
	static BOOL __stdcall DeleteObjectHook(HGDIOBJ obj);
	static BOOL __stdcall TextOutAHook(HDC dc, int x, int y, LPCSTR pString, int count);

	static inline std::map<HDC, Font*> CurrentFonts{};
};
