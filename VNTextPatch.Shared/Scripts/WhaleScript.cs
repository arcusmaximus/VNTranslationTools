using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts
{
    public class WhaleScript : PlainTextScript
    {
        private static readonly string[] MessageCommands = { "CS", "MS.HS" };

        public override string Extension => null;

        protected override IEnumerable<Range> GetRanges(string script)
        {
            TrackingStringReader reader = new TrackingStringReader(script);
            while (true)
            {
                int lineStartPos = reader.Position;
                string line = reader.ReadLine();
                if (line == null)
                    break;

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("*"))
                    continue;

                foreach (Range range in GetLineRanges(line, lineStartPos))
                {
                    yield return range;
                }
            }
        }

        private static IEnumerable<Range> GetLineRanges(string line, int lineStartPos)
        {
            if (line[0] > 0xFF)
                return GetMessageRanges(line, lineStartPos);

            if (line.StartsWith("SELECT "))
                return GetSelectRanges(line, lineStartPos);

            if (MessageCommands.Any(c => line.StartsWith(c + " ")))
                return GetCommandRanges(line, lineStartPos);

            return Enumerable.Empty<Range>();
        }

        private static IEnumerable<Range> GetMessageRanges(string line, int lineStartPos)
        {
            Match dialogueMatch = Regex.Match(line, @"^(?:【(?<name>.+?)(?:,\w+)*】)?(?:「(?<message>.+?)」?|(?<message>（(?:.+?)）?))$");
            if (dialogueMatch.Success)
            {
                Group name = dialogueMatch.Groups["name"];
                Group message = dialogueMatch.Groups["message"];
                if (name.Success)
                    yield return new Range(lineStartPos + name.Index, name.Length, ScriptStringType.CharacterName);

                yield return new Range(lineStartPos + message.Index, message.Length, ScriptStringType.Message);
            }
            else
            {
                yield return new Range(lineStartPos, line.Length, ScriptStringType.Message);
            }
        }

        private static IEnumerable<Range> GetSelectRanges(string line, int lineStartPos)
        {
            Match match = Regex.Match(line, @"^SELECT\s+(?:""(?<choice>[^"",]+),\*\w+""[,\s]*)+$");
            if (!match.Success)
                yield break;

            foreach (Capture capture in match.Groups["choice"].Captures)
            {
                yield return new Range(lineStartPos + capture.Index, capture.Length, ScriptStringType.Message);
            }
        }

        private static IEnumerable<Range> GetCommandRanges(string line, int lineStartPos)
        {
            Match match = Regex.Match(line, @"^[A-Z0-9\.]+\s+(?:(?:""(?<arg>[^""]+)""|(?<arg>[^,\s]+))[,\s]*)+$");
            if (!match.Success)
                yield break;

            foreach (Capture capture in match.Groups["arg"].Captures)
            {
                if (!StringUtil.ContainsJapaneseText(capture.Value))
                    continue;

                foreach (Range range in GetMessageRanges(capture.Value, lineStartPos + capture.Index))
                {
                    yield return range;
                }
            }
        }

        protected override string GetTextForRead(Range range)
        {
            return base.GetTextForRead(range).Replace("[n]", "\r\n");
        }

        protected override string GetTextForWrite(Range range, ScriptString str)
        {
            string text = base.GetTextForWrite(range, str);
            text = MonospaceWordWrapper.Default.Wrap(text);
            text = text.Replace("\r\n", "[n]");
            if (text.Length > 0 && IsAtStartOfLine(range) && !StringUtil.IsJapaneseCharacter(text[0]))
                text = "　" + text.Replace("[n]", "[n]　");

            return text;
        }

        private bool IsAtStartOfLine(Range range)
        {
            return range.Offset == 0 || GetScriptSubstring(range.Offset - 1, 1) == "\n";
        }
    }
}
