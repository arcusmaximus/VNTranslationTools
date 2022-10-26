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

        public static int IndexOf(byte[] data, byte[] search, int startIdx = 0, int length = -1)
        {
            if (length < 0)
                length = data.Length - startIdx;

            int endIdx = startIdx + length - search.Length;
            for (int i = startIdx; i <= endIdx; i++)
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

        public static void ReplaceInPlace(byte[] data, int offset, int length, Dictionary<byte[], byte[]> replacements)
        {
            int searchStartIdx = offset;
            int searchEndIdx = offset + length;
            while (searchStartIdx < searchEndIdx)
            {
                int foundIdx = -1;
                byte[] search = null;
                byte[] replace = null;
                foreach (KeyValuePair<byte[], byte[]> replacement in replacements)
                {
                    int index = IndexOf(data, replacement.Key, searchStartIdx, searchEndIdx - searchStartIdx);
                    if (index < 0)
                        continue;

                    if (foundIdx < 0 || index < foundIdx || (index == foundIdx && replacement.Key.Length > search.Length))
                    {
                        foundIdx = index;
                        search = replacement.Key;
                        replace = replacement.Value;
                    }
                }

                if (foundIdx < 0)
                    break;

                if (search.Length != replace.Length)
                    throw new ArgumentException();

                for (int i = 0; i < search.Length; i++)
                {
                    data[foundIdx + i] = replace[i];
                }
                searchStartIdx = foundIdx + search.Length;
            }
        }

        public static byte[] Replace(byte[] input, Dictionary<byte[], byte[]> replacements)
        {
            return Replace(input, 0, input.Length, replacements);
        }

        public static byte[] Replace(byte[] input, int offset, int length, Dictionary<byte[], byte[]> replacements)
        {
            MemoryStream inputStream = new MemoryStream(input);
            MemoryStream outputStream = new MemoryStream();
            BinaryPatcher patcher = new BinaryPatcher(inputStream, outputStream);
            int searchStartIdx = offset;
            int searchEndIdx = offset + length;
            while (searchStartIdx < searchEndIdx)
            {
                int foundIdx = -1;
                byte[] search = null;
                byte[] replace = null;

                foreach (KeyValuePair<byte[], byte[]> replacement in replacements)
                {
                    int index = IndexOf(input, replacement.Key, searchStartIdx, searchEndIdx - searchEndIdx);
                    if (index < 0)
                        continue;

                    if (foundIdx < 0 || index < foundIdx || (index == foundIdx && replacement.Key.Length > search.Length))
                    {
                        foundIdx = index;
                        search = replacement.Key;
                        replace = replacement.Value;
                    }
                }

                if (foundIdx < 0)
                    break;

                patcher.CopyUpTo(foundIdx);
                patcher.ReplaceBytes(search.Length, replace);
                searchStartIdx = foundIdx + search.Length;
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
