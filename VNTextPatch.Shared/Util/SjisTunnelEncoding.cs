using System;
using System.Collections.Generic;
using System.Text;

namespace VNTextPatch.Shared.Util
{
    public class SjisTunnelEncoding : Encoding
    {
        private static readonly Encoding SjisEncoding = GetEncoding(932, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

        private readonly char[] _charArray = new char[1];
        private readonly Dictionary<char, char> _mappings = new Dictionary<char, char>();

        public SjisTunnelEncoding()
        {
        }

        public SjisTunnelEncoding(byte[] map)
        {
            SetMappingTable(map);
        }

        public override int GetByteCount(string str)
        {
            int byteCount = 0;
            for (int i = 0; i < str.Length; i++)
            {
                bool tunneled = _mappings.ContainsKey(str[i]);

                if (!tunneled)
                {
                    try
                    {
                        _charArray[0] = str[i];
                        byteCount += SjisEncoding.GetByteCount(_charArray);
                    }
                    catch
                    {
                        tunneled = true;
                    }
                }

                if (tunneled)
                    byteCount += 2;
            }
            return byteCount;
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            return GetByteCount(new string(chars, index, count));
        }

        public override int GetMaxByteCount(int charCount)
        {
            return charCount * 2;
        }

        public override byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[GetByteCount(str)];
            GetBytes(str, 0, str.Length, bytes, 0);
            return bytes;
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            return GetBytes(new string(chars, charIndex, charCount), 0, charCount, bytes, byteIndex);
        }

        public override int GetBytes(string str, int startCharIdx, int charCount, byte[] bytes, int startByteIdx)
        {
            int byteIdx = startByteIdx;
            for (int charIdx = startCharIdx; charIdx < startCharIdx + charCount; charIdx++)
            {
                bool tunneled = _mappings.TryGetValue(str[charIdx], out char tunnelChar);

                if (!tunneled)
                {
                    tunneled = !TrySjisEncode(str, charIdx, 1, bytes, byteIdx, out int numBytes);
                    if (tunneled)
                        tunnelChar = GetSjisTunnelChar(str[charIdx]);
                    else
                        byteIdx += numBytes;
                }

                if (tunneled)
                {
                    bytes[byteIdx++] = (byte)(tunnelChar >> 8);
                    bytes[byteIdx++] = (byte)tunnelChar;
                }
            }
            return byteIdx;
        }

        private static bool TrySjisEncode(string str, int charIdx, int numChars, byte[] bytes, int byteIdx, out int numBytes)
        {
            numBytes = 0;

            try
            {
                numBytes = SjisEncoding.GetBytes(str, charIdx, numChars, bytes, byteIdx);
                if (bytes[byteIdx] >= 0xF0)
                {
                    numBytes = 0;
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            throw new NotSupportedException();
        }

        public override int GetMaxCharCount(int byteCount)
        {
            throw new NotSupportedException();
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            throw new NotSupportedException();
        }

        public byte[] GetMappingTable()
        {
            byte[] table = new byte[_mappings.Count * 2];
            int byteIdx = 0;
            foreach (char c in _mappings.Keys)
            {
                table[byteIdx++] = (byte)c;
                table[byteIdx++] = (byte)(c >> 8);
            }
            return table;
        }

        public void SetMappingTable(byte[] table)
        {
            if (table.Length % 2 != 0)
                throw new ArgumentException();

            _mappings.Clear();
            for (int i = 0; i < table.Length; i += 2)
            {
                char c = (char)(table[i] | (table[i + 1] << 8));
                GetSjisTunnelChar(c);
            }
        }

        private char GetSjisTunnelChar(char origChar)
        {
            if (char.IsHighSurrogate(origChar) || char.IsLowSurrogate(origChar))
                throw new NotSupportedException("Surrogate chars not supported");

            char sjisChar;
            if (_mappings.TryGetValue(origChar, out sjisChar))
                return sjisChar;

            int sjisIdx = _mappings.Count;
            if (sjisIdx == 0x3B * 0x3B)
                throw new Exception("SJIS tunnel limit exceeded");

            int highSjisIdx = Math.DivRem(sjisIdx, 0x3B, out int lowSjisIdx);
            int highByte = highSjisIdx < 0x1F ? 0x81 + highSjisIdx : 0xE0 + (highSjisIdx - 0x1F);
            int lowByte = 1 + lowSjisIdx;
            if (lowByte >= '\t')
                lowByte++;
            if (lowByte >= '\n')
                lowByte++;
            if (lowByte >= '\r')
                lowByte++;
            if (lowByte >= ' ')
                lowByte++;

            sjisChar = (char)((highByte << 8) | lowByte);
            _mappings[origChar] = sjisChar;
            return sjisChar;
        }
    }
}
