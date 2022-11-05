#pragma once

using namespace std;

class membuf : public streambuf
{
public:
    membuf(char* pData, int length)
    {
        setg(pData, pData, pData + length);
    }
};
