#pragma once

class LocaleEmulator
{
public:
	static bool Relaunch();

private:
	typedef struct ML_PROCESS_INFORMATION : PROCESS_INFORMATION
	{
		PVOID FirstCallLdrLoadDll;

	} ML_PROCESS_INFORMATION, *PML_PROCESS_INFORMATION;

	typedef struct _TIME_FIELDS
	{
		SHORT Year;        // range [1601...]
		SHORT Month;       // range [1..12]
		SHORT Day;         // range [1..31]
		SHORT Hour;        // range [0..23]
		SHORT Minute;      // range [0..59]
		SHORT Second;      // range [0..59]
		SHORT Milliseconds;// range [0..999]
		SHORT Weekday;     // range [0..6] == [Sunday..Saturday]
	} TIME_FIELDS, *PTIME_FIELDS;

	typedef struct _RTL_TIME_ZONE_INFORMATION
	{
		LONG        Bias;
		WCHAR       StandardName[32];
		TIME_FIELDS StandardStart;
		LONG        StandardBias;
		WCHAR       DaylightName[32];
		TIME_FIELDS DaylightStart;
		LONG        DaylightBias;
	} RTL_TIME_ZONE_INFORMATION, *PRTL_TIME_ZONE_INFORMATION;

	typedef struct _REG_TZI_FORMAT
	{
		int Bias;
		int StandardBias;
		int DaylightBias;
		_SYSTEMTIME StandardDate;
		_SYSTEMTIME DaylightDate;
	} REG_TZI_FORMAT;

	typedef struct
	{
		USHORT Length;
		USHORT MaximumLength;
		union
		{
			PWSTR  Buffer;
			ULONG64 Dummy;
		};
	} UNICODE_STRING3264, *PUNICODE_STRING3264;

	typedef UNICODE_STRING3264 UNICODE_STRING64;
	typedef PUNICODE_STRING3264 PUNICODE_STRING64;

	typedef struct
	{
		ULONG64             Root;
		UNICODE_STRING64    SubKey;
		UNICODE_STRING64    ValueName;
		ULONG               DataType;
		PVOID64             Data;
		ULONG64             DataSize;
	} REGISTRY_ENTRY64;

	typedef struct
	{
		REGISTRY_ENTRY64 Original;
		REGISTRY_ENTRY64 Redirected;
	} REGISTRY_REDIRECTION_ENTRY64, *PREGISTRY_REDIRECTION_ENTRY64;

	typedef struct
	{
		ULONG                           AnsiCodePage;
		ULONG                           OemCodePage;
		ULONG                           LocaleID;
		ULONG                           DefaultCharset;
		ULONG                           HookUILanguageApi;
		WCHAR                           DefaultFaceName[LF_FACESIZE];
		RTL_TIME_ZONE_INFORMATION       Timezone;
		ULONG64                         NumberOfRegistryRedirectionEntries;
		REGISTRY_REDIRECTION_ENTRY64    RegistryReplacement[1];
	} LOCALE_ENUMLATOR_ENVIRONMENT_BLOCK, * PLOCALE_ENUMLATOR_ENVIRONMENT_BLOCK, LEB, *PLEB;

	typedef DWORD (WINAPI* LeCreateProcess_t)(
		PLEB                    leb,
		PCWSTR                  applicationName,
		PCWSTR                  commandLine,
		PCWSTR                  currentDirectory,
		ULONG                   creationFlags,
		LPSTARTUPINFOW          startupInfo,
		PML_PROCESS_INFORMATION processInformation,
		LPSECURITY_ATTRIBUTES   processAttributes,
		LPSECURITY_ATTRIBUTES   threadAttributes,
		PVOID                   environment,
		HANDLE                  token
	);

	static LEB CreateLeb();
};
