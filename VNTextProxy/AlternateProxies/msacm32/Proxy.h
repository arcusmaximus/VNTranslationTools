#pragma once

class Proxy
{
public:
    static void Init(HMODULE hProxy);

    static inline HMODULE ProxyModuleHandle{};
    static inline HMODULE OriginalModuleHandle{};
    
    static inline void*                                 OriginalXRegThunkEntry{};
    static inline decltype(acmDriverAddA)*              OriginalacmDriverAddA{};
    static inline decltype(acmDriverAddW)*              OriginalacmDriverAddW{};
    static inline decltype(acmDriverClose)*             OriginalacmDriverClose{};
    static inline decltype(acmDriverDetailsA)*          OriginalacmDriverDetailsA{};
    static inline decltype(acmDriverDetailsW)*          OriginalacmDriverDetailsW{};
    static inline decltype(acmDriverEnum)*              OriginalacmDriverEnum{};
    static inline decltype(acmDriverID)*                OriginalacmDriverID{};
    static inline decltype(acmDriverMessage)*           OriginalacmDriverMessage{};
    static inline decltype(acmDriverOpen)*              OriginalacmDriverOpen{};
    static inline decltype(acmDriverPriority)*          OriginalacmDriverPriority{};
    static inline decltype(acmDriverRemove)*            OriginalacmDriverRemove{};
    static inline decltype(acmFilterChooseA)*           OriginalacmFilterChooseA{};
    static inline decltype(acmFilterChooseW)*           OriginalacmFilterChooseW{};
    static inline decltype(acmFilterDetailsA)*          OriginalacmFilterDetailsA{};
    static inline decltype(acmFilterDetailsW)*          OriginalacmFilterDetailsW{};
    static inline decltype(acmFilterEnumA)*             OriginalacmFilterEnumA{};
    static inline decltype(acmFilterEnumW)*             OriginalacmFilterEnumW{};
    static inline decltype(acmFilterTagDetailsA)*       OriginalacmFilterTagDetailsA{};
    static inline decltype(acmFilterTagDetailsW)*       OriginalacmFilterTagDetailsW{};
    static inline decltype(acmFilterTagEnumA)*          OriginalacmFilterTagEnumA{};
    static inline decltype(acmFilterTagEnumW)*          OriginalacmFilterTagEnumW{};
    static inline decltype(acmFormatChooseA)*           OriginalacmFormatChooseA{};
    static inline decltype(acmFormatChooseW)*           OriginalacmFormatChooseW{};
    static inline decltype(acmFormatDetailsA)*          OriginalacmFormatDetailsA{};
    static inline decltype(acmFormatDetailsW)*          OriginalacmFormatDetailsW{};
    static inline decltype(acmFormatEnumA)*             OriginalacmFormatEnumA{};
    static inline decltype(acmFormatEnumW)*             OriginalacmFormatEnumW{};
    static inline decltype(acmFormatSuggest)*           OriginalacmFormatSuggest{};
    static inline decltype(acmFormatTagDetailsA)*       OriginalacmFormatTagDetailsA{};
    static inline decltype(acmFormatTagDetailsW)*       OriginalacmFormatTagDetailsW{};
    static inline decltype(acmFormatTagEnumA)*          OriginalacmFormatTagEnumA{};
    static inline decltype(acmFormatTagEnumW)*          OriginalacmFormatTagEnumW{};
    static inline decltype(acmGetVersion)*              OriginalacmGetVersion{};
    static inline void*                                 OriginalacmMessage32{};
    static inline decltype(acmMetrics)*                 OriginalacmMetrics{};
    static inline decltype(acmStreamClose)*             OriginalacmStreamClose{};
    static inline decltype(acmStreamConvert)*           OriginalacmStreamConvert{};
    static inline decltype(acmStreamMessage)*           OriginalacmStreamMessage{};
    static inline decltype(acmStreamOpen)*              OriginalacmStreamOpen{};
    static inline decltype(acmStreamPrepareHeader)*     OriginalacmStreamPrepareHeader{};
    static inline decltype(acmStreamReset)*             OriginalacmStreamReset{};
    static inline decltype(acmStreamSize)*              OriginalacmStreamSize{};
    static inline decltype(acmStreamUnprepareHeader)*   OriginalacmStreamUnprepareHeader{};
};
