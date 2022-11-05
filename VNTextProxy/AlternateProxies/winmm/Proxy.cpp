#include "pch.h"

void Proxy::Init(HMODULE hProxy)
{
    ProxyModuleHandle = hProxy;
    
    wchar_t realDllPath[MAX_PATH];
    GetSystemDirectory(realDllPath, MAX_PATH);
    wcscat_s(realDllPath, L"\\winmm.dll");
    OriginalModuleHandle = LoadLibrary(realDllPath);
    if (OriginalModuleHandle == nullptr)
    {
        MessageBox(nullptr, L"Cannot load original winmm.dll library", L"Proxy", MB_ICONERROR);
        ExitProcess(0);
    }

#define RESOLVE(fn) Original##fn = GetProcAddress(OriginalModuleHandle, #fn)
    RESOLVE(CloseDriver);
    RESOLVE(DefDriverProc);
    RESOLVE(DriverCallback);
    RESOLVE(DrvGetModuleHandle);
    RESOLVE(GetDriverModuleHandle);
    RESOLVE(OpenDriver);
    RESOLVE(PlaySound);
    RESOLVE(PlaySoundA);
    RESOLVE(PlaySoundW);
    RESOLVE(SendDriverMessage);
    RESOLVE(WOWAppExit);
    RESOLVE(auxGetDevCapsA);
    RESOLVE(auxGetDevCapsW);
    RESOLVE(auxGetNumDevs);
    RESOLVE(auxGetVolume);
    RESOLVE(auxOutMessage);
    RESOLVE(auxSetVolume);
    RESOLVE(joyConfigChanged);
    RESOLVE(joyGetDevCapsA);
    RESOLVE(joyGetDevCapsW);
    RESOLVE(joyGetNumDevs);
    RESOLVE(joyGetPos);
    RESOLVE(joyGetPosEx);
    RESOLVE(joyGetThreshold);
    RESOLVE(joyReleaseCapture);
    RESOLVE(joySetCapture);
    RESOLVE(joySetThreshold);
    RESOLVE(mciDriverNotify);
    RESOLVE(mciDriverYield);
    RESOLVE(mciExecute);
    RESOLVE(mciFreeCommandResource);
    RESOLVE(mciGetCreatorTask);
    RESOLVE(mciGetDeviceIDA);
    RESOLVE(mciGetDeviceIDFromElementIDA);
    RESOLVE(mciGetDeviceIDFromElementIDW);
    RESOLVE(mciGetDeviceIDW);
    RESOLVE(mciGetDriverData);
    RESOLVE(mciGetErrorStringA);
    RESOLVE(mciGetErrorStringW);
    RESOLVE(mciGetYieldProc);
    RESOLVE(mciLoadCommandResource);
    RESOLVE(mciSendCommandA);
    RESOLVE(mciSendCommandW);
    RESOLVE(mciSendStringA);
    RESOLVE(mciSendStringW);
    RESOLVE(mciSetDriverData);
    RESOLVE(mciSetYieldProc);
    RESOLVE(midiConnect);
    RESOLVE(midiDisconnect);
    RESOLVE(midiInAddBuffer);
    RESOLVE(midiInClose);
    RESOLVE(midiInGetDevCapsA);
    RESOLVE(midiInGetDevCapsW);
    RESOLVE(midiInGetErrorTextA);
    RESOLVE(midiInGetErrorTextW);
    RESOLVE(midiInGetID);
    RESOLVE(midiInGetNumDevs);
    RESOLVE(midiInMessage);
    RESOLVE(midiInOpen);
    RESOLVE(midiInPrepareHeader);
    RESOLVE(midiInReset);
    RESOLVE(midiInStart);
    RESOLVE(midiInStop);
    RESOLVE(midiInUnprepareHeader);
    RESOLVE(midiOutCacheDrumPatches);
    RESOLVE(midiOutCachePatches);
    RESOLVE(midiOutClose);
    RESOLVE(midiOutGetDevCapsA);
    RESOLVE(midiOutGetDevCapsW);
    RESOLVE(midiOutGetErrorTextA);
    RESOLVE(midiOutGetErrorTextW);
    RESOLVE(midiOutGetID);
    RESOLVE(midiOutGetNumDevs);
    RESOLVE(midiOutGetVolume);
    RESOLVE(midiOutLongMsg);
    RESOLVE(midiOutMessage);
    RESOLVE(midiOutOpen);
    RESOLVE(midiOutPrepareHeader);
    RESOLVE(midiOutReset);
    RESOLVE(midiOutSetVolume);
    RESOLVE(midiOutShortMsg);
    RESOLVE(midiOutUnprepareHeader);
    RESOLVE(midiStreamClose);
    RESOLVE(midiStreamOpen);
    RESOLVE(midiStreamOut);
    RESOLVE(midiStreamPause);
    RESOLVE(midiStreamPosition);
    RESOLVE(midiStreamProperty);
    RESOLVE(midiStreamRestart);
    RESOLVE(midiStreamStop);
    RESOLVE(mixerClose);
    RESOLVE(mixerGetControlDetailsA);
    RESOLVE(mixerGetControlDetailsW);
    RESOLVE(mixerGetDevCapsA);
    RESOLVE(mixerGetDevCapsW);
    RESOLVE(mixerGetID);
    RESOLVE(mixerGetLineControlsA);
    RESOLVE(mixerGetLineControlsW);
    RESOLVE(mixerGetLineInfoA);
    RESOLVE(mixerGetLineInfoW);
    RESOLVE(mixerGetNumDevs);
    RESOLVE(mixerMessage);
    RESOLVE(mixerOpen);
    RESOLVE(mixerSetControlDetails);
    RESOLVE(mmDrvInstall);
    RESOLVE(mmGetCurrentTask);
    RESOLVE(mmTaskBlock);
    RESOLVE(mmTaskCreate);
    RESOLVE(mmTaskSignal);
    RESOLVE(mmTaskYield);
    RESOLVE(mmioAdvance);
    RESOLVE(mmioAscend);
    RESOLVE(mmioClose);
    RESOLVE(mmioCreateChunk);
    RESOLVE(mmioDescend);
    RESOLVE(mmioFlush);
    RESOLVE(mmioGetInfo);
    RESOLVE(mmioInstallIOProcA);
    RESOLVE(mmioInstallIOProcW);
    RESOLVE(mmioOpenA);
    RESOLVE(mmioOpenW);
    RESOLVE(mmioRead);
    RESOLVE(mmioRenameA);
    RESOLVE(mmioRenameW);
    RESOLVE(mmioSeek);
    RESOLVE(mmioSendMessage);
    RESOLVE(mmioSetBuffer);
    RESOLVE(mmioSetInfo);
    RESOLVE(mmioStringToFOURCCA);
    RESOLVE(mmioStringToFOURCCW);
    RESOLVE(mmioWrite);
    RESOLVE(mmsystemGetVersion);
    RESOLVE(sndPlaySoundA);
    RESOLVE(sndPlaySoundW);
    RESOLVE(timeBeginPeriod);
    RESOLVE(timeEndPeriod);
    RESOLVE(timeGetDevCaps);
    RESOLVE(timeGetSystemTime);
    RESOLVE(timeGetTime);
    RESOLVE(timeKillEvent);
    RESOLVE(timeSetEvent);
    RESOLVE(waveInAddBuffer);
    RESOLVE(waveInClose);
    RESOLVE(waveInGetDevCapsA);
    RESOLVE(waveInGetDevCapsW);
    RESOLVE(waveInGetErrorTextA);
    RESOLVE(waveInGetErrorTextW);
    RESOLVE(waveInGetID);
    RESOLVE(waveInGetNumDevs);
    RESOLVE(waveInGetPosition);
    RESOLVE(waveInMessage);
    RESOLVE(waveInOpen);
    RESOLVE(waveInPrepareHeader);
    RESOLVE(waveInReset);
    RESOLVE(waveInStart);
    RESOLVE(waveInStop);
    RESOLVE(waveInUnprepareHeader);
    RESOLVE(waveOutBreakLoop);
    RESOLVE(waveOutClose);
    RESOLVE(waveOutGetDevCapsA);
    RESOLVE(waveOutGetDevCapsW);
    RESOLVE(waveOutGetErrorTextA);
    RESOLVE(waveOutGetErrorTextW);
    RESOLVE(waveOutGetID);
    RESOLVE(waveOutGetNumDevs);
    RESOLVE(waveOutGetPitch);
    RESOLVE(waveOutGetPlaybackRate);
    RESOLVE(waveOutGetPosition);
    RESOLVE(waveOutGetVolume);
    RESOLVE(waveOutMessage);
    RESOLVE(waveOutOpen);
    RESOLVE(waveOutPause);
    RESOLVE(waveOutPrepareHeader);
    RESOLVE(waveOutReset);
    RESOLVE(waveOutRestart);
    RESOLVE(waveOutSetPitch);
    RESOLVE(waveOutSetPlaybackRate);
    RESOLVE(waveOutSetVolume);
    RESOLVE(waveOutUnprepareHeader);
    RESOLVE(waveOutWrite);
#undef RESOLVE
}

__declspec(naked) void FakeCloseDriver()                    { __asm { jmp [Proxy::OriginalCloseDriver] } }
__declspec(naked) void FakeDefDriverProc()                  { __asm { jmp [Proxy::OriginalDefDriverProc] } }
__declspec(naked) void FakeDriverCallback()                 { __asm { jmp [Proxy::OriginalDriverCallback] } }
__declspec(naked) void FakeDrvGetModuleHandle()             { __asm { jmp [Proxy::OriginalDrvGetModuleHandle] } }
__declspec(naked) void FakeGetDriverModuleHandle()          { __asm { jmp [Proxy::OriginalGetDriverModuleHandle] } }
__declspec(naked) void FakeOpenDriver()                     { __asm { jmp [Proxy::OriginalOpenDriver] } }
__declspec(naked) void FakePlaySound()                      { __asm { jmp [Proxy::OriginalPlaySound] } }
__declspec(naked) void FakePlaySoundA()                     { __asm { jmp [Proxy::OriginalPlaySoundA] } }
__declspec(naked) void FakePlaySoundW()                     { __asm { jmp [Proxy::OriginalPlaySoundW] } }
__declspec(naked) void FakeSendDriverMessage()              { __asm { jmp [Proxy::OriginalSendDriverMessage] } }
__declspec(naked) void FakeWOWAppExit()                     { __asm { jmp [Proxy::OriginalWOWAppExit] } }
__declspec(naked) void FakeauxGetDevCapsA()                 { __asm { jmp [Proxy::OriginalauxGetDevCapsA] } }
__declspec(naked) void FakeauxGetDevCapsW()                 { __asm { jmp [Proxy::OriginalauxGetDevCapsW] } }
__declspec(naked) void FakeauxGetNumDevs()                  { __asm { jmp [Proxy::OriginalauxGetNumDevs] } }
__declspec(naked) void FakeauxGetVolume()                   { __asm { jmp [Proxy::OriginalauxGetVolume] } }
__declspec(naked) void FakeauxOutMessage()                  { __asm { jmp [Proxy::OriginalauxOutMessage] } }
__declspec(naked) void FakeauxSetVolume()                   { __asm { jmp [Proxy::OriginalauxSetVolume] } }
__declspec(naked) void FakejoyConfigChanged()               { __asm { jmp [Proxy::OriginaljoyConfigChanged] } }
__declspec(naked) void FakejoyGetDevCapsA()                 { __asm { jmp [Proxy::OriginaljoyGetDevCapsA] } }
__declspec(naked) void FakejoyGetDevCapsW()                 { __asm { jmp [Proxy::OriginaljoyGetDevCapsW] } }
__declspec(naked) void FakejoyGetNumDevs()                  { __asm { jmp [Proxy::OriginaljoyGetNumDevs] } }
__declspec(naked) void FakejoyGetPos()                      { __asm { jmp [Proxy::OriginaljoyGetPos] } }
__declspec(naked) void FakejoyGetPosEx()                    { __asm { jmp [Proxy::OriginaljoyGetPosEx] } }
__declspec(naked) void FakejoyGetThreshold()                { __asm { jmp [Proxy::OriginaljoyGetThreshold] } }
__declspec(naked) void FakejoyReleaseCapture()              { __asm { jmp [Proxy::OriginaljoyReleaseCapture] } }
__declspec(naked) void FakejoySetCapture()                  { __asm { jmp [Proxy::OriginaljoySetCapture] } }
__declspec(naked) void FakejoySetThreshold()                { __asm { jmp [Proxy::OriginaljoySetThreshold] } }
__declspec(naked) void FakemciDriverNotify()                { __asm { jmp [Proxy::OriginalmciDriverNotify] } }
__declspec(naked) void FakemciDriverYield()                 { __asm { jmp [Proxy::OriginalmciDriverYield] } }
__declspec(naked) void FakemciExecute()                     { __asm { jmp [Proxy::OriginalmciExecute] } }
__declspec(naked) void FakemciFreeCommandResource()         { __asm { jmp [Proxy::OriginalmciFreeCommandResource] } }
__declspec(naked) void FakemciGetCreatorTask()              { __asm { jmp [Proxy::OriginalmciGetCreatorTask] } }
__declspec(naked) void FakemciGetDeviceIDA()                { __asm { jmp [Proxy::OriginalmciGetDeviceIDA] } }
__declspec(naked) void FakemciGetDeviceIDFromElementIDA()   { __asm { jmp [Proxy::OriginalmciGetDeviceIDFromElementIDA] } }
__declspec(naked) void FakemciGetDeviceIDFromElementIDW()   { __asm { jmp [Proxy::OriginalmciGetDeviceIDFromElementIDW] } }
__declspec(naked) void FakemciGetDeviceIDW()                { __asm { jmp [Proxy::OriginalmciGetDeviceIDW] } }
__declspec(naked) void FakemciGetDriverData()               { __asm { jmp [Proxy::OriginalmciGetDriverData] } }
__declspec(naked) void FakemciGetErrorStringA()             { __asm { jmp [Proxy::OriginalmciGetErrorStringA] } }
__declspec(naked) void FakemciGetErrorStringW()             { __asm { jmp [Proxy::OriginalmciGetErrorStringW] } }
__declspec(naked) void FakemciGetYieldProc()                { __asm { jmp [Proxy::OriginalmciGetYieldProc] } }
__declspec(naked) void FakemciLoadCommandResource()         { __asm { jmp [Proxy::OriginalmciLoadCommandResource] } }
__declspec(naked) void FakemciSendCommandA()                { __asm { jmp [Proxy::OriginalmciSendCommandA] } }
__declspec(naked) void FakemciSendCommandW()                { __asm { jmp [Proxy::OriginalmciSendCommandW] } }
__declspec(naked) void FakemciSendStringA()                 { __asm { jmp [Proxy::OriginalmciSendStringA] } }
__declspec(naked) void FakemciSendStringW()                 { __asm { jmp [Proxy::OriginalmciSendStringW] } }
__declspec(naked) void FakemciSetDriverData()               { __asm { jmp [Proxy::OriginalmciSetDriverData] } }
__declspec(naked) void FakemciSetYieldProc()                { __asm { jmp [Proxy::OriginalmciSetYieldProc] } }
__declspec(naked) void FakemidiConnect()                    { __asm { jmp [Proxy::OriginalmidiConnect] } }
__declspec(naked) void FakemidiDisconnect()                 { __asm { jmp [Proxy::OriginalmidiDisconnect] } }
__declspec(naked) void FakemidiInAddBuffer()                { __asm { jmp [Proxy::OriginalmidiInAddBuffer] } }
__declspec(naked) void FakemidiInClose()                    { __asm { jmp [Proxy::OriginalmidiInClose] } }
__declspec(naked) void FakemidiInGetDevCapsA()              { __asm { jmp [Proxy::OriginalmidiInGetDevCapsA] } }
__declspec(naked) void FakemidiInGetDevCapsW()              { __asm { jmp [Proxy::OriginalmidiInGetDevCapsW] } }
__declspec(naked) void FakemidiInGetErrorTextA()            { __asm { jmp [Proxy::OriginalmidiInGetErrorTextA] } }
__declspec(naked) void FakemidiInGetErrorTextW()            { __asm { jmp [Proxy::OriginalmidiInGetErrorTextW] } }
__declspec(naked) void FakemidiInGetID()                    { __asm { jmp [Proxy::OriginalmidiInGetID] } }
__declspec(naked) void FakemidiInGetNumDevs()               { __asm { jmp [Proxy::OriginalmidiInGetNumDevs] } }
__declspec(naked) void FakemidiInMessage()                  { __asm { jmp [Proxy::OriginalmidiInMessage] } }
__declspec(naked) void FakemidiInOpen()                     { __asm { jmp [Proxy::OriginalmidiInOpen] } }
__declspec(naked) void FakemidiInPrepareHeader()            { __asm { jmp [Proxy::OriginalmidiInPrepareHeader] } }
__declspec(naked) void FakemidiInReset()                    { __asm { jmp [Proxy::OriginalmidiInReset] } }
__declspec(naked) void FakemidiInStart()                    { __asm { jmp [Proxy::OriginalmidiInStart] } }
__declspec(naked) void FakemidiInStop()                     { __asm { jmp [Proxy::OriginalmidiInStop] } }
__declspec(naked) void FakemidiInUnprepareHeader()          { __asm { jmp [Proxy::OriginalmidiInUnprepareHeader] } }
__declspec(naked) void FakemidiOutCacheDrumPatches()        { __asm { jmp [Proxy::OriginalmidiOutCacheDrumPatches] } }
__declspec(naked) void FakemidiOutCachePatches()            { __asm { jmp [Proxy::OriginalmidiOutCachePatches] } }
__declspec(naked) void FakemidiOutClose()                   { __asm { jmp [Proxy::OriginalmidiOutClose] } }
__declspec(naked) void FakemidiOutGetDevCapsA()             { __asm { jmp [Proxy::OriginalmidiOutGetDevCapsA] } }
__declspec(naked) void FakemidiOutGetDevCapsW()             { __asm { jmp [Proxy::OriginalmidiOutGetDevCapsW] } }
__declspec(naked) void FakemidiOutGetErrorTextA()           { __asm { jmp [Proxy::OriginalmidiOutGetErrorTextA] } }
__declspec(naked) void FakemidiOutGetErrorTextW()           { __asm { jmp [Proxy::OriginalmidiOutGetErrorTextW] } }
__declspec(naked) void FakemidiOutGetID()                   { __asm { jmp [Proxy::OriginalmidiOutGetID] } }
__declspec(naked) void FakemidiOutGetNumDevs()              { __asm { jmp [Proxy::OriginalmidiOutGetNumDevs] } }
__declspec(naked) void FakemidiOutGetVolume()               { __asm { jmp [Proxy::OriginalmidiOutGetVolume] } }
__declspec(naked) void FakemidiOutLongMsg()                 { __asm { jmp [Proxy::OriginalmidiOutLongMsg] } }
__declspec(naked) void FakemidiOutMessage()                 { __asm { jmp [Proxy::OriginalmidiOutMessage] } }
__declspec(naked) void FakemidiOutOpen()                    { __asm { jmp [Proxy::OriginalmidiOutOpen] } }
__declspec(naked) void FakemidiOutPrepareHeader()           { __asm { jmp [Proxy::OriginalmidiOutPrepareHeader] } }
__declspec(naked) void FakemidiOutReset()                   { __asm { jmp [Proxy::OriginalmidiOutReset] } }
__declspec(naked) void FakemidiOutSetVolume()               { __asm { jmp [Proxy::OriginalmidiOutSetVolume] } }
__declspec(naked) void FakemidiOutShortMsg()                { __asm { jmp [Proxy::OriginalmidiOutShortMsg] } }
__declspec(naked) void FakemidiOutUnprepareHeader()         { __asm { jmp [Proxy::OriginalmidiOutUnprepareHeader] } }
__declspec(naked) void FakemidiStreamClose()                { __asm { jmp [Proxy::OriginalmidiStreamClose] } }
__declspec(naked) void FakemidiStreamOpen()                 { __asm { jmp [Proxy::OriginalmidiStreamOpen] } }
__declspec(naked) void FakemidiStreamOut()                  { __asm { jmp [Proxy::OriginalmidiStreamOut] } }
__declspec(naked) void FakemidiStreamPause()                { __asm { jmp [Proxy::OriginalmidiStreamPause] } }
__declspec(naked) void FakemidiStreamPosition()             { __asm { jmp [Proxy::OriginalmidiStreamPosition] } }
__declspec(naked) void FakemidiStreamProperty()             { __asm { jmp [Proxy::OriginalmidiStreamProperty] } }
__declspec(naked) void FakemidiStreamRestart()              { __asm { jmp [Proxy::OriginalmidiStreamRestart] } }
__declspec(naked) void FakemidiStreamStop()                 { __asm { jmp [Proxy::OriginalmidiStreamStop] } }
__declspec(naked) void FakemixerClose()                     { __asm { jmp [Proxy::OriginalmixerClose] } }
__declspec(naked) void FakemixerGetControlDetailsA()        { __asm { jmp [Proxy::OriginalmixerGetControlDetailsA] } }
__declspec(naked) void FakemixerGetControlDetailsW()        { __asm { jmp [Proxy::OriginalmixerGetControlDetailsW] } }
__declspec(naked) void FakemixerGetDevCapsA()               { __asm { jmp [Proxy::OriginalmixerGetDevCapsA] } }
__declspec(naked) void FakemixerGetDevCapsW()               { __asm { jmp [Proxy::OriginalmixerGetDevCapsW] } }
__declspec(naked) void FakemixerGetID()                     { __asm { jmp [Proxy::OriginalmixerGetID] } }
__declspec(naked) void FakemixerGetLineControlsA()          { __asm { jmp [Proxy::OriginalmixerGetLineControlsA] } }
__declspec(naked) void FakemixerGetLineControlsW()          { __asm { jmp [Proxy::OriginalmixerGetLineControlsW] } }
__declspec(naked) void FakemixerGetLineInfoA()              { __asm { jmp [Proxy::OriginalmixerGetLineInfoA] } }
__declspec(naked) void FakemixerGetLineInfoW()              { __asm { jmp [Proxy::OriginalmixerGetLineInfoW] } }
__declspec(naked) void FakemixerGetNumDevs()                { __asm { jmp [Proxy::OriginalmixerGetNumDevs] } }
__declspec(naked) void FakemixerMessage()                   { __asm { jmp [Proxy::OriginalmixerMessage] } }
__declspec(naked) void FakemixerOpen()                      { __asm { jmp [Proxy::OriginalmixerOpen] } }
__declspec(naked) void FakemixerSetControlDetails()         { __asm { jmp [Proxy::OriginalmixerSetControlDetails] } }
__declspec(naked) void FakemmDrvInstall()                   { __asm { jmp [Proxy::OriginalmmDrvInstall] } }
__declspec(naked) void FakemmGetCurrentTask()               { __asm { jmp [Proxy::OriginalmmGetCurrentTask] } }
__declspec(naked) void FakemmTaskBlock()                    { __asm { jmp [Proxy::OriginalmmTaskBlock] } }
__declspec(naked) void FakemmTaskCreate()                   { __asm { jmp [Proxy::OriginalmmTaskCreate] } }
__declspec(naked) void FakemmTaskSignal()                   { __asm { jmp [Proxy::OriginalmmTaskSignal] } }
__declspec(naked) void FakemmTaskYield()                    { __asm { jmp [Proxy::OriginalmmTaskYield] } }
__declspec(naked) void FakemmioAdvance()                    { __asm { jmp [Proxy::OriginalmmioAdvance] } }
__declspec(naked) void FakemmioAscend()                     { __asm { jmp [Proxy::OriginalmmioAscend] } }
__declspec(naked) void FakemmioClose()                      { __asm { jmp [Proxy::OriginalmmioClose] } }
__declspec(naked) void FakemmioCreateChunk()                { __asm { jmp [Proxy::OriginalmmioCreateChunk] } }
__declspec(naked) void FakemmioDescend()                    { __asm { jmp [Proxy::OriginalmmioDescend] } }
__declspec(naked) void FakemmioFlush()                      { __asm { jmp [Proxy::OriginalmmioFlush] } }
__declspec(naked) void FakemmioGetInfo()                    { __asm { jmp [Proxy::OriginalmmioGetInfo] } }
__declspec(naked) void FakemmioInstallIOProcA()             { __asm { jmp [Proxy::OriginalmmioInstallIOProcA] } }
__declspec(naked) void FakemmioInstallIOProcW()             { __asm { jmp [Proxy::OriginalmmioInstallIOProcW] } }
__declspec(naked) void FakemmioOpenA()                      { __asm { jmp [Proxy::OriginalmmioOpenA] } }
__declspec(naked) void FakemmioOpenW()                      { __asm { jmp [Proxy::OriginalmmioOpenW] } }
__declspec(naked) void FakemmioRead()                       { __asm { jmp [Proxy::OriginalmmioRead] } }
__declspec(naked) void FakemmioRenameA()                    { __asm { jmp [Proxy::OriginalmmioRenameA] } }
__declspec(naked) void FakemmioRenameW()                    { __asm { jmp [Proxy::OriginalmmioRenameW] } }
__declspec(naked) void FakemmioSeek()                       { __asm { jmp [Proxy::OriginalmmioSeek] } }
__declspec(naked) void FakemmioSendMessage()                { __asm { jmp [Proxy::OriginalmmioSendMessage] } }
__declspec(naked) void FakemmioSetBuffer()                  { __asm { jmp [Proxy::OriginalmmioSetBuffer] } }
__declspec(naked) void FakemmioSetInfo()                    { __asm { jmp [Proxy::OriginalmmioSetInfo] } }
__declspec(naked) void FakemmioStringToFOURCCA()            { __asm { jmp [Proxy::OriginalmmioStringToFOURCCA] } }
__declspec(naked) void FakemmioStringToFOURCCW()            { __asm { jmp [Proxy::OriginalmmioStringToFOURCCW] } }
__declspec(naked) void FakemmioWrite()                      { __asm { jmp [Proxy::OriginalmmioWrite] } }
__declspec(naked) void FakemmsystemGetVersion()             { __asm { jmp [Proxy::OriginalmmsystemGetVersion] } }
__declspec(naked) void FakesndPlaySoundA()                  { __asm { jmp [Proxy::OriginalsndPlaySoundA] } }
__declspec(naked) void FakesndPlaySoundW()                  { __asm { jmp [Proxy::OriginalsndPlaySoundW] } }
__declspec(naked) void FaketimeBeginPeriod()                { __asm { jmp [Proxy::OriginaltimeBeginPeriod] } }
__declspec(naked) void FaketimeEndPeriod()                  { __asm { jmp [Proxy::OriginaltimeEndPeriod] } }
__declspec(naked) void FaketimeGetDevCaps()                 { __asm { jmp [Proxy::OriginaltimeGetDevCaps] } }
__declspec(naked) void FaketimeGetSystemTime()              { __asm { jmp [Proxy::OriginaltimeGetSystemTime] } }
__declspec(naked) void FaketimeGetTime()                    { __asm { jmp [Proxy::OriginaltimeGetTime] } }
__declspec(naked) void FaketimeKillEvent()                  { __asm { jmp [Proxy::OriginaltimeKillEvent] } }
__declspec(naked) void FaketimeSetEvent()                   { __asm { jmp [Proxy::OriginaltimeSetEvent] } }
__declspec(naked) void FakewaveInAddBuffer()                { __asm { jmp [Proxy::OriginalwaveInAddBuffer] } }
__declspec(naked) void FakewaveInClose()                    { __asm { jmp [Proxy::OriginalwaveInClose] } }
__declspec(naked) void FakewaveInGetDevCapsA()              { __asm { jmp [Proxy::OriginalwaveInGetDevCapsA] } }
__declspec(naked) void FakewaveInGetDevCapsW()              { __asm { jmp [Proxy::OriginalwaveInGetDevCapsW] } }
__declspec(naked) void FakewaveInGetErrorTextA()            { __asm { jmp [Proxy::OriginalwaveInGetErrorTextA] } }
__declspec(naked) void FakewaveInGetErrorTextW()            { __asm { jmp [Proxy::OriginalwaveInGetErrorTextW] } }
__declspec(naked) void FakewaveInGetID()                    { __asm { jmp [Proxy::OriginalwaveInGetID] } }
__declspec(naked) void FakewaveInGetNumDevs()               { __asm { jmp [Proxy::OriginalwaveInGetNumDevs] } }
__declspec(naked) void FakewaveInGetPosition()              { __asm { jmp [Proxy::OriginalwaveInGetPosition] } }
__declspec(naked) void FakewaveInMessage()                  { __asm { jmp [Proxy::OriginalwaveInMessage] } }
__declspec(naked) void FakewaveInOpen()                     { __asm { jmp [Proxy::OriginalwaveInOpen] } }
__declspec(naked) void FakewaveInPrepareHeader()            { __asm { jmp [Proxy::OriginalwaveInPrepareHeader] } }
__declspec(naked) void FakewaveInReset()                    { __asm { jmp [Proxy::OriginalwaveInReset] } }
__declspec(naked) void FakewaveInStart()                    { __asm { jmp [Proxy::OriginalwaveInStart] } }
__declspec(naked) void FakewaveInStop()                     { __asm { jmp [Proxy::OriginalwaveInStop] } }
__declspec(naked) void FakewaveInUnprepareHeader()          { __asm { jmp [Proxy::OriginalwaveInUnprepareHeader] } }
__declspec(naked) void FakewaveOutBreakLoop()               { __asm { jmp [Proxy::OriginalwaveOutBreakLoop] } }
__declspec(naked) void FakewaveOutClose()                   { __asm { jmp [Proxy::OriginalwaveOutClose] } }
__declspec(naked) void FakewaveOutGetDevCapsA()             { __asm { jmp [Proxy::OriginalwaveOutGetDevCapsA] } }
__declspec(naked) void FakewaveOutGetDevCapsW()             { __asm { jmp [Proxy::OriginalwaveOutGetDevCapsW] } }
__declspec(naked) void FakewaveOutGetErrorTextA()           { __asm { jmp [Proxy::OriginalwaveOutGetErrorTextA] } }
__declspec(naked) void FakewaveOutGetErrorTextW()           { __asm { jmp [Proxy::OriginalwaveOutGetErrorTextW] } }
__declspec(naked) void FakewaveOutGetID()                   { __asm { jmp [Proxy::OriginalwaveOutGetID] } }
__declspec(naked) void FakewaveOutGetNumDevs()              { __asm { jmp [Proxy::OriginalwaveOutGetNumDevs] } }
__declspec(naked) void FakewaveOutGetPitch()                { __asm { jmp [Proxy::OriginalwaveOutGetPitch] } }
__declspec(naked) void FakewaveOutGetPlaybackRate()         { __asm { jmp [Proxy::OriginalwaveOutGetPlaybackRate] } }
__declspec(naked) void FakewaveOutGetPosition()             { __asm { jmp [Proxy::OriginalwaveOutGetPosition] } }
__declspec(naked) void FakewaveOutGetVolume()               { __asm { jmp [Proxy::OriginalwaveOutGetVolume] } }
__declspec(naked) void FakewaveOutMessage()                 { __asm { jmp [Proxy::OriginalwaveOutMessage] } }
__declspec(naked) void FakewaveOutOpen()                    { __asm { jmp [Proxy::OriginalwaveOutOpen] } }
__declspec(naked) void FakewaveOutPause()                   { __asm { jmp [Proxy::OriginalwaveOutPause] } }
__declspec(naked) void FakewaveOutPrepareHeader()           { __asm { jmp [Proxy::OriginalwaveOutPrepareHeader] } }
__declspec(naked) void FakewaveOutReset()                   { __asm { jmp [Proxy::OriginalwaveOutReset] } }
__declspec(naked) void FakewaveOutRestart()                 { __asm { jmp [Proxy::OriginalwaveOutRestart] } }
__declspec(naked) void FakewaveOutSetPitch()                { __asm { jmp [Proxy::OriginalwaveOutSetPitch] } }
__declspec(naked) void FakewaveOutSetPlaybackRate()         { __asm { jmp [Proxy::OriginalwaveOutSetPlaybackRate] } }
__declspec(naked) void FakewaveOutSetVolume()               { __asm { jmp [Proxy::OriginalwaveOutSetVolume] } }
__declspec(naked) void FakewaveOutUnprepareHeader()         { __asm { jmp [Proxy::OriginalwaveOutUnprepareHeader] } }
__declspec(naked) void FakewaveOutWrite()                   { __asm { jmp [Proxy::OriginalwaveOutWrite] } }
