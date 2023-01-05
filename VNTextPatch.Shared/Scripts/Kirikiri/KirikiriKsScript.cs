using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Kirikiri
{
    public class KirikiriKsScript : PlainTextScript
    {
        private static readonly Regex LineCommandRegex   = new Regex(@"^\s*@(?<command>[^ ]+)(?: +(?<attrname>[^= ]+)(?: *= *(?<attrvalue>""(?:\\""|[^""])*""|'(?:\\'|[^'])*'|[^""' ]*))?)*", RegexOptions.Compiled);
        private static readonly Regex InlineCommandRegex = new Regex(@"\[(?<command>[^\]' ]+)(?: +(?<attrname>[^\]= ]+)(?: *= *(?<attrvalue>""(?:\\""|[^""])*""|'(?:\\'|[^'])*'|[^\]""' ]*))?)* *\]", RegexOptions.Compiled);

        private static readonly Regex PlainRubyRegex = new Regex(@"\[(?<text>[^/\]]+?)/(?<ruby>[^\]]+?)\]", RegexOptions.Compiled);

        private static readonly string[] NameCommands = { "nm", "set_title", "speaker", "Talk", "talk", "cn", "name", "名前" };
        private static readonly string[] EnterNameCommands = { "ns" };
        private static readonly string[] ExitNameCommands = { "nse" };
        private static readonly string[] MessageCommands = { "sel01", "sel02", "sel03", "sel04", "AddSelect", "ruby" };
        private static readonly string[] AllowedInlineCommands = { "r", "ruby", "ruby_c", "heart", "mruby", "・", "★" };

        private ScriptStringType _currentStringType;

        public override string Extension => ".ks";

        protected override ArraySegment<byte> DecryptScript(ArraySegment<byte> data)
        {
            return KirikiriDescrambler.Descramble(data);
        }

        protected override Encoding GetReadEncoding(ArraySegment<byte> data)
        {
            if (data.Count >= 3 && data.Get(0) == 0xEF && data.Get(1) == 0xBB && data.Get(2) == 0xBF)
                return Encoding.UTF8;

            if (data.Count >= 2 && data.Get(0) == 0xFF && data.Get(1) == 0xFE)
                return Encoding.Unicode;

            return StringUtil.SjisEncoding;
        }

        protected override Encoding GetWriteEncoding()
        {
            return Encoding.Unicode;
        }

        protected override string PreprocessScript(string script)
        {
            return Regex.Replace(script, @"\r\n *;.*?\r\n", "\r\n");
        }

        protected override IEnumerable<Range> GetRanges(string script)
        {
            _currentStringType = ScriptStringType.Message;

            using TrackingStringReader reader = new TrackingStringReader(script);
            bool inScript = false;
            Range currentRange = new Range(0, 0, ScriptStringType.Message);
            while (true)
            {
                int lineOffset = reader.Position;
                string line = reader.ReadLine();
                if (line == null)
                    break;

                if (line == "[iscript]" || line == "@iscript" || line.StartsWith("[macro") || line.StartsWith("@macro"))
                    inScript = true;
                else if (line == "[endscript]" || line == "@endscript" || line == "[endmacro]" || line == "@endmacro")
                    inScript = false;

                if (inScript)
                    continue;

                foreach (Range range in GetLineRanges(lineOffset, line).Where(r => !string.IsNullOrWhiteSpace(GetTextForRead(r))))
                {
                    if (AreRangesContiguous(currentRange, lineOffset, range))
                    {
                        currentRange.Length = range.Offset + range.Length - currentRange.Offset;
                    }
                    else
                    {
                        if (currentRange.Length > 0)
                            yield return currentRange;

                        currentRange = range;
                    }
                }
            }
            if (currentRange.Length > 0)
                yield return currentRange;
        }

        private IEnumerable<Range> GetLineRanges(int lineOffset, string line)
        {
            string trimmedLine = line.TrimStart();

            if (trimmedLine.StartsWith(";"))
                return Enumerable.Empty<Range>();

            if (trimmedLine.StartsWith("@"))
                return GetLineCommandRanges(lineOffset, line);

            if (trimmedLine.StartsWith("*"))
                return GetLabelRanges(lineOffset, line);

            if (trimmedLine.StartsWith("#"))
                return GetNameRanges(lineOffset, line);

            return GetMessageRanges(lineOffset, line);
        }

        private IEnumerable<Range> GetLineCommandRanges(int lineOffset, string line)
        {
            if (line == "@r")
                return new[] { new Range(lineOffset, line.Length, ScriptStringType.Message) };

            Match command = LineCommandRegex.Match(line);
            if (NameCommands.Contains(GetCommandName(command)))
                return GetAttributeValueRanges(lineOffset, command, ScriptStringType.CharacterName);

            if (MessageCommands.Contains(GetCommandName(command)))
                return GetAttributeValueRanges(lineOffset, command, ScriptStringType.Message);

            return Enumerable.Empty<Range>();
        }

        private IEnumerable<Range> GetLabelRanges(int lineOffset, string line)
        {
            int pipeIdx = line.IndexOf('|');
            if (pipeIdx < 0 || pipeIdx == line.Length - 1)
                return Enumerable.Empty<Range>();

            int nameIdx = pipeIdx + 1;
            return new[] { new Range(lineOffset + nameIdx, line.Length - nameIdx, ScriptStringType.Message) };
        }

        private IEnumerable<Range> GetNameRanges(int lineOffset, string line)
        {
            yield return new Range(lineOffset + 1, line.Length - 1, ScriptStringType.CharacterName);
        }

        private IEnumerable<Range> GetMessageRanges(int lineOffset, string line)
        {
            int segmentStart = 0;
            foreach (Match commandMatch in InlineCommandRegex.Matches(line))
            {
                string commandName = GetCommandName(commandMatch);
                if (AllowedInlineCommands.Contains(commandName))
                    continue;

                int segmentEnd = commandMatch.Index;
                if (segmentEnd > segmentStart)
                    yield return new Range(lineOffset + segmentStart, segmentEnd - segmentStart, _currentStringType);

                if (commandName.StartsWith("【") && commandName.EndsWith("】"))
                    yield return new Range(lineOffset + commandMatch.Groups["command"].Index + 1, commandName.Length - 2, ScriptStringType.CharacterName);
                
                if (NameCommands.Contains(commandName) || (commandName.StartsWith("【") && commandName.EndsWith("】")))
                {
                    foreach (Range range in GetAttributeValueRanges(lineOffset, commandMatch, ScriptStringType.CharacterName))
                    {
                        yield return range;
                    }
                }
                else if (EnterNameCommands.Contains(commandName))
                {
                    _currentStringType = ScriptStringType.CharacterName;
                }
                else if (ExitNameCommands.Contains(commandName))
                {
                    _currentStringType = ScriptStringType.Message;
                }
                else if (MessageCommands.Contains(commandName))
                {
                    foreach (Range range in GetAttributeValueRanges(lineOffset, commandMatch, ScriptStringType.Message))
                    {
                        yield return range;
                    }
                }

                segmentStart = commandMatch.Index + commandMatch.Length;
            }

            if (segmentStart < line.Length)
                yield return new Range(lineOffset + segmentStart, line.Length - segmentStart, _currentStringType);
        }

        private static IEnumerable<Range> GetAttributeValueRanges(int lineOffset, Match command, ScriptStringType stringType)
        {
            foreach (Capture capture in command.Groups["attrvalue"].Captures)
            {
                string value = capture.Value;
                if (!StringUtil.ContainsJapaneseText(value))
                    continue;

                if (value.StartsWith("\"") || value.StartsWith("'"))
                    yield return new Range(lineOffset + capture.Index + 1, capture.Length - 2, stringType);
                else
                    yield return new Range(lineOffset + capture.Index, capture.Length, stringType);
            }
        }

        private static bool AreRangesContiguous(Range firstRange, int secondRangeLineOffset, Range secondRange)
        {
            if (secondRange.Type != firstRange.Type)
                return false;

            if (firstRange.Offset + firstRange.Length == secondRange.Offset)
                return true;

            if (secondRange.Offset == secondRangeLineOffset)
                return firstRange.Offset + firstRange.Length + 2 == secondRange.Offset;

            return false;
        }

        protected override string GetTextForRead(Range range)
        {
            string text = base.GetTextForRead(range);
            text = text.Replace("\r\n", "");
            text = text.Replace("@r", "\r\n");
            text = ConvertKirikiriRubyToPlain(text);
            text = text.Replace("[l]", "|");
            text = text.Replace("[r]", "\r\n");
            return text;
        }

        protected override string GetTextForWrite(Range range, ScriptString str)
        {
            if (str.Type != ScriptStringType.Message)
                return str.Text;

            string text = str.Text;
            if (StringUtil.ContainsJapaneseText(text))
                text = PlainRubyRegex.Replace(text, ConvertPlainRubyToKirikiri);

            text = ProportionalWordWrapper.Default.Wrap(text);
            text = text.Replace("\r\n", "[r]\r\n");
            return text;
        }

        private static string ConvertKirikiriRubyToPlain(string str)
        {
            MatchCollection commands = InlineCommandRegex.Matches(str);
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                Match command = commands[i];
                if (GetCommandName(command) != "ruby")
                    continue;

                string ruby = GetAttributeValue(command, "text");
                if (ruby == null)
                    continue;

                string chars = GetAttributeValue(command, "char");
                int textLength;
                string text = null;
                if (chars == null)
                    textLength = 1;
                else if (!int.TryParse(chars, out textLength))
                    text = chars;

                if (textLength > 0)
                {
                    if (command.Index + command.Length + textLength > str.Length)
                        continue;

                    text = str.Substring(command.Index + command.Length, textLength);
                }

                str = str.Substring(0, command.Index) + $"[{text}/{ruby}]" + str.Substring(command.Index + command.Length + textLength);
            }

            return str;
        }

        private static string ConvertPlainRubyToKirikiri(Match match)
        {
            string text = match.Groups["text"].Value;
            string ruby = match.Groups["ruby"].Value;
            return $"[wrap text=\"{text.Replace("\"", "\\\"")}\"][ruby text=\"{ruby.Replace("\"", "\\\"")}\" char={text.Length}]{text}";
        }

        private static string GetCommandName(Match command)
        {
            return command.Groups["command"].Value;
        }

        private static string GetAttributeValue(Match command, string name)
        {
            CaptureCollection names = command.Groups["attrname"].Captures;
            CaptureCollection values = command.Groups["attrvalue"].Captures;
            for (int i = 0; i < names.Count; i++)
            {
                if (names[i].Value == name)
                {
                    string value = values[i].Value;
                    if (value.StartsWith("\""))
                        value = value.Substring(1, value.Length - 2).Replace("\\\"", "\"");
                    else if (value.StartsWith("'"))
                        value = value.Substring(1, value.Length - 2).Replace("\\'", "'");

                    return value;
                }
            }
            return null;
        }
    }
}
