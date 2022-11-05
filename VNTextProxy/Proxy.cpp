#include "pch.h"

void Proxy::Init(HMODULE hProxy)
{
    ProxyModuleHandle = hProxy;

    wchar_t path[MAX_PATH];
    GetSystemDirectory(path, MAX_PATH);
    wcscat_s(path, L"\\d2d1.dll");
    OriginalModuleHandle = LoadLibrary(path);
    if (OriginalModuleHandle == nullptr)
    {
        MessageBox(nullptr, L"Cannot load original D2D1 library", L"Proxy", MB_ICONERROR);
        ExitProcess(0);
    }

#define RESOLVE(fn) Original##fn = reinterpret_cast<decltype(Original##fn)>(GetProcAddress(OriginalModuleHandle, #fn))

    RESOLVE(D2D1ComputeMaximumScaleFactor);
    RESOLVE(D2D1ConvertColorSpace);
    RESOLVE(D2D1CreateDevice);
    RESOLVE(D2D1CreateDeviceContext);
    RESOLVE(D2D1CreateFactory);
    RESOLVE(D2D1GetGradientMeshInteriorPointsFromCoonsPatch);
    RESOLVE(D2D1InvertMatrix);
    RESOLVE(D2D1IsMatrixInvertible);
    RESOLVE(D2D1MakeRotateMatrix);
    RESOLVE(D2D1MakeSkewMatrix);
    RESOLVE(D2D1SinCos);
    RESOLVE(D2D1Tan);
    RESOLVE(D2D1Vec3Length);

#undef RESOLVE
}

__declspec(naked) void FakeD2D1ComputeMaximumScaleFactor()  { _asm { jmp [Proxy::OriginalD2D1ComputeMaximumScaleFactor] } }
__declspec(naked) void FakeD2D1ConvertColorSpace()          { _asm { jmp [Proxy::OriginalD2D1ConvertColorSpace] } }
__declspec(naked) void FakeD2D1CreateDevice()               { _asm { jmp [Proxy::OriginalD2D1CreateDevice] } }
__declspec(naked) void FakeD2D1CreateDeviceContext()        { _asm { jmp [Proxy::OriginalD2D1CreateDeviceContext] } }
__declspec(naked) void FakeD2D1CreateFactory()              { _asm { jmp [Proxy::OriginalD2D1CreateFactory] } }
__declspec(naked) void FakeD2D1GetGradientMeshInteriorPointsFromCoonsPatch() { _asm { jmp [Proxy::OriginalD2D1GetGradientMeshInteriorPointsFromCoonsPatch] } }
__declspec(naked) void FakeD2D1InvertMatrix()               { _asm { jmp [Proxy::OriginalD2D1InvertMatrix] } }
__declspec(naked) void FakeD2D1IsMatrixInvertible()         { _asm { jmp [Proxy::OriginalD2D1IsMatrixInvertible] } }
__declspec(naked) void FakeD2D1MakeRotateMatrix()           { _asm { jmp [Proxy::OriginalD2D1MakeRotateMatrix] } }
__declspec(naked) void FakeD2D1MakeSkewMatrix()             { _asm { jmp [Proxy::OriginalD2D1MakeSkewMatrix] } }
__declspec(naked) void FakeD2D1SinCos()                     { _asm { jmp [Proxy::OriginalD2D1SinCos] } }
__declspec(naked) void FakeD2D1Tan()                        { _asm { jmp [Proxy::OriginalD2D1Tan] } }
__declspec(naked) void FakeD2D1Vec3Length()                 { _asm { jmp [Proxy::OriginalD2D1Vec3Length] } }
