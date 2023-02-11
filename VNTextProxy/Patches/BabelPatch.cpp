#include "pch.h"

// Hooks the "Babel" text transcoding library to add SJIS tunneling support
// See https://github.com/luaforge/lldebug/blob/master/lldebug/extralib/babel/

void BabelPatch::Apply()
{
    void** pVtable = CompilerHelper::FindVTable("babel::sjis_cp932_to_unicode_engine");
    if (pVtable == nullptr)
        return;

    MemoryUtil::WritePointer(&pVtable[1], Hook_Translate);
}

__declspec(naked) void BabelPatch::Hook_Translate()
{
    __asm
    {
        push ecx
        call Handle_Translate
        add esp, 4
        ret
    }
}

void BabelPatch::Handle_Translate(bbl_translate_engine* pEngine)
{
    pEngine->translated_buffer = SjisTunnelEncoding::Decode(pEngine->untranslated_buffer);
    pEngine->untranslated_buffer.clear();
}
