#include "pch.h"

void Proxy::Init(HMODULE hProxy)
{
    ProxyModuleHandle = hProxy;

    wchar_t path[MAX_PATH];
    GetSystemDirectory(path, MAX_PATH);
    wcscat_s(path, L"\\msacm32.dll");
    OriginalModuleHandle = LoadLibrary(path);
    if (OriginalModuleHandle == nullptr)
    {
        MessageBox(nullptr, L"Cannot load original msacm32 library", L"Proxy", MB_ICONERROR);
        ExitProcess(0);
    }

#define RESOLVE(fn) Original##fn = reinterpret_cast<decltype(Original##fn)>(GetProcAddress(OriginalModuleHandle, #fn))

    RESOLVE(XRegThunkEntry);
    RESOLVE(acmDriverAddA);
    RESOLVE(acmDriverAddW);
    RESOLVE(acmDriverClose);
    RESOLVE(acmDriverDetailsA);
    RESOLVE(acmDriverDetailsW);
    RESOLVE(acmDriverEnum);
    RESOLVE(acmDriverID);
    RESOLVE(acmDriverMessage);
    RESOLVE(acmDriverOpen);
    RESOLVE(acmDriverPriority);
    RESOLVE(acmDriverRemove);
    RESOLVE(acmFilterChooseA);
    RESOLVE(acmFilterChooseW);
    RESOLVE(acmFilterDetailsA);
    RESOLVE(acmFilterDetailsW);
    RESOLVE(acmFilterEnumA);
    RESOLVE(acmFilterEnumW);
    RESOLVE(acmFilterTagDetailsA);
    RESOLVE(acmFilterTagDetailsW);
    RESOLVE(acmFilterTagEnumA);
    RESOLVE(acmFilterTagEnumW);
    RESOLVE(acmFormatChooseA);
    RESOLVE(acmFormatChooseW);
    RESOLVE(acmFormatDetailsA);
    RESOLVE(acmFormatDetailsW);
    RESOLVE(acmFormatEnumA);
    RESOLVE(acmFormatEnumW);
    RESOLVE(acmFormatSuggest);
    RESOLVE(acmFormatTagDetailsA);
    RESOLVE(acmFormatTagDetailsW);
    RESOLVE(acmFormatTagEnumA);
    RESOLVE(acmFormatTagEnumW);
    RESOLVE(acmGetVersion);
    RESOLVE(acmMessage32);
    RESOLVE(acmMetrics);
    RESOLVE(acmStreamClose);
    RESOLVE(acmStreamConvert);
    RESOLVE(acmStreamMessage);
    RESOLVE(acmStreamOpen);
    RESOLVE(acmStreamPrepareHeader);
    RESOLVE(acmStreamReset);
    RESOLVE(acmStreamSize);
    RESOLVE(acmStreamUnprepareHeader);

#undef RESOLVE
}

__declspec(naked) void FakeXRegThunkEntry()             { _asm { jmp [Proxy::OriginalXRegThunkEntry] } }
__declspec(naked) void FakeacmDriverAddA()              { _asm { jmp [Proxy::OriginalacmDriverAddA] } }
__declspec(naked) void FakeacmDriverAddW()              { _asm { jmp [Proxy::OriginalacmDriverAddW] } }
__declspec(naked) void FakeacmDriverClose()             { _asm { jmp [Proxy::OriginalacmDriverClose] } }
__declspec(naked) void FakeacmDriverDetailsA()          { _asm { jmp [Proxy::OriginalacmDriverDetailsA] } }
__declspec(naked) void FakeacmDriverDetailsW()          { _asm { jmp [Proxy::OriginalacmDriverDetailsW] } }
__declspec(naked) void FakeacmDriverEnum()              { _asm { jmp [Proxy::OriginalacmDriverEnum] } }
__declspec(naked) void FakeacmDriverID()                { _asm { jmp [Proxy::OriginalacmDriverID] } }
__declspec(naked) void FakeacmDriverMessage()           { _asm { jmp [Proxy::OriginalacmDriverMessage] } }
__declspec(naked) void FakeacmDriverOpen()              { _asm { jmp [Proxy::OriginalacmDriverOpen] } }
__declspec(naked) void FakeacmDriverPriority()          { _asm { jmp [Proxy::OriginalacmDriverPriority] } }
__declspec(naked) void FakeacmDriverRemove()            { _asm { jmp [Proxy::OriginalacmDriverRemove] } }
__declspec(naked) void FakeacmFilterChooseA()           { _asm { jmp [Proxy::OriginalacmFilterChooseA] } }
__declspec(naked) void FakeacmFilterChooseW()           { _asm { jmp [Proxy::OriginalacmFilterChooseW] } }
__declspec(naked) void FakeacmFilterDetailsA()          { _asm { jmp [Proxy::OriginalacmFilterDetailsA] } }
__declspec(naked) void FakeacmFilterDetailsW()          { _asm { jmp [Proxy::OriginalacmFilterDetailsW] } }
__declspec(naked) void FakeacmFilterEnumA()             { _asm { jmp [Proxy::OriginalacmFilterEnumA] } }
__declspec(naked) void FakeacmFilterEnumW()             { _asm { jmp [Proxy::OriginalacmFilterEnumW] } }
__declspec(naked) void FakeacmFilterTagDetailsA()       { _asm { jmp [Proxy::OriginalacmFilterTagDetailsA] } }
__declspec(naked) void FakeacmFilterTagDetailsW()       { _asm { jmp [Proxy::OriginalacmFilterTagDetailsW] } }
__declspec(naked) void FakeacmFilterTagEnumA()          { _asm { jmp [Proxy::OriginalacmFilterTagEnumA] } }
__declspec(naked) void FakeacmFilterTagEnumW()          { _asm { jmp [Proxy::OriginalacmFilterTagEnumW] } }
__declspec(naked) void FakeacmFormatChooseA()           { _asm { jmp [Proxy::OriginalacmFormatChooseA] } }
__declspec(naked) void FakeacmFormatChooseW()           { _asm { jmp [Proxy::OriginalacmFormatChooseW] } }
__declspec(naked) void FakeacmFormatDetailsA()          { _asm { jmp [Proxy::OriginalacmFormatDetailsA] } }
__declspec(naked) void FakeacmFormatDetailsW()          { _asm { jmp [Proxy::OriginalacmFormatDetailsW] } }
__declspec(naked) void FakeacmFormatEnumA()             { _asm { jmp [Proxy::OriginalacmFormatEnumA] } }
__declspec(naked) void FakeacmFormatEnumW()             { _asm { jmp [Proxy::OriginalacmFormatEnumW] } }
__declspec(naked) void FakeacmFormatSuggest()           { _asm { jmp [Proxy::OriginalacmFormatSuggest] } }
__declspec(naked) void FakeacmFormatTagDetailsA()       { _asm { jmp [Proxy::OriginalacmFormatTagDetailsA] } }
__declspec(naked) void FakeacmFormatTagDetailsW()       { _asm { jmp [Proxy::OriginalacmFormatTagDetailsW] } }
__declspec(naked) void FakeacmFormatTagEnumA()          { _asm { jmp [Proxy::OriginalacmFormatTagEnumA] } }
__declspec(naked) void FakeacmFormatTagEnumW()          { _asm { jmp [Proxy::OriginalacmFormatTagEnumW] } }
__declspec(naked) void FakeacmGetVersion()              { _asm { jmp [Proxy::OriginalacmGetVersion] } }
__declspec(naked) void FakeacmMessage32()               { _asm { jmp [Proxy::OriginalacmMessage32] } }
__declspec(naked) void FakeacmMetrics()                 { _asm { jmp [Proxy::OriginalacmMetrics] } }
__declspec(naked) void FakeacmStreamClose()             { _asm { jmp [Proxy::OriginalacmStreamClose] } }
__declspec(naked) void FakeacmStreamConvert()           { _asm { jmp [Proxy::OriginalacmStreamConvert] } }
__declspec(naked) void FakeacmStreamMessage()           { _asm { jmp [Proxy::OriginalacmStreamMessage] } }
__declspec(naked) void FakeacmStreamOpen()              { _asm { jmp [Proxy::OriginalacmStreamOpen] } }
__declspec(naked) void FakeacmStreamPrepareHeader()     { _asm { jmp [Proxy::OriginalacmStreamPrepareHeader] } }
__declspec(naked) void FakeacmStreamReset()             { _asm { jmp [Proxy::OriginalacmStreamReset] } }
__declspec(naked) void FakeacmStreamSize()              { _asm { jmp [Proxy::OriginalacmStreamSize] } }
__declspec(naked) void FakeacmStreamUnprepareHeader()   { _asm { jmp [Proxy::OriginalacmStreamUnprepareHeader] } }
