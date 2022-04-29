#pragma once

class SjisTunnelEncoding
{
public:
    static std::wstring Decode(const char* pText, int count = -1);
    static std::wstring Decode(const std::string& str);

    static std::string Encode(const wchar_t* pText, int count = -1);
    static std::string Encode(const std::wstring& str);

private:
    static void Init();

    static inline bool Initialized{};
    static inline std::vector<wchar_t> Mappings{};
};
