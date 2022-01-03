using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.AdvHD
{
    public class AdvHdDisassembler
    {
        private static readonly Dictionary<byte, string> OperandTemplates =
            new Dictionary<byte, string>
            {
                { 0x00, "" },
                { 0x01, "bhfaa" },  // Conditional jmp
                { 0x02, "a" },      // Unconditional jmp
                { 0x04, "s" },
                { 0x05, "" },
                { 0x06, "a" },      // Unconditional jmp
                { 0x07, "s" },
                { 0x08, "b" },
                { 0x09, "bhf" },
                { 0x0A, "hf" },
                { 0x0B, "hb" },
                { 0x0C, "hbh*" },
                { 0x0D, "hhf" },
                { 0x0E, "hhb" },
                { 0x0F, "b" },      // Choice screen
                { 0x11, "sf" },
                { 0x12, "sbs" },
                { 0x13, "" },
                { 0x14, "iss" },    // Message
                { 0x15, "s" },      // Character name
                { 0x16, "b" },
                { 0x17, "" },
                { 0x18, "bs" },
                { 0x19, "" },
                { 0x1A, "s" },
                { 0x1B, "b" },
                { 0x1C, "sshb" },
                { 0x1D, "h" },
                { 0x1E, "ssffhhb" },
                { 0x1F, "sf" },
                { 0x20, "sfh" },
                { 0x21, "shhh" },
                { 0x22, "sb" },
                { 0x28, "ssffhhbhhb" },
                { 0x29, "sf" },
                { 0x2A, "sfh" },
                { 0x2B, "s" },
                { 0x2C, "s" },
                { 0x2D, "sb" },
                { 0x2E, "" },
                { 0x2F, "shf" },
                { 0x32, "s" },
                { 0x33, "ssbb" },
                { 0x34, "ssbb" },
                { 0x35, "ssbbb" },
                { 0x36, "sfffffffbb" },
                { 0x37, "s" },
                { 0x38, "sb" },
                { 0x39, "sbbh*" },
                { 0x3A, "sbb" },
                { 0x3B, "sshhhffffffff" },
                { 0x3C, "s" },
                { 0x3D, "h" },
                { 0x3E, "" },
                { 0x3F, "s*" },
                { 0x40, "ssb" },
                { 0x41, "sb" },
                { 0x42, "sh" },
                { 0x43, "s" },
                { 0x44, "ssb" },
                { 0x45, "shffff" },
                { 0x46, "shbffff" },
                { 0x47, "sshbbfffffhf" },
                { 0x48, "sshbbs" },
                { 0x49, "sss" },
                { 0x4A, "ss" },
                { 0x4B, "shhffff" },
                { 0x4C, "shhbffff" },
                { 0x4D, "sshhbbfffffhf" },
                { 0x4E, "sshhbbs" },
                { 0x4F, "sshs" },
                { 0x50, "ssh" },
                { 0x51, "sshfb" },
                { 0x52, "ssfhfbs" },
                { 0x53, "ss" },
                { 0x54, "sss" },
                { 0x55, "ss" },
                { 0x56, "sbhfffffffffffbffffbhshssf" },
                { 0x57, "sh" },
                { 0x58, "ss" },
                { 0x59, "ssh" },
                { 0x5A, "sh*" },
                { 0x5B, "shb" },
                { 0x5C, "s" },
                { 0x5D, "ssb" },
                { 0x5E, "sff" },
                { 0x5F, "s" },
                { 0x60, "hhhh" },
                { 0x61, "bffff" },
                { 0x62, "s" },
                { 0x63, "sb" },
                { 0x64, "b" },
                { 0x65, "hbffbs" },
                { 0x66, "s" },
                { 0x67, "bbhfffffb" },
                { 0x68, "b" },
                { 0x69, "sbbfffffhf" },
                { 0x6A, "shbbs" },
                { 0x6E, "ss" },
                { 0x6F, "s" },
                { 0x70, "sh" },
                { 0x71, "" },
                { 0x72, "shhs" },
                { 0x73, "ssh" },
                { 0x74, "ss" },
                { 0x75, "ss" },
                { 0x78, "ssbb" },
                { 0x79, "ssf" },
                { 0x7A, "ssfbbs" },
                { 0x7B, "ss" },
                { 0x7C, "ssf" },
                { 0x7D, "sf" },
                { 0x7E, "s" },
                { 0xF8, "" },
                { 0xF9, "bs" },
                { 0xFA, "" },
                { 0xFB, "b" },
                { 0xFC, "h" },
                { 0xFD, "" },
                { 0xFE, "s" },
                { 0xFF, "" }
            };

        private readonly Stream _stream;
        private readonly BinaryReader _reader;
        private readonly Dictionary<byte, Action<List<object>>> _opcodeHandlers;

        public AdvHdDisassembler(Stream stream)
        {
            _stream = stream;
            _reader = new BinaryReader(stream);
            _opcodeHandlers = new Dictionary<byte, Action<List<object>>>
                              {
                                  { 0x0F, HandleChoiceScreen },
                                  { 0x14, HandleMessage },
                                  { 0x15, HandleCharacterName }
                              };
        }

        public event Action<int> AddressEncountered;
        public event Action<Range> TextEncountered;

        public void Disassemble()
        {
            while (_stream.Position < _stream.Length - 8)
            {
                (byte opcode, List<object> operands) = ReadInstruction();
                if (_opcodeHandlers.TryGetValue(opcode, out Action<List<object>> handler))
                    handler(operands);
            }
        }

        public string DisassembleToText()
        {
            StringBuilder result = new StringBuilder();

            while (_stream.Position < _stream.Length - 8)
            {
                int offset = (int)_stream.Position;
                (byte opcode, List<object> operands) = ReadInstruction();
                if (_opcodeHandlers.TryGetValue(opcode, out Action<List<object>> handler))
                    handler(operands);

                result.AppendLine($"{offset:X08}: {opcode:X02} {string.Join(", ", operands.Select(OperandToString))}");
            }

            return result.ToString();
        }

        private string OperandToString(object o)
        {
            switch (o)
            {
                case int i:
                    return $"0x{i:X}";

                case float f:
                    return f.ToString(CultureInfo.InvariantCulture);

                case Range range:
                    long position = _stream.Position;
                    _stream.Position = range.Offset;
                    string str = _reader.ReadZeroTerminatedSjisString();
                    _stream.Position = position;
                    return "\"" + str + "\"";

                default:
                    return o.ToString();
            }
        }

        private void HandleCharacterName(List<object> operands)
        {
            Range range = (Range)operands[0];
            if (range.Length > 1)
            {
                range.Type = ScriptStringType.CharacterName;
                TextEncountered?.Invoke(range);
            }
        }

        private void HandleMessage(List<object> operands)
        {
            Range range = (Range)operands[2];
            if (range.Length > 1)
            {
                range.Type = ScriptStringType.Message;
                TextEncountered?.Invoke(range);
            }
        }

        private void HandleChoiceScreen(List<object> operands)
        {
            int numChoices = (byte)operands[0];
            for (int i = 0; i < numChoices; i++)
            {
                List<object> choiceOperands = ReadOperands("hsbh");
                operands.AddRange(choiceOperands);

                Range range = (Range)choiceOperands[1];
                if (range.Length > 1)
                {
                    range.Type = ScriptStringType.Message;
                    TextEncountered?.Invoke(range);
                }

                (byte jumpOpcode, List<object> jumpOperands) = ReadInstruction();
                operands.AddRange(jumpOperands);
            }
        }

        private (byte, List<object>) ReadInstruction()
        {
            byte opcode = _reader.ReadByte();
            List<object> operands = ReadOperands(OperandTemplates[opcode]);
            return (opcode, operands);
        }

        private List<object> ReadOperands(string template)
        {
            List<object> operands = new List<object>();
            for (int i = 0; i < template.Length; i++)
            {
                char type = template[i];
                if (i < template.Length - 1 && template[i + 1] == '*')
                {
                    i++;
                    operands.AddRange(ReadOperandArray(type));
                }
                else
                {
                    operands.Add(ReadOperand(type));
                }
            }
            return operands;
        }

        private IEnumerable<object> ReadOperandArray(char type)
        {
            int count = _reader.ReadByte();
            for (int i = 0; i < count; i++)
            {
                yield return ReadOperand(type);
            }
        }

        private object ReadOperand(char type)
        {
            switch (type)
            {
                case 'b':
                    return _reader.ReadByte();

                case 'h':
                    return _reader.ReadInt16();

                case 'i':
                    return _reader.ReadInt32();

                case 'a':
                    AddressEncountered?.Invoke((int)_stream.Position);
                    return _reader.ReadInt32();

                case 'f':
                    return _reader.ReadSingle();

                case 's':
                    int offset = (int)_stream.Position;
                    _reader.SkipZeroTerminatedSjisString();
                    int length = (int)_stream.Position - offset;
                    return new Range(offset, length, ScriptStringType.Internal);

                default:
                    throw new ArgumentException();
            }
        }
    }
}
