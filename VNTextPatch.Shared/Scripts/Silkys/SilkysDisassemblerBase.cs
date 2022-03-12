using System;
using System.Collections.Generic;
using System.IO;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Silkys
{
    internal abstract class SilkysDisassemblerBase
    {
        protected readonly BinaryReader _reader;

        protected SilkysDisassemblerBase(Stream stream)
        {
            Stream = stream;
            _reader = new BinaryReader(stream);
        }

        public Stream Stream
        {
            get;
        }

        public abstract SilkysOpcodes Opcodes
        {
            get;
        }

        protected abstract Dictionary<byte, string> OperandTemplates
        {
            get;
        }

        public abstract SilkysSyscalls[] Syscalls
        {
            get;
        }

        public abstract int CodeOffset
        {
            get;
        }

        public event Action<int> LittleEndianAddressEncountered;
        public event Action<int> BigEndianAddressEncountered;

        public abstract void ReadHeader();

        public (byte, List<object>) ReadInstruction()
        {
            byte opcode = _reader.ReadByte();
            List<object> operands = new List<object>();
            foreach (char operandType in OperandTemplates[opcode])
            {
                operands.Add(ReadOperand(operandType));
            }
            return (opcode, operands);
        }

        private object ReadOperand(char type)
        {
            switch (type)
            {
                case 'b':
                    return _reader.ReadByte();

                case 'i':
                    return BinaryUtil.FlipEndianness(_reader.ReadInt32());

                case 'a':
                    BigEndianAddressEncountered?.Invoke((int)Stream.Position);
                    return BinaryUtil.FlipEndianness(_reader.ReadInt32());

                case 's':
                case 't':
                    int startOffset = (int)Stream.Position;
                    _reader.SkipZeroTerminatedSjisString();
                    return new Range(startOffset, (int)Stream.Position - startOffset, ScriptStringType.Internal);

                default:
                    throw new ArgumentException();
            }
        }

        protected void RaiseLittleEndianAddressEncountered(int offset)
        {
            LittleEndianAddressEncountered?.Invoke(offset);
        }
    }
}
