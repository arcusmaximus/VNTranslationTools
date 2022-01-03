using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Majiro
{
    internal class MajiroDisassembler
    {
        private readonly Stream _stream;
        private readonly BinaryReader _reader;

        public MajiroDisassembler(Stream stream)
        {
            _stream = stream;
            _reader = new BinaryReader(stream);
        }

        public event Action<int> RelativeAddressEncountered;

        public (short, List<object>) ReadInstruction()
        {
            short opcode = _reader.ReadInt16();
            List<object> operands = ReadOperands(opcode);
            return (opcode, operands);
        }

        private List<object> ReadOperands(short opcode)
        {
            List<object> operands = new List<object>();
            foreach (char operandType in MajiroOpcodes.OperandTemplates[opcode])
            {
                switch (operandType)
                {
                    case 't':
                    {
                        int numTypes = _reader.ReadUInt16();
                        byte[] typeList = _reader.ReadBytes(numTypes);
                        operands.AddRange(typeList.Select(t => (object)(int)t));
                        break;
                    }

                    case 's':
                    {
                        int length = _reader.ReadUInt16();
                        if (length == 0)
                        {
                            operands.Add(string.Empty);
                        }
                        else
                        {
                            byte[] bytes = _reader.ReadBytes(length - 1);
                            if (_reader.ReadByte() != 0)
                                throw new InvalidDataException();

                            string str = StringUtil.SjisEncoding.GetString(bytes);
                            operands.Add(str);
                        }
                        break;
                    }

                    case 'f':
                    {
                        int flags = _reader.ReadUInt16();
                        operands.Add(flags);
                        break;
                    }

                    case 'h':
                    {
                        int nameHash = _reader.ReadInt32();
                        operands.Add(nameHash);
                        break;
                    }

                    case 'o':
                    {
                        int varOffset = _reader.ReadInt16();
                        operands.Add(varOffset);
                        break;
                    }

                    case '0':
                    {
                        int addressPlaceholder = _reader.ReadInt32();
                        operands.Add(addressPlaceholder);
                        break;
                    }

                    case 'i':
                    {
                        int value = _reader.ReadInt32();
                        operands.Add(value);
                        break;
                    }

                    case 'r':
                    {
                        float value = _reader.ReadSingle();
                        operands.Add(value);
                        break;
                    }

                    case 'a':
                    {
                        int argCount = _reader.ReadUInt16();
                        operands.Add(argCount);
                        break;
                    }

                    case 'j':
                    {
                        RelativeAddressEncountered?.Invoke((int)_stream.Position);
                        int branchOffset = _reader.ReadInt32();
                        operands.Add(branchOffset);
                        break;
                    }

                    case 'l':
                    {
                        int lineNum = _reader.ReadUInt16();
                        operands.Add(lineNum);
                        break;
                    }

                    case 'c':
                    {
                        int numCases = _reader.ReadUInt16();
                        for (int i = 0; i < numCases; i++)
                        {
                            RelativeAddressEncountered?.Invoke((int)_stream.Position);
                            int caseOffset = _reader.ReadInt32();
                            operands.Add(caseOffset);
                        }

                        break;
                    }
                }
            }
            return operands;
        }
    }
}
