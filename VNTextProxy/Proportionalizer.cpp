#include "pch.h"

#define PROPORTIONALIZER_CHARACTER_SPACING 5

using namespace std;

void Proportionalizer::Init()
{
    FontName = LoadCustomFont();
    PatchGameImports(
        {
            { "MultiByteToWideChar", MultiByteToWideCharHook },
            { "MessageBoxA", MessageBoxAHook },
            { "SetWindowTextA", SetWindowTextAHook },
            { "CreateWindowExA", CreateWindowExAHook }
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
    case L'龠':
        Bold = !Bold;
        return true;

    case L'籥':
        Italic = !Italic;
        return true;

    case L'鑰':
        Underline = !Underline;
        return true;
    }

    return false;
}

wstring Proportionalizer::LoadCustomFont()
{
    wchar_t folderPath[MAX_PATH];
    GetModuleFileName(GetModuleHandle(nullptr), folderPath, sizeof(folderPath) / sizeof(wchar_t));
    wchar_t* pLastSlash = wcsrchr(folderPath, L'\\');
    if (pLastSlash != nullptr)
        *pLastSlash = L'\0';

    WIN32_FIND_DATA findData;
    wstring searchPath = wstring(folderPath) + L"\\*.ttf";
    HANDLE hFind = FindFirstFile(searchPath.c_str(), &findData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        searchPath = wstring(folderPath) + L"\\*.otf";
        hFind = FindFirstFile(searchPath.c_str(), &findData);
        if (hFind == INVALID_HANDLE_VALUE)
            return L"";
    }
    FindClose(hFind);

    wstring fontFilePath = wstring(folderPath) + L"\\" + findData.cFileName;
    AddFontResourceExW(fontFilePath.c_str(), FR_PRIVATE, nullptr);

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
    //wstr = StringUtil::ToHalfWidth(wstr);
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

int Proportionalizer::MessageBoxAHook(HWND hWnd, LPCSTR lpText, LPCSTR lpCaption, UINT uType)
{
    wstring caption = SjisTunnelDecoder::Decode(lpCaption);
    wstring text = SjisTunnelDecoder::Decode(lpText);
    return MessageBoxW(hWnd, text.c_str(), caption.c_str(), uType);
}

BOOL Proportionalizer::SetWindowTextAHook(HWND hWnd, LPCSTR lpString)
{
    wstring text = SjisTunnelDecoder::Decode(lpString);
    //text = StringUtil::ToHalfWidth(text);
    return SetWindowTextW(hWnd, text.c_str());
}

HWND Proportionalizer::CreateWindowExAHook(DWORD dwExStyle, LPCSTR lpClassName, LPCSTR lpWindowName, DWORD dwStyle, int X, int Y, int nWidth, int nHeight, HWND hWndParent, HMENU hMenu, HINSTANCE hInstance, LPVOID lpParam)
{
    wstring className = StringUtil::ToWString(lpClassName);
    wstring windowName = SjisTunnelDecoder::Decode(lpWindowName);
    //windowName = StringUtil::ToHalfWidth(windowName);
    return CreateWindowExW(dwExStyle, className.c_str(), windowName.c_str(), dwStyle, X, Y, nWidth, nHeight, hWndParent, hMenu, hInstance, lpParam);
}
