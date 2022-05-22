#include "pch.h"

using namespace std;

wstring SjisTunnelEncoding::Decode(const char* pText, int count)
{
    Init();

    wstring result;
    if (pText == nullptr)
        return result;

    int i = 0;
    while (count < 0 ? pText[i] != '\0' : i < count)
    {
        BYTE highByte = pText[i++];
        BYTE lowByte = 0;

        if (IsSjisHighByte(highByte))
        {
            lowByte = pText[i++];
            if (lowByte == 0)
                break;

            int mappingIdx = TunnelCharToMappingIndex((WORD)((highByte << 8) | lowByte));
            if (mappingIdx >= 0)
            {
                result += Mappings[mappingIdx];
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

wstring SjisTunnelEncoding::Decode(const string& str)
{
    return Decode(str.c_str());
}

string SjisTunnelEncoding::Encode(const wchar_t* pText, int count)
{
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
                if (mappingIdx == 0x3B * (0x40 - sizeof(LowBytesToAvoid) - 1))
                    throw exception("SJIS tunnel limit exceeded");
            }
            else
            {
                mappingIdx = distance(Mappings.begin(), it);
            }

            WORD tunnelChar = MappingIndexToTunnelChar(mappingIdx);
            multibyte[0] = (char)(tunnelChar >> 8);
            multibyte[1] = (char)(tunnelChar);
            multibyteLength = 2;
        }
        result.append(multibyte, multibyteLength);
    }
    return result;
}

string SjisTunnelEncoding::Encode(const wstring& str)
{
    return Encode(str.c_str());
}

void SjisTunnelEncoding::PatchGameLookupTable()
{
    // Certain SJIS engines actually convert text to UTF16 before rendering it, but do so using an internal lookup table
    // rather than calling MultiByteToWideChar(). Find the table and patch it to support tunneling there as well.

    Init();
    if (Mappings.empty())
        return;

    void* pImageStart = GetModuleHandle(nullptr);
    DWORD imageSize = DetourGetModuleSize(nullptr);
    wchar_t* pLookupTable = (wchar_t*)MemoryUtil::FindData(pImageStart, imageSize, LookupTableSearchPattern, sizeof(LookupTableSearchPattern));
    if (pLookupTable == nullptr)
        return;

    // The pattern we found corresponds to the first valid entries in the lookup table (0x8140, 0x8141, ...).
    // Subtract 0x8140 to get its base.
    pLookupTable -= 0x8140;

    map<void*, MemoryUnprotector> unprotectors;
    for (int mappingIdx = 0; mappingIdx < Mappings.size(); mappingIdx++)
    {
        WORD tunnelChar = MappingIndexToTunnelChar(mappingIdx);
        wchar_t* pLookupEntry = &pLookupTable[tunnelChar];

        void* pLookupEntryPage = (void*)((DWORD)pLookupEntry & ~0xFFF);
        unprotectors.try_emplace(pLookupEntryPage, pLookupEntryPage, 0x1000);

        *pLookupEntry = Mappings[mappingIdx];
    }
}

void SjisTunnelEncoding::Init()
{
    if (Initialized)
        return;

    Initialized = true;

    wstring filePath = Path::Combine(Path::GetModuleFolderPath(nullptr), L"sjis_ext.bin");
    FILE* pFile;
    _wfopen_s(&pFile, filePath.c_str(), L"rb");
    if (pFile == nullptr)
        return;

    fseek(pFile, 0, SEEK_END);
    int fileSize = ftell(pFile);
    fseek(pFile, 0, SEEK_SET);
    Mappings.resize(fileSize / sizeof(wchar_t));
    fread(Mappings.data(), sizeof(wchar_t), Mappings.size(), pFile);
    fclose(pFile);
}

WORD SjisTunnelEncoding::MappingIndexToTunnelChar(int index)
{
    int highIdx = index / (0x40 - sizeof(LowBytesToAvoid) - 1);
    int lowIdx = index % (0x40 - sizeof(LowBytesToAvoid) - 1);
    BYTE highByte = highIdx < 0x1F ? 0x81 + highIdx : 0xE0 + (highIdx - 0x1F);
    BYTE lowByte = 1 + lowIdx;
    for (int i = 0; i < sizeof(LowBytesToAvoid); i++)
    {
        if (lowByte >= LowBytesToAvoid[i])
            lowByte++;
    }

    return (WORD)((highByte << 8) | lowByte);
}

int SjisTunnelEncoding::TunnelCharToMappingIndex(WORD tunnelChar)
{
    BYTE highByte = (BYTE)(tunnelChar >> 8);
    BYTE lowByte = (BYTE)tunnelChar;

    if (!IsSjisHighByte(highByte) || lowByte == 0 || lowByte >= 0x40)
        return -1;

    int highIdx = highByte < 0xA0 ? highByte - 0x81 : 0x1F + (highByte - 0xE0);

    int lowIdx = lowByte;
    for (int i = sizeof(LowBytesToAvoid) - 1; i >= 0; i--)
    {
        if (lowIdx > LowBytesToAvoid[i])
            lowIdx--;
    }
    lowIdx--;
    
    return highIdx * (0x40 - sizeof(LowBytesToAvoid) - 1) + lowIdx;
}

bool SjisTunnelEncoding::IsSjisHighByte(BYTE byte)
{
    return (byte >= 0x81 && byte < 0xA0) || (byte >= 0xE0 && byte < 0xFD);
}
