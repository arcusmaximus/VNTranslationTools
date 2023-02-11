#pragma once

class StringUtil
{
public:
    static std::wstring ToWString(const char* psz, int numBytes = -1, int codepage = 932);
    static std::wstring ToHalfWidth(const std::wstring& fullWidth);

    template<typename TChar>
    static std::vector<std::basic_string<TChar>> Split(const std::basic_string<TChar>& str, const std::basic_string<TChar>& delimiter)
    {
        std::vector<std::basic_string<TChar>> result;
        int start = 0;
        while (start < str.size())
        {
            int end = str.find(delimiter.c_str(), start);
            if (end < 0)
                end = str.size();

            result.push_back(str.substr(start, end - start));
            start = end + delimiter.size();
        }
        return result;
    }

    template<typename TChar>
    static std::basic_string<TChar> Join(const std::vector<std::basic_string<TChar>>& elements, const std::basic_string<TChar>& delimiter)
    {
        std::basic_string<TChar> result;
        for (int i = 0; i < elements.size(); i++)
        {
            if (i > 0)
                result += delimiter;

            result += elements[i];
        }
        return result;
    }
};
