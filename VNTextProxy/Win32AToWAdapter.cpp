#include "pch.h"

using namespace std;

void Win32AToWAdapter::Init()
{
    ImeListener::Init();
    ImeListener::OnCompositionEnded = HandleImeCompositionEnded;

    ImportHooker::Hook(
        {
            { "GetACP", GetACPHook },
            { "IsDBCSLeadByte", IsDBCSLeadByteHook },
            { "MultiByteToWideChar", MultiByteToWideCharHook },
            { "WideCharToMultiByte", WideCharToMultiByteHook },

            { "CreateEventA", CreateEventAHook },
            { "OpenEventA", OpenEventAHook },
            { "CreateMutexA", CreateMutexAHook },
            { "OpenMutexA", OpenMutexAHook },

            { "GetModuleFileNameA", GetModuleFileNameAHook },
            { "LoadLibraryA", LoadLibraryAHook },
            { "LoadLibraryExA", LoadLibraryExAHook },

            { "GetFullPathNameA", GetFullPathNameAHook },
            { "FindFirstFileA", FindFirstFileAHook },
            { "FindNextFileA", FindNextFileAHook },
            { "SearchPathA", SearchPathAHook },
            { "GetFileAttributesA", GetFileAttributesAHook },
            { "CreateFileA", CreateFileAHook },
            { "DeleteFileA", DeleteFileAHook },
            { "CreateDirectoryA", CreateDirectoryAHook },
            { "RemoveDirectoryA", RemoveDirectoryA },
            { "GetCurrentDirectoryA", GetCurrentDirectoryAHook },
            { "GetTempPathA", GetTempPathAHook },
            { "GetTempFileNameA", GetTempFileNameAHook },

            { "RegCreateKeyExA", RegCreateKeyExA },
            { "RegOpenKeyExA", RegOpenKeyExA },
            { "RegQueryValueExA", RegQueryValueExAHook },
            { "RegSetValueExA", RegSetValueExAHook },

            { "SetWindowLongA", SetWindowLongAHook },
            { "DestroyWindow", DestroyWindowHook },
            { "PeekMessageA", PeekMessageAHook },
            { "GetMessageA", GetMessageAHook },
            { "DispatchMessageA", DispatchMessageAHook },
            { "DefWindowProcA", DefWindowProcAHook },
            { "AppendMenuA", AppendMenuAHook },
            { "InsertMenuA", InsertMenuAHook },
            { "InsertMenuItemA", InsertMenuItemAHook },
            { "MessageBoxA", MessageBoxAHook },

            { "GetMonitorInfoA", GetMonitorInfoAHook },
            { "EnumDisplayDevicesA", EnumDisplayDevicesAHook },
            { "EnumDisplaySettingsA", EnumDisplaySettingsAHook },
            { "ChangeDisplaySettingsA", ChangeDisplaySettingsAHook },
            { "ChangeDisplaySettingsExA", ChangeDisplaySettingsExAHook },

            { "DirectDrawEnumerateA", DirectDrawEnumerateAHook },
            { "DirectDrawEnumerateExA", DirectDrawEnumerateExAHook },

            { "DirectSoundEnumerateA", DirectSoundEnumerateAHook }
        }
    );
}

UINT Win32AToWAdapter::GetACPHook()
{
    return 932;
}

BOOL Win32AToWAdapter::IsDBCSLeadByteHook(BYTE TestChar)
{
    return (TestChar >= 0x81 && TestChar < 0xA0) || (TestChar >= 0xE0 && TestChar < 0xFD);
}

int Win32AToWAdapter::MultiByteToWideCharHook(UINT codePage, DWORD flags, LPCCH lpMultiByteStr, int cbMultiByte, LPWSTR lpWideCharStr, int cchWideChar)
{
    if (codePage != CP_ACP && codePage != CP_THREAD_ACP && codePage != 932)
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

HANDLE Win32AToWAdapter::CreateEventAHook(LPSECURITY_ATTRIBUTES lpEventAttributes, BOOL bManualReset, BOOL bInitialState, LPCSTR lpName)
{
    return CreateEventW(lpEventAttributes, bManualReset, bInitialState, lpName != nullptr ? SjisTunnelEncoding::Decode(lpName).c_str() : nullptr);
}

HANDLE Win32AToWAdapter::OpenEventAHook(DWORD dwDesiredAccess, BOOL bInheritHandle, LPCSTR lpName)
{
    return OpenEventW(dwDesiredAccess, bInheritHandle, SjisTunnelEncoding::Decode(lpName).c_str());
}

HANDLE Win32AToWAdapter::CreateMutexAHook(LPSECURITY_ATTRIBUTES lpMutexAttributes, BOOL bInitialOwner, LPCSTR lpName)
{
    return CreateMutexW(lpMutexAttributes, bInitialOwner, lpName != nullptr ? SjisTunnelEncoding::Decode(lpName).c_str() : nullptr);
}

HANDLE Win32AToWAdapter::OpenMutexAHook(DWORD dwDesiredAccess, BOOL bInheritHandle, LPCSTR lpName)
{
    return OpenMutexW(dwDesiredAccess, bInheritHandle, SjisTunnelEncoding::Decode(lpName).c_str());
}

DWORD Win32AToWAdapter::GetModuleFileNameAHook(HMODULE hModule, LPSTR lpFilename, DWORD nSize)
{
    wstring fileNameW;
    fileNameW.resize(nSize);
    DWORD result = GetModuleFileNameW(hModule, fileNameW.data(), fileNameW.size());
    if (result == 0)
        return 0;

    string fileNameA = SjisTunnelEncoding::Encode(fileNameW);
    if (fileNameA.size() >= nSize)
    {
        memcpy(lpFilename, fileNameA.c_str(), nSize - 1);
        lpFilename[nSize - 1] = '\0';
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        return nSize;
    }

    memcpy(lpFilename, fileNameA.c_str(), fileNameA.size() + 1);
    return fileNameA.size();
}

HMODULE Win32AToWAdapter::LoadLibraryAHook(LPCSTR lpLibFileName)
{
    wstring libName = SjisTunnelEncoding::Decode(lpLibFileName);
    HMODULE hModule = GetModuleHandleW(libName.c_str());
    if (hModule != nullptr)
        return hModule;

    hModule = LoadLibraryW(libName.c_str());
    if (hModule)
        ImportHooker::ApplyToModule(hModule);

    return hModule;
}

HMODULE Win32AToWAdapter::LoadLibraryExAHook(LPCSTR lpLibFileName, HANDLE hFile, DWORD dwFlags)
{
    HMODULE hModule = LoadLibraryExW(SjisTunnelEncoding::Decode(lpLibFileName).c_str(), hFile, dwFlags);
    if (hModule)
        ImportHooker::ApplyToModule(hModule);

    return hModule;
}

DWORD Win32AToWAdapter::GetFullPathNameAHook(LPCSTR lpFileName, DWORD nBufferLength, LPSTR lpBuffer, LPSTR* lpFilePart)
{
    wstring bufferW;
    bufferW.resize(nBufferLength);
    DWORD result = GetFullPathNameW(SjisTunnelEncoding::Decode(lpFileName).c_str(), bufferW.size(), bufferW.data(), nullptr);
    if (result == 0)
        return 0;

    if (result > bufferW.size())
        return result * 2;

    string bufferA = SjisTunnelEncoding::Encode(bufferW);
    if (bufferA.size() + 1 > nBufferLength)
    {
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        return bufferA.size() + 1;
    }

    memcpy(lpBuffer, bufferA.c_str(), bufferA.size() + 1);
    if (lpFilePart != nullptr)
    {
        *lpFilePart = strrchr(lpBuffer, '\\');
        if (*lpFilePart != nullptr)
            (*lpFilePart)++;
    }

    return bufferA.size();
}

HANDLE Win32AToWAdapter::FindFirstFileAHook(LPCSTR lpFileName, LPWIN32_FIND_DATAA lpFindFileData)
{
    WIN32_FIND_DATAW findDataW;
    HANDLE hFind = FindFirstFileW(SjisTunnelEncoding::Decode(lpFileName).c_str(), &findDataW);
    if (hFind == INVALID_HANDLE_VALUE)
        return INVALID_HANDLE_VALUE;

    *lpFindFileData = ConvertFindDataWToA(findDataW);
    return hFind;
}

BOOL Win32AToWAdapter::FindNextFileAHook(HANDLE hFindFile, LPWIN32_FIND_DATAA lpFindFileData)
{
    WIN32_FIND_DATAW findDataW;
    BOOL found = FindNextFileW(hFindFile, &findDataW);
    if (!found)
        return false;

    *lpFindFileData = ConvertFindDataWToA(findDataW);
    return true;
}

DWORD Win32AToWAdapter::SearchPathAHook(LPCSTR lpPath, LPCSTR lpFileName, LPCSTR lpExtension, DWORD nBufferLength, LPSTR lpBuffer, LPSTR* lpFilePart)
{
    wstring bufferW;
    bufferW.resize(nBufferLength);
    DWORD result = SearchPathW(
        lpPath != nullptr ? SjisTunnelEncoding::Decode(lpPath).c_str() : nullptr,
        SjisTunnelEncoding::Decode(lpFileName).c_str(),
        lpExtension != nullptr ? SjisTunnelEncoding::Decode(lpExtension).c_str() : nullptr,
        bufferW.size(),
        bufferW.data(),
        nullptr
    );
    if (result == 0)
        return 0;

    if (result > bufferW.size())
        return result * 2;

    string bufferA = SjisTunnelEncoding::Encode(bufferW);
    if (bufferA.size() + 1 > nBufferLength)
    {
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        return bufferA.size() + 1;
    }

    memcpy(lpBuffer, bufferA.c_str(), bufferA.size() + 1);
    if (lpFilePart != nullptr)
    {
        *lpFilePart = strrchr(lpBuffer, '\\');
        if (*lpFilePart != nullptr)
            (*lpFilePart)++;
    }

    return bufferA.size();
}

DWORD Win32AToWAdapter::GetFileAttributesAHook(LPCSTR lpFileName)
{
    return GetFileAttributesW(SjisTunnelEncoding::Decode(lpFileName).c_str());
}

HANDLE Win32AToWAdapter::CreateFileAHook(LPCSTR lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode, LPSECURITY_ATTRIBUTES lpSecurityAttributes, DWORD dwCreationDisposition, DWORD dwFlagsAndAttributes, HANDLE hTemplateFile)
{
    return CreateFileW(SjisTunnelEncoding::Decode(lpFileName).c_str(), dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
}

HANDLE Win32AToWAdapter::CreateFileMappingAHook(HANDLE hFile, LPSECURITY_ATTRIBUTES lpFileMappingAttributes, DWORD flProtect, DWORD dwMaximumSizeHigh, DWORD dwMaximumSizeLow, LPCSTR lpName)
{
    return CreateFileMappingW(hFile, lpFileMappingAttributes, flProtect, dwMaximumSizeHigh, dwMaximumSizeLow,
        lpName != nullptr ? SjisTunnelEncoding::Decode(lpName).c_str() : nullptr);
}

BOOL Win32AToWAdapter::DeleteFileAHook(LPCSTR lpFileName)
{
    return DeleteFileW(SjisTunnelEncoding::Decode(lpFileName).c_str());
}

BOOL Win32AToWAdapter::CreateDirectoryAHook(LPCSTR lpPathName, LPSECURITY_ATTRIBUTES lpSecurityAttributes)
{
    return CreateDirectoryW(SjisTunnelEncoding::Decode(lpPathName).c_str(), lpSecurityAttributes);
}

BOOL Win32AToWAdapter::RemoveDirectoryAHook(LPCSTR lpPathName)
{
    return RemoveDirectoryW(SjisTunnelEncoding::Decode(lpPathName).c_str());
}

DWORD Win32AToWAdapter::GetCurrentDirectoryAHook(DWORD nBufferLength, LPSTR lpBuffer)
{
    wstring currentDirW;
    currentDirW.resize(nBufferLength);
    DWORD result = GetCurrentDirectoryW(currentDirW.size(), currentDirW.data());
    if (result == 0)
        return 0;

    if (result > currentDirW.size())
        return result * 2;

    string currentDirA = SjisTunnelEncoding::Encode(currentDirW);
    if (currentDirA.size() + 1 > nBufferLength)
    {
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        return currentDirA.size() + 1;
    }

    memcpy(lpBuffer, currentDirA.c_str(), currentDirA.size() + 1);
    return currentDirA.size();
}

DWORD Win32AToWAdapter::GetTempPathAHook(DWORD nBufferLength, LPSTR lpBuffer)
{
    wstring tempPathW;
    tempPathW.resize(nBufferLength);
    DWORD result = GetTempPathW(tempPathW.size(), tempPathW.data());
    if (result == 0)
        return 0;

    if (result > tempPathW.size())
        return result * 2;

    string tempPathA = SjisTunnelEncoding::Encode(tempPathW);
    if (tempPathA.size() + 1 > nBufferLength)
    {
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        return tempPathA.size() + 1;
    }

    memcpy(lpBuffer, tempPathA.c_str(), tempPathA.size() + 1);
    return tempPathA.size();
}

UINT Win32AToWAdapter::GetTempFileNameAHook(LPCSTR lpPathName, LPCSTR lpPrefixString, UINT uUnique, LPSTR lpTempFileName)
{
    wchar_t wszTempFileName[MAX_PATH];
    UINT result = GetTempFileNameW(
        SjisTunnelEncoding::Decode(lpPathName).c_str(),
        SjisTunnelEncoding::Decode(lpPrefixString).c_str(),
        uUnique,
        wszTempFileName
    );
    if (result == 0)
        return 0;

    string strTempFileName = SjisTunnelEncoding::Encode(wszTempFileName);
    strncpy_s(lpTempFileName, MAX_PATH, strTempFileName.c_str(), strTempFileName.size());
    return result;
}

LSTATUS Win32AToWAdapter::RegCreateKeyExAHook(HKEY hKey, LPCSTR lpSubKey, DWORD Reserved, LPSTR lpClass, DWORD dwOptions, REGSAM samDesired, const LPSECURITY_ATTRIBUTES lpSecurityAttributes, PHKEY phkResult, LPDWORD lpdwDisposition)
{
    return RegCreateKeyExW(
        hKey,
        SjisTunnelEncoding::Decode(lpSubKey).c_str(),
        0,
        lpClass != nullptr ? const_cast<wchar_t*>(SjisTunnelEncoding::Decode(lpClass).c_str()) : nullptr,
        dwOptions,
        samDesired,
        lpSecurityAttributes,
        phkResult,
        lpdwDisposition
    );
}

LSTATUS Win32AToWAdapter::RegOpenKeyExAHook(HKEY hKey, LPCSTR lpSubKey, DWORD ulOptions, REGSAM samDesired, PHKEY phkResult)
{
    return RegOpenKeyExW(hKey, SjisTunnelEncoding::Decode(lpSubKey).c_str(), ulOptions, samDesired, phkResult);
}

LSTATUS Win32AToWAdapter::RegQueryValueExAHook(HKEY hKey, LPCSTR lpValueName, LPDWORD lpReserved, LPDWORD lpType, LPBYTE lpData, LPDWORD lpcbData)
{
    DWORD type;
    DWORD sizeW;
    LSTATUS status = RegQueryValueExW(hKey, lpValueName != nullptr ? SjisTunnelEncoding::Decode(lpValueName).c_str() : nullptr, nullptr, &type, nullptr, &sizeW);
    if (status != ERROR_SUCCESS)
        return status;

    if (type != REG_SZ &&
        type != REG_EXPAND_SZ &&
        type != REG_MULTI_SZ)
    {
        return RegQueryValueExW(hKey, lpValueName != nullptr ? SjisTunnelEncoding::Decode(lpValueName).c_str() : nullptr, nullptr, lpType, lpData, lpcbData);
    }

    wstring dataW;
    dataW.resize(sizeW / sizeof(wchar_t));
    status = RegQueryValueExW(hKey, lpValueName != nullptr ? SjisTunnelEncoding::Decode(lpValueName).c_str() : nullptr, nullptr, lpType, (BYTE*)dataW.data(), &sizeW);
    if (status != ERROR_SUCCESS)
        return status;

    string dataA = SjisTunnelEncoding::Encode(dataW.data(), dataW.size());
    if (lpData == nullptr)
    {
        if (lpcbData != nullptr)
            *lpcbData = dataA.size();

        return ERROR_SUCCESS;
    }

    if (lpcbData == nullptr || *lpcbData < dataA.size())
        return ERROR_MORE_DATA;

    memcpy(lpData, dataA.data(), dataA.size());
    *lpcbData = dataA.size();
    return ERROR_SUCCESS;
}

LSTATUS Win32AToWAdapter::RegSetValueExAHook(HKEY hKey, LPCSTR lpValueName, DWORD Reserved, DWORD dwType, const BYTE* lpData, DWORD cbData)
{
    if (lpData == nullptr ||
        dwType != REG_SZ &&
        dwType != REG_EXPAND_SZ &&
        dwType != REG_MULTI_SZ)
    {
        return RegSetValueExW(hKey, lpValueName != nullptr ? SjisTunnelEncoding::Decode(lpValueName).c_str() : nullptr, 0, dwType, lpData, cbData);
    }

    wstring dataW = SjisTunnelEncoding::Decode((const char*)lpData, cbData);
    return RegSetValueExW(hKey, lpValueName != nullptr ? SjisTunnelEncoding::Decode(lpValueName).c_str() : nullptr, 0, dwType, (BYTE*)dataW.data(), dataW.size() * sizeof(wchar_t));
}

LONG Win32AToWAdapter::SetWindowLongAHook(HWND hWnd, int nIndex, LONG dwNewLong)
{
    // Manually keep track of window procedures as we can't rely on GetWindowLong() to give us the real one
    // (may return a fake value for use with CallWindowProc)
    if (nIndex == GWL_WNDPROC)
        WindowProcs[hWnd] = (WNDPROC)dwNewLong;

    return SetWindowLongA(hWnd, nIndex, dwNewLong);
}

BOOL Win32AToWAdapter::DestroyWindowHook(HWND hWnd)
{
    WindowProcs.erase(hWnd);
    return DestroyWindow(hWnd);
}

void Win32AToWAdapter::HandleImeCompositionEnded(const std::wstring& text)
{
    PendingImeCompositionChars = text;
}

BOOL Win32AToWAdapter::PeekMessageAHook(LPMSG lpMsg, HWND hWnd, UINT wMsgFilterMin, UINT wMsgFilterMax, UINT wRemoveMsg)
{
    BOOL messageAvailable = PeekMessageW(lpMsg, hWnd, wMsgFilterMin, wMsgFilterMax, PM_NOREMOVE | (wRemoveMsg & PM_NOYIELD));
    if (messageAvailable && lpMsg->message == WM_CHAR)
    {
        // For non-IME text input, we receive Unicode WM_CHARs - nice and easy. But IME input, for whatever reason,
        // comes in as '?' for characters outside the codepage, despite us calling PeekMessageW().
        // So, if the user completed an IME composition earlier, we should ignore the resulting WM_CHARs
        // and use the text we got from TSF instead (see ImeListener.cpp).
        wchar_t c;
        if (!PendingImeCompositionChars.empty())
        {
            c = PendingImeCompositionChars[0];
            PendingImeCompositionChars.erase(0, 1);
        }
        else
        {
            c = (wchar_t)lpMsg->wParam;
        }

        string str = SjisTunnelEncoding::Encode(&c, 1);
        for (char c : str)
        {
            lpMsg->wParam = (BYTE)c;
            PendingWindowMessages.push_back(*lpMsg);
        }
        PeekMessageW(lpMsg, hWnd, wMsgFilterMin, wMsgFilterMax, PM_REMOVE | (wRemoveMsg & PM_NOYIELD));
    }

    if (!PendingWindowMessages.empty())
    {
        *lpMsg = PendingWindowMessages[0];
        if (wRemoveMsg & PM_REMOVE)
            PendingWindowMessages.erase(PendingWindowMessages.begin());

        return true;
    }

    return PeekMessageA(lpMsg, hWnd, wMsgFilterMin, wMsgFilterMax, wRemoveMsg);
}

BOOL Win32AToWAdapter::GetMessageAHook(LPMSG lpMsg, HWND hWnd, UINT wMsgFilterMin, UINT wMsgFilterMax)
{
    if (!PendingWindowMessages.empty())
    {
        *lpMsg = PendingWindowMessages[0];
        PendingWindowMessages.erase(PendingWindowMessages.begin());
        return true;
    }

    return GetMessageA(lpMsg, hWnd, wMsgFilterMin, wMsgFilterMax);
}

LRESULT Win32AToWAdapter::DispatchMessageAHook(const MSG* lpMsg)
{
    if (lpMsg->message == WM_CHAR)
    {
        // Bypass all the "helpful" extra code that sits between calling DispatchMessageA() and the invocation of the window procedure
        // so that our tunneled SJIS codepoints are preserved and not turned into question marks
        auto it = WindowProcs.find(lpMsg->hwnd);
        WNDPROC pWndProc = it != WindowProcs.end() ? it->second : (WNDPROC)GetClassLongPtrA(lpMsg->hwnd, GCLP_WNDPROC);
        return pWndProc(lpMsg->hwnd, lpMsg->message, lpMsg->wParam, lpMsg->lParam);
    }

    return DispatchMessageA(lpMsg);
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

            if ((DWORD)pCreateA->lpszClass & 0xFFFF0000)
            {
                wstring className = StringUtil::ToWString(pCreateA->lpszClass);
                createW.lpszClass = className.c_str();
            }

            return DefWindowProcW(hWnd, msg, wParam, (LPARAM)&createW);
        }

        case WM_GETTEXT:
        {
            wstring wtext;
            wtext.resize(wParam);
            int wsize = DefWindowProcW(hWnd, msg, wParam, (LPARAM)wtext.data());
            wtext.resize(wsize);

            string text = SjisTunnelEncoding::Encode(wtext);
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

BOOL Win32AToWAdapter::GetMonitorInfoAHook(HMONITOR hMonitor, LPMONITORINFO lpmi)
{
    if (lpmi == nullptr)
        return false;

    if (lpmi->cbSize == sizeof(MONITORINFO))
        return GetMonitorInfoA(hMonitor, lpmi);

    if (lpmi->cbSize != sizeof(MONITORINFOEXA))
        return false;

    MONITORINFOEXW infoW;
    infoW.cbSize = sizeof(infoW);
    if (!GetMonitorInfoW(hMonitor, &infoW))
        return false;

    lpmi->dwFlags = infoW.dwFlags;
    lpmi->rcMonitor = infoW.rcMonitor;
    lpmi->rcWork = infoW.rcWork;
    strcpy_s(((LPMONITORINFOEXA)lpmi)->szDevice, SjisTunnelEncoding::Encode(infoW.szDevice).c_str());
    return true;
}

BOOL Win32AToWAdapter::EnumDisplayDevicesAHook(LPCSTR lpDevice, DWORD iDevNum, PDISPLAY_DEVICEA lpDisplayDevice, DWORD dwFlags)
{
    if (lpDisplayDevice == nullptr || lpDisplayDevice->cb != sizeof(DISPLAY_DEVICEA))
        return false;

    DISPLAY_DEVICEW deviceW;
    deviceW.cb = sizeof(deviceW);
    if (!EnumDisplayDevicesW(lpDevice != nullptr ? SjisTunnelEncoding::Decode(lpDevice).c_str() : nullptr, iDevNum, &deviceW, dwFlags))
        return false;

    strcpy_s(lpDisplayDevice->DeviceID, SjisTunnelEncoding::Encode(deviceW.DeviceID).c_str());
    strcpy_s(lpDisplayDevice->DeviceKey, SjisTunnelEncoding::Encode(deviceW.DeviceKey).c_str());
    strcpy_s(lpDisplayDevice->DeviceName, SjisTunnelEncoding::Encode(deviceW.DeviceName).c_str());
    strcpy_s(lpDisplayDevice->DeviceString, SjisTunnelEncoding::Encode(deviceW.DeviceString).c_str());
    lpDisplayDevice->StateFlags = deviceW.StateFlags;
    return true;
}

BOOL Win32AToWAdapter::EnumDisplaySettingsAHook(LPCSTR lpszDeviceName, DWORD iModeNum, DEVMODEA* lpDevMode)
{
    if (lpDevMode == nullptr)
        return false;

    DEVMODEW devModeW;
    devModeW.dmSize = sizeof(DEVMODEW);
    devModeW.dmDriverExtra = 0;

    if (!EnumDisplaySettingsW(lpszDeviceName != nullptr ? SjisTunnelEncoding::Decode(lpszDeviceName).c_str() : nullptr, iModeNum, &devModeW))
        return false;

    *lpDevMode = ConvertDevModeWToA(devModeW);
    return true;
}

LONG Win32AToWAdapter::ChangeDisplaySettingsAHook(DEVMODEA* lpDevMode, DWORD dwFlags)
{
    if (lpDevMode != nullptr)
    {
        DEVMODEW devModeW = ConvertDevModeAToW(*lpDevMode);
        return ChangeDisplaySettingsW(&devModeW, dwFlags);
    }
    else
    {
        return ChangeDisplaySettingsW(nullptr, dwFlags);
    }
}

LONG Win32AToWAdapter::ChangeDisplaySettingsExAHook(LPCSTR lpszDeviceName, DEVMODEA* lpDevMode, HWND hwnd, DWORD dwflags, LPVOID lParam)
{
    if (lpDevMode != nullptr)
    {
        DEVMODEW devModeW = ConvertDevModeAToW(*lpDevMode);
        return ChangeDisplaySettingsExW(
            lpszDeviceName != nullptr ? SjisTunnelEncoding::Decode(lpszDeviceName).c_str() : nullptr,
            &devModeW,
            hwnd,
            dwflags,
            lParam
        );
    }
    else
    {
        return ChangeDisplaySettingsExW(
            lpszDeviceName != nullptr ? SjisTunnelEncoding::Decode(lpszDeviceName).c_str() : nullptr,
            nullptr,
            hwnd,
            dwflags,
            lParam
        );
    }
}

HRESULT Win32AToWAdapter::DirectDrawEnumerateAHook(LPDDENUMCALLBACKA lpCallback, LPVOID lpContext)
{
    DirectDrawEnumerateContext context;
    context.OriginalCallback = lpCallback;
    context.OriginalContext = lpContext;
    return DirectDrawEnumerateA(DirectDrawEnumerateCallback, &context);     // DirectDrawEnumerateW exists but doesn't actually work
}

BOOL Win32AToWAdapter::DirectDrawEnumerateCallback(GUID* pGuid, LPSTR pszDriverName, LPSTR pszDriverDescription, LPVOID pContext)
{
    DirectDrawEnumerateContext* pOrigContext = (DirectDrawEnumerateContext*)pContext;
    return pOrigContext->OriginalCallback(
        pGuid,
        pszDriverName != nullptr ? const_cast<char*>(SjisTunnelEncoding::Encode(StringUtil::ToWString(pszDriverName, -1, CP_ACP)).c_str()) : nullptr,
        pszDriverDescription != nullptr ? const_cast<char*>(SjisTunnelEncoding::Encode(StringUtil::ToWString(pszDriverDescription, -1, CP_ACP)).c_str()) : nullptr,
        pOrigContext->OriginalContext
    );
}

HRESULT Win32AToWAdapter::DirectDrawEnumerateExAHook(LPDDENUMCALLBACKEXA lpCallback, LPVOID lpContext, DWORD dwFlags)
{
    DirectDrawEnumerateExContext context;
    context.OriginalCallback = lpCallback;
    context.OriginalContext = lpContext;
    return DirectDrawEnumerateExA(DirectDrawEnumerateExCallback, &context, dwFlags);        // DirectDrawEnumerateExW exists but doesn't actually work
}

BOOL Win32AToWAdapter::DirectDrawEnumerateExCallback(GUID* pGuid, LPSTR pszDriverName, LPSTR pszDriverDescription, LPVOID pContext, HMONITOR hMonitor)
{
    DirectDrawEnumerateExContext* pOrigContext = (DirectDrawEnumerateExContext*)pContext;
    return pOrigContext->OriginalCallback(
        pGuid,
        pszDriverName != nullptr ? const_cast<char*>(SjisTunnelEncoding::Encode(StringUtil::ToWString(pszDriverName, -1, CP_ACP)).c_str()) : nullptr,
        pszDriverDescription != nullptr ? const_cast<char*>(SjisTunnelEncoding::Encode(StringUtil::ToWString(pszDriverDescription, -1, CP_ACP)).c_str()) : nullptr,
        pOrigContext->OriginalContext,
        hMonitor
    );
}

HRESULT Win32AToWAdapter::DirectSoundEnumerateAHook(LPDSENUMCALLBACKA pDSEnumCallback, LPVOID pContext)
{
    DirectSoundEnumerateContext origContext;
    origContext.OriginalCallback = pDSEnumCallback;
    origContext.OriginalContext = pContext;
    return DirectSoundEnumerateW(&DirectSoundEnumerateCallback, &origContext);
}

BOOL Win32AToWAdapter::DirectSoundEnumerateCallback(LPGUID lpGuid, LPCWSTR lpcstrDescription, LPCWSTR lpcstrModule, LPVOID lpContext)
{
    DirectSoundEnumerateContext* pOrigContext = (DirectSoundEnumerateContext*)lpContext;
    return pOrigContext->OriginalCallback(
        lpGuid,
        SjisTunnelEncoding::Encode(lpcstrDescription).c_str(),
        SjisTunnelEncoding::Encode(lpcstrModule).c_str(),
        pOrigContext->OriginalContext
    );
}

WIN32_FIND_DATAA Win32AToWAdapter::ConvertFindDataWToA(const WIN32_FIND_DATAW& findDataW)
{
    WIN32_FIND_DATAA findDataA;
    strcpy_s(findDataA.cAlternateFileName, SjisTunnelEncoding::Encode(findDataW.cAlternateFileName).c_str());
    strcpy_s(findDataA.cFileName, SjisTunnelEncoding::Encode(findDataW.cFileName).c_str());
    findDataA.dwFileAttributes = findDataW.dwFileAttributes;
    findDataA.dwReserved0 = findDataW.dwReserved0;
    findDataA.dwReserved1 = findDataW.dwReserved1;
    findDataA.ftCreationTime = findDataW.ftCreationTime;
    findDataA.ftLastAccessTime = findDataW.ftLastAccessTime;
    findDataA.ftLastWriteTime = findDataW.ftLastWriteTime;
    findDataA.nFileSizeHigh = findDataW.nFileSizeHigh;
    findDataA.nFileSizeLow = findDataW.nFileSizeLow;
    return findDataA;
}

DEVMODEA Win32AToWAdapter::ConvertDevModeWToA(const DEVMODEW& devModeW)
{
    DEVMODEA devModeA;

    strcpy_s((char*)devModeA.dmDeviceName, sizeof(devModeA.dmDeviceName), SjisTunnelEncoding::Encode(devModeW.dmDeviceName).c_str());
    devModeA.dmSpecVersion = devModeW.dmSpecVersion;
    devModeA.dmDriverVersion = devModeW.dmDriverVersion;
    devModeA.dmSize = sizeof(DEVMODEA);
    devModeA.dmDriverExtra = devModeW.dmDriverExtra;
    devModeA.dmFields = devModeW.dmFields;
    devModeA.dmPosition = devModeW.dmPosition;
    devModeA.dmDisplayOrientation = devModeW.dmDisplayOrientation;
    devModeA.dmColor = devModeW.dmColor;
    devModeA.dmDuplex = devModeW.dmDuplex;
    devModeA.dmYResolution = devModeW.dmYResolution;
    devModeA.dmTTOption = devModeW.dmTTOption;
    devModeA.dmCollate = devModeW.dmCollate;
    strcpy_s((char*)devModeA.dmFormName, sizeof(devModeA.dmFormName), SjisTunnelEncoding::Encode(devModeW.dmFormName).c_str());
    devModeA.dmLogPixels = devModeW.dmLogPixels;
    devModeA.dmBitsPerPel = devModeW.dmBitsPerPel;
    devModeA.dmPelsWidth = devModeW.dmPelsWidth;
    devModeA.dmPelsHeight = devModeW.dmPelsHeight;
    devModeA.dmDisplayFlags = devModeW.dmDisplayFlags;
    devModeA.dmDisplayFrequency = devModeW.dmDisplayFrequency;
    devModeA.dmICMMethod = devModeW.dmICMMethod;
    devModeA.dmICMIntent = devModeW.dmICMIntent;
    devModeA.dmMediaType = devModeW.dmMediaType;
    devModeA.dmDitherType = devModeW.dmDitherType;
    devModeA.dmReserved1 = devModeW.dmReserved1;
    devModeA.dmReserved2 = devModeW.dmReserved2;
    devModeA.dmPanningWidth = devModeW.dmPanningWidth;
    devModeA.dmPanningHeight = devModeW.dmPanningHeight;

    return devModeA;
}

DEVMODEW Win32AToWAdapter::ConvertDevModeAToW(const DEVMODEA& devModeA)
{
    DEVMODEW devModeW;

    wcscpy_s(devModeW.dmDeviceName, SjisTunnelEncoding::Decode((char*)devModeA.dmDeviceName).c_str());
    devModeW.dmSpecVersion = devModeA.dmSpecVersion;
    devModeW.dmDriverVersion = devModeA.dmDriverVersion;
    devModeW.dmSize = sizeof(DEVMODEW);
    devModeW.dmDriverExtra = devModeA.dmDriverExtra;
    devModeW.dmFields = devModeA.dmFields;
    devModeW.dmPosition = devModeA.dmPosition;
    devModeW.dmDisplayOrientation = devModeA.dmDisplayOrientation;
    devModeW.dmColor = devModeA.dmColor;
    devModeW.dmDuplex = devModeA.dmDuplex;
    devModeW.dmYResolution = devModeA.dmYResolution;
    devModeW.dmTTOption = devModeA.dmTTOption;
    devModeW.dmCollate = devModeA.dmCollate;
    wcscpy_s(devModeW.dmFormName, SjisTunnelEncoding::Decode((char*)devModeA.dmFormName).c_str());
    devModeW.dmLogPixels = devModeA.dmLogPixels;
    devModeW.dmBitsPerPel = devModeA.dmBitsPerPel;
    devModeW.dmPelsWidth = devModeA.dmPelsWidth;
    devModeW.dmPelsHeight = devModeA.dmPelsHeight;
    devModeW.dmDisplayFlags = devModeA.dmDisplayFlags;
    devModeW.dmDisplayFrequency = devModeA.dmDisplayFrequency;
    devModeW.dmICMMethod = devModeA.dmICMMethod;
    devModeW.dmICMIntent = devModeA.dmICMIntent;
    devModeW.dmMediaType = devModeA.dmMediaType;
    devModeW.dmDitherType = devModeA.dmDitherType;
    devModeW.dmReserved1 = devModeA.dmReserved1;
    devModeW.dmReserved2 = devModeA.dmReserved2;
    devModeW.dmPanningWidth = devModeA.dmPanningWidth;
    devModeW.dmPanningHeight = devModeA.dmPanningHeight;
    
    return devModeW;
}
