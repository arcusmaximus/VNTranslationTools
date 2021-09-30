#pragma once

class Font
{
public:
    Font(const LOGFONTW& info);
    ~Font();

    const LOGFONTW* GetInfo() const;
    std::wstring GetFaceName() const;
    int GetHeight() const;
    bool IsBold() const;
    bool IsItalic() const;
    bool IsUnderline() const;

    HFONT GetGdiHandle() const;
    IDWriteTextFormat* GetDWriteTextFormat();
    int GetKernAmount(wchar_t first, wchar_t second) const;

    int MeasureCharWidth(wchar_t c);
    int MeasureStringWidth(const std::wstring& str);

private:
    HDC _dc;
    LOGFONTW _info;
    HFONT _gdiHandle;
    IDWriteTextFormat* _pDWriteTextFormat;
    std::map<DWORD, int> _kernAmounts;
};
