#include "pch.h"

using namespace std;

wstring StringUtil::ToWString(const char* psz, int numBytes, int codepage)
{
    int numChars = MultiByteToWideChar(codepage, 0, psz, numBytes, nullptr, 0);
    wstring wideString;
    wideString.resize(numChars);
    MultiByteToWideChar(codepage, 0, psz, numBytes, wideString.data(), numChars);

    if (!wideString.empty() && wideString[wideString.size() - 1] == L'\0')
        wideString.resize(wideString.size() - 1);

    return wideString;
}

wstring StringUtil::ToHalfWidth(const wstring& fullWidth)
{
    wstring halfWidth;
    halfWidth.resize(fullWidth.size());
    LCMapStringEx(L"ja-JP", LCMAP_HALFWIDTH, fullWidth.data(), fullWidth.size(), halfWidth.data(), halfWidth.size(), nullptr, nullptr, 0);
    return halfWidth;
}
