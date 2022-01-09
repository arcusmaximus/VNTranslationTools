#pragma once

class Proxy
{
public:
    static void Init();

    static inline void *OriginalDirect3DCreate9{};
};
