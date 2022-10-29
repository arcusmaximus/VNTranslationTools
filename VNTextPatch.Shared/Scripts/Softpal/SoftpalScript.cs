using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Softpal
{
    public class SoftpalScript : IScript
    {
        private byte[] _code;
        private byte[] _text;
        private readonly List<TextOperand> _textOperands = new List<TextOperand>();

        public string Extension => ".src";

        public void Load(ScriptLocation location)
        {
            string codeFilePath = location.ToFilePath();
            _code = File.ReadAllBytes(codeFilePath);

            string folderPath = Path.GetDirectoryName(codeFilePath);
            string textFilePath = Path.Combine(folderPath, "TEXT.DAT");
            if (!File.Exists(textFilePath))
                throw new FileNotFoundException($"TEXT.DAT not found at {textFilePath}");

            string pointFilePath = Path.Combine(folderPath, "POINT.DAT");
            if (!File.Exists(pointFilePath))
                throw new FileNotFoundException($"POINT.DAT not found at {pointFilePath}");

            _text = File.ReadAllBytes(textFilePath);
            _text[0] = (byte)'_';       // Explicitly mark as not encrypted
            List<int> labelOffsets = ReadPointDat(pointFilePath);

            _textOperands.Clear();
            using MemoryStream codeStream = new MemoryStream(_code);
            using StreamWriter writer = GetDisassemblyWriter(codeFilePath);
            SoftpalDisassembler disassembler = new SoftpalDisassembler(codeStream, labelOffsets, writer);
            disassembler.TextAddressEncountered += (offset, type) => _textOperands.Add(new TextOperand(offset, type));
            disassembler.Disassemble();
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            MemoryStream textStream = new MemoryStream(_text);
            BinaryReader textReader = new BinaryReader(textStream);

            foreach (TextOperand operand in _textOperands)
            {
                int addr = BitConverter.ToInt32(_code, operand.Offset);
                textStream.Position = addr + 4;
                string text = textReader.ReadZeroTerminatedSjisString();
                text = text.Replace("<br>", "\r\n");
                yield return new ScriptString(text, operand.Type);
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            string codeFilePath = location.ToFilePath();
            using Stream codeStream = File.Open(codeFilePath, FileMode.Create, FileAccess.Write);
            BinaryWriter codeWriter = new BinaryWriter(codeStream);
            codeWriter.Write(_code);

            string textFilePath = Path.Combine(Path.GetDirectoryName(codeFilePath), "TEXT.DAT");
            using Stream textStream = File.Open(textFilePath, FileMode.Create, FileAccess.Write);
            BinaryWriter textWriter = new BinaryWriter(textStream);
            textWriter.Write(_text);

            using IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator();
            foreach (TextOperand operand in _textOperands)
            {
                if (!stringEnumerator.MoveNext())
                    throw new InvalidDataException("Not enough lines in translation");

                if (stringEnumerator.Current.Type != operand.Type)
                    throw new InvalidDataException("String type mismatch");

                string text = stringEnumerator.Current.Text;
                text = ProportionalWordWrapper.Default.Wrap(text);
                text = text.Replace("\r\n", "<br>");

                int newAddr = (int)textStream.Length;
                textWriter.Write(0);
                textWriter.WriteZeroTerminatedSjisString(text);

                codeStream.Position = operand.Offset;
                codeWriter.Write(newAddr);
            }

            if (stringEnumerator.MoveNext())
                throw new InvalidDataException("Too many lines in translation");
        }

        private static List<int> ReadPointDat(string filePath)
        {
            using Stream stream = File.OpenRead(filePath);
            BinaryReader reader = new BinaryReader(stream);

            string magic = Encoding.ASCII.GetString(reader.ReadBytes(0x10));
            if (magic != "$POINT_LIST_****")
                throw new InvalidDataException("Failed to read POINT.DAT: invalid magic");

            List<int> labelOffsets = new List<int>();
            while (stream.Position < stream.Length)
            {
                labelOffsets.Add(SoftpalDisassembler.CodeOffset + reader.ReadInt32());
            }
            labelOffsets.Reverse();
            return labelOffsets;
        }

        private static StreamWriter GetDisassemblyWriter(string codeFilePath)
        {
            return null;

            Stream stream = File.Open(Path.ChangeExtension(codeFilePath, ".txt"), FileMode.Create, FileAccess.Write);
            return new StreamWriter(stream);
        }

        private readonly struct TextOperand
        {
            public TextOperand(int offset, ScriptStringType type)
            {
                Offset = offset;
                Type = type;
            }

            public readonly int Offset;
            public readonly ScriptStringType Type;
        }
    }
}
