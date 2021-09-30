#include "pch.h"

bool LocaleEmulator::Relaunch()
{
    LEB leb = CreateLeb();

    wchar_t exePath[MAX_PATH];
    GetModuleFileName(nullptr, exePath, sizeof(exePath) / sizeof(exePath[0]));

    const wchar_t* commandLine = GetCommandLine();

    wchar_t currentDirectory[MAX_PATH];
    GetCurrentDirectory(sizeof(currentDirectory) / sizeof(currentDirectory[0]), currentDirectory);

    STARTUPINFO startInfo{};
    ML_PROCESS_INFORMATION processInfo{};

    HMODULE hLoader = LoadLibrary(L"LoaderDll.dll");
    if (hLoader == nullptr)
        return false;

    LeCreateProcess_t LeCreateProcess = (LeCreateProcess_t)GetProcAddress(hLoader, "LeCreateProcess");
    if (LeCreateProcess == nullptr)
        return false;

    return LeCreateProcess(
        &leb,
        exePath,
        commandLine,
        currentDirectory,
        0,
        &startInfo,
        &processInfo,
        nullptr,
        nullptr,
        nullptr,
        nullptr
    ) == ERROR_SUCCESS;
}

LocaleEmulator::LEB LocaleEmulator::CreateLeb()
{
    LEB leb{};
    leb.AnsiCodePage = 932;
    leb.OemCodePage = 932;
    leb.LocaleID = 1041;
    leb.DefaultCharset = SHIFTJIS_CHARSET;

    HKEY hTimeZone;
    if (RegOpenKey(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Time Zones\\Tokyo Standard Time", &hTimeZone) == ERROR_SUCCESS)
    {
        DWORD bufferSize = sizeof(leb.Timezone.StandardName);
        RegGetValue(hTimeZone, nullptr, L"Std", RRF_RT_REG_SZ, nullptr, leb.Timezone.StandardName, &bufferSize);

        bufferSize = sizeof(leb.Timezone.DaylightName);
        RegGetValue(hTimeZone, nullptr, L"Dlt", RRF_RT_REG_SZ, nullptr, leb.Timezone.DaylightName, &bufferSize);

        REG_TZI_FORMAT timeZoneInfo;
        bufferSize = sizeof(timeZoneInfo);
        RegGetValue(hTimeZone, nullptr, L"TZI", RRF_RT_REG_BINARY, nullptr, &timeZoneInfo, &bufferSize);
        leb.Timezone.Bias = timeZoneInfo.Bias;
        leb.Timezone.StandardBias = timeZoneInfo.StandardBias;
        leb.Timezone.DaylightBias = 0;

        RegCloseKey(hTimeZone);
    }

    return leb;
}
