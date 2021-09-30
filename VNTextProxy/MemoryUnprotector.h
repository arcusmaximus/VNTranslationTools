#pragma once

class MemoryUnprotector
{
public:
    MemoryUnprotector(void* ptr, int length);
    ~MemoryUnprotector();

private:
    void* _ptr;
    int _length;
    DWORD _oldProtect;
};
