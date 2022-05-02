#include "pch.h"

using namespace std;

void* MemoryUtil::FindData(const void* pHaystack, int haystackLength, const void* pNeedle, int needleLength)
{
    BYTE* pTest = (BYTE*)pHaystack;
    BYTE* pEnd = pTest + haystackLength - needleLength;
    for (; pTest <= pEnd; pTest++)
    {
        if (memcmp(pTest, pNeedle, needleLength) == 0)
            return pTest;
    }
    return nullptr;
}

void* MemoryUtil::FindData(const void* pHaystack, int haystackLength, const void* pNeedle, const void* pNeedleMask, int needleLength)
{
    BYTE* pTest = (BYTE*)pHaystack;
    BYTE* pEnd = pTest + haystackLength - needleLength;
    for (; pTest <= pEnd; pTest++)
    {
        int offset = 0;
        bool failed = false;
        for (; offset < (needleLength & ~3); offset += 4)
        {
            DWORD test   = *(DWORD*)(pTest + offset);
            DWORD needle = *(DWORD*)((BYTE*)pNeedle + offset);
            DWORD mask   = *(DWORD*)((BYTE*)pNeedleMask + offset);
            if ((test & mask) != needle)
            {
                failed = true;
                break;
            }
        }
        if (failed)
            continue;

        for (; offset < needleLength; offset++)
        {
            BYTE test   = pTest[offset];
            BYTE needle = *((BYTE*)pNeedle + offset);
            BYTE mask   = *((BYTE*)pNeedleMask + offset);
            if ((test & mask) != needle)
            {
                failed = true;
                break;
            }
        }
        if (failed)
            continue;

        return pTest;
    }
    return nullptr;
}

void MemoryUtil::WritePointer(void** ptr, void* value)
{
    MemoryUnprotector unprotector(ptr, sizeof(value));
    *ptr = value;
}
