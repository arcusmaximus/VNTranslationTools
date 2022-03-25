#include "pch.h"

using namespace std;

wstring SjisTunnelEncoding::Decode(const char* pText, int count)
{
    if (!Initialized)
        Init();

    wstring result;
    if (pText == nullptr)
        return result;

    int i = 0;
    while (count < 0 ? pText[i] != '\0' : i < count)
    {
        BYTE highByte = pText[i++];
        BYTE lowByte = 0;

        if ((highByte >= 0x81 && highByte < 0xA0) || (highByte >= 0xE0 && highByte < 0xFD))
        {
            int highIdx = highByte < 0xA0 ? highByte - 0x81 : 0x1F + (highByte - 0xE0);

            lowByte = pText[i++];
            if (lowByte == 0)
                break;

            if (lowByte < 0x40)
            {
                int lowIdx = lowByte;
                if (lowIdx > ',')
                    lowIdx--;
                if (lowIdx > ' ')
                    lowIdx--;
                if (lowIdx > '\r')
                    lowIdx--;
                if (lowIdx > '\n')
                    lowIdx--;
                if (lowIdx > '\t')
                    lowIdx--;

                lowIdx--;

                int index = highIdx * 0x3A + lowIdx;
                result += Mappings[index];
                continue;
            }
        }

        int charLength = lowByte == 0 ? 1 : 2;
        wchar_t wc;
        MultiByteToWideChar(932, 0, pText + i - charLength, charLength, &wc, 1);
        result += wc;
    }
    return result;
}

string SjisTunnelEncoding::Encode(const wchar_t* pText, int count)
{
    if (!Initialized)
        Init();

    string result;
    if (pText == nullptr)
        return result;

    int i = 0;
    while (count < 0 ? pText[i] != L'\0' : i < count)
    {
        wchar_t widechar = pText[i++];
        char multibyte[2];
        BOOL failed;
        int multibyteLength = WideCharToMultiByte(932, WC_NO_BEST_FIT_CHARS, &widechar, 1, multibyte, sizeof(multibyte), nullptr, &failed);
        if (failed || multibyte[0] >= 0xF0)
        {
            auto it = find(Mappings.begin(), Mappings.end(), widechar);
            int mappingIdx;
            if (it == Mappings.end())
            {
                Mappings.push_back(widechar);
                mappingIdx = Mappings.size() - 1;
                if (mappingIdx == 0x3B * 0x3A)
                    throw exception("SJIS tunnel limit exceeded");
            }
            else
            {
                mappingIdx = distance(Mappings.begin(), it);
            }

            int highIdx = mappingIdx / 0x3A;
            int lowIdx = mappingIdx % 0x3A;
            BYTE highByte = highIdx < 0x1F ? 0x81 + highIdx : 0xE0 + (highIdx - 0x1F);
            BYTE lowByte = 1 + lowIdx;
            if (lowByte >= '\t')
                lowByte++;
            if (lowByte >= '\n')
                lowByte++;
            if (lowByte >= '\r')
                lowByte++;
            if (lowByte >= ' ')
                lowByte++;
            if (lowByte >= ',')
                lowByte++;
            
            multibyte[0] = highByte;
            multibyte[1] = lowByte;
            multibyteLength = 2;
        }
        result.append(multibyte, multibyteLength);
    }
    return result;
}

void SjisTunnelEncoding::Init()
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
