#pragma once

class CompilerHelper
{
public:
    static void         Init                        ();

    static void**       FindVTable                  (const std::string& className);
    static void**       FindVTable                  (HMODULE hModule, CompilerType compilerType, const std::string& className);

    static inline CompilerType CompilerType{};

private:
    static bool         HasBorlandTypeDescriptor    (void** pVTable, const std::string& className, void* pModuleStart, void* pModuleEnd);
    static bool         HasMsvcTypeDescriptor       (void** pVTable, const std::string& className, void* pModuleStart, void* pModuleEnd);
};
