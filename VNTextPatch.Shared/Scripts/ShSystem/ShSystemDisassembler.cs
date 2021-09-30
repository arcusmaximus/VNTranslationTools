using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.ShSystem
{
    internal class ShSystemDisassembler
    {
        private static readonly Dictionary<byte, string> OperandTemplates =
            new Dictionary<byte, string>
            {
                { 0x00, "e" },
                { 0x01, "ee" },
                { 0x02, "lo" },
                { 0x03, "el" },
                { 0x04, "ee" },
                { 0x05, "e" },
                { 0x06, "seel" },
                { 0x07, "e" },
                { 0x08, "s" },
                { 0x09, "e" },
                { 0x0A, "" },
                { 0x0B, "e" },
                { 0x0C, "ee" },
                { 0x0D, "eee" },
                { 0x0E, "eeeeeeee" },
                { 0x0F, "eeee" },
                { 0x10, "eseeeee" },
                { 0x11, "esese" },
                { 0x12, "eeeee" },
                { 0x13, "es" },
                { 0x14, "eee" },
                { 0x15, "eeeeeeee" },
                { 0x16, "eeeeeeeeeeee" },
                { 0x17, "eeeseeee" },
                { 0x18, "eee" },
                { 0x19, "e" },
                { 0x1A, "e" },
                { 0x1B, "e" },
                { 0x1C, "ee" },
                { 0x1D, "e" },
                { 0x1E, "" },
                { 0x1F, "ee" },
                { 0x20, "" },
                { 0x21, "" },
                { 0x22, "" },
                { 0x23, "e" },
                { 0x24, "e" },
                { 0x25, "e" },
                { 0x26, "eo" },
                { 0x27, "eo" },
                { 0x28, "eoo" },
                { 0x29, "o" },
                { 0x2B, "s" },
                { 0x2C, "esee" },
                { 0x2D, "ese" },
                { 0x2E, "ese" },
                { 0x2F, "eee" },
                { 0x30, "" },
                { 0x31, "eeee" },
                { 0x32, "seeeee" },
                { 0x33, "e" },
                { 0x34, "s" },
                { 0x35, "ee" },
                { 0x37, "s" },
                { 0x38, "ee" },
                { 0x39, "" },
                { 0x3A, "eee" },
                { 0x3B, "eee" },
                { 0x3C, "e" },
                { 0x3D, "s" },
                { 0x3E, "es" },
                { 0x3F, "" },
                { 0x40, "e" },
                { 0x41, "se" },
                { 0x42, "ss" },
                { 0x43, "ee" },
                { 0x44, "eee" },
                { 0x45, "ees" },
                { 0x46, "ee" },
                { 0x47, "ee" },
                { 0x48, "eeee" },
                { 0x49, "ee" },
                { 0x4A, "ee" },
                { 0x4B, "eee" },
                { 0x4C, "eee" },
                { 0x4D, "eee" },
                { 0x4E, "ee" },
                { 0x4F, "e" },
                { 0x50, "ee" },
                { 0x51, "" },
                { 0x52, "es" },
                { 0x53, "ss" },
                { 0x54, "es" },
                { 0x55, "seee" },
                { 0x56, "es" },
                { 0x57, "s" },
                { 0x58, "se" },
                { 0x59, "esee" },
                { 0x5A, "es" },
                { 0x5B, "eeeeee" },
                { 0x5C, "e" },
                { 0x5D, "" },
                { 0x5E, "ss" },
                { 0x5F, "eee" },
                { 0x60, "eeeee" },
                { 0x61, "es" },
                { 0x62, "s" },
                { 0x63, "eeee" },
                { 0xFF, "" }
            };

        private readonly Stream _stream;
        private readonly BinaryReader _reader;
        private readonly bool _hasSourceInfo;
        private readonly Dictionary<byte, Action> _opcodeHandlers;

        public ShSystemDisassembler(Stream stream)
        {
            _stream = stream;
            _reader = new BinaryReader(stream);
            _opcodeHandlers =
                new Dictionary<byte, Action>
                {
                    { 0x03, HandleScriptCall },
                    { 0x2A, HandleSwitch },
                    { 0x36, Handle36 },
                };

            _stream.Position = 0;
            byte[] signature = _reader.ReadBytes(8);
            if (Encoding.ASCII.GetString(signature) != "SHSysSC\0")
                throw new InvalidDataException("Invalid SHSysSC signature");

            int fileSize = ReadShOffset();
            if (fileSize != _stream.Length)
                throw new InvalidDataException("Incorrect file size in SHSysSC header");

            _hasSourceInfo = _reader.ReadByte() != 0;
            _reader.Skip(4);
            if (_hasSourceInfo)
                _reader.SkipZeroTerminatedSjisString();

            CodeOffset = (int)_stream.Position;
        }

        public int CodeOffset
        {
            get;
        }

        public event Action<int> AddressEncountered;
        public event Action<int, List<ShValueRange>> ScriptCallEncountered;

        public void Disassemble()
        {
            _stream.Position = CodeOffset;
            while (_stream.Position < _stream.Length)
            {
                if (_hasSourceInfo)
                    _reader.Skip(2);        // Skip line number

                byte opcode = _reader.ReadByte();
                if (_opcodeHandlers.TryGetValue(opcode, out Action handler))
                    handler();
                else
                    SkipOperands(OperandTemplates[opcode]);
            }
        }

        private void SkipOperands(string template)
        {
            foreach (char type in template)
            {
                switch (type)
                {
                    case 'e':
                        SkipExpression();
                        break;

                    case 's':
                        SkipString();
                        break;

                    case 'l':
                        SkipList();
                        break;

                    case 'o':
                        AddressEncountered?.Invoke((int)_stream.Position);
                        _reader.Skip(3);
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }
        }

        private void HandleScriptCall()
        {
            if (!TryReadLiteralExpression(out int scriptIdx))
            {
                SkipExpression();
                SkipList();
                return;
            }

            List<ShValueRange> argRanges = SkipList();
            ScriptCallEncountered?.Invoke(scriptIdx, argRanges);
        }

        private void HandleSwitch()
        {
            SkipExpression();
            int numCases = ReadShShort();
            for (int i = 0; i < numCases; i++)
            {
                ReadShOffset();
            }
        }

        private void Handle36()
        {
            SkipExpression();
            byte type = _reader.ReadByte();
            if (type == 0)
                SkipExpression();
            else
                SkipString();
        }

        private int ReadShShort()
        {
            byte b1 = _reader.ReadByte();
            byte b0 = _reader.ReadByte();
            return (b1 << 8) | b0;
        }

        private int ReadShOffset()
        {
            AddressEncountered?.Invoke((int)_stream.Position);
            byte b2 = _reader.ReadByte();
            byte b1 = _reader.ReadByte();
            byte b0 = _reader.ReadByte();
            return (b2 << 16) | (b1 << 8) | b0;
        }

        private int ReadShInt()
        {
            byte b3 = _reader.ReadByte();
            byte b2 = _reader.ReadByte();
            byte b1 = _reader.ReadByte();
            byte b0 = _reader.ReadByte();
            return (b3 << 24) | (b2 << 16) | (b1 << 8) | b0;
        }

        private List<ShValueRange> SkipList()
        {
            List<ShValueRange> list = new List<ShValueRange>();

            while (true)
            {
                byte type = _reader.ReadByte();
                if (type == 0)
                    break;

                if (type == 1)
                    list.Add(SkipString());
                else
                    list.Add(SkipExpression());
            }

            return list;
        }

        private ShValueRange SkipString()
        {
            int startOffset = (int)_stream.Position;
            ShValueType rangeType;

            byte type = _reader.PeekByte();
            if (type < 0x20)
            {
                _reader.Skip(1);

                if (type == 0)
                {
                    rangeType = ShValueType.EmptyString;
                }
                else if (type < 6)
                {
                    rangeType = ShValueType.StringVariable;
                    _reader.Skip(1);
                }
                else if (type < 12)
                {
                    rangeType = ShValueType.StringVariable;
                    _reader.Skip(2);
                }
                else
                {
                    rangeType = ShValueType.StringExpression;
                    SkipExpression();
                }
            }
            else
            {
                rangeType = ShValueType.StringLiteral;
                _reader.SkipZeroTerminatedSjisString();
            }

            return new ShValueRange(startOffset, (int)_stream.Position - startOffset, rangeType);
        }

        private ShValueRange SkipExpression()
        {
            int startOffset = (int)_stream.Position;

            while (true)
            {
                byte tag = _reader.ReadByte();
                if (tag == 0xFF)
                    break;

                byte operation = (byte)(tag >> 4);
                byte index = (byte)(tag & 0xF);
                switch (operation)
                {
                    case 0:
                        if (index < 13)
                            ;
                        else if (index == 13)
                            _reader.Skip(1);
                        else if (index == 14)
                            _reader.Skip(2);
                        else
                            _reader.Skip(4);

                        break;

                    case 1:
                    case 2:
                    case 3:
                        if (index < 14)
                            ;
                        else if (index == 14)
                            _reader.Skip(1);
                        else
                            _reader.Skip(2);

                        break;
                }
            }

            return new ShValueRange(startOffset, (int)_stream.Position - startOffset, ShValueType.Expression);
        }

        private bool TryReadLiteralExpression(out int value)
        {
            int startOffset = (int)_stream.Position;

            byte valueTag = _reader.ReadByte();
            byte operation = (byte)(valueTag >> 4);
            byte index = (byte)(valueTag & 0x0F);

            if (operation != 0 || index < 13)
            {
                _stream.Position = startOffset;
                value = 0;
                return false;
            }

            if (index == 13)
                value = _reader.ReadByte();
            else if (index == 14)
                value = ReadShShort();
            else
                value = ReadShInt();

            byte endTag = _reader.ReadByte();
            if (endTag != 0xFF)
            {
                _stream.Position = startOffset;
                value = 0;
                return false;
            }

            return true;
        }

        public enum ShValueType
        {
            EmptyString,
            StringLiteral,
            StringVariable,
            StringExpression,
            Expression
        }

        public struct ShValueRange
        {
            public ShValueRange(int offset, int length, ShValueType type)
            {
                Offset = offset;
                Length = length;
                Type = type;
            }

            public int Offset;
            public int Length;
            public ShValueType Type;
        }
    }
}
