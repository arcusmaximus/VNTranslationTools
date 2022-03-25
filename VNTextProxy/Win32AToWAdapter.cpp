#include "pch.h"

using namespace std;

void Win32AToWAdapter::Init()
{
    ImportHooker::Hook(
        {
            { "MultiByteToWideChar", MultiByteToWideCharHook },
            { "WideCharToMultiByte", WideCharToMultiByteHook },
            { "DefWindowProcA", DefWindowProcAHook },
            { "SetWindowTextA", SetWindowTextAHook },
            { "AppendMenuA", AppendMenuAHook },
            { "InsertMenuA", InsertMenuAHook },
            { "InsertMenuItemA", InsertMenuItemAHook },
            { "MessageBoxA", MessageBoxAHook }
        }
    );
}

int Win32AToWAdapter::MultiByteToWideCharHook(UINT codePage, DWORD flags, LPCCH lpMultiByteStr, int cbMultiByte, LPWSTR lpWideCharStr, int cchWideChar)
{
    if (codePage != CP_ACP && codePage != 932)
        return MultiByteToWideChar(codePage, flags, lpMultiByteStr, cbMultiByte, lpWideCharStr, cchWideChar);

    wstring wstr = SjisTunnelEncoding::Decode(lpMultiByteStr, cbMultiByte);
    int numWchars = wstr.size();
    if (cbMultiByte < 0)
        numWchars++;

    if (cchWideChar > 0)
    {
        memcpy(lpWideCharStr, wstr.c_str(), min(numWchars, cchWideChar) * sizeof(wchar_t));
        if (cchWideChar < numWchars)
        {
            SetLastError(ERROR_INSUFFICIENT_BUFFER);
            return 0;
        }
    }

    return numWchars;
}

int Win32AToWAdapter::WideCharToMultiByteHook(UINT codePage, DWORD flags, LPCWCH lpWideCharStr, int cchWideChar, LPSTR lpMultiByteStr, int cbMultiByte, LPCCH lpDefaultChar, LPBOOL lpUsedDefaultChar)
{
    if (codePage != CP_ACP && codePage != 932)
        return WideCharToMultiByte(codePage, flags, lpWideCharStr, cchWideChar, lpMultiByteStr, cbMultiByte, lpDefaultChar, lpUsedDefaultChar);

    if (lpUsedDefaultChar != nullptr)
        *lpUsedDefaultChar = false;

    string str = SjisTunnelEncoding::Encode(lpWideCharStr, cchWideChar);
    int numChars = str.size();
    if (cchWideChar < 0)
        numChars++;

    if (cbMultiByte > 0)
    {
        memcpy(lpMultiByteStr, str.c_str(), min(numChars, cbMultiByte));
        if (cbMultiByte < numChars)
        {
            SetLastError(ERROR_INSUFFICIENT_BUFFER);
            return 0;
        }
    }

    return numChars;
}

LRESULT Win32AToWAdapter::DefWindowProcAHook(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    switch (msg)
    {
        case WM_NCCREATE:
        {
            CREATESTRUCTA* pCreateA = (CREATESTRUCTA*)lParam;
            CREATESTRUCTW createW;
            memcpy(&createW, pCreateA, sizeof(createW));

            wstring name = SjisTunnelEncoding::Decode(pCreateA->lpszName);
            createW.lpszName = name.c_str();

            wstring className = StringUtil::ToWString(pCreateA->lpszClass);
            createW.lpszClass = className.c_str();

            return DefWindowProcW(hWnd, msg, wParam, (LPARAM)&createW);
        }

        case WM_GETTEXT:
        {
            wstring wtext;
            wtext.resize(wParam);
            int wsize = DefWindowProcW(hWnd, msg, wParam, (LPARAM)wtext.data());
            wtext.resize(wsize);

            string text = SjisTunnelEncoding::Encode(wtext.c_str());
            int size = min(text.size(), wParam - 1);
            memcpy((char*)lParam, text.data(), size);
            ((char*)lParam)[size] = '\0';
            return size;
        }

        case WM_SETTEXT:
        {
            wstring wtext = SjisTunnelEncoding::Decode((const char*)lParam);
            return DefWindowProcW(hWnd, msg, wParam, (LPARAM)wtext.c_str());
        }

        default:
        {
            return DefWindowProcA(hWnd, msg, wParam, lParam);
        }
    }
}

BOOL Win32AToWAdapter::SetWindowTextAHook(HWND hWnd, LPCSTR lpString)
{
    return SetWindowTextW(hWnd, SjisTunnelEncoding::Decode(lpString).c_str());
}

BOOL Win32AToWAdapter::AppendMenuAHook(HMENU hMenu, UINT uFlags, UINT_PTR uIDNewItem, LPCSTR lpNewItem)
{
    return AppendMenuW(hMenu, uFlags, uIDNewItem, SjisTunnelEncoding::Decode(lpNewItem).c_str());
}

BOOL Win32AToWAdapter::InsertMenuAHook(HMENU hMenu, UINT uPosition, UINT uFlags, UINT_PTR uIDNewItem, LPCSTR lpNewItem)
{
    return InsertMenuW(hMenu, uPosition, uFlags, uIDNewItem, SjisTunnelEncoding::Decode(lpNewItem).c_str());
}

BOOL Win32AToWAdapter::InsertMenuItemAHook(HMENU hmenu, UINT item, BOOL fByPosition, LPCMENUITEMINFOA lpmi)
{
    MENUITEMINFOW menuItemW;
    memcpy(&menuItemW, lpmi, sizeof(menuItemW));

    wstring text;
    if (((lpmi->fMask & MIIM_TYPE) && lpmi->fType == MFT_STRING) ||
        (lpmi->fMask & MIIM_STRING))
    {
        text = SjisTunnelEncoding::Decode(lpmi->dwTypeData);
        menuItemW.dwTypeData = const_cast<wchar_t*>(text.c_str());
    }

    return InsertMenuItemW(hmenu, item, fByPosition, &menuItemW);
}

int Win32AToWAdapter::MessageBoxAHook(HWND hWnd, LPCSTR lpText, LPCSTR lpCaption, UINT uType)
{
    return MessageBoxW(hWnd, SjisTunnelEncoding::Decode(lpText).c_str(), SjisTunnelEncoding::Decode(lpCaption).c_str(), uType);
}
