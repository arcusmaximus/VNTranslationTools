@echo off

for /F "tokens=*" %%f in ('dir /B /AD /S bin') do rmdir /S /Q "%%f"
for /F "tokens=*" %%f in ('dir /B /AD /S obj') do rmdir /S /Q "%%f"
rmdir /S /Q Build
rmdir /S /Q Debug
rmdir /S /Q Release

mkdir Build
mkdir Build\VNTextPatch
mkdir Build\VNTextProxy

dotnet restore VNTextPatch\VNTextPatch.csproj /p:RuntimeIdentifiers=win
msbuild VNTextPatch\VNTextPatch.csproj /p:Configuration=Release /p:OutputPath=..\Build\VNTextPatch\
del Build\VNTextPatch\FreeMote*.xml
del Build\VNTextPatch\*.pdb
del Build\VNTextPatch\*.txt

for /D %%p in (VNTextProxy\AlternateProxies\*) do (
    copy /Y VNTextProxy\AlternateProxies\%%~np\*.* VNTextProxy
    msbuild VNTextProxy\VNTextProxy.vcxproj /p:Configuration=Release /p:TargetName=%%~np
    copy /Y VNTextProxy\Release\%%~np.dll Build\VNTextProxy
    rmdir /S /Q VNTextProxy\Release
)
