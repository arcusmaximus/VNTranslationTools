using System;
using System.Collections.Generic;
using System.IO;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts
{
    internal class CSystemScript : IScript
    {
        private byte[] _data;
        private List<CSystemStringRange> _stringRanges;

        public string Extension => ".a0";

        public void Load(ScriptLocation location)
        {
            _data = File.ReadAllBytes(location.ToFilePath());
            _stringRanges = new List<CSystemStringRange>();

            int offset = 0;
            bool append = false;
            while (offset < _data.Length)
            {
                int length = 4 + BitConverter.ToInt32(_data, offset);
                char type = (char)_data[offset + 4];
                if (type == 'S')
                {
                    int xorKeyOffset = offset + 4 + 1;
                    bool hasXorKey = xorKeyOffset + 4 < _data.Length &&
                                     _data[xorKeyOffset + 1] == 0 &&
                                     _data[xorKeyOffset + 2] == 0 &&
                                     _data[xorKeyOffset + 3] == 0;
                    _stringRanges.Add(new CSystemStringRange(offset, length, hasXorKey, append));
                    append = true;  
                }
                else
                {
                    append = false;
                }

                offset += length;
            }
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            int rangeIdx = 0;
            while (rangeIdx < _stringRanges.Count)
            {
                string text = null;
                do
                {
                    if (text != null)
                        text += "\r\n";

                    text += DecodeString(_stringRanges[rangeIdx]);
                    rangeIdx++;
                } while (!text.StartsWith("【") && rangeIdx < _stringRanges.Count && _stringRanges[rangeIdx].Append);

                if (text.StartsWith("【") && text.EndsWith("】"))
                    yield return new ScriptString(text.Substring(1, text.Length - 2), ScriptStringType.CharacterName);
                else
                    yield return new ScriptString(text, ScriptStringType.Message);
            }
        }

        private string DecodeString(CSystemStringRange range)
        {
            byte xorKey = range.HasXorKey ? (byte)BitConverter.ToInt32(_data, range.XorKeyOffset) : (byte)0;
            if (xorKey != 0)
            {
                BinaryUtil.Xor(_data, range.TextOffset, range.TextLength, xorKey);
                _data[range.XorKeyOffset] = 0;
            }

            byte[] plainRuby = null;
            int binRubyEnd = range.TextOffset;
            int plainRubyEnd = 0;
            for (int i = 0; i < range.TextLength; i += 2)
            {
                if (_data[range.TextOffset + i] == 0xFF && _data[range.TextOffset + i + 1] == 0xFF)
                {
                    int rubyLength = _data[range.TextOffset + i + 2] * 2;
                    int textLength = _data[range.TextOffset + i + 3 + rubyLength] * 2;

                    if (plainRuby == null)
                        plainRuby = new byte[range.TextLength];

                    int inbetween = range.TextOffset + i - binRubyEnd;
                    Array.Copy(_data, binRubyEnd, plainRuby, plainRubyEnd, inbetween);
                    binRubyEnd += inbetween + 3;
                    plainRubyEnd += inbetween;
                    plainRuby[plainRubyEnd++] = (byte)'[';

                    Array.Copy(_data, binRubyEnd, plainRuby, plainRubyEnd, rubyLength);
                    binRubyEnd += rubyLength + 1;
                    plainRubyEnd += rubyLength;
                    plainRuby[plainRubyEnd++] = (byte)'/';

                    Array.Copy(_data, binRubyEnd, plainRuby, plainRubyEnd, textLength);
                    binRubyEnd += textLength;
                    plainRubyEnd += textLength;
                    plainRuby[plainRubyEnd++] = (byte)']';
                }
            }

            if (plainRuby != null)
            {
                int remaining = range.TextOffset + (range.TextLength / 2 * 2) - binRubyEnd;
                Array.Copy(_data, binRubyEnd, plainRuby, plainRubyEnd, remaining);
                plainRubyEnd += remaining;
                return StringUtil.SjisEncoding.GetString(plainRuby, 0, plainRubyEnd);
            }

            return StringUtil.SjisEncoding.GetString(_data, range.TextOffset, range.TextLength / 2 * 2);
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            using Stream inputStream = new MemoryStream(_data);
            using Stream outputStream = File.Open(location.ToFilePath(), FileMode.Create, FileAccess.Write);
            BinaryPatcher patcher = new BinaryPatcher(inputStream, outputStream);

            using IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator();
            int rangeIdx = 0;
            while (rangeIdx < _stringRanges.Count)
            {
                if (!stringEnumerator.MoveNext())
                    throw new InvalidDataException("Not enough strings in translation file");

                int startOffset = _stringRanges[rangeIdx].Offset;
                int endOffset;
                bool hasXorKey = false;
                do
                {
                    CSystemStringRange range = _stringRanges[rangeIdx];
                    endOffset = range.Offset + range.Length;
                    hasXorKey |= range.HasXorKey;
                    rangeIdx++;
                } while (stringEnumerator.Current.Type != ScriptStringType.CharacterName && rangeIdx < _stringRanges.Count && _stringRanges[rangeIdx].Append);

                string text = stringEnumerator.Current.Text;
                text = text.Replace("<b>", "龠")
                           .Replace("</b>", "龠")
                           .Replace("<i>", "籥")
                           .Replace("</i>", "籥")
                           .Replace("<u>", "鑰")
                           .Replace("</u>", "鑰");

                if (stringEnumerator.Current.Type == ScriptStringType.CharacterName)
                    text = $"【{text}】";
                else
                    text = ProportionalWordWrapper.Default.Wrap(text);

                text = StringUtil.ToFullWidth(text);

                ArraySegment<byte> textBytes = EncodeString(text, hasXorKey);
                patcher.CopyUpTo(startOffset);
                patcher.ReplaceBytes(endOffset - startOffset, textBytes);
            }

            if (stringEnumerator.MoveNext())
                throw new InvalidDataException("Too many strings in translation file");

            patcher.CopyUpTo(_data.Length);
        }

        private static ArraySegment<byte> EncodeString(string text, bool withXorKey)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            foreach (string line in text.Split(new[] { "\r\n" }, StringSplitOptions.None))
            {
                byte[] textBytes = StringUtil.SjisTunnelEncoding.GetBytes(line);
                writer.Write(1 + 4 + textBytes.Length);     // Length
                writer.Write((byte)'S');                    // Type (string)
                if (withXorKey)
                {
                    byte xorKey = (byte)textBytes.Length;
                    writer.Write((int)xorKey);              // XOR key
                    for (int i = 0; i < textBytes.Length; i++)
                    {
                        textBytes[i] ^= xorKey;
                    }
                }

                writer.Write(textBytes);                    // Text
            }

            stream.TryGetBuffer(out ArraySegment<byte> result);
            return result;
        }

        private struct CSystemStringRange
        {
            public CSystemStringRange(int offset, int length, bool hasXorKey, bool append)
            {
                Offset = offset;
                Length = length;
                HasXorKey = hasXorKey;
                Append = append;
            }

            public int Offset
            {
                get;
            }

            public int Length
            {
                get;
            }

            public bool HasXorKey
            {
                get;
            }

            public bool Append
            {
                get;
            }

            public int ContentLengthOffset => Offset;

            public int TypeOffset => Offset + 4;

            public int XorKeyOffset => HasXorKey ? TypeOffset + 1 : -1;

            public int TextOffset => HasXorKey ? XorKeyOffset + 4 : TypeOffset + 1;

            public int TextLength => Offset + Length - TextOffset;
        }
    }
}
