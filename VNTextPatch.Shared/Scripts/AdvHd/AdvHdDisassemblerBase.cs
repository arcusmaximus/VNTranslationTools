using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.AdvHd
{
    internal abstract class AdvHdDisassemblerBase
    {
        private readonly Stream _stream;
        private readonly BinaryReader _reader;
        private readonly Dictionary<byte, string> _operandTemplates;
        private readonly Dictionary<byte, Action<List<object>>> _opcodeHandlers;

        protected AdvHdDisassemblerBase(Stream stream, Dictionary<byte, string> operandTemplates)
        {
            _stream = stream;
            _reader = new BinaryReader(stream);
            _operandTemplates = operandTemplates;
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
            string operandTemplate = _operandTemplates.GetOrDefault(opcode);
            if (operandTemplate == null)
                throw new InvalidDataException($"Invalid opcode encountered: {opcode:X02}");

            List<object> operands = ReadOperands(operandTemplate);
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
                {
                    int offset = (int)_stream.Position;
                    int addr = _reader.ReadInt32();
                    if (addr < 0 || addr >= _stream.Length)
                        throw new InvalidDataException();

                    AddressEncountered?.Invoke(offset);
                    return addr;
                }

                case 'f':
                    return _reader.ReadSingle();

                case 's':
                {
                    int offset = (int)_stream.Position;
                    _reader.SkipZeroTerminatedSjisString();
                    int length = (int)_stream.Position - offset;
                    return new Range(offset, length, ScriptStringType.Internal);
                }

                default:
                    throw new ArgumentException();
            }
        }
    }
}
