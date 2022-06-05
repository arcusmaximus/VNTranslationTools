@echo off

set DOTNET_CLI_TELEMETRY_OPTOUT=1

for /F "tokens=*" %%f in ('dir /B /AD /S bin') do rmdir /S /Q "%%f"
for /F "tokens=*" %%f in ('dir /B /AD /S obj') do rmdir /S /Q "%%f"
if exist Build rmdir /S /Q Build
if exist Debug rmdir /S /Q Debug
if exist Release rmdir /S /Q Release

mkdir Build
mkdir Build\VNTextPatch
mkdir Build\VNTextProxy

dotnet restore VNTextPatch\VNTextPatch.csproj /p:RuntimeIdentifiers=win
msbuild VNTextPatch\VNTextPatch.csproj /p:LangVersion=9 /p:AllowUnsafeBlocks=true /p:Platform=AnyCPU /p:Configuration=Release /p:OutputPath=..\Build\VNTextPatch\
del Build\VNTextPatch\FreeMote*.xml
del Build\VNTextPatch\*.pdb
del Build\VNTextPatch\*.txt

msbuild VNTextProxy\VNTextProxy.vcxproj /p:Platform=Win32 /p:Configuration=Release /p:TargetName=d2d1
copy /Y VNTextProxy\Release\d2d1.dll Build\VNTextProxy
rmdir /S /Q VNTextProxy\Release

for /D %%p in (VNTextProxy\AlternateProxies\*) do (
    copy /Y VNTextProxy\AlternateProxies\%%~np\*.* VNTextProxy
    msbuild VNTextProxy\VNTextProxy.vcxproj /p:Platform=Win32 /p:Configuration=Release /p:TargetName=%%~np
    copy /Y VNTextProxy\Release\%%~np.dll Build\VNTextProxy
    rmdir /S /Q VNTextProxy\Release
)
