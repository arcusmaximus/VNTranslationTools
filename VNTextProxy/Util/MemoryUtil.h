#pragma once

class MemoryUtil
{
public:
    static void*            FindData                (const void* pHaystack, int haystackLength, const void* pNeedle, int needleLength);
    static void*            FindData                (const void* pHaystack, int haystackLength, const void* pNeedle, const void* pNeedleMask, int needleLength);
    static void             WritePointer            (void** ptr, void* value);
};
