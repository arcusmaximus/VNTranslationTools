using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts
{
    internal class QlieScript : PlainTextScript
    {
        public override string Extension => ".s";

        private Encoding _writeEncoding;

        protected override Encoding GetReadEncoding(ArraySegment<byte> data)
        {
            if (data.Contains((byte)0))
            {
                _writeEncoding = Encoding.Unicode;
                return Encoding.Unicode;
            }
            else
            {
                _writeEncoding = StringUtil.SjisTunnelEncoding;
                return StringUtil.SjisEncoding;
            }
        }

        protected override Encoding GetWriteEncoding()
        {
            return _writeEncoding;
        }

        protected override IEnumerable<Range> GetRanges(string script)
        {
            using TrackingStringReader reader = new TrackingStringReader(script);
            while (true)
            {
                int position = reader.Position;
                string line = reader.ReadLine();
                if (line == null)
                    break;

                line = line.Trim();
                if (line.Length == 0)
                    continue;

                if (line.StartsWith("^select,"))
                {
                    foreach (Range argRange in GetCommandArgumentRanges(position, line))
                    {
                        yield return argRange;
                    }
                    continue;
                }

                if (line.StartsWith("@") ||
                    line.StartsWith("^") ||
                    line.StartsWith("\\") ||
                    line.StartsWith("％"))
                {
                    continue;
                }

                if (line.StartsWith("【") && line.EndsWith("】"))
                    yield return new Range(position + 1, line.Length - 2, ScriptStringType.CharacterName);
                else
                    yield return new Range(position, line.Length, ScriptStringType.Message);
            }
        }

        private IEnumerable<Range> GetCommandArgumentRanges(int linePos, string line)
        {
            int start = line.IndexOf(",");
            while (start >= 0 && start < line.Length)
            {
                start++;
                int end = line.IndexOf(",", start);
                if (end < 0)
                    end = line.Length;

                yield return new Range(linePos + start, end - start, ScriptStringType.Message);
                start = end;
            }
        }
    }
}
