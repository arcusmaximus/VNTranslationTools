using System;
using System.IO;
using System.Text;

namespace VNTextPatch.Shared.Util
{
    internal static class IoExtensions
    {
        private static readonly byte[] TextBuffer = new byte[1024];

        public static void Skip(this BinaryReader reader, int length)
        {
            reader.BaseStream.Position += length;
        }

        public static byte PeekByte(this BinaryReader reader)
        {
            byte b = reader.ReadByte();
            reader.BaseStream.Position--;
            return b;
        }

        public static byte[] ReadByteArray(this BinaryReader reader)
        {
            int count = reader.ReadInt32();
            return reader.ReadBytes(count);
        }

        public static short[] ReadInt16Array(this BinaryReader reader)
        {
            return reader.ReadArray(r => r.ReadInt16());
        }

        public static int[] ReadInt32Array(this BinaryReader reader)
        {
            return reader.ReadArray(r => r.ReadInt32());
        }

        public static long[] ReadInt64Array(this BinaryReader reader)
        {
            return reader.ReadArray(r => r.ReadInt64());
        }

        public static double[] ReadDoubleArray(this BinaryReader reader)
        {
            return reader.ReadArray(r => r.ReadDouble());
        }

        public static string ReadCharArrayAsString(this BinaryReader reader)
        {
            int count = reader.ReadInt32();
            char[] chars = reader.ReadChars(count);
            return new string(chars);
        }

        public static T[] ReadArray<T>(this BinaryReader reader, Func<BinaryReader, T> readItem)
        {
            int count = reader.ReadInt32();
            T[] result = new T[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = readItem(reader);
            }

            return result;
        }

        public static string ReadZeroTerminatedSjisString(this BinaryReader reader)
        {
            int index = 0;
            byte b;
            while ((b = reader.ReadByte()) != 0)
            {
                TextBuffer[index++] = b;
            }
            return StringUtil.SjisEncoding.GetString(TextBuffer, 0, index);
        }

        public static string ReadZeroTerminatedUtf8String(this BinaryReader reader)
        {
            int index = 0;
            byte b;
            while ((b = reader.ReadByte()) != 0)
            {
                TextBuffer[index++] = b;
            }
            return Encoding.UTF8.GetString(TextBuffer, 0, index);
        }

        public static string ReadZeroTerminatedUtf16String(this BinaryReader reader)
        {
            int index = 0;
            while (true)
            {
                byte low = reader.ReadByte();
                byte high = reader.ReadByte();
                if (low == 0 && high == 0)
                    break;

                TextBuffer[index++] = low;
                TextBuffer[index++] = high;
            }
            return Encoding.Unicode.GetString(TextBuffer, 0, index);
        }

        public static int SkipZeroTerminatedSjisString(this BinaryReader reader)
        {
            long startPos = reader.BaseStream.Position;
            while (reader.ReadByte() != 0)
                ;
            return (int)(reader.BaseStream.Position - startPos);
        }

        public static int SkipZeroTerminatedUtf8String(this BinaryReader reader)
        {
            return reader.SkipZeroTerminatedSjisString();
        }

        public static int SkipZeroTerminatedUtf16String(this BinaryReader reader)
        {
            long startPos = reader.BaseStream.Position;
            while (reader.ReadUInt16() != 0)
                ;
            return (int)(reader.BaseStream.Position - startPos);
        }

        public static void Write(this BinaryWriter writer, ArraySegment<byte> data)
        {
            writer.Write(data.Array, data.Offset, data.Count);
        }

        public static void WriteArray(this BinaryWriter writer, byte[] items)
        {
            writer.Write(items.Length);
            writer.Write(items);
        }

        public static void WriteArray(this BinaryWriter writer, short[] items)
        {
            writer.WriteArray(items, (w, s) => w.Write(s));
        }

        public static void WriteArray(this BinaryWriter writer, int[] items)
        {
            writer.WriteArray(items, (w, i) => w.Write(i));
        }

        public static void WriteArray(this BinaryWriter writer, long[] items)
        {
            writer.WriteArray(items, (w, l) => w.Write(l));
        }

        public static void WriteArray(this BinaryWriter writer, double[] items)
        {
            writer.WriteArray(items, (w, d) => w.Write(d));
        }

        public static void WriteArray(this BinaryWriter writer, string s)
        {
            writer.WriteArray(s.ToCharArray(), (w, c) => w.Write(c));
        }

        public static void WriteArray<T>(this BinaryWriter writer, T[] items, Action<BinaryWriter, T> writeItem)
        {
            writer.Write(items.Length);
            foreach (T item in items)
            {
                writeItem(writer, item);
            }
        }

        public static int WriteZeroTerminatedSjisString(this BinaryWriter writer, string str)
        {
            int length = StringUtil.SjisTunnelEncoding.GetBytes(str, 0, str.Length, TextBuffer, 0);
            writer.Write(TextBuffer, 0, length);
            writer.Write((byte)0);
            return length + 1;
        }

        public static int WriteZeroTerminatedUtf8String(this BinaryWriter writer, string str)
        {
            int length = Encoding.UTF8.GetBytes(str, 0, str.Length, TextBuffer, 0);
            writer.Write(TextBuffer, 0, length);
            writer.Write((byte)0);
            return length + 1;
        }

        public static int WriteZeroTerminatedUtf16String(this BinaryWriter writer, string str)
        {
            int length = Encoding.Unicode.GetBytes(str, 0, str.Length, TextBuffer, 0);
            writer.Write(TextBuffer, 0, length);
            writer.Write((ushort)0);
            return length + 2;
        }
    }
}
