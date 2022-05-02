#include "pch.h"

MemoryUnprotector::MemoryUnprotector(void* ptr, int length)
{
    _ptr = ptr;
    _length = length;
    VirtualProtect(_ptr, _length, PAGE_READWRITE, &_oldProtect);
}

MemoryUnprotector::~MemoryUnprotector()
{
    DWORD dummy;
    VirtualProtect(_ptr, _length, _oldProtect, &dummy);
}
