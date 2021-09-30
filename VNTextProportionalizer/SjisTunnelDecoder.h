#pragma once

class SjisTunnelDecoder
{
public:
	static std::wstring Decode(const char* pText, int count = -1);
	

private:
	static void Init();

	static inline bool Initialized{};
	static inline std::vector<wchar_t> Mappings{};
};
