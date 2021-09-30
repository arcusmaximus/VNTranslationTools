#pragma once

class FontManager
{
public:
    ~FontManager();

    Font* FetchFont(const std::wstring& faceName, int height, bool bold, bool italic, bool underline);
    Font* FetchFont(const LOGFONTW& fontInfo);
    Font* GetFont(HFONT handle);

    int GetKernAmount(HFONT handle, wchar_t first, wchar_t second);

private:
    static bool FontInfosEqual(const LOGFONTW* pInfo1, const LOGFONTW* pInfo2);

    std::vector<Font*> _fonts;
};
