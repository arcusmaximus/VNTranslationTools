using System;
using System.IO;
using System.Linq;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Ethornell
{
    public abstract class EthornellDisassembler
    {
        protected readonly BinaryReader _reader;
        protected int _largestCodeAddressOperandEncountered;

        protected EthornellDisassembler(Stream stream)
        {
            _reader = new BinaryReader(stream);
        }

        public static EthornellDisassembler Create(Stream stream)
        {
            if (StreamStartsWith(stream, EthornellV1Disassembler.Magic))
                return new EthornellV1Disassembler(stream);

            return new EthornellV0Disassembler(stream);
        }

        public abstract void Disassemble();

        public delegate void CodeAddressHandler(int offset, int address);
        public delegate void StringAddressHandler(int offset, int address, ScriptStringType type);

        public event CodeAddressHandler CodeAddressEncountered;
        public event StringAddressHandler StringAddressEncountered;

        public abstract int CodeOffset { get; }

        protected void ReadOperands(string template)
        {
            foreach (char c in template)
            {
                switch (c)
                {
                    case 'h':
                        ReadInt16Operand();
                        break;

                    case 'i':
                        ReadInt32Operand();
                        break;

                    case 'c':
                        ReadCodeAddress();
                        break;

                    case 'n':
                        ReadStringAddress(ScriptStringType.CharacterName);
                        break;

                    case 'm':
                        ReadStringAddress(ScriptStringType.Message);
                        break;

                    case 'z':
                        SkipInlineStringOperand();
                        break;

                    default:
                        throw new ArgumentException($"Unknown operand template character '{c}'");
                }
            }
        }

        private void ReadInt16Operand()
        {
            _reader.ReadInt16();
        }

        private void ReadInt32Operand()
        {
            _reader.ReadInt32();
        }

        protected void ReadCodeAddress()
        {
            int offset = (int)_reader.BaseStream.Position;
            int address = _reader.ReadInt32();
            OnCodeAddressEncountered(offset, address);
        }

        protected void ReadStringAddress(ScriptStringType type)
        {
            int offset = (int)_reader.BaseStream.Position;
            int address = _reader.ReadInt32();
            OnStringAddressEncountered(offset, address, type);
        }

        protected void SkipInlineStringOperand()
        {
            _reader.SkipZeroTerminatedSjisString();
        }

        protected bool IsEmptyString(int addr)
        {
            long prevPos = _reader.BaseStream.Position;
            _reader.BaseStream.Position = CodeOffset + addr;
            bool isEmpty = _reader.ReadByte() == 0;
            _reader.BaseStream.Position = prevPos;
            return isEmpty;
        }

        protected string ReadStringAtAddress(int addr)
        {
            long prevPos = _reader.BaseStream.Position;
            _reader.BaseStream.Position = CodeOffset + addr;
            string str = _reader.ReadZeroTerminatedSjisString();
            _reader.BaseStream.Position = prevPos;
            return str;
        }

        protected void OnCodeAddressEncountered(int offset, int address)
        {
            CodeAddressEncountered?.Invoke(offset, address);
            _largestCodeAddressOperandEncountered = Math.Max(_largestCodeAddressOperandEncountered, address);
        }

        protected void OnStringAddressEncountered(int offset, int address, ScriptStringType type)
        {
            StringAddressEncountered?.Invoke(offset, address, type);
        }

        private static bool StreamStartsWith(Stream stream, byte[] magic)
        {
            if (stream.Length < magic.Length)
                return false;

            byte[] data = new byte[magic.Length];
            stream.Read(data, 0, data.Length);
            stream.Position = 0;
            return data.SequenceEqual(magic);
        }
    }
}
