#pragma once

class Win32AToWAdapter
{
public:
    static void Init();

private:
    static UINT __stdcall GetACPHook();
    static BOOL __stdcall IsDBCSLeadByteHook(BYTE TestChar);
    static int __stdcall MultiByteToWideCharHook(UINT codePage, DWORD flags, LPCCH lpMultiByteStr, int cbMultiByte, LPWSTR lpWideCharStr, int cchWideChar);
    static int __stdcall WideCharToMultiByteHook(UINT codePage, DWORD flags, LPCWCH lpWideCharStr, int cchWideChar, LPSTR lpMultiByteStr, int cbMultiByte, LPCCH lpDefaultChar, LPBOOL lpUsedDefaultChar);
    
    static HANDLE __stdcall CreateEventAHook(LPSECURITY_ATTRIBUTES lpEventAttributes, BOOL bManualReset, BOOL bInitialState, LPCSTR lpName);
    static HANDLE __stdcall OpenEventAHook(DWORD dwDesiredAccess, BOOL bInheritHandle, LPCSTR lpName);
    static HANDLE __stdcall CreateMutexAHook(LPSECURITY_ATTRIBUTES lpMutexAttributes, BOOL bInitialOwner, LPCSTR lpName);
    static HANDLE __stdcall OpenMutexAHook(DWORD dwDesiredAccess, BOOL bInheritHandle, LPCSTR lpName);

    static DWORD __stdcall GetModuleFileNameAHook(HMODULE hModule, LPSTR lpFilename, DWORD nSize);
    static HMODULE __stdcall LoadLibraryAHook(LPCSTR lpLibFileName);
    static HMODULE __stdcall LoadLibraryExAHook(LPCSTR lpLibFileName, HANDLE hFile, DWORD dwFlags);

    static DWORD __stdcall GetFullPathNameAHook(LPCSTR lpFileName, DWORD nBufferLength, LPSTR lpBuffer, LPSTR* lpFilePart);
    static HANDLE __stdcall FindFirstFileAHook(LPCSTR lpFileName, LPWIN32_FIND_DATAA lpFindFileData);
    static BOOL __stdcall FindNextFileAHook(HANDLE hFindFile, LPWIN32_FIND_DATAA lpFindFileData);
    static DWORD __stdcall SearchPathAHook(LPCSTR lpPath, LPCSTR lpFileName, LPCSTR lpExtension, DWORD nBufferLength, LPSTR lpBuffer, LPSTR* lpFilePart);
    static DWORD __stdcall GetFileAttributesAHook(LPCSTR lpFileName);
    static HANDLE __stdcall CreateFileAHook(LPCSTR lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode, LPSECURITY_ATTRIBUTES lpSecurityAttributes, DWORD dwCreationDisposition, DWORD dwFlagsAndAttributes, HANDLE hTemplateFile);
    static HANDLE __stdcall CreateFileMappingAHook(HANDLE hFile, LPSECURITY_ATTRIBUTES lpFileMappingAttributes, DWORD flProtect, DWORD dwMaximumSizeHigh, DWORD dwMaximumSizeLow, LPCSTR lpName);
    static BOOL __stdcall DeleteFileAHook(LPCSTR lpFileName);
    static BOOL __stdcall CreateDirectoryAHook(LPCSTR lpPathName, LPSECURITY_ATTRIBUTES lpSecurityAttributes);
    static BOOL __stdcall RemoveDirectoryAHook(LPCSTR lpPathName);
    static DWORD __stdcall GetCurrentDirectoryAHook(DWORD nBufferLength, LPSTR lpBuffer);
    static DWORD __stdcall GetTempPathAHook(DWORD nBufferLength, LPSTR lpBuffer);
    static UINT __stdcall GetTempFileNameAHook(LPCSTR lpPathName, LPCSTR lpPrefixString, UINT uUnique, LPSTR lpTempFileName);

    static LSTATUS __stdcall RegCreateKeyExAHook(HKEY hKey, LPCSTR lpSubKey, DWORD Reserved, LPSTR lpClass, DWORD dwOptions, REGSAM samDesired, const LPSECURITY_ATTRIBUTES lpSecurityAttributes, PHKEY phkResult, LPDWORD lpdwDisposition);
    static LSTATUS __stdcall RegOpenKeyExAHook(HKEY hKey, LPCSTR lpSubKey, DWORD ulOptions, REGSAM samDesired, PHKEY phkResult);
    static LSTATUS __stdcall RegQueryValueExAHook(HKEY hKey, LPCSTR lpValueName, LPDWORD lpReserved, LPDWORD lpType, LPBYTE lpData, LPDWORD lpcbData);
    static LSTATUS __stdcall RegSetValueExAHook(HKEY hKey, LPCSTR lpValueName, DWORD Reserved, DWORD dwType, const BYTE* lpData, DWORD cbData);
    
    static LONG __stdcall SetWindowLongAHook(HWND hWnd, int nIndex, LONG dwNewLong);
    static BOOL __stdcall DestroyWindowHook(HWND hWnd);
    static void __stdcall HandleImeCompositionEnded(const std::wstring& text);
    static BOOL __stdcall PeekMessageAHook(LPMSG lpMsg, HWND hWnd, UINT wMsgFilterMin, UINT wMsgFilterMax, UINT wRemoveMsg);
    static BOOL __stdcall GetMessageAHook(LPMSG lpMsg, HWND hWnd, UINT wMsgFilterMin, UINT wMsgFilterMax);
    static LRESULT __stdcall DispatchMessageAHook(const MSG* lpMsg);
    static LRESULT __stdcall DefWindowProcAHook(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);
    static BOOL __stdcall AppendMenuAHook(HMENU hMenu, UINT uFlags, UINT_PTR uIDNewItem, LPCSTR lpNewItem);
    static BOOL __stdcall InsertMenuAHook(HMENU hMenu, UINT uPosition, UINT uFlags, UINT_PTR uIDNewItem, LPCSTR lpNewItem);
    static BOOL __stdcall InsertMenuItemAHook(HMENU hmenu, UINT item, BOOL fByPosition, LPCMENUITEMINFOA lpmi);
    static int __stdcall MessageBoxAHook(HWND hWnd, LPCSTR lpText, LPCSTR lpCaption, UINT uType);

    static BOOL __stdcall GetMonitorInfoAHook(HMONITOR hMonitor, LPMONITORINFO lpmi);
    static BOOL __stdcall EnumDisplayDevicesAHook(LPCSTR lpDevice, DWORD iDevNum, PDISPLAY_DEVICEA lpDisplayDevice, DWORD dwFlags);
    static BOOL __stdcall EnumDisplaySettingsAHook(LPCSTR lpszDeviceName, DWORD iModeNum, DEVMODEA* lpDevMode);
    static LONG __stdcall ChangeDisplaySettingsAHook(DEVMODEA* lpDevMode, DWORD dwFlags);
    static LONG __stdcall ChangeDisplaySettingsExAHook(LPCSTR lpszDeviceName, DEVMODEA* lpDevMode, HWND hwnd, DWORD dwflags, LPVOID lParam);

    static HRESULT __stdcall DirectDrawEnumerateAHook(LPDDENUMCALLBACKA lpCallback, LPVOID lpContext);
    static BOOL __stdcall DirectDrawEnumerateCallback(GUID* pGuid, LPSTR pszDriverName, LPSTR pszDriverDescription, LPVOID pContext);

    static HRESULT __stdcall DirectDrawEnumerateExAHook(LPDDENUMCALLBACKEXA lpCallback, LPVOID lpContext, DWORD dwFlags);
    static BOOL __stdcall DirectDrawEnumerateExCallback(GUID* pGuid, LPSTR pszDriverName, LPSTR pszDriverDescription, LPVOID pContext, HMONITOR hMonitor);

    static HRESULT __stdcall DirectSoundEnumerateAHook(LPDSENUMCALLBACKA pDSEnumCallback, LPVOID pContext);
    static BOOL __stdcall DirectSoundEnumerateCallback(LPGUID lpGuid, LPCWSTR lpcstrDescription, LPCWSTR lpcstrModule, LPVOID lpContext);

    static WIN32_FIND_DATAA ConvertFindDataWToA(const WIN32_FIND_DATAW& findDataW);
    static DEVMODEA ConvertDevModeWToA(const DEVMODEW& devModeW);
    static DEVMODEW ConvertDevModeAToW(const DEVMODEA& devModeA);

    struct DirectDrawEnumerateContext
    {
        LPDDENUMCALLBACKA OriginalCallback;
        LPVOID OriginalContext;
    };

    struct DirectDrawEnumerateExContext
    {
        LPDDENUMCALLBACKEXA OriginalCallback;
        LPVOID OriginalContext;
    };

    struct DirectSoundEnumerateContext
    {
        LPDSENUMCALLBACKA OriginalCallback;
        LPVOID OriginalContext;
    };

    static inline std::map<HWND, WNDPROC> WindowProcs{};
    static inline std::wstring PendingImeCompositionChars{};
    static inline std::vector<MSG> PendingWindowMessages{};
};
