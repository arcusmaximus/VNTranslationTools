#pragma once

class StringUtil
{
public:
	static std::wstring ToWString(const char* psz, int numBytes = -1, int codepage = 932);
	static std::wstring ToHalfWidth(const std::wstring& fullWidth);
};
