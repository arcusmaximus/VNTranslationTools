using System;
using System.Collections.Generic;
using System.IO;

namespace VNTextPatch.Shared.Scripts.Propeller
{
    internal class PropellerV1Disassembler
    {
        private static readonly Dictionary<short, string> OperandTemplates =
            new Dictionary<short, string>
            {
                { 0x0000, "bibbi" },
                { 0x0001, "bbihbii" },
                { 0x0002, "bi" },
                { 0x0003, "bi" },
                { 0x0004, "" },
                { 0x0005, "bib" },
                { 0x0006, "" },
                { 0x0007, "b" },
                { 0x0008, "s" },
                { 0x0009, "" },
                { 0x000A, "b" },
                { 0x000B, "" },
                { 0x000C, "" },
                { 0x000D, "" },
                { 0x000E, "" },
                { 0x000F, "" },
                { 0x0010, "bi" },
                { 0x0011, "bi" },
                { 0x0012, "" },
                { 0x0013, "" },
                { 0x0014, "" },
                { 0x0016, "b" },
                { 0x0017, "bi" },
                { 0x0020, "bibih" },
                { 0x0030, "" },
                { 0x0031, "" },
                { 0x0032, "" },
                { 0x0033, "bi" },
                { 0x0034, "bi" },
                { 0x0035, "" },
                { 0x0036, "b" },
                { 0x0037, "hibi" },

                { 0x0100, "s" },
                { 0x0101, "bs" },
                { 0x0102, "bi" },
                { 0x0103, "iss" },
                { 0x0104, "bs" },
                { 0x0105, "bibi" },
                { 0x0106, "bibi" },
                { 0x0107, "bibi" },
                { 0x0108, "bibibibibibibi" },
                { 0x0109, "sssb" },     // NamePrefix, NameSuffix, NameSeparator, ?
                { 0x010A, "bibi" },     // MaxMessageLineLength, MaxMessageLines
                { 0x010B, "bibis" },
                { 0x010C, "hi" },
                { 0x010D, "biss" },
                { 0x010E, "hibibibi" },
                { 0x010F, "bi" },
                { 0x0110, "hibibi" },
                { 0x0111, "bi" },
                { 0x0112, "bibibibibibibi" },   // MaxBacklogEntries, BacklogNameFontSize, ?, BacklogMessageFontSize, ?, MaxBacklogLineLength, MaxBacklogLinesPerEntry
                { 0x0113, "bibibibibi" },
                { 0x0114, "bi" },
                { 0x0115, "bs" },
                { 0x0116, "bi" },
                { 0x0117, "ss" },
                { 0x0118, "s" },
                { 0x0119, "sbi" },
                { 0x011A, "h" },
                { 0x011B, "hi" },
                { 0x011C, "hibi" },
                { 0x011D, "hibi" },
                { 0x011E, "ss" },
                { 0x011F, "sss" },

                { 0x0200, "bis" },
                { 0x0201, "bibibi" },
                { 0x0202, "bib" },
                { 0x0203, "bibibibibi" },
                { 0x0204, "bibi" },
                { 0x0205, "bibibi" },
                { 0x0206, "bibi" },
                { 0x0207, "bibi" },
                { 0x0208, "bibibibi" },
                { 0x0209, "bibibibibibi" },
                { 0x020A, "bibibi" },
                { 0x020B, "bibibibi" },
                { 0x020C, "bibibibib" },
                { 0x020D, "bibi" },
                { 0x020E, "bibi" },
                { 0x020F, "bibi" },
                { 0x0210, "bibibi" },
                { 0x0211, "bibibi" },
                { 0x0212, "bibibibibis" },
                { 0x0213, "bibi" },
                { 0x0214, "bisbibibib" },
                { 0x0215, "bibibibi" },
                { 0x0216, "bisbibibibi" },
                { 0x0217, "bib" },

                { 0x0300, "bisbib" },
                { 0x0301, "bibib" },
                { 0x0302, "bi" },
                { 0x0303, "sb" },

                { 0x0400, "bihi" },
                { 0x0401, "bib" },
                { 0x0402, "bib" },
                { 0x0403, "bibi" },
                { 0x0404, "bibi" },

                { 0x0500, "bis" },
                { 0x0501, "bibibibibibibibi" },     // ?, MessageX, MessageY, FontSize, ...
                { 0x0502, "bibibibibibibi" },       // ?, ?, MessageNameX, MessageNameY, MessageNameFontSize, ?, ?
                { 0x0503, "bibi" },
                { 0x0504, "bibi" },
                { 0x0505, "b" },
                { 0x0506, "" },
                { 0x0507, "h" },

                { 0x0600, "shi" },
                { 0x0601, "bi" },
                { 0x0602, "s" },
                { 0x0603, "" },
                { 0x0604, "bis" },
                { 0x0605, "bib" },
                { 0x0606, "bi" },
                { 0x0607, "" },
                { 0x0608, "bi" },
                { 0x0609, "bi" },
                { 0x060A, "s" },
                { 0x060B, "hibi" },
                { 0x060C, "bibibibi" },
                { 0x060D, "" },
                { 0x060E, "bibi" },
                { 0x060F, "bi" },
                { 0x0611, "bi" },
                { 0x0613, "bibi" },
                { 0x0614, "bibibi" },
                { 0x0615, "bi" }
            };

        private readonly Stream _stream;
        private readonly BinaryReader _reader;
        private readonly Dictionary<short, Action<object[]>> _opcodeHandlers;

        public PropellerV1Disassembler(Stream stream)
        {
            _stream = stream;
            _reader = new BinaryReader(stream);

            _opcodeHandlers =
                new Dictionary<short, Action<object[]>>
                {
                    { 0x0100, HandleSetGameTitle },
                    { 0x010D, HandleSetScenarioName },
                    { 0x0212, HandleSetChoiceOption },
                    { 0x0500, HandleMessage }
                };

            _stream.Position = 0;
            int signature = _reader.ReadInt16();
            if (signature != 0)
                throw new InvalidDataException("Invalid MSC signature");

            CodeOffset = _reader.ReadInt32();
        }

        public int CodeOffset
        {
            get;
        }

        public event Action<int> AddressEncountered;
        public event Action<Range> TextEncountered;

        public void Disassemble()
        {
            _stream.Position = 2 + 4;

            ReadLabelList();
            ReadLabelList();

            if (_stream.Position != CodeOffset)
                throw new InvalidDataException("Code offset doesn't match end of label lists");

            while (_stream.Position < _stream.Length)
            {
                ReadInstruction();
            }
        }

        private void ReadLabelList()
        {
            int listSize = _reader.ReadInt32();
            if (listSize % 9 != 0)
                throw new InvalidDataException("Label list size must be a multiple of 9");

            int listEndOffset = (int)_stream.Position + listSize;
            while (_stream.Position < listEndOffset)
            {
                byte marker = _reader.ReadByte();
                if (marker != 0)
                    throw new InvalidDataException("Label marker must be 0");

                int labelNumber = _reader.ReadInt32();
                AddressEncountered?.Invoke((int)_stream.Position);
                int labelAddress = _reader.ReadInt32();
            }
        }

        private void ReadInstruction()
        {
            byte opcodeHigh = _reader.ReadByte();
            byte opcodeLow = _reader.ReadByte();
            short opcode = (short)((opcodeHigh << 8) | opcodeLow);

            if (!OperandTemplates.TryGetValue(opcode, out string operandTypes))
                throw new InvalidDataException($"Unsupported opcode {opcode:X04} encountered");

            object[] operands = new object[operandTypes.Length];
            for (int i = 0; i < operandTypes.Length; i++)
            {
                operands[i] = operandTypes[i] switch
                              {
                                  'b' => _reader.ReadByte(),
                                  'h' => _reader.ReadInt16(),
                                  'i' => _reader.ReadInt32(),
                                  's' => ReadStringRange()
                              };
            }

            if (_opcodeHandlers.TryGetValue(opcode, out Action<object[]> handler))
                handler(operands);
        }

        private void HandleSetGameTitle(object[] operands)
        {
            TextEncountered?.Invoke((Range)operands[0]);
        }

        private void HandleSetScenarioName(object[] operands)
        {
            TextEncountered?.Invoke((Range)operands[2]);
        }

        private void HandleSetChoiceOption(object[] operands)
        {
            TextEncountered?.Invoke((Range)operands[10]);
        }

        private void HandleMessage(object[] operands)
        {
            TextEncountered?.Invoke((Range)operands[2]);
        }

        private Range ReadStringRange()
        {
            int offset = (int)_stream.Position;
            int length = _reader.ReadInt32();
            _stream.Position += length;
            return new Range(offset, 4 + length, ScriptStringType.Message);
        }
    }
}
