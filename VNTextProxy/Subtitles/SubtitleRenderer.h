#pragma once

class SubtitleRenderer
{
public:
    static void PlayFromResource(const wchar_t* type, const wchar_t* name);
    static void Stop();

    static void Render(BYTE* pScreenBuffer, int screenWidth);

    static DWORD GetTime();

private:
    class GdiPlusInitializer
    {
    public:
        GdiPlusInitializer();
        ~GdiPlusInitializer();

        Gdiplus::PrivateFontCollection* FontCollection = nullptr;

    private:
        ULONG_PTR _gdiPlusToken = 0;
    };
    static inline GdiPlusInitializer Initializer{};

    static void UpdateCurrentLine(DWORD time);
    static void CreateCurrentLineBitmap();
    static void DeleteCurrentLineBitmap();
    

    static inline bool Playing = false;
    static inline DWORD StartTime = 0;
    static inline SubtitleDocument Document{};
    static inline int CurrentLineIdx = -1;
    static inline Gdiplus::Bitmap* CurrentLineBitmap = nullptr;
    static inline Gdiplus::BitmapData CurrentLineBitmapData{};
    static inline Gdiplus::RectF CurrentLineBoundingBox{};
};
