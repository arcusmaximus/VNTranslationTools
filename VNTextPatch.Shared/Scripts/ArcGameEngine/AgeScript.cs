using System.Collections.Generic;
using System.IO;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.ArcGameEngine
{
    public class AgeScript : IScript
    {
        private byte[] _scenario;
        private int _stringTableOffset;
        private int _arrayTableOffset;

        private readonly List<int> _textAddressOffsets = new List<int>();
        private readonly List<Range> _textRanges = new List<Range>();

        private readonly List<int> _arrayAddressOffsets = new List<int>();

        public string Extension => ".bin";

        public void Load(ScriptLocation location)
        {
            _scenario = File.ReadAllBytes(location.ToFilePath());
            _textAddressOffsets.Clear();
            _textRanges.Clear();
            _arrayAddressOffsets.Clear();
            using (MemoryStream stream = new MemoryStream(_scenario))
            {
                AgeDisassembler disassembler = new AgeDisassembler(stream);
                disassembler.TextAddressEncountered += offset => _textAddressOffsets.Add(offset);
                disassembler.TextEncountered += range => _textRanges.Add(range);
                disassembler.ArrayAddressEncountered += offset => _arrayAddressOffsets.Add(offset);
                disassembler.Disassemble();

                _stringTableOffset = disassembler.StringTableOffset;
                _arrayTableOffset = disassembler.ArrayTableOffset;
            }
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            foreach (Range range in _textRanges)
            {
                if (IsEmptyString(range))
                    continue;

                string text = GetString(range);
                yield return new ScriptString(text, range.Type);
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            using (Stream inputStream = new MemoryStream(_scenario))
            using (Stream outputStream = File.Open(location.ToFilePath(), FileMode.Create, FileAccess.ReadWrite))
            {
                BinaryPatcher patcher = new BinaryPatcher(inputStream, outputStream, AgeDisassembler.AddressToOffset, AgeDisassembler.OffsetToAddress);
                patcher.CopyUpTo(_stringTableOffset);

                MemoryStream stringsStream = new MemoryStream();
                using (IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator())
                {
                    for (int i = 0; i < _textRanges.Count; i++)
                    {
                        string text;
                        if (IsEmptyString(_textRanges[i]))
                            text = string.Empty;
                        else if (!stringEnumerator.MoveNext())
                            throw new InvalidDataException("Too few strings in translation file");
                        else
                            text = stringEnumerator.Current.Text;

                        patcher.PatchInt32(_textAddressOffsets[i], AgeDisassembler.OffsetToAddress(_stringTableOffset + (int)stringsStream.Length));
                        WriteString(stringsStream, text);
                    }

                    if (stringEnumerator.MoveNext())
                        throw new InvalidDataException("Too many strings in translation file");
                }

                patcher.ReplaceBytes(_arrayTableOffset - _stringTableOffset, stringsStream.GetBuffer(), 0, (int)stringsStream.Length);

                patcher.CopyUpTo(_scenario.Length);

                patcher.PatchAddress(0x28);
                patcher.PatchAddress(0x30);
                patcher.PatchAddress(0x38);
                foreach (int offset in _arrayAddressOffsets)
                {
                    patcher.PatchAddress(offset);
                }
            }
        }

        private string GetString(Range range)
        {
            byte[] data = new byte[range.Length];
            for (int i = 0; i < range.Length; i++)
            {
                data[i] = (byte)(_scenario[range.Offset + i] ^ 0xFF);
            }
            return StringUtil.SjisEncoding.GetString(data);
        }

        private bool IsEmptyString(Range range)
        {
            return _scenario[range.Offset] == 0xFF;
        }

        private static void WriteString(Stream stream, string text)
        {
            if (text == "/")
                text = string.Empty;

            int length = StringUtil.SjisEncoding.GetByteCount(text);
            int paddedLength = length + 1;
            if ((paddedLength & 3) != 0)
                paddedLength = (paddedLength & ~3) + 4;

            byte[] data = new byte[paddedLength];
            StringUtil.SjisEncoding.GetBytes(text, 0, text.Length, data, 0);
            for (int i = 0; i < paddedLength; i++)
            {
                data[i] ^= 0xFF;
            }
            stream.Write(data, 0, data.Length);
        }
    }
}
