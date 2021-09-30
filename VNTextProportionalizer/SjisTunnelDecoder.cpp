#include "pch.h"

using namespace std;

wstring SjisTunnelDecoder::Decode(const char* pText, int count)
{
    if (!Initialized)
        Init();

    wstring result;
    if (pText == nullptr)
        return result;

    int charIdx = 0;
    while (count < 0 ? pText[charIdx] != '\0' : charIdx < count)
    {
        BYTE highByte = pText[charIdx++];
        BYTE lowByte = 0;

        if ((highByte >= 0x81 && highByte < 0xA0) || (highByte >= 0xE0 && highByte < 0xFD))
        {
            int highIdx = highByte < 0xA0 ? highByte - 0x81 : 0x1F + (highByte - 0xE0);

            lowByte = pText[charIdx++];
            if (lowByte == 0)
                break;

            if (lowByte < 0x40)
            {
                int lowIdx = lowByte - 0xE;

                int index = highIdx * 0x32 + lowIdx;
                result += Mappings[index];
                continue;
            }
        }

        int charLength = lowByte == 0 ? 1 : 2;
        wchar_t wc;
        MultiByteToWideChar(932, 0, pText + charIdx - charLength, charLength, &wc, 1);
        result += wc;
    }
    return result;
}

void SjisTunnelDecoder::Init()
{
    Initialized = true;

    HMODULE hExe = GetModuleHandle(nullptr);
    wchar_t folderPath[MAX_PATH];
    GetModuleFileName(hExe, folderPath, sizeof(folderPath) / sizeof(folderPath[0]));
    wchar_t* pLastSlash = wcsrchr(folderPath, L'\\');
    if (pLastSlash == nullptr)
        return;

    wcsncpy_s(pLastSlash + 1, sizeof(folderPath) / sizeof(folderPath[0]) - (pLastSlash + 1 - folderPath), L"sjis_ext.bin", 100);
    FILE* pFile;
    _wfopen_s(&pFile, folderPath, L"rb");
    if (pFile == nullptr)
        return;

    fseek(pFile, 0, SEEK_END);
    int fileSize = ftell(pFile);
    fseek(pFile, 0, SEEK_SET);
    Mappings.resize(fileSize / sizeof(wchar_t));
    fread(Mappings.data(), sizeof(wchar_t), fileSize / sizeof(wchar_t), pFile);
    fclose(pFile);
}
