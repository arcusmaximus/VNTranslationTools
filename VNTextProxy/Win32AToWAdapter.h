#pragma once

class Win32AToWAdapter
{
public:
    static void Init();

private:
    static int __stdcall MultiByteToWideCharHook(UINT codePage, DWORD flags, LPCCH lpMultiByteStr, int cbMultiByte, LPWSTR lpWideCharStr, int cchWideChar);
    static int __stdcall WideCharToMultiByteHook(UINT codePage, DWORD flags, LPCWCH lpWideCharStr, int cchWideChar, LPSTR lpMultiByteStr, int cbMultiByte, LPCCH lpDefaultChar, LPBOOL lpUsedDefaultChar);
    static LRESULT __stdcall DefWindowProcAHook(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);
    static BOOL __stdcall SetWindowTextAHook(HWND hWnd, LPCSTR lpString);
    static BOOL __stdcall AppendMenuAHook(HMENU hMenu, UINT uFlags, UINT_PTR uIDNewItem, LPCSTR lpNewItem);
    static BOOL __stdcall InsertMenuAHook(HMENU hMenu, UINT uPosition, UINT uFlags, UINT_PTR uIDNewItem, LPCSTR lpNewItem);
    static BOOL __stdcall InsertMenuItemAHook(HMENU hmenu, UINT item, BOOL fByPosition, LPCMENUITEMINFOA lpmi);
    static int __stdcall MessageBoxAHook(HWND hWnd, LPCSTR lpText, LPCSTR lpCaption, UINT uType);
};
