# VNTextPatch
A tool for extracting original text from, and patching translated text into, a variety of visual novel formats. Currently the following engines are supported:

| Engine                               | Extension(s)   | Remarks                                                                        |
| ------------------------------------ | -------------- | ------------------------------------------------------------------------------ |
| AdvHD                                | .ws2           |                                                                                |
| ArcGameEngine                        | .bin           |                                                                                |
| Artemis                              | .asb/.ast/.txt | Append `--format=artemistxt` to command line for .txt                          |
| Buriko General Interpreter/Ethornell | (none)         | Append `--format=ethornell` to command line                                    |
| CatSystem2                           | .cst           |                                                                                |
| Cyberworks C,system                  | .a0            |                                                                                |
| KaGuYa                               |  message.dat   |                                                                                |
| Kirikiri                             |  .ks/.scn/.txt | Append `--format=kirikiriks` to command line for .txt                          |
| Majiro                               | .mjo           |                                                                                |
| Musica                               | .sc            |                                                                                |
| Mware                                | .nut           |                                                                                |
| Propeller/Stuff Script Engine        | .msc           |                                                                                |
| RealLive                             | .txt           | Append `--format=reallive` to command line                                     |
| Ren'Py                               | .rpy           |                                                                                |
| ShSystem                             | .hst           |                                                                                |
| Silky's/AI6WIN                       | .mes/.map      |                                                                                |
| Qlie                                 | .s             |                                                                                |
| Softpal                              | script.src     | Make sure text.dat and point.dat are available in the same folder              |
| SystemNNN                            | .nnn/.spt      |                                                                                |
| TmrHiroAdvSystem                     | .srp/(none)    | Append `--format=tmrhiroadvsystemtext` to command line in case of no extension |
| Whale                                | .txt           | Append `--format=whale` to command line                                        |
| YU-RIS                               | .ybn           |                                                                                |

The tool can extract text into Excel workbooks (.xlsx) or JSON files (.json), and reinsert text from Excel, JSON, or Google Docs Spreadsheets. Working with .xlsx files does not require Microsoft Excel to be installed.

The command line syntax is as follows:

```
Excel:
VNTextPatch extractlocal <folder containing original game files> script.xlsx
VNTextPatch insertlocal <folder containing original game files> script.xlsx <folder to receive patched game files>

JSON:
VNTextPatch extractlocal <folder containing original game files> <folder to receive .json files>
VNTextPatch insertlocal <folder containing original game files> <folder containing .json files> <folder to receive patched game files>

Google Documents:
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

VNTextPatch offers a facility called "SJIS tunneling" for solving this problem. When patching a translation into a game file, it'll detect unsupported characters, replace them by unused SJIS code points (so they'll be accepted by the game), and store the mapping from the unused code point to the original character inside a separate file called "sjis_ext.bin". The proxy DLL (described in the section below) will then read this file, and whenever the game renders text on screen, replace any unused code points by their corresponding original character.

This approach makes it possible to have a large number of otherwise unsupported characters - enough to, say, translate a SJIS game to simplified Chinese. There's no need for the classic approach of shipping a modified font with the glyphs of certain Japanese characters replaced by those of another language.

By default, sjis_ext.bin is created inside the output folder, next to the patched game files. You can pass a fourth argument to `insertlocal` and `insertgdocs` to specify an alternative path (which should include the file name). The proxy DLL expects the file to be in the same folder as the game .exe.

# VNTextProxy
A proxy d2d1.dll that hooks into the game and helps with making it display non-Japanese text. It can do the following:
* Render non-SJIS characters in SJIS-only games (see previous section).
* Load a custom font (.ttf/.ttc/.otf) from the game's folder and force the game to use it. The font file name should match the font name.
* Reposition rendered characters for correct text display with proportional fonts (as many visual novel engines only do monospace). This currently works for TextOutA() and ID2D1RenderTarget::DrawText().
* To a certain degree, make SJIS-only games Unicode compatible. This allows them not only to run on non-Japanese systems without crashing, but also to work with non-SJIS file paths and accept non-SJIS keyboard input (including from IMEs).
* If the above is not sufficient for preventing crashes, VNTextProxy can also automatically relaunch the game using [Locale Emulator](https://github.com/xupefei/Locale-Emulator) on non-Japanese systems. Users don't need to install the emulator for this; instead, you should bundle LoaderDll.dll and LocaleEmulator.dll with the game.

If the game doesn't reference d2d1.dll, you can use the files from one of the "AlternateProxies" subfolders to turn the DLL into a proxy for, say, version.dll. If the game doesn't reference any of the provided proxies, you can use [DLLProxyGenerator](https://github.com/nitrog0d/DLLProxyGenerator/releases/tag/v1.0.0) to make your own.

Apart from this, chances are you'll need to make some changes to the source code.
* In dllmain.cpp, you can uncomment the Locale Emulator relauncher and enable/disable GDI and Direct2D support as needed.
* In EnginePatches.cpp, you can add any game-specific hooks you might need. Microsoft's [Detours](https://github.com/microsoft/Detours) library is already included.

# Other VN tools
Depending on your game's engine, you may also be interested in these other tools:
* [CSystemTools](https://github.com/arcusmaximus/CSystemTools)
* [EthornellTools](https://github.com/arcusmaximus/EthornellTools)
* [KirikiriTools](https://github.com/arcusmaximus/KirikiriTools)
