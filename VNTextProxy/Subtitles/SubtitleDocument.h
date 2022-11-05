#pragma once

class SubtitleDocument
{
public:
    void LoadFromResource(const wchar_t* type, const wchar_t* name);
    void LoadFromStream(std::wistream& stream);
    void Unload();

    std::vector<SubtitleLine> Lines;

private:
    static std::wstring ReadLine(std::wistream& stream);
};
