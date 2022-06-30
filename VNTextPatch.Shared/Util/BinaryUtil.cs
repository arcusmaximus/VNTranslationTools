using System;
using System.Collections.Generic;
using System.IO;

namespace VNTextPatch.Shared.Util
{
    internal static class BinaryUtil
    {
        public static void Xor(byte[] data, int offset, int length, byte key)
        {
            byte[] repeatedKey = { key, key, key, key, key, key, key, key };
            Xor(data, offset, length, repeatedKey);
        }

        public static void Xor(byte[] data, int offset, int length, uint key)
        {
            byte[] repeatedKey =
                {
                    (byte)key,
                    (byte)(key >> 8),
                    (byte)(key >> 16),
                    (byte)(key >> 24),
                    (byte)key,
                    (byte)(key >> 8),
                    (byte)(key >> 16),
                    (byte)(key >> 24)
                };
            Xor(data, offset, length, repeatedKey);
        }

        public static unsafe void Xor(byte[] data, int offset, int length, byte[] key)
        {
            if (offset < 0 || length < 0 || offset + length > data.Length)
                throw new ArgumentOutOfRangeException();

            fixed (byte* pData = data)
            fixed (byte* pKey = key)
            {
                int keyOffset = 0;
                while (length > 0)
                {
                    int chunkLength = Math.Min(length, key.Length - keyOffset);
                    if (chunkLength >= 8)
                    {
                        chunkLength = 8;
                        *(ulong*)(pData + offset) ^= *(ulong*)(pKey + keyOffset);
                    }
                    else if (chunkLength >= 4)
                    {
                        chunkLength = 4;
                        *(uint*)(pData + offset) ^= *(uint*)(pKey + keyOffset);
                    }
                    else
                    {
                        chunkLength = 1;
                        pData[offset] ^= pKey[keyOffset];
                    }
                    offset += chunkLength;
                    length -= chunkLength;
                    keyOffset += chunkLength;
                    if (keyOffset == key.Length)
                        keyOffset = 0;
                }
            }
        }

        public static int IndexOf(byte[] data, byte[] search, int startIdx = 0)
        {
            for (int i = startIdx; i <= data.Length - search.Length; i++)
            {
                bool equal = true;
                for (int j = 0; j < search.Length; j++)
                {
                    if (data[i + j] != search[j])
                    {
                        equal = false;
                        break;
                    }
                }
                if (equal)
                    return i;
            }
            return -1;
        }

        public static byte[] Replace(byte[] input, Dictionary<byte[], byte[]> replacements)
        {
            MemoryStream inputStream = new MemoryStream(input);
            MemoryStream outputStream = new MemoryStream();
            BinaryPatcher patcher = new BinaryPatcher(inputStream, outputStream);
            int nextIdx = 0;
            while (nextIdx < input.Length)
            {
                int inputIdx = -1;
                byte[] search = null;
                byte[] replace = null;

                foreach (KeyValuePair<byte[], byte[]> replacement in replacements)
                {
                    int index = IndexOf(input, replacement.Key, nextIdx);
                    if (index < 0)
                        continue;

                    if (inputIdx < 0 || index < inputIdx || (index == inputIdx && replacement.Key.Length > search.Length))
                    {
                        inputIdx = index;
                        search = replacement.Key;
                        replace = replacement.Value;
                    }
                }

                if (inputIdx < 0)
                    break;

                patcher.CopyUpTo(inputIdx);
                patcher.ReplaceBytes(search.Length, replace);
                nextIdx = inputIdx + search.Length;
            }

            patcher.CopyUpTo(input.Length);
            return outputStream.ToArray();
        }

        public static void ReplaceSjisCodepoint(byte[] data, int offset, int length, ushort origChar, ushort newChar)
        {
            for (int i = offset; i < offset + length; i += StringUtil.IsShiftJisLeadByte(data[i]) ? 2 : 1)
            {
                if (data[i] == (byte)(origChar >> 8) && data[i + 1] == (byte)origChar)
                {
                    data[i] = (byte)(newChar >> 8);
                    data[i + 1] = (byte)newChar;
                }
            }
        }

        public static int FlipEndianness(int value)
        {
            return ((value >> 24) & 0xFF) |
                   ((value >> 8) & 0xFF00) |
                   ((value << 8) & 0xFF0000) |
                   (value << 24);
        }
    }
}
