#include "pch.h"

void EnginePatches::Init()
{
    DetourTransactionBegin();

    BabelPatch::Apply();
    
    DetourTransactionCommit();
}
