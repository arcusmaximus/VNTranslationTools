using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts
{
    public class MusicaScript : PlainTextScript
    {
        public override string Extension => ".sc";

        protected override IEnumerable<Range> GetRanges(string script)
        {
            TrackingStringReader reader = new TrackingStringReader(script);
            while (true)
            {
                int lineStartPos = reader.Position;
                string line = reader.ReadLine();
                if (line == null)
                    break;

                Match match = Regex.Match(line, @"^(?<command>\.\w+)(?:[ \t](?<arg>[^ \t]*))+$");
                if (!match.Success)
                    continue;

                string command = match.Groups["command"].Value;
                CaptureCollection args = match.Groups["arg"].Captures;
                IEnumerable<Range> ranges = command switch
                                            {
                                                ".message" => GetMessageRanges(lineStartPos, args),
                                                ".select" => GetSelectRanges(lineStartPos, args),
                                                _ => Enumerable.Empty<Range>()
                                            };

                foreach (Range range in ranges)
                {
                    yield return range;
                }
            }
        }

        private static IEnumerable<Range> GetMessageRanges(int lineStartPos, CaptureCollection args)
        {
            if (args.Count < 4)
                yield break;

            Capture name = args[2];
            if (name.Length > 0)
            {
                int namePos = name.Index;
                int nameLength = name.Length;
                if (name.Value[0] == '@')
                {
                    namePos++;
                    nameLength--;
                }

                yield return new Range(lineStartPos + namePos, nameLength, ScriptStringType.CharacterName);
            }

            Capture messageStart = args[3];
            Capture messageEnd = args[args.Count - 1];
            yield return new Range(lineStartPos + messageStart.Index, messageEnd.Index + messageEnd.Length - messageStart.Index, ScriptStringType.Message);
        }

        private static IEnumerable<Range> GetSelectRanges(int lineStartPos, CaptureCollection args)
        {
            foreach (Capture choice in args)
            {
                int colonIdx = choice.Value.IndexOf(':');
                if (colonIdx < 0)
                    continue;

                yield return new Range(lineStartPos + choice.Index, colonIdx, ScriptStringType.Message);
            }
        }

        protected override string GetTextForRead(Range range)
        {
            string text = base.GetTextForRead(range);

            if (range.Type == ScriptStringType.CharacterName && StringUtil.ContainsJapaneseText(text))
                text = text.Replace("　", "");
            else
                text = text.Replace("　", " ");

            text = text.Replace("\\n", "\r\n");
            text = Regex.Replace(
                text,
                @"\\\$(..)",
                m => ((char)int.Parse(m.Groups[1].Value, NumberStyles.HexNumber)).ToString()
            );
            return text;
        }

        protected override string GetTextForWrite(Range range, ScriptString str)
        {
            string text = MonospaceWordWrapper.Default.Wrap(str.Text);
            return text.Replace(" ", "　")
                       .Replace("\r\n", "\\n");
        }
    }
}
