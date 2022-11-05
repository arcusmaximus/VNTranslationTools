#include "pch.h"

using namespace std;

void SubtitleDocument::LoadFromResource(const wchar_t* type, const wchar_t* name)
{
    Unload();

    HMODULE hModule = Proxy::ProxyModuleHandle;
    HRSRC hResourceInfo = FindResourceW(hModule, name, type);
    if (hResourceInfo == nullptr)
        return;

    HGLOBAL hResourceData = LoadResource(hModule, hResourceInfo);
    void* pResourceData = LockResource(hResourceData);
    DWORD resourceSize = SizeofResource(hModule, hResourceInfo);

    membuf buf((char*)pResourceData, resourceSize);
    wbuffer_convert<codecvt_utf8_utf16<wchar_t>> wbuf(&buf);
    wistream stream(&wbuf, false);
    LoadFromStream(stream);

    FreeResource(hResourceData);
}

void SubtitleDocument::LoadFromStream(wistream& stream)
{
    wstring line;

    while (!stream.eof())
    {
        line = ReadLine(stream);
        int lineNumber;
        if (swscanf_s(line.c_str(), L"%d", &lineNumber) != 1)
            break;

        line = ReadLine(stream);
        int startHours;
        int startMinutes;
        int startSeconds;
        int startMilliseconds;
        int endHours;
        int endMinutes;
        int endSeconds;
        int endMilliseconds;
        int numTimestampFields = swscanf_s(
            line.c_str(),
            L"%d:%d:%d,%d --> %d:%d:%d,%d",
            &startHours,
            &startMinutes,
            &startSeconds,
            &startMilliseconds,
            &endHours,
            &endMinutes,
            &endSeconds,
            &endMilliseconds
        );
        if (numTimestampFields != 8)
            break;

        Lines.emplace_back(
            (((startHours * 60) + startMinutes) * 60 + startSeconds) * 1000 + startMilliseconds,
            (((endHours * 60) + endMinutes) * 60 + endSeconds) * 1000 + endMilliseconds
        );
        SubtitleLine& subtitle = Lines.back();
        while (true)
        {
            line = ReadLine(stream);
            if (line.empty())
                break;

            if (!subtitle.Text.empty())
                subtitle.Text.append(L"\r\n");

            subtitle.Text.append(line);
        }
    }
}

void SubtitleDocument::Unload()
{
    Lines.clear();
}

wstring SubtitleDocument::ReadLine(wistream& stream)
{
    wstring line;
    getline(stream, line);
    if (line.size() > 0 && line[0] == 0xFEFF)
        line.erase(0, 1);

    if (line.size() > 0 && line[line.size() - 1] == L'\r')
        line.erase(line.size() - 1);

    return line;
}
