#include "pch.h"

void Proxy::Init()
{
    wchar_t realDllPath[MAX_PATH];
    GetSystemDirectory(realDllPath, MAX_PATH);
    wcscat_s(realDllPath, L"\\winmm.dll");
    HMODULE hDll = LoadLibrary(realDllPath);
    if (hDll == nullptr)
    {
        MessageBox(nullptr, L"Cannot load original winmm.dll library", L"Proxy", MB_ICONERROR);
        ExitProcess(0);
    }

#define RESOLVE(fn) Original##fn = GetProcAddress(hDll, #fn)
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

__declspec(naked) void fCloseDriver() { __asm { jmp [Proxy::OriginalCloseDriver] } }
__declspec(naked) void fDefDriverProc() { __asm { jmp [Proxy::OriginalDefDriverProc] } }
__declspec(naked) void fDriverCallback() { __asm { jmp [Proxy::OriginalDriverCallback] } }
__declspec(naked) void fDrvGetModuleHandle() { __asm { jmp [Proxy::OriginalDrvGetModuleHandle] } }
__declspec(naked) void fGetDriverModuleHandle() { __asm { jmp [Proxy::OriginalGetDriverModuleHandle] } }
__declspec(naked) void fOpenDriver() { __asm { jmp [Proxy::OriginalOpenDriver] } }
__declspec(naked) void fPlaySound() { __asm { jmp [Proxy::OriginalPlaySound] } }
__declspec(naked) void fPlaySoundA() { __asm { jmp [Proxy::OriginalPlaySoundA] } }
__declspec(naked) void fPlaySoundW() { __asm { jmp [Proxy::OriginalPlaySoundW] } }
__declspec(naked) void fSendDriverMessage() { __asm { jmp [Proxy::OriginalSendDriverMessage] } }
__declspec(naked) void fWOWAppExit() { __asm { jmp [Proxy::OriginalWOWAppExit] } }
__declspec(naked) void fauxGetDevCapsA() { __asm { jmp [Proxy::OriginalauxGetDevCapsA] } }
__declspec(naked) void fauxGetDevCapsW() { __asm { jmp [Proxy::OriginalauxGetDevCapsW] } }
__declspec(naked) void fauxGetNumDevs() { __asm { jmp [Proxy::OriginalauxGetNumDevs] } }
__declspec(naked) void fauxGetVolume() { __asm { jmp [Proxy::OriginalauxGetVolume] } }
__declspec(naked) void fauxOutMessage() { __asm { jmp [Proxy::OriginalauxOutMessage] } }
__declspec(naked) void fauxSetVolume() { __asm { jmp [Proxy::OriginalauxSetVolume] } }
__declspec(naked) void fjoyConfigChanged() { __asm { jmp [Proxy::OriginaljoyConfigChanged] } }
__declspec(naked) void fjoyGetDevCapsA() { __asm { jmp [Proxy::OriginaljoyGetDevCapsA] } }
__declspec(naked) void fjoyGetDevCapsW() { __asm { jmp [Proxy::OriginaljoyGetDevCapsW] } }
__declspec(naked) void fjoyGetNumDevs() { __asm { jmp [Proxy::OriginaljoyGetNumDevs] } }
__declspec(naked) void fjoyGetPos() { __asm { jmp [Proxy::OriginaljoyGetPos] } }
__declspec(naked) void fjoyGetPosEx() { __asm { jmp [Proxy::OriginaljoyGetPosEx] } }
__declspec(naked) void fjoyGetThreshold() { __asm { jmp [Proxy::OriginaljoyGetThreshold] } }
__declspec(naked) void fjoyReleaseCapture() { __asm { jmp [Proxy::OriginaljoyReleaseCapture] } }
__declspec(naked) void fjoySetCapture() { __asm { jmp [Proxy::OriginaljoySetCapture] } }
__declspec(naked) void fjoySetThreshold() { __asm { jmp [Proxy::OriginaljoySetThreshold] } }
__declspec(naked) void fmciDriverNotify() { __asm { jmp [Proxy::OriginalmciDriverNotify] } }
__declspec(naked) void fmciDriverYield() { __asm { jmp [Proxy::OriginalmciDriverYield] } }
__declspec(naked) void fmciExecute() { __asm { jmp [Proxy::OriginalmciExecute] } }
__declspec(naked) void fmciFreeCommandResource() { __asm { jmp [Proxy::OriginalmciFreeCommandResource] } }
__declspec(naked) void fmciGetCreatorTask() { __asm { jmp [Proxy::OriginalmciGetCreatorTask] } }
__declspec(naked) void fmciGetDeviceIDA() { __asm { jmp [Proxy::OriginalmciGetDeviceIDA] } }
__declspec(naked) void fmciGetDeviceIDFromElementIDA() { __asm { jmp [Proxy::OriginalmciGetDeviceIDFromElementIDA] } }
__declspec(naked) void fmciGetDeviceIDFromElementIDW() { __asm { jmp [Proxy::OriginalmciGetDeviceIDFromElementIDW] } }
__declspec(naked) void fmciGetDeviceIDW() { __asm { jmp [Proxy::OriginalmciGetDeviceIDW] } }
__declspec(naked) void fmciGetDriverData() { __asm { jmp [Proxy::OriginalmciGetDriverData] } }
__declspec(naked) void fmciGetErrorStringA() { __asm { jmp [Proxy::OriginalmciGetErrorStringA] } }
__declspec(naked) void fmciGetErrorStringW() { __asm { jmp [Proxy::OriginalmciGetErrorStringW] } }
__declspec(naked) void fmciGetYieldProc() { __asm { jmp [Proxy::OriginalmciGetYieldProc] } }
__declspec(naked) void fmciLoadCommandResource() { __asm { jmp [Proxy::OriginalmciLoadCommandResource] } }
__declspec(naked) void fmciSendCommandA() { __asm { jmp [Proxy::OriginalmciSendCommandA] } }
__declspec(naked) void fmciSendCommandW() { __asm { jmp [Proxy::OriginalmciSendCommandW] } }
__declspec(naked) void fmciSendStringA() { __asm { jmp [Proxy::OriginalmciSendStringA] } }
__declspec(naked) void fmciSendStringW() { __asm { jmp [Proxy::OriginalmciSendStringW] } }
__declspec(naked) void fmciSetDriverData() { __asm { jmp [Proxy::OriginalmciSetDriverData] } }
__declspec(naked) void fmciSetYieldProc() { __asm { jmp [Proxy::OriginalmciSetYieldProc] } }
__declspec(naked) void fmidiConnect() { __asm { jmp [Proxy::OriginalmidiConnect] } }
__declspec(naked) void fmidiDisconnect() { __asm { jmp [Proxy::OriginalmidiDisconnect] } }
__declspec(naked) void fmidiInAddBuffer() { __asm { jmp [Proxy::OriginalmidiInAddBuffer] } }
__declspec(naked) void fmidiInClose() { __asm { jmp [Proxy::OriginalmidiInClose] } }
__declspec(naked) void fmidiInGetDevCapsA() { __asm { jmp [Proxy::OriginalmidiInGetDevCapsA] } }
__declspec(naked) void fmidiInGetDevCapsW() { __asm { jmp [Proxy::OriginalmidiInGetDevCapsW] } }
__declspec(naked) void fmidiInGetErrorTextA() { __asm { jmp [Proxy::OriginalmidiInGetErrorTextA] } }
__declspec(naked) void fmidiInGetErrorTextW() { __asm { jmp [Proxy::OriginalmidiInGetErrorTextW] } }
__declspec(naked) void fmidiInGetID() { __asm { jmp [Proxy::OriginalmidiInGetID] } }
__declspec(naked) void fmidiInGetNumDevs() { __asm { jmp [Proxy::OriginalmidiInGetNumDevs] } }
__declspec(naked) void fmidiInMessage() { __asm { jmp [Proxy::OriginalmidiInMessage] } }
__declspec(naked) void fmidiInOpen() { __asm { jmp [Proxy::OriginalmidiInOpen] } }
__declspec(naked) void fmidiInPrepareHeader() { __asm { jmp [Proxy::OriginalmidiInPrepareHeader] } }
__declspec(naked) void fmidiInReset() { __asm { jmp [Proxy::OriginalmidiInReset] } }
__declspec(naked) void fmidiInStart() { __asm { jmp [Proxy::OriginalmidiInStart] } }
__declspec(naked) void fmidiInStop() { __asm { jmp [Proxy::OriginalmidiInStop] } }
__declspec(naked) void fmidiInUnprepareHeader() { __asm { jmp [Proxy::OriginalmidiInUnprepareHeader] } }
__declspec(naked) void fmidiOutCacheDrumPatches() { __asm { jmp [Proxy::OriginalmidiOutCacheDrumPatches] } }
__declspec(naked) void fmidiOutCachePatches() { __asm { jmp [Proxy::OriginalmidiOutCachePatches] } }
__declspec(naked) void fmidiOutClose() { __asm { jmp [Proxy::OriginalmidiOutClose] } }
__declspec(naked) void fmidiOutGetDevCapsA() { __asm { jmp [Proxy::OriginalmidiOutGetDevCapsA] } }
__declspec(naked) void fmidiOutGetDevCapsW() { __asm { jmp [Proxy::OriginalmidiOutGetDevCapsW] } }
__declspec(naked) void fmidiOutGetErrorTextA() { __asm { jmp [Proxy::OriginalmidiOutGetErrorTextA] } }
__declspec(naked) void fmidiOutGetErrorTextW() { __asm { jmp [Proxy::OriginalmidiOutGetErrorTextW] } }
__declspec(naked) void fmidiOutGetID() { __asm { jmp [Proxy::OriginalmidiOutGetID] } }
__declspec(naked) void fmidiOutGetNumDevs() { __asm { jmp [Proxy::OriginalmidiOutGetNumDevs] } }
__declspec(naked) void fmidiOutGetVolume() { __asm { jmp [Proxy::OriginalmidiOutGetVolume] } }
__declspec(naked) void fmidiOutLongMsg() { __asm { jmp [Proxy::OriginalmidiOutLongMsg] } }
__declspec(naked) void fmidiOutMessage() { __asm { jmp [Proxy::OriginalmidiOutMessage] } }
__declspec(naked) void fmidiOutOpen() { __asm { jmp [Proxy::OriginalmidiOutOpen] } }
__declspec(naked) void fmidiOutPrepareHeader() { __asm { jmp [Proxy::OriginalmidiOutPrepareHeader] } }
__declspec(naked) void fmidiOutReset() { __asm { jmp [Proxy::OriginalmidiOutReset] } }
__declspec(naked) void fmidiOutSetVolume() { __asm { jmp [Proxy::OriginalmidiOutSetVolume] } }
__declspec(naked) void fmidiOutShortMsg() { __asm { jmp [Proxy::OriginalmidiOutShortMsg] } }
__declspec(naked) void fmidiOutUnprepareHeader() { __asm { jmp [Proxy::OriginalmidiOutUnprepareHeader] } }
__declspec(naked) void fmidiStreamClose() { __asm { jmp [Proxy::OriginalmidiStreamClose] } }
__declspec(naked) void fmidiStreamOpen() { __asm { jmp [Proxy::OriginalmidiStreamOpen] } }
__declspec(naked) void fmidiStreamOut() { __asm { jmp [Proxy::OriginalmidiStreamOut] } }
__declspec(naked) void fmidiStreamPause() { __asm { jmp [Proxy::OriginalmidiStreamPause] } }
__declspec(naked) void fmidiStreamPosition() { __asm { jmp [Proxy::OriginalmidiStreamPosition] } }
__declspec(naked) void fmidiStreamProperty() { __asm { jmp [Proxy::OriginalmidiStreamProperty] } }
__declspec(naked) void fmidiStreamRestart() { __asm { jmp [Proxy::OriginalmidiStreamRestart] } }
__declspec(naked) void fmidiStreamStop() { __asm { jmp [Proxy::OriginalmidiStreamStop] } }
__declspec(naked) void fmixerClose() { __asm { jmp [Proxy::OriginalmixerClose] } }
__declspec(naked) void fmixerGetControlDetailsA() { __asm { jmp [Proxy::OriginalmixerGetControlDetailsA] } }
__declspec(naked) void fmixerGetControlDetailsW() { __asm { jmp [Proxy::OriginalmixerGetControlDetailsW] } }
__declspec(naked) void fmixerGetDevCapsA() { __asm { jmp [Proxy::OriginalmixerGetDevCapsA] } }
__declspec(naked) void fmixerGetDevCapsW() { __asm { jmp [Proxy::OriginalmixerGetDevCapsW] } }
__declspec(naked) void fmixerGetID() { __asm { jmp [Proxy::OriginalmixerGetID] } }
__declspec(naked) void fmixerGetLineControlsA() { __asm { jmp [Proxy::OriginalmixerGetLineControlsA] } }
__declspec(naked) void fmixerGetLineControlsW() { __asm { jmp [Proxy::OriginalmixerGetLineControlsW] } }
__declspec(naked) void fmixerGetLineInfoA() { __asm { jmp [Proxy::OriginalmixerGetLineInfoA] } }
__declspec(naked) void fmixerGetLineInfoW() { __asm { jmp [Proxy::OriginalmixerGetLineInfoW] } }
__declspec(naked) void fmixerGetNumDevs() { __asm { jmp [Proxy::OriginalmixerGetNumDevs] } }
__declspec(naked) void fmixerMessage() { __asm { jmp [Proxy::OriginalmixerMessage] } }
__declspec(naked) void fmixerOpen() { __asm { jmp [Proxy::OriginalmixerOpen] } }
__declspec(naked) void fmixerSetControlDetails() { __asm { jmp [Proxy::OriginalmixerSetControlDetails] } }
__declspec(naked) void fmmDrvInstall() { __asm { jmp [Proxy::OriginalmmDrvInstall] } }
__declspec(naked) void fmmGetCurrentTask() { __asm { jmp [Proxy::OriginalmmGetCurrentTask] } }
__declspec(naked) void fmmTaskBlock() { __asm { jmp [Proxy::OriginalmmTaskBlock] } }
__declspec(naked) void fmmTaskCreate() { __asm { jmp [Proxy::OriginalmmTaskCreate] } }
__declspec(naked) void fmmTaskSignal() { __asm { jmp [Proxy::OriginalmmTaskSignal] } }
__declspec(naked) void fmmTaskYield() { __asm { jmp [Proxy::OriginalmmTaskYield] } }
__declspec(naked) void fmmioAdvance() { __asm { jmp [Proxy::OriginalmmioAdvance] } }
__declspec(naked) void fmmioAscend() { __asm { jmp [Proxy::OriginalmmioAscend] } }
__declspec(naked) void fmmioClose() { __asm { jmp [Proxy::OriginalmmioClose] } }
__declspec(naked) void fmmioCreateChunk() { __asm { jmp [Proxy::OriginalmmioCreateChunk] } }
__declspec(naked) void fmmioDescend() { __asm { jmp [Proxy::OriginalmmioDescend] } }
__declspec(naked) void fmmioFlush() { __asm { jmp [Proxy::OriginalmmioFlush] } }
__declspec(naked) void fmmioGetInfo() { __asm { jmp [Proxy::OriginalmmioGetInfo] } }
__declspec(naked) void fmmioInstallIOProcA() { __asm { jmp [Proxy::OriginalmmioInstallIOProcA] } }
__declspec(naked) void fmmioInstallIOProcW() { __asm { jmp [Proxy::OriginalmmioInstallIOProcW] } }
__declspec(naked) void fmmioOpenA() { __asm { jmp [Proxy::OriginalmmioOpenA] } }
__declspec(naked) void fmmioOpenW() { __asm { jmp [Proxy::OriginalmmioOpenW] } }
__declspec(naked) void fmmioRead() { __asm { jmp [Proxy::OriginalmmioRead] } }
__declspec(naked) void fmmioRenameA() { __asm { jmp [Proxy::OriginalmmioRenameA] } }
__declspec(naked) void fmmioRenameW() { __asm { jmp [Proxy::OriginalmmioRenameW] } }
__declspec(naked) void fmmioSeek() { __asm { jmp [Proxy::OriginalmmioSeek] } }
__declspec(naked) void fmmioSendMessage() { __asm { jmp [Proxy::OriginalmmioSendMessage] } }
__declspec(naked) void fmmioSetBuffer() { __asm { jmp [Proxy::OriginalmmioSetBuffer] } }
__declspec(naked) void fmmioSetInfo() { __asm { jmp [Proxy::OriginalmmioSetInfo] } }
__declspec(naked) void fmmioStringToFOURCCA() { __asm { jmp [Proxy::OriginalmmioStringToFOURCCA] } }
__declspec(naked) void fmmioStringToFOURCCW() { __asm { jmp [Proxy::OriginalmmioStringToFOURCCW] } }
__declspec(naked) void fmmioWrite() { __asm { jmp [Proxy::OriginalmmioWrite] } }
__declspec(naked) void fmmsystemGetVersion() { __asm { jmp [Proxy::OriginalmmsystemGetVersion] } }
__declspec(naked) void fsndPlaySoundA() { __asm { jmp [Proxy::OriginalsndPlaySoundA] } }
__declspec(naked) void fsndPlaySoundW() { __asm { jmp [Proxy::OriginalsndPlaySoundW] } }
__declspec(naked) void ftimeBeginPeriod() { __asm { jmp [Proxy::OriginaltimeBeginPeriod] } }
__declspec(naked) void ftimeEndPeriod() { __asm { jmp [Proxy::OriginaltimeEndPeriod] } }
__declspec(naked) void ftimeGetDevCaps() { __asm { jmp [Proxy::OriginaltimeGetDevCaps] } }
__declspec(naked) void ftimeGetSystemTime() { __asm { jmp [Proxy::OriginaltimeGetSystemTime] } }
__declspec(naked) void ftimeGetTime() { __asm { jmp [Proxy::OriginaltimeGetTime] } }
__declspec(naked) void ftimeKillEvent() { __asm { jmp [Proxy::OriginaltimeKillEvent] } }
__declspec(naked) void ftimeSetEvent() { __asm { jmp [Proxy::OriginaltimeSetEvent] } }
__declspec(naked) void fwaveInAddBuffer() { __asm { jmp [Proxy::OriginalwaveInAddBuffer] } }
__declspec(naked) void fwaveInClose() { __asm { jmp [Proxy::OriginalwaveInClose] } }
__declspec(naked) void fwaveInGetDevCapsA() { __asm { jmp [Proxy::OriginalwaveInGetDevCapsA] } }
__declspec(naked) void fwaveInGetDevCapsW() { __asm { jmp [Proxy::OriginalwaveInGetDevCapsW] } }
__declspec(naked) void fwaveInGetErrorTextA() { __asm { jmp [Proxy::OriginalwaveInGetErrorTextA] } }
__declspec(naked) void fwaveInGetErrorTextW() { __asm { jmp [Proxy::OriginalwaveInGetErrorTextW] } }
__declspec(naked) void fwaveInGetID() { __asm { jmp [Proxy::OriginalwaveInGetID] } }
__declspec(naked) void fwaveInGetNumDevs() { __asm { jmp [Proxy::OriginalwaveInGetNumDevs] } }
__declspec(naked) void fwaveInGetPosition() { __asm { jmp [Proxy::OriginalwaveInGetPosition] } }
__declspec(naked) void fwaveInMessage() { __asm { jmp [Proxy::OriginalwaveInMessage] } }
__declspec(naked) void fwaveInOpen() { __asm { jmp [Proxy::OriginalwaveInOpen] } }
__declspec(naked) void fwaveInPrepareHeader() { __asm { jmp [Proxy::OriginalwaveInPrepareHeader] } }
__declspec(naked) void fwaveInReset() { __asm { jmp [Proxy::OriginalwaveInReset] } }
__declspec(naked) void fwaveInStart() { __asm { jmp [Proxy::OriginalwaveInStart] } }
__declspec(naked) void fwaveInStop() { __asm { jmp [Proxy::OriginalwaveInStop] } }
__declspec(naked) void fwaveInUnprepareHeader() { __asm { jmp [Proxy::OriginalwaveInUnprepareHeader] } }
__declspec(naked) void fwaveOutBreakLoop() { __asm { jmp [Proxy::OriginalwaveOutBreakLoop] } }
__declspec(naked) void fwaveOutClose() { __asm { jmp [Proxy::OriginalwaveOutClose] } }
__declspec(naked) void fwaveOutGetDevCapsA() { __asm { jmp [Proxy::OriginalwaveOutGetDevCapsA] } }
__declspec(naked) void fwaveOutGetDevCapsW() { __asm { jmp [Proxy::OriginalwaveOutGetDevCapsW] } }
__declspec(naked) void fwaveOutGetErrorTextA() { __asm { jmp [Proxy::OriginalwaveOutGetErrorTextA] } }
__declspec(naked) void fwaveOutGetErrorTextW() { __asm { jmp [Proxy::OriginalwaveOutGetErrorTextW] } }
__declspec(naked) void fwaveOutGetID() { __asm { jmp [Proxy::OriginalwaveOutGetID] } }
__declspec(naked) void fwaveOutGetNumDevs() { __asm { jmp [Proxy::OriginalwaveOutGetNumDevs] } }
__declspec(naked) void fwaveOutGetPitch() { __asm { jmp [Proxy::OriginalwaveOutGetPitch] } }
__declspec(naked) void fwaveOutGetPlaybackRate() { __asm { jmp [Proxy::OriginalwaveOutGetPlaybackRate] } }
__declspec(naked) void fwaveOutGetPosition() { __asm { jmp [Proxy::OriginalwaveOutGetPosition] } }
__declspec(naked) void fwaveOutGetVolume() { __asm { jmp [Proxy::OriginalwaveOutGetVolume] } }
__declspec(naked) void fwaveOutMessage() { __asm { jmp [Proxy::OriginalwaveOutMessage] } }
__declspec(naked) void fwaveOutOpen() { __asm { jmp [Proxy::OriginalwaveOutOpen] } }
__declspec(naked) void fwaveOutPause() { __asm { jmp [Proxy::OriginalwaveOutPause] } }
__declspec(naked) void fwaveOutPrepareHeader() { __asm { jmp [Proxy::OriginalwaveOutPrepareHeader] } }
__declspec(naked) void fwaveOutReset() { __asm { jmp [Proxy::OriginalwaveOutReset] } }
__declspec(naked) void fwaveOutRestart() { __asm { jmp [Proxy::OriginalwaveOutRestart] } }
__declspec(naked) void fwaveOutSetPitch() { __asm { jmp [Proxy::OriginalwaveOutSetPitch] } }
__declspec(naked) void fwaveOutSetPlaybackRate() { __asm { jmp [Proxy::OriginalwaveOutSetPlaybackRate] } }
__declspec(naked) void fwaveOutSetVolume() { __asm { jmp [Proxy::OriginalwaveOutSetVolume] } }
__declspec(naked) void fwaveOutUnprepareHeader() { __asm { jmp [Proxy::OriginalwaveOutUnprepareHeader] } }
__declspec(naked) void fwaveOutWrite() { __asm { jmp [Proxy::OriginalwaveOutWrite] } }