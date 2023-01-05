using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FreeMote.Psb;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Kirikiri
{
    public class KirikiriScnScript : IScript
    {
        private const int LanguageIndex = 0;

        private static readonly Regex ControlCodeRegex = new Regex(
          @"    \\.        # Escape sequence
              | \[.+?\]    # Ruby text
              | %f.*?;     # Font
              | %b.        # Bold on/off
              | %i.        # Italic on/off
              | %s.        # Shadow on/off
              | %e.        # Edge on/off
              | %\d+;      # Font size
              | %B         # Big font size
              | %S         # Small font size
              | \#.*?;     # Color
              | %r         # Reset
              | %C         # Center-aligned
              | %R         # Right-aligned
              | %L         # Left-aligned
              | %p\d*;     # Pitch
              | %d\d*;     # Delay (percentage)
              | %w\d*;     # Wait (percentage)
              | %D\d*;     # Delay (ms)
              | %D\$.*?;   # Delay (label name)
              | \$.*?;     # Eval
              | &.*?;      # Image
            ",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled
        );

        private static readonly Regex NonbreakingRegex = new Regex(@"[\w,;.!?()\[\]{}<>'""]+[- ]?", RegexOptions.Compiled);

        private PSB _psb;

        public string Extension => ".scn";

        public void Load(ScriptLocation location)
        {
            _psb = new PSB(location.ToFilePath());
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            return GetPsbStrings().Select(s => s.ToScriptString());
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            GetPsbStrings().ZipStrict(
                strings,
                (psb, str) =>
                {
                    if (str.Type == ScriptStringType.Message)
                    {
                        psb.Text.Value = ProportionalWordWrapper.Default.Wrap(StringUtil.FancifyQuotes(str.Text, ControlCodeRegex), ControlCodeRegex, "\\n");
                    }
                    else
                    {
                        psb.Text.Value = str.Text;
                    }
                }
            );

            _psb.Merge();
            File.WriteAllBytes(location.ToFilePath(), _psb.Build());
        }

        private IEnumerable<ScriptPsbString> GetPsbStrings()
        {
            PsbList scenes = _psb.Objects["scenes"] as PsbList;
            if (scenes == null)
                yield break;

            foreach (PsbDictionary scene in scenes.OfType<PsbDictionary>())
            {
                foreach (ScriptPsbString text in GetTextStrings(scene))
                    yield return text;

                foreach (ScriptPsbString select in GetSelectStrings(scene))
                    yield return select;
            }
        }

        private IEnumerable<ScriptPsbString> GetTextStrings(PsbDictionary scene)
        {
            PsbList texts = scene["texts"] as PsbList;
            if (texts == null)
                yield break;

            foreach (PsbList text in texts.OfType<PsbList>())
            {
                PsbString realCharacterName = GetIsolatedString(text, 0);
                PsbString displayCharacterName;
                IPsbValue message;
                if (text[1] is PsbList)
                {
                    displayCharacterName = null;
                    message = text[1];
                }
                else
                {
                    displayCharacterName = GetIsolatedString(text, 1);
                    if (displayCharacterName == null && realCharacterName != null && realCharacterName.Value != "＠")
                    {
                        displayCharacterName = new PsbString(realCharacterName.Value);
                        text[1] = displayCharacterName;
                    }
                    message = GetIsolatedString(text, 2) ?? text[2];
                }

                if (message is PsbList multiLanguageTexts && multiLanguageTexts.Count > LanguageIndex &&
                    multiLanguageTexts[LanguageIndex] is PsbList languageText && languageText.Count >= 2)
                {
                    // [name, text, speechtext, searchtext]
                    displayCharacterName = GetIsolatedString(languageText, 0);
                    message = GetIsolatedString(languageText, 1);
                }

                if (message is PsbString)
                {
                    if (!string.IsNullOrEmpty(realCharacterName?.Value))
                        yield return new ScriptPsbString(displayCharacterName ?? realCharacterName, ScriptStringType.CharacterName);

                    yield return new ScriptPsbString((PsbString)message, ScriptStringType.Message);
                }
            }
        }

        private static PsbString GetIsolatedString(PsbList list, int index)
        {
            if (list[index] is PsbString str)
            {
                str = new PsbString(str.Value);
                list[index] = str;
                return str;
            }
            return null;
        }

        private IEnumerable<ScriptPsbString> GetSelectStrings(PsbDictionary scene)
        {
            PsbList selects = scene["selects"] as PsbList;
            if (selects == null)
                yield break;

            foreach (PsbDictionary select in selects.OfType<PsbDictionary>())
            {
                PsbString text = null;
                if (select["language"] is PsbList multiLanguageSelects &&
                    multiLanguageSelects.Count > LanguageIndex &&
                    multiLanguageSelects[LanguageIndex] is PsbDictionary languageSelect)
                {
                    text = languageSelect["text"] as PsbString;
                }

                if (text == null)
                    text = select["text"] as PsbString;

                if (text != null)
                    yield return new ScriptPsbString(text, ScriptStringType.Message);
            }
        }

        private static string InsertWrappingCommands(string text)
        {
            if (StringUtil.ContainsJapaneseText(text))
                return text;

            return StringUtil.ReplaceMatchSurroundings(text, ControlCodeRegex, InsertWrappingCommandsInSegment);
        }

        private static string InsertWrappingCommandsInSegment(int startPos, string text)
        {
            return NonbreakingRegex.Replace(
                text,
                m =>
                {
                    int posArg = startPos + m.Index;
                    string textArg = "\"" + m.Value.Replace("\"", "\\\"").Replace(";", "\" + semicolon + \"") + "\"";
                    return $"$wrap({posArg},{textArg});{m.Value}";
                }
            );
        }

        private struct ScriptPsbString
        {
            public ScriptPsbString(PsbString text, ScriptStringType type)
            {
                Text = text;
                Type = type;
            }

            public readonly PsbString Text;
            public readonly ScriptStringType Type;

            public ScriptString ToScriptString()
            {
                return new ScriptString(Text.Value.Replace("\\n", "\r\n"), Type);
            }
        }
    }
}
