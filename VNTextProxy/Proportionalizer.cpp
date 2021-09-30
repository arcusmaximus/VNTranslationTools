#include "pch.h"

#define PROPORTIONALIZER_CHARACTER_SPACING 5

using namespace std;

void Proportionalizer::Init()
{
    FontName = LoadCustomFont();
    PatchGameImports(
        {
            { "MultiByteToWideChar", MultiByteToWideCharHook }
        }
    );
}

int Proportionalizer::MeasureStringWidth(const wstring& str, int fontSize)
{
    Font* pFont = FontManager.FetchFont(FontName, fontSize, false, false, false);
    return pFont->MeasureStringWidth(str);
}

bool Proportionalizer::AdaptRenderArgs(const wchar_t* pText, int length, int fontSize, int& x, int& y)
{
    static int startX = 0;
    static int lastMonospaceX = 0;
    static int lastMonospaceY = 0;
    static int lastProportionalX = 0;
    static wchar_t lastChar = L'\0';
    static int nextProportionalX = 0;

    if (length != 1)
        return true;

    wchar_t currentChar = pText[0];
    if (HandleFormattingCode(currentChar))
        return false;

    Font* pFont = FontManager.FetchFont(FontName, fontSize, Bold, Italic, Underline);

    if (x == 0 || x < lastMonospaceX - 4 || abs(y - lastMonospaceY) > 4)
    {
        // To the left of previously rendered text or different Y -> reset
        startX = x;
        lastMonospaceX = x;
        lastMonospaceY = y;
        lastProportionalX = x;

        lastChar = currentChar;
        nextProportionalX = x + pFont->MeasureCharWidth(currentChar) + PROPORTIONALIZER_CHARACTER_SPACING;
    }
    else if (x <= lastMonospaceX + 4)
    {
        // Close to previously rendered text (e.g. shadow) -> calculate offset
        int offset = x - lastMonospaceX;
        x = lastProportionalX + offset;
    }
    else
    {
        // Far to the right of previously rendered text -> next char
        lastMonospaceX = x;

        x = nextProportionalX + pFont->GetKernAmount(lastChar, currentChar);
        lastProportionalX = x;
        lastChar = currentChar;
        nextProportionalX = x + pFont->MeasureCharWidth(currentChar) + PROPORTIONALIZER_CHARACTER_SPACING;
    }

    LastLineEnd = nextProportionalX;
    return true;
}

bool Proportionalizer::HandleFormattingCode(wchar_t c)
{
    switch (c)
    {
    case L'êž':
        Bold = !Bold;
        return true;

    case L'âÞ':
        Italic = !Italic;
        return true;

    case L'èo':
        Underline = !Underline;
        return true;
    }

    return false;
}

wstring Proportionalizer::LoadCustomFont()
{
    WIN32_FIND_DATA findData;
    HANDLE hFind = FindFirstFile(L"*.ttf", &findData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        hFind = FindFirstFile(L"*.otf", &findData);
        if (hFind == INVALID_HANDLE_VALUE)
            return L"";
    }
    FindClose(hFind);

    AddFontResourceExW(findData.cFileName, FR_PRIVATE, nullptr);
    wchar_t* pDot = wcsrchr(findData.cFileName, L'.');
    *pDot = L'\0';
    return findData.cFileName;
}

void Proportionalizer::PatchGameImports(const map<string, void*>& replacementFuncs)
{
    HMODULE hExe = GetModuleHandle(nullptr);
    DetourEnumerateImportsEx(hExe, (void*)&replacementFuncs, nullptr, PatchGameImport);
}

BOOL Proportionalizer::PatchGameImport(void* pContext, DWORD nOrdinal, LPCSTR pszFunc, void** ppvFunc)
{
    if (pszFunc == nullptr || ppvFunc == nullptr)
        return true;

    map<string, void*>* pReplacementFuncs = (map<string, void*>*)pContext;
    auto it = pReplacementFuncs->find(pszFunc);
    if (it != pReplacementFuncs->end())
    {
        MemoryUnprotector unprotect(ppvFunc, 4);
        *ppvFunc = it->second;
    }

    return true;
}

int Proportionalizer::MultiByteToWideCharHook(UINT codePage, DWORD flags, LPCCH lpMultiByteStr, int cbMultiByte, LPWSTR lpWideCharStr, int cchWideChar)
{
    wstring wstr = SjisTunnelDecoder::Decode(lpMultiByteStr, cbMultiByte);
    wstr = StringUtil::ToHalfWidth(wstr);
    int numWchars = wstr.size();
    if (cbMultiByte < 0)
        numWchars++;

    if (cchWideChar > 0)
    {
        if (numWchars > cchWideChar)
            return 0;

        memcpy(lpWideCharStr, wstr.c_str(), numWchars * sizeof(wchar_t));
    }
    return numWchars;
}
