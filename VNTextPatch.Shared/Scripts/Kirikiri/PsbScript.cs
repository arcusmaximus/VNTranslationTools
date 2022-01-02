using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FreeMote.Psb;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Kirikiri
{
    public class PsbScript : IScript
    {
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
                    if (str.Type == ScriptStringType.Message && psb.TextIndex != null)
                    {
                        psb.Text.Value = ProportionalWordWrapper.Default.Wrap(StringUtil.FancifyQuotes(str.Text, @"\$.+?;"));
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
            PsbList lines = scene["lines"] as PsbList;
            PsbList texts = scene["texts"] as PsbList;
            if (lines == null || texts == null)
                yield break;

            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                PsbNumber textIndex = lines[lineIndex] as PsbNumber;
                if (textIndex == null)
                    continue;

                PsbList text = (PsbList)texts[textIndex.IntValue - 1];
                PsbString realCharacterName = text[0] as PsbString;
                if (realCharacterName != null)
                {
                    PsbString displayCharacterName = text[1] as PsbString;
                    if (displayCharacterName == null && realCharacterName.Value != "＠")
                    {
                        displayCharacterName = new PsbString(realCharacterName.Value);
                        text[1] = displayCharacterName;
                    }
                    yield return new ScriptPsbString(displayCharacterName ?? realCharacterName, ScriptStringType.CharacterName);
                }

                PsbList messageList = text;
                int messageIndex = 2;
                if (messageList[messageIndex] is PsbList multiLanguageTexts && multiLanguageTexts.Count >= 1)
                {
                    if (multiLanguageTexts[0] is PsbList japaneseText && japaneseText.Count >= 2)
                    {
                        // [name, text, speechtext, searchtext]
                        messageList = japaneseText;
                        messageIndex = 1;
                    }
                }

                if (messageList[messageIndex] is PsbString message)
                {
                    message = new PsbString(message.Value);
                    messageList[messageIndex] = message;
                    yield return new ScriptPsbString(scene, lineIndex, textIndex, message, ScriptStringType.Message);
                }
            }
        }

        private IEnumerable<ScriptPsbString> GetSelectStrings(PsbDictionary scene)
        {
            PsbList selects = scene["selects"] as PsbList;
            if (selects == null)
                yield break;

            foreach (PsbDictionary select in selects.OfType<PsbDictionary>())
            {
                PsbString text = select["text"] as PsbString;
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
                : this(null, -1, null, text, type)
            {
            }

            public ScriptPsbString(PsbDictionary scene, int lineIndex, PsbNumber textIndex, PsbString text, ScriptStringType type)
            {
                Scene = scene;
                LineIndex = lineIndex;
                TextIndex = textIndex;
                Text = text;
                Type = type;
            }

            public readonly PsbDictionary Scene;
            public readonly int LineIndex;
            public readonly PsbNumber TextIndex;
            public readonly PsbString Text;
            public readonly ScriptStringType Type;

            public ScriptString ToScriptString()
            {
                return new ScriptString(Text.Value.Replace("\\n", "\r\n"), Type);
            }
        }
    }
}
