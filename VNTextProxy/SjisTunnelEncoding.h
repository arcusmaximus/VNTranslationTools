#pragma once

class SjisTunnelEncoding
{
public:
    static std::wstring Decode(const char* pText, int count = -1);
    static std::wstring Decode(const std::string& str);

    static std::string Encode(const wchar_t* pText, int count = -1);
    static std::string Encode(const std::wstring& str);

    static void PatchGameLookupTable();

private:
    static void Init();

    static WORD MappingIndexToTunnelChar(int index);
    static int TunnelCharToMappingIndex(WORD tunnelChar);
    static bool IsSjisHighByte(BYTE byte);

    static inline bool Initialized{};
    static inline std::vector<wchar_t> Mappings{};

    static inline BYTE LowBytesToAvoid[] = { '\t', '\n', '\r', ' ', ',' };
    static inline BYTE LookupTableSearchPattern[] = {
        0x00, 0x30, 0x01, 0x30, 0x02, 0x30, 0x0C, 0xFF, 0x0E, 0xFF, 0xFB, 0x30, 0x1A, 0xFF, 0x1B, 0xFF,
        0x1F, 0xFF, 0x01, 0xFF, 0x9B, 0x30, 0x9C, 0x30, 0xB4, 0x00, 0x40, 0xFF, 0xA8, 0x00, 0x3E, 0xFF
    };
};
