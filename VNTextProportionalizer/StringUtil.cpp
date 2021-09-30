#include "pch.h"

std::wstring StringUtil::ToWString(const char* psz, int numBytes)
{
    int numChars = MultiByteToWideChar(932, 0, psz, numBytes, nullptr, 0);
    std::wstring wideString;
    wideString.resize(numChars);
    MultiByteToWideChar(932, 0, psz, numBytes, const_cast<wchar_t*>(wideString.data()), numChars);

    if (!wideString.empty() && wideString[wideString.size() - 1] == L'\0')
        wideString.resize(wideString.size() - 1);

    return wideString;
}

std::wstring StringUtil::ToHalfWidth(const std::wstring& fullWidth)
{
    std::wstring halfWidth;
    halfWidth.resize(fullWidth.size());
    LCMapStringEx(L"ja-JP", LCMAP_HALFWIDTH, fullWidth.data(), fullWidth.size(), const_cast<wchar_t*>(halfWidth.data()), halfWidth.size(), nullptr, nullptr, 0);
    return halfWidth;
}
