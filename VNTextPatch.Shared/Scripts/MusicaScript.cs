using System.Collections.Generic;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts
{
    public class MusicaScript : PlainTextScript
    {
        public override string Extension => ".sc";

        protected override IEnumerable<Range> GetRanges(string script)
        {
            char[] argSeparators = { ' ', '\t' };

            TrackingStringReader reader = new TrackingStringReader(script);
            while (true)
            {
                int lineStartPos = reader.Position;
                string line = reader.ReadLine();
                if (line == null)
                    break;

                if (!line.StartsWith(".message "))
                    continue;

                int messageNumPos = line.IndexOfAny(argSeparators) + 1;
                if (messageNumPos == 0)
                    continue;

                int audioPos = line.IndexOfAny(argSeparators, messageNumPos) + 1;
                if (audioPos == 0)
                    continue;

                int namePos = line.IndexOfAny(argSeparators, audioPos) + 1;
                if (namePos == 0)
                    continue;

                if (script[lineStartPos + namePos] == '@')
                    namePos++;

                int messagePos = line.IndexOfAny(argSeparators, namePos) + 1;
                if (messagePos == 0)
                    continue;

                if (messagePos - namePos > 1)
                    yield return new Range(lineStartPos + namePos, messagePos - namePos - 1, ScriptStringType.CharacterName);

                yield return new Range(lineStartPos + messagePos, line.Length - messagePos, ScriptStringType.Message);
            }
        }

        protected override string GetTextForRead(Range range)
        {
            string text = base.GetTextForRead(range);
            if (range.Type == ScriptStringType.CharacterName)
                text = text.Replace("　", "");

            return text;
        }

        protected override string GetTextForWrite(ScriptString str)
        {
            if (str.Type == ScriptStringType.CharacterName)
                return str.Text.Replace(" ", "　");

            return str.Text;
        }
    }
}
