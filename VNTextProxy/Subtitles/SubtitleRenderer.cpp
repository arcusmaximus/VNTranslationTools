#include "pch.h"

using namespace std;

constexpr int SubtitleAreaY = 50;
constexpr int SubtitleAreaWidth = 500;
constexpr int SubtitleAreaHeight = 100;
constexpr int SubtitleFadeDuration = 1000;

SubtitleRenderer::GdiPlusInitializer::GdiPlusInitializer()
{
    Gdiplus::GdiplusStartupInput input;
    Gdiplus::GdiplusStartup(&_gdiPlusToken, &input, nullptr);
}

SubtitleRenderer::GdiPlusInitializer::~GdiPlusInitializer()
{
    if (FontCollection != nullptr)
        delete FontCollection;

    Gdiplus::GdiplusShutdown(_gdiPlusToken);
}

void SubtitleRenderer::PlayFromResource(const wchar_t* type, const wchar_t* name)
{
    if (Playing)
        Stop();

    Document.LoadFromResource(type, name);
    Playing = true;
    StartTime = GetTime();
    CurrentLineIdx = -1;
}

void SubtitleRenderer::Stop()
{
    if (!Playing)
        return;

    Playing = false;
    StartTime = 0;
    CurrentLineIdx = -1;
    Document.Unload();
    DeleteCurrentLineBitmap();
}

void SubtitleRenderer::Render(BYTE* pScreenBuffer, int screenWidth)
{
    if (!Playing)
        return;

    DWORD time = GetTime() - StartTime;
    UpdateCurrentLine(time);
    if (CurrentLineIdx < 0)
        return;

    SubtitleLine& line = Document.Lines[CurrentLineIdx];
    int overallAlpha;
    if (time < line.StartTime + SubtitleFadeDuration)
        overallAlpha = (time - line.StartTime) * 255 / SubtitleFadeDuration;
    else if (time < line.EndTime - SubtitleFadeDuration)
        overallAlpha = 255;
    else if (time < line.EndTime)
        overallAlpha = (line.EndTime - time) * 255 / SubtitleFadeDuration;
    else
        return;

    int boxX = (screenWidth - (int)CurrentLineBoundingBox.Width) / 2;
    int boxY = SubtitleAreaY + (int)CurrentLineBoundingBox.Y;
    int boxWidth = (int)CurrentLineBoundingBox.Width;
    int boxHeight = (int)CurrentLineBoundingBox.Height;
    BYTE* pScreenBufferRow = pScreenBuffer + ((boxY * screenWidth) + boxX) * 4;
    BYTE* pBitmapRow = (BYTE*)CurrentLineBitmapData.Scan0 + (int)CurrentLineBoundingBox.Y * CurrentLineBitmapData.Stride + (int)CurrentLineBoundingBox.X * 4;
    for (int y = 0; y < boxHeight; y++)
    {
        for (int x = 0; x < boxWidth; x++)
        {
            int pixelAlpha = pBitmapRow[x * 4 + 3] * overallAlpha / 255;
            for (int i = 0; i < 3; i++)
            {
                pScreenBufferRow[x * 4 + i] = (pScreenBufferRow[x * 4 + i] * (255 - pixelAlpha) + pBitmapRow[x * 4 + i] * pixelAlpha) / 255;
            }
        }
        pScreenBufferRow += screenWidth * 4;
        pBitmapRow += CurrentLineBitmapData.Stride;
    }
}

void SubtitleRenderer::UpdateCurrentLine(DWORD time)
{
    if (time >= Document.Lines.back().EndTime)
    {
        Stop();
        return;
    }

    bool lineChanged = false;
    while (CurrentLineIdx + 1 < Document.Lines.size() && Document.Lines[CurrentLineIdx + 1].StartTime <= time)
    {
        CurrentLineIdx++;
        lineChanged = true;
    }

    if (!lineChanged)
        return;

    CreateCurrentLineBitmap();
}

void SubtitleRenderer::CreateCurrentLineBitmap()
{
    if (CurrentLineBitmap != nullptr)
        DeleteCurrentLineBitmap();

    if (Initializer.FontCollection == nullptr)
    {
        Initializer.FontCollection = new Gdiplus::PrivateFontCollection();
        if (!Proportionalizer::CustomFontFilePath.empty())
            Initializer.FontCollection->AddFontFile(Proportionalizer::CustomFontFilePath.c_str());
    }

    CurrentLineBitmap = new Gdiplus::Bitmap(SubtitleAreaWidth, SubtitleAreaHeight, PixelFormat32bppARGB);

    Gdiplus::Graphics graphics(CurrentLineBitmap);
    Gdiplus::Font font(
        Proportionalizer::LastFontName.c_str(),
        24,
        Gdiplus::FontStyleRegular,
        Gdiplus::UnitPixel,
        Initializer.FontCollection->GetFamilyCount() > 0 ? Initializer.FontCollection : nullptr
    );
    Gdiplus::RectF layoutRect(0, 0, SubtitleAreaWidth, SubtitleAreaHeight);
    Gdiplus::StringFormat format;
    format.SetAlignment(Gdiplus::StringAlignmentCenter);
    format.SetLineAlignment(Gdiplus::StringAlignmentCenter);
    Gdiplus::SolidBrush backgroundBrush(Gdiplus::Color(150, 0, 0, 0));
    Gdiplus::SolidBrush textBrush(Gdiplus::Color(255, 255, 255, 255));

    wstring& text = Document.Lines[CurrentLineIdx].Text;

    graphics.MeasureString(text.c_str(), -1, &font, layoutRect, &format, &CurrentLineBoundingBox);
    CurrentLineBoundingBox.Inflate(3, 3);
    graphics.FillRectangle(&backgroundBrush, CurrentLineBoundingBox);

    graphics.SetTextRenderingHint(Gdiplus::TextRenderingHintAntiAlias);
    graphics.DrawString(text.c_str(), -1, &font, layoutRect, &format, &textBrush);

    Gdiplus::Rect lockRect(0, 0, SubtitleAreaWidth, SubtitleAreaHeight);
    CurrentLineBitmap->LockBits(&lockRect, Gdiplus::ImageLockModeRead, PixelFormat32bppARGB, &CurrentLineBitmapData);
}

void SubtitleRenderer::DeleteCurrentLineBitmap()
{
    if (CurrentLineBitmap == nullptr)
        return;

    CurrentLineBitmap->UnlockBits(&CurrentLineBitmapData);
    delete CurrentLineBitmap;
    CurrentLineBitmap = nullptr;
}

DWORD SubtitleRenderer::GetTime()
{
    static decltype(timeGetTime)* pTimeGetTime = nullptr;

    if (pTimeGetTime == nullptr)
    {
#ifdef VNTEXTPROXY_WINMM
        pTimeGetTime = (decltype(timeGetTime)*)Proxy::OriginaltimeGetTime;
#else
        HMODULE hWinMM = LoadLibrary(L"winmm.dll");
        pTimeGetTime = (decltype(timeGetTime)*)GetProcAddress(hWinMM, "timeGetTime");
#endif
    }

    return pTimeGetTime();
}
