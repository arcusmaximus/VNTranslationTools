# VNTextPatch
A tool for extracting original text from, and patching translated text into, a variety of visual novel formats. Currently the following engines are supported:
* ArcGameEngine (.bin)
* Artemis (.txt; append `--format=artemis` to command line)
* Buriko General Interpreter/Ethornell (no file extension; rename to .bgi or append `--format=ethornell` to command line)
* CatSystem2 (.cst)
* Cyberworks C,system (no file extension; rename to .csa or append `--format=csystem` to command line)
* Kirikiri (.ks/.scn)
* Majiro (.mjo)
* Musica (.sc)
* Propeller/Stuff Script Engine (.msc)
* RealLive (.rl)
* Ren'Py (.rpy)
* ShSystem (.hst)
* Silky's/AI6WIN (.mes)
* Qlie (.s)
* SystemNNN (.nnn/.spt)
* WillPlus AdvHD (.ws2)
* YU-RIS (.ybn)

The tool can extract text into Excel spreadsheets (.xlsx), and reinsert text from Excel or Google Docs spreadsheets.

The command line syntax is as follows:

```
VNTextPatch extractlocal <folder containing original game files> script.xlsx
VNTextPatch insertlocal <folder containing original game files> script.xlsx <folder to receive patched game files>
VNTextPatch insertgdocs <folder containing original game files> <Google Docs spreadsheet identifier> <folder to receive patched game files>
```

The input folder should only contain the original scenario files. If it contains files of another format, VNTextPatch may not be able to determine the input format to use.

Depending on the game, some customization in the tool's source code may be needed. For example, Kirikiri .ks files have no uniform way of indicating character names in dialogue lines, so you may need to extend KirikiriScript.cs to make the tool aware of the method your specific game uses.

To use `insertgdocs`, you need either a Google API key or a Google service account. Both can be created and managed on the [Google Cloud Dashboard](https://console.cloud.google.com/apis/credentials).
* If you want to use an API key, open VNTextPatch.exe.config and paste the key in the entry called "GoogleApiKey".
* If you want to use a service account, place the account's private key in a file called "google-service-account.json" next to VNTextPatch.exe.

The Google Spreadsheet API needs to be enabled for the API key/service account.

## Character name translation
After running `extractlocal`, VNTextPatch will populate a file called names.xml with all the character names it encountered. If you add translations for these names inside this file and run `extractlocal` again, the newly extracted spreadsheet will have the translated names prefilled as a convenience.

## Word wrapping
Most visual novel engines do not support automatic word wrapping. While it's sometimes possible to change their code to add this support, it's generally easier to "precalculate" the word wrapping by adding explicit line breaks in the patched game files. VNTextPatch can add these line breaks automatically for both monospace fonts (MonospaceWordWrapper.cs) and proportional fonts (ProportionalWordWrapper.cs). Both classes can be configured in VNTextPatch.exe.config. You may need to adapt the tool's source code to make it use the wrapper you want.

## Shift JIS extension
Many visual novel engines use Shift JIS (more specifically Microsoft code page 932) to encode text, meaning they can't display characters not supported by this code page. Even in English, though, there are words such as "café" that contain such unsupported characters.

VNTextPatch offers a facility for solving this problem. When patching a translation into a game file, it'll detect unsupported characters, replace them by unused Shift JIS code points (so they'll be accepted by the game), and store the mapping from the unused code point to the original character inside a separate file called "sjis_ext.bin". The proxy DLL (described in the section below) will then read this file, and whenever the game renders text on screen, replace any unused code points by their corresponding original character.

This approach makes it possible to have a large number of otherwise unsupported characters - enough to, say, translate a Shift JIS game to simplified Chinese.

By default, sjis_ext.bin is created inside the output folder, next to the patched game files. You can pass a fourth argument to `insertlocal` and `insertgdocs` to specify an alternative path (which should include the file name). The proxy DLL expects the file to be in the same folder as the game .exe.

# VNTextProxy
A proxy d2d1.dll that hooks into the game and helps with making it display non-Japanese text. It can do the following:
* Detect whether the user's machine is using the Japanese locale, and if not, relaunch the game using [Locale Emulator](https://github.com/xupefei/Locale-Emulator). This is useful for games that crash on non-Japanese systems. Note that you need to bundle LoaderDll.dll and LocaleEmulator.dll if you intend to use this feature.
* Load a custom font (.ttf/.ttc/.otf) from the game's folder.
* Catch calls to CreateFontA(), CreateFontIndirectA() and IDWriteFactory::CreateTextFormat() and make them use this custom font instead.
* Catch calls to TextOutA() and ID2D1RenderTarget::DrawText() to correctly position characters when using a proportional font (as many visual novel engines only do monospace).
* Restore non-Shift JIS characters using sjis_ext.bin (see previous section).

If the game doesn't reference d2d1.dll, you can use the files from one of the "AlternateProxies" subfolders to turn the DLL into a proxy for, say, version.dll. If the game doesn't reference any of the provided proxies, you can use [DLLProxyGenerator](https://github.com/nitrog0d/DLLProxyGenerator/releases/tag/v1.0.0) to make your own.

The source code comes with an empty "EnginePatches" class where you can add game-specific hooks. Microsoft's [Detours](https://github.com/microsoft/Detours) library is already included.

# Other VN tools
Depending on your game's engine, you may also be interested in these other tools:
* [CSystemTools](https://github.com/arcusmaximus/CSystemTools)
* [EthornellTools](https://github.com/arcusmaximus/EthornellTools)
* [KirikiriTools](https://github.com/arcusmaximus/KirikiriTools)
