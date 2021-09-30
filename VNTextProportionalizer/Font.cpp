#include "pch.h"

using namespace std;

Font::Font(const LOGFONTW& info)
{
    _dc = CreateCompatibleDC(GetDC(nullptr));
    _info = info;
    _gdiHandle = CreateFontIndirectW(&info);
    _pDWriteTextFormat = nullptr;

    SelectObject(_dc, _gdiHandle);

    DWORD numKernings = GetKerningPairsW(_dc, 0, nullptr);
    vector<KERNINGPAIR> kernings(numKernings);
    GetKerningPairsW(_dc, numKernings, kernings.data());

    for (auto& kerning : kernings)
    {
        DWORD kerningKey = kerning.wFirst | (kerning.wSecond << 16);
        _kernAmounts[kerningKey] = kerning.iKernAmount;
    }
}

Font::~Font()
{
    if (_pDWriteTextFormat != nullptr)
    {
        _pDWriteTextFormat->Release();
        _pDWriteTextFormat = nullptr;
    }

    DeleteObject(_gdiHandle);
    _gdiHandle = nullptr;

    DeleteDC(_dc);
    _dc = nullptr;
}

const LOGFONTW* Font::GetInfo() const
{
    return &_info;
}

wstring Font::GetFaceName() const
{
    return _info.lfFaceName;
}

int Font::GetHeight() const
{
    return _info.lfHeight;
}

bool Font::IsBold() const
{
    return _info.lfWeight >= FW_BOLD;
}

bool Font::IsItalic() const
{
    return _info.lfItalic;
}

bool Font::IsUnderline() const
{
    return _info.lfUnderline;
}

HFONT Font::GetGdiHandle() const
{
    return _gdiHandle;
}

IDWriteTextFormat* Font::GetDWriteTextFormat()
{
    if (_pDWriteTextFormat == nullptr)
    {
        IDWriteFactory* pDWriteFactory;
        DWriteCreateFactory(DWRITE_FACTORY_TYPE_ISOLATED, __uuidof(IDWriteFactory), (IUnknown**)&pDWriteFactory);

        pDWriteFactory->CreateTextFormat(
            _info.lfFaceName,
            nullptr,
            (DWRITE_FONT_WEIGHT)_info.lfWeight,
            _info.lfItalic ? DWRITE_FONT_STYLE_ITALIC : DWRITE_FONT_STYLE_NORMAL,
            DWRITE_FONT_STRETCH_NORMAL,
            _info.lfHeight,
            L"",
            &_pDWriteTextFormat
        );

        pDWriteFactory->Release();
    }
    return _pDWriteTextFormat;
}

int Font::GetKernAmount(wchar_t first, wchar_t second) const
{
    DWORD kerningKey = first | (second << 16);
    auto kerningIt = _kernAmounts.find(kerningKey);
    return kerningIt != _kernAmounts.end() ? kerningIt->second : 0;
}

int Font::MeasureCharWidth(wchar_t c)
{
    ABCFLOAT abc;
    GetCharABCWidthsFloatW(_dc, c, c, &abc);
    return static_cast<int>(abc.abcfA + abc.abcfB + abc.abcfC);
}

int Font::MeasureStringWidth(const wstring& str)
{
    int width = 0;
    for (wchar_t c : str)
    {
        if (c == L'\0')
            return width;

        width += MeasureCharWidth(c);
    }
    return width;
}

