#include "pch.h"

using namespace std;

FontManager::~FontManager()
{
    for (Font* pFont : _fonts)
    {
        delete pFont;
    }
    _fonts.clear();
}

Font* FontManager::FetchFont(const wstring& faceName, int height, bool bold, bool italic, bool underline)
{
    LOGFONTW info;
    wcscpy_s(info.lfFaceName, faceName.c_str());
    info.lfHeight = height;
    info.lfWeight = bold ? FW_BOLD : FW_NORMAL;
    info.lfItalic = italic;
    info.lfUnderline = underline;

    info.lfWidth = 0;
    info.lfCharSet = DEFAULT_CHARSET;
    info.lfClipPrecision = CLIP_DEFAULT_PRECIS;
    info.lfEscapement = 0;
    info.lfOrientation = 0;
    info.lfOutPrecision = OUT_DEFAULT_PRECIS;
    info.lfPitchAndFamily = DEFAULT_PITCH;
    info.lfQuality = DEFAULT_QUALITY;
    info.lfStrikeOut = false;

    return FetchFont(info);
}

Font* FontManager::FetchFont(const LOGFONTW& fontInfo)
{
    auto it = std::ranges::find_if(_fonts, [&](Font* pFont) { return FontInfosEqual(pFont->GetInfo(), &fontInfo); });
    Font* pFont;
    if (it != _fonts.end())
    {
        pFont = *it;
    }
    else
    {
        pFont = new Font(fontInfo);
        _fonts.push_back(pFont);
    }

    return pFont;
}

Font* FontManager::GetFont(HFONT handle)
{
    auto it = std::ranges::find_if(_fonts, [=](Font* pFont) { return pFont->GetGdiHandle() == handle; });
    return it != _fonts.end() ? *it : nullptr;
}

int FontManager::GetKernAmount(HFONT handle, wchar_t first, wchar_t second)
{
    Font* pFont = GetFont(handle);
    if (pFont == nullptr)
        return 0;

    return pFont->GetKernAmount(first, second);
}

bool FontManager::FontInfosEqual(const LOGFONTW* pInfo1, const LOGFONTW* pInfo2)
{
    return wcscmp(pInfo1->lfFaceName, pInfo2->lfFaceName) == 0 &&
        pInfo1->lfCharSet           == pInfo2->lfCharSet &&
        pInfo1->lfClipPrecision     == pInfo2->lfClipPrecision &&
        pInfo1->lfEscapement        == pInfo2->lfEscapement &&
        pInfo1->lfHeight            == pInfo2->lfHeight &&
        pInfo1->lfItalic            == pInfo2->lfItalic &&
        pInfo1->lfOrientation       == pInfo2->lfOrientation &&
        pInfo1->lfOutPrecision      == pInfo2->lfOutPrecision &&
        pInfo1->lfPitchAndFamily    == pInfo2->lfPitchAndFamily &&
        pInfo1->lfQuality           == pInfo2->lfQuality &&
        pInfo1->lfStrikeOut         == pInfo2->lfStrikeOut &&
        pInfo1->lfUnderline         == pInfo2->lfUnderline &&
        pInfo1->lfWeight            == pInfo2->lfWeight &&
        pInfo1->lfWidth             == pInfo2->lfWidth;
}
