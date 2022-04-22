using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts
{
    public abstract class PlainTextScript : IScript
    {
        private string _script;

        public abstract string Extension { get; }

        public virtual void Load(ScriptLocation location)
        {
            string filePath = location.ToFilePath();
            ArraySegment<byte> data = new ArraySegment<byte>(File.ReadAllBytes(filePath));
            data = DecryptScript(data);

            Encoding encoding = GetReadEncoding(data);
            int preambleLength = encoding.GetPreamble().Length;
            _script = encoding.GetString(data.Array, data.Offset + preambleLength, data.Count - preambleLength);
            _script = PreprocessScript(_script);
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            foreach (Range range in GetRanges(_script))
            {
                string text = GetTextForRead(range);
                yield return new ScriptString(text, range.Type);
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            MemoryStream outputMemStream = new MemoryStream();

            using (IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator())
            using (StreamWriter writer = new StreamWriter(outputMemStream, GetWriteEncoding()))
            {
                int lineIdx = 0;
                int copyStart = 0;
                foreach (Range range in GetRanges(_script))
                {
                    int copyEnd = range.Offset;
                    writer.Write(GetScriptSubstring(copyStart, copyEnd - copyStart));

                    if (!stringEnumerator.MoveNext())
                        throw new InvalidDataException($"Translation file is missing line {1 + lineIdx} ({GetTextForRead(range)})");

                    writer.Write(GetTextForWrite(range, stringEnumerator.Current));

                    copyStart = range.Offset + range.Length;
                    lineIdx++;
                }

                if (stringEnumerator.MoveNext())
                    throw new InvalidDataException("Translation file has too many lines");

                if (copyStart < _script.Length)
                    writer.Write(GetScriptSubstring(copyStart, _script.Length - copyStart));
            }

            outputMemStream.TryGetBuffer(out ArraySegment<byte> data);
            data = EncryptScript(data);
            using (Stream outputFileStream = File.Open(location.ToFilePath(), FileMode.Create, FileAccess.Write))
            {
                outputFileStream.Write(data.Array, data.Offset, data.Count);
            }
        }

        protected virtual Encoding GetReadEncoding(ArraySegment<byte> data)
        {
            return StringUtil.SjisEncoding;
        }

        protected virtual Encoding GetWriteEncoding()
        {
            return StringUtil.SjisTunnelEncoding;
        }

        protected virtual ArraySegment<byte> DecryptScript(ArraySegment<byte> data)
        {
            return data;
        }

        protected virtual ArraySegment<byte> EncryptScript(ArraySegment<byte> data)
        {
            return data;
        }

        protected virtual string PreprocessScript(string script)
        {
            return script;
        }

        protected abstract IEnumerable<Range> GetRanges(string script);

        protected virtual string GetTextForRead(Range range)
        {
            return GetScriptSubstring(range.Offset, range.Length);
        }

        protected string GetScriptSubstring(Range range)
        {
            return GetScriptSubstring(range.Offset, range.Length);
        }

        protected string GetScriptSubstring(int offset, int length)
        {
            if (_script == null)
                throw new InvalidOperationException();

            return _script.Substring(offset, length);
        }

        protected virtual string GetTextForWrite(Range range, ScriptString str)
        {
            return str.Text;
        }
    }
}
