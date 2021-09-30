#pragma once

class Proportionalizer
{
public:
	static void Init();
	static int MeasureStringWidth(const std::wstring& str, int fontSize);
	
	static inline int LastLineEnd;

protected:
	static void PatchGameImports(const std::map<std::string, void*>& replacementFuncs);

	static bool AdaptRenderArgs(const wchar_t* pText, int length, int fontSize, int& x, int& y);
	
	static inline std::wstring FontName{};
	static inline FontManager FontManager{};
	static inline bool Bold{};
	static inline bool Italic{};
	static inline bool Underline{};

private:
	static std::wstring LoadCustomFont();
	static bool HandleFormattingCode(wchar_t c);
	
	static BOOL __stdcall PatchGameImport(void* pContext, DWORD nOrdinal, LPCSTR pszFunc, void** ppvFunc);
	static int __stdcall MultiByteToWideCharHook(UINT codePage, DWORD flags, LPCCH lpMultiByteStr, int cbMultiByte, LPWSTR lpWideCharStr, int cchWideChar);
};
