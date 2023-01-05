using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Kirikiri
{
    public class KirikiriTjsScript : IScript
    {
        private static readonly byte[] Magic = { 0x54, 0x4A, 0x53, 0x32, 0x31, 0x30, 0x30, 0x00 };
        private static readonly byte[] DataTag = { 0x44, 0x41, 0x54, 0x41 };

        private byte[] _bytes;
        private short[] _shorts;
        private int[] _ints;
        private long[] _longs;
        private double[] _doubles;
        private string[] _strings;
        private byte[][] _blobs;

        private byte[] _fileRemainder;

        public string Extension => ".tjs";

        public void Load(ScriptLocation location)
        {
            using (Stream stream = File.OpenRead(location.ToFilePath()))
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Unicode))
            {
                byte[] magic = reader.ReadBytes(8);
                if (!magic.SequenceEqual(Magic))
                    throw new InvalidDataException("Invalid magic");

                int fileSize = reader.ReadInt32();
                if (fileSize != stream.Length)
                    throw new InvalidDataException("Invalid file size");

                byte[] dataTag = reader.ReadBytes(4);
                if (!dataTag.SequenceEqual(DataTag))
                    throw new InvalidDataException("Invalid DATA tag");

                int dataSectionSize = reader.ReadInt32();
                ReadConstantArrays(reader);
                if (dataSectionSize != stream.Position - (Magic.Length + 4))
                    throw new InvalidDataException("Invalid DATA size");

                _fileRemainder = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
            }
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            return PatchableStringIndices.Select(i => new ScriptString(_strings[i], ScriptStringType.Message));
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            PatchStrings(strings);

            ArraySegment<byte> constantArrays;
            using (MemoryStream constantArrayStream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(constantArrayStream, Encoding.Unicode))
            {
                WriteConstantArrays(writer);
                constantArrayStream.TryGetBuffer(out constantArrays);
            }

            int dataSectionSize = DataTag.Length + 4 + constantArrays.Count;
            int fileSize = Magic.Length + 4 + dataSectionSize + _fileRemainder.Length;

            using (Stream stream = File.Open(location.ToFilePath(), FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.Unicode))
            {
                writer.Write(Magic);
                writer.Write(fileSize);
                writer.Write(DataTag);
                writer.Write(dataSectionSize);
                writer.Write(constantArrays);
                writer.Write(_fileRemainder);
            }
        }

        private IEnumerable<int> PatchableStringIndices
        {
            get
            {
                for (int i = 0; i < _strings.Length; i++)
                {
                    if (StringUtil.ContainsJapaneseText(_strings[i]))
                        yield return i;
                }
            }
        }

        private void PatchStrings(IEnumerable<ScriptString> strings)
        {
            using (IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator())
            {
                foreach (int patchIndex in PatchableStringIndices)
                {
                    if (!stringEnumerator.MoveNext())
                        throw new InvalidDataException("Script file does not have enough lines");

                    _strings[patchIndex] = stringEnumerator.Current.Text;
                }

                if (stringEnumerator.MoveNext())
                    throw new InvalidDataException("Script file has too many lines");
            }
        }

        private void ReadConstantArrays(BinaryReader reader)
        {
            _bytes = ReadByteArrayAligned(reader);
            _shorts = ReadInt16ArrayAligned(reader);
            _ints = reader.ReadInt32Array();
            _longs = reader.ReadInt64Array();
            _doubles = reader.ReadDoubleArray();
            _strings = reader.ReadArray(ReadCharsAsStringAligned);
            _blobs = reader.ReadArray(ReadByteArrayAligned);
        }

        private void WriteConstantArrays(BinaryWriter writer)
        {
            WriteArrayAligned(writer, _bytes);
            WriteArrayAligned(writer, _shorts);
            writer.WriteArray(_ints);
            writer.WriteArray(_longs);
            writer.WriteArray(_doubles);
            writer.WriteArray(_strings, WriteArrayAligned);
            writer.WriteArray(_blobs, WriteArrayAligned);
        }

        private static byte[] ReadByteArrayAligned(BinaryReader reader)
        {
            byte[] result = reader.ReadByteArray();
            Align(reader);
            return result;
        }

        private static short[] ReadInt16ArrayAligned(BinaryReader reader)
        {
            short[] result = reader.ReadInt16Array();
            Align(reader);
            return result;
        }

        private static string ReadCharsAsStringAligned(BinaryReader reader)
        {
            string str = reader.ReadCharArrayAsString();
            Align(reader);
            return str;
        }

        private static void Align(BinaryReader reader)
        {
            while (reader.BaseStream.Position % 4 != 0)
            {
                reader.ReadByte();
            }
        }

        private static void WriteArrayAligned(BinaryWriter writer, byte[] items)
        {
            writer.WriteArray(items);
            Align(writer);
        }

        private static void WriteArrayAligned(BinaryWriter writer, short[] items)
        {
            writer.WriteArray(items);
            Align(writer);
        }

        private static void WriteArrayAligned(BinaryWriter writer, string s)
        {
            writer.WriteArray(s);
            Align(writer);
        }

        private static void Align(BinaryWriter writer)
        {
            while (writer.BaseStream.Length % 4 != 0)
            {
                writer.Write((byte)0);
            }
        }
    }
}
