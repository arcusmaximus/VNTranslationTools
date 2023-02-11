#pragma once

class BabelPatch
{
public:
    static void Apply();

private:
    class bbl_translate_engine
    {
    public:
        virtual ~bbl_translate_engine() = 0;
        virtual void translate() = 0;
        virtual void flush() = 0;
        virtual void clear() = 0;

        int refcount;
        std::string untranslated_buffer;
        std::wstring translated_buffer;
    };

    static void Hook_Translate();
    static void Handle_Translate(bbl_translate_engine* pEngine);
};
