#pragma once

class Proportionalizer
{
public:
    static void Init();
    static int MeasureStringWidth(const std::wstring& str, int fontSize);
    
    static inline std::wstring CustomFontName{};
    static inline std::wstring CustomFontFilePath{};
    static inline std::wstring LastFontName{};
    static inline int LastLineEnd;

protected:
    static bool AdaptRenderArgs(const wchar_t* pText, int length, int fontSize, int& x, int& y);
    
    static inline FontManager FontManager{};
    static inline bool Bold{};
    static inline bool Italic{};
    static inline bool Underline{};

private:
    typedef BOOL (__stdcall GetFontResourceInfoW_t)(const wchar_t* lpszFilename, LPDWORD cbBuffer, LPVOID lpBuffer, DWORD dwQueryType);
    static std::wstring LoadCustomFont();
    static std::wstring FindCustomFontFile();
    static bool HandleFormattingCode(wchar_t c);
};
