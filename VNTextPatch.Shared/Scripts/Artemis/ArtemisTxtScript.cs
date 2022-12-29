using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Artemis
{
    internal class ArtemisTxtScript : PlainTextScript
    {
        public override string Extension => ".art";
        
        private static readonly Regex CommandRegex = new Regex(@"^\s*\[(?<command>[^\]' ]+)(?: +(?<attrname>[^\]= ]+)(?: *= *(?<attrvalue>""(?:\\""|[^""])*""|'(?:\\'|[^'])*'|[^\]""' ]*))?)*\]\s*$", RegexOptions.Compiled);

        private bool _currentRangeIsUnquotedAttribute;

        protected override IEnumerable<Range> GetRanges(string script)
        {
            TrackingStringReader reader = new TrackingStringReader(script);
            int messageStartPos = -1;
            int messageEndPos = -1;
            while (true)
            {
                int lineStartPos = reader.Position;
                string line = reader.ReadLine();
                if (line == null)
                    break;

                line = RemoveComment(line);
                if (IsLabel(line))
                    continue;

                Match commandMatch = CommandRegex.Match(line);
                if (commandMatch.Success)
                {
                    foreach (Range lineRange in GetCommandRanges(commandMatch))
                    {
                        Range fileRange = lineRange;
                        fileRange.Offset += lineStartPos;
                        _currentRangeIsUnquotedAttribute = script[fileRange.Offset - 1] != '"' &&
                                                           script[fileRange.Offset - 1] != '\'';
                        yield return fileRange;
                    }
                    continue;
                }

                _currentRangeIsUnquotedAttribute = false;

                if (!string.IsNullOrWhiteSpace(line))
                {
                    if (messageStartPos < 0)
                        messageStartPos = lineStartPos;

                    messageEndPos = lineStartPos + line.Length;
                    continue;
                }

                if (messageStartPos >= 0)
                {
                    yield return new Range(messageStartPos, messageEndPos - messageStartPos, ScriptStringType.Message);
                    messageStartPos = -1;
                    messageEndPos = -1;
                }
            }
        }

        private static string RemoveComment(string line)
        {
            return Regex.Replace(line, @"(//|;).*", "");
        }

        private static bool IsLabel(string line)
        {
            return line.StartsWith("*");
        }

        private static IEnumerable<Range> GetCommandRanges(Match command)
        {
            switch (GetCommandName(command))
            {
                case "name":
                    Capture nameCapture = command.Groups["attrname"].Captures.Cast<Capture>().FirstOrDefault();
                    if (nameCapture != null)
                        yield return new Range(nameCapture.Index, nameCapture.Length, ScriptStringType.CharacterName);

                    break;

                case "sel_text":
                    Range? textRange = GetAttributeValueRange(command, "text");
                    if (textRange != null)
                        yield return textRange.Value;

                    break;
            }
        }

        private static string GetCommandName(Match command)
        {
            return command.Groups["command"].Value;
        }

        private static Range? GetAttributeValueRange(Match command, string name)
        {
            CaptureCollection names = command.Groups["attrname"].Captures;
            CaptureCollection values = command.Groups["attrvalue"].Captures;
            for (int i = 0; i < names.Count; i++)
            {
                if (names[i].Value != name)
                    continue;

                Capture value = values[i];
                if (value.Value.StartsWith("\"") || value.Value.StartsWith("'"))
                    return new Range(value.Index + 1, value.Length - 2, ScriptStringType.Message);

                return new Range(value.Index, value.Length, ScriptStringType.Message);
            }
            return null;
        }

        protected override string GetTextForWrite(Range range, ScriptString str)
        {
            string text = ProportionalWordWrapper.Default.Wrap(str.Text);
            if (_currentRangeIsUnquotedAttribute && text.Contains(" "))
                text = $"\"{text}\"";

            return text;
        }
    }
}
