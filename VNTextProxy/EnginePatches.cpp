#include "pch.h"

void EnginePatches::Init()
{
    DetourTransactionBegin();
    
    DetourTransactionCommit();
}
