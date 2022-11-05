#pragma once

class SubtitleLine
{
public:
    SubtitleLine(int startTime, int endTime);

    int StartTime;
    int EndTime;
    std::wstring Text;
};
