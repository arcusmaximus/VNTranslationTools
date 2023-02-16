#pragma once

class GdiProportionalizer : public Proportionalizer
{
public:
    static void Init();

private:
    static int __stdcall EnumFontsAHook(HDC hdc, LPCSTR lpLogfont, FONTENUMPROCA lpProc, LPARAM lParam);
    static int __stdcall EnumFontFamiliesExAHook(HDC hdc, LPLOGFONTA lpLogfont, FONTENUMPROCA lpProc, LPARAM lParam, DWORD dwFlags);
    static int __stdcall EnumFontsProc(const LOGFONTW* lplf, const TEXTMETRICW* lptm, DWORD dwType, LPARAM lpData);

    static HFONT __stdcall CreateFontAHook(int cHeight, int cWidth, int cEscapement, int cOrientation, int cWeight, DWORD bItalic,
        DWORD bUnderline, DWORD bStrikeOut, DWORD iCharSet, DWORD iOutPrecision, DWORD iClipPrecision,
        DWORD iQuality, DWORD iPitchAndFamily, LPCSTR pszFaceName);
    static HFONT __stdcall CreateFontIndirectAHook(LOGFONTA* pFontInfo);
    static HFONT __stdcall CreateFontWHook(int cHeight, int cWidth, int cEscapement, int cOrientation, int cWeight, DWORD bItalic,
        DWORD bUnderline, DWORD bStrikeOut, DWORD iCharSet, DWORD iOutPrecision, DWORD iClipPrecision,
        DWORD iQuality, DWORD iPitchAndFamily, LPCWSTR pszFaceName);
    static HFONT __stdcall CreateFontIndirectWHook(LOGFONTW* pFontInfo);
    static HGDIOBJ __stdcall SelectObjectHook(HDC hdc, HGDIOBJ obj);
    static BOOL __stdcall DeleteObjectHook(HGDIOBJ obj);
    static BOOL __stdcall GetTextExtentPointAHook(HDC hdc, LPCSTR lpString, int c, LPSIZE lpsz);
    static BOOL __stdcall GetTextExtentPoint32AHook(HDC hdc, LPCSTR lpString, int c, LPSIZE psizl);
    static BOOL __stdcall TextOutAHook(HDC dc, int x, int y, LPCSTR pString, int count);
    static DWORD __stdcall GetGlyphOutlineAHook(HDC hdc, UINT uChar, UINT fuFormat, LPGLYPHMETRICS lpgm, DWORD cjBuffer, LPVOID pvBuffer, MAT2* lpmat2);

    static inline std::map<HDC, Font*> CurrentFonts{};

    static LOGFONTA ConvertLogFontWToA(const LOGFONTW& logFontW);
    static LOGFONTW ConvertLogFontAToW(const LOGFONTA& logFontA);

    static TEXTMETRICA ConvertTextMetricWToA(const TEXTMETRICW& textMetricW);

    struct EnumFontsContext
    {
        FONTENUMPROCA OriginalProc;
        LPARAM OriginalContext;
        bool Extended;
    };
};
