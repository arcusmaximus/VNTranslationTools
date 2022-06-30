using System;
using System.IO;
using System.Text;

namespace VNTextPatch.Shared.Util
{
    internal class GuessedEncoding : Encoding
    {
        private static readonly Encoding[] Encodings =
        {
            StringUtil.SjisTunnelEncoding,
            new UTF8Encoding(false, true)
        };

        private int _encodingIdx;

        private Encoding Encoding => Encodings[_encodingIdx];

        public override int GetByteCount(string s)
        {
            return Try(() => Encoding.GetByteCount(s));
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            return Try(() => Encoding.GetByteCount(chars, index, count));
        }

        public override byte[] GetBytes(string s)
        {
            return Try(() => Encoding.GetBytes(s));
        }

        public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            return Try(() => Encoding.GetBytes(s, charIndex, charCount, bytes, byteIndex));
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            return Try(() => Encoding.GetBytes(chars, charIndex, charCount, bytes, byteIndex));
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return Try(() => Encoding.GetCharCount(bytes, index, count));
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            return Try(() => Encoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex));
        }

        public override string GetString(byte[] bytes, int index, int count)
        {
            return Try(() => Encoding.GetString(bytes, index, count));
        }

        public override int GetMaxByteCount(int charCount)
        {
            return Encoding.GetMaxByteCount(charCount);
        }

        public override int GetMaxCharCount(int byteCount)
        {
            return Encoding.GetMaxCharCount(byteCount);
        }

        private T Try<T>(Func<T> func)
        {
            while (_encodingIdx < Encodings.Length)
            {
                try
                {
                    return func();
                }
                catch
                {
                    _encodingIdx++;
                }
            }

            throw new InvalidDataException();
        }
    }
}
