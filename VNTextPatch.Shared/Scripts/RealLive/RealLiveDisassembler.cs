using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.RealLive
{
    public class RealLiveDisassembler
    {
        private static readonly Dictionary<byte, ushort[]> GoToFunctions =
            new Dictionary<byte, ushort[]>
            {
                { 0x01, new ushort[] { 0x0000, 0x0001, 0x0002, 0x0005, 0x0006, 0x0007, 0x0010 } },
                { 0x05, new ushort[] { 0x0001, 0x0002, 0x0005, 0x0006, 0x0007 } }
            };

        private static readonly Dictionary<byte, ushort[]> ParameterlessGoToFunctions =
            new Dictionary<byte, ushort[]>
            {
                { 0x01, new ushort[] { 0x0000, 0x0005 } },
                { 0x05, new ushort[] { 0x0001, 0x0005 } }
            };

        private static readonly Dictionary<byte, ushort[]> GoToOnFunctions =
            new Dictionary<byte, ushort[]>
            {
                { 0x01, new ushort[] { 0x0003, 0x0008 } },
                { 0x05, new ushort[] { 0x0003, 0x0008 } }
            };

        private static readonly Dictionary<byte, ushort[]> GoToCaseFunctions =
            new Dictionary<byte, ushort[]>
            {
                { 0x01, new ushort[] { 0x0004, 0x0009 } },
                { 0x05, new ushort[] { 0x0004, 0x0009 } }
            };

        private static readonly Dictionary<byte, ushort[]> MessageFunctions =
            new Dictionary<byte, ushort[]>
            {
                { 0x03, new ushort[] { 0x0070 } }
            };

        private static readonly byte[] SceneEndMarker =
        {
            0x82, 0x72, 0x82, 0x85, 0x82, 0x85, 0x82, 0x8E, 0x82, 0x64, 0x82, 0x8E,
            0x82, 0x84, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
        };

        private const byte SelectModule = 0x02;

        private readonly Stream _stream;
        private readonly BinaryReader _reader;
        private readonly int _lineNumbersOffset;

        private byte? _currentModule;
        private ushort? _currentFunction;

        public RealLiveDisassembler(Stream stream)
        {
            _stream = stream;
            _reader = new BinaryReader(stream);

            _stream.Position = 8;
            _lineNumbersOffset = _reader.ReadInt32();

            _stream.Position = 0x20;
            CodeOffset = _reader.ReadInt32();
        }

        public void Disassemble()
        {
            _stream.Position = CodeOffset;
            while (Read())
            {
            }
        }

        public int CodeOffset
        {
            get;
        }

        public event Action<int> AddressEncountered;
        public event Action<Range> TextEncountered;

        private bool Read()
        {
            char opcode = (char)_reader.ReadByte();
            switch (opcode)
            {
                case '\0':
                    return false;

                case '\n':
                    _reader.ReadUInt16();
                    return true;

                case '!':
                case '@':
                    ReadKidokuFlag();
                    return true;

                case ',':
                case '?':
                    return true;

                case '#':
                    ReadFunctionCall();
                    return true;

                case '$':
                    ReadExpression();
                    return true;

                case '\\':
                case 'a':
                    _reader.ReadByte();
                    return true;

                case '(':
                    _stream.Position--;
                    ReadItemList('(', ')');
                    return true;

                case '{':
                    _stream.Position--;
                    ReadItemList('{', '}');
                    return true;

                case '"':
                    _stream.Position--;
                    ReadQuotedString();
                    return true;

                default:
                    _stream.Position--;
                    ReadUnquotedString();
                    return true;
            }
        }

        private void ReadKidokuFlag()
        {
            int lineNumberIndex = _reader.ReadUInt16();
            int pos = (int)_stream.Position;

            _stream.Position = _lineNumbersOffset + 4 * lineNumberIndex;
            int lineNumber = _reader.ReadInt32() - 1000000;
            if (lineNumber >= 0)
            {
                int entryPointOffset = 0x34 + lineNumber * 4;
                AddressEncountered?.Invoke(entryPointOffset);
            }

            _stream.Position = pos;
        }

        private void ReadFunctionCall()
        {
            byte type = _reader.ReadByte();
            _currentModule = _reader.ReadByte();
            _currentFunction = _reader.ReadUInt16();
            ushort numArgs = _reader.ReadUInt16();
            byte overload = _reader.ReadByte();

            if (!IsCurrentFunctionOneOf(ParameterlessGoToFunctions) && (char)_reader.PeekByte() == '(')
                ReadItemList('(', ')');

            if (IsCurrentFunctionOneOf(GoToFunctions))
                ReadGoTo();
            else if (IsCurrentFunctionOneOf(GoToOnFunctions))
                ReadGoToOn(numArgs);
            else if (IsCurrentFunctionOneOf(GoToCaseFunctions))
                ReadGoToCase();
            else if (_currentModule == SelectModule)
                ReadSelect();

            _currentModule = null;
            _currentFunction = null;
        }

        private bool IsInFunctionCall()
        {
            return _currentFunction != null;
        }

        private bool IsCurrentFunctionOneOf(Dictionary<byte, ushort[]> functions)
        {
            ushort[] functionsOfModule;
            return functions.TryGetValue(_currentModule.Value, out functionsOfModule) && functionsOfModule.Contains(_currentFunction.Value);
        }

        private void ReadGoTo()
        {
            ReadOffset();
        }

        private void ReadGoToOn(int numArgs)
        {
            char open = (char)_reader.ReadByte();
            if (open != '{')
                throw new InvalidDataException();

            for (int i = 0; i < numArgs; i++)
            {
                ReadOffset();
            }

            char close = (char)_reader.ReadByte();
            if (close != '}')
                throw new InvalidDataException();
        }

        private void ReadGoToCase()
        {
            ReadItemList('{', '}', ReadGoToCaseItem);
        }

        private void ReadGoToCaseItem()
        {
            ReadItemList('(', ')');
            ReadOffset();
        }

        private void ReadSelect()
        {
            if ((char)_reader.PeekByte() == '{')
                ReadItemList('{', '}', ReadSelectItem);
        }

        private void ReadSelectItem()
        {
            SkipDebugMarkers();

            if ((char)_reader.PeekByte() == '(')
            {
                _reader.ReadByte();

                if ((char)_reader.PeekByte() == '(')
                {
                    // Read condition
                    ReadItemList('(', ')');
                }

                // Read function
                _reader.ReadByte();

                // Read argument
                while ((char)_reader.PeekByte() != ')')
                {
                    Read();
                }
                _reader.ReadByte();
            }

            // Read text
            ReadString();

            SkipDebugMarkers();
        }

        private void ReadExpression()
        {
            byte variable = _reader.ReadByte();
            if (variable == 0xC8)
                return;

            if (variable == 0xFF)
            {
                _reader.ReadInt32();
                return;
            }

            ReadItemList('[', ']');
        }

        private void ReadItemList(char openChar, char closeChar)
        {
            ReadItemList(openChar, closeChar, () => Read());
        }

        private void ReadItemList(char openChar, char closeChar, Action readItem)
        {
            char c = (char)_reader.ReadByte();
            if (c != openChar)
                throw new InvalidDataException();

            while (true)
            {
                c = (char)_reader.PeekByte();
                if (c == closeChar)
                {
                    _reader.ReadByte();
                    return;
                }
                readItem();
            }
        }

        private void ReadOffset()
        {
            AddressEncountered?.Invoke((int)_stream.Position);
            _reader.ReadInt32();
        }

        private void ReadString()
        {
            if ((char)_reader.PeekByte() == '"')
                ReadQuotedString();
            else
                ReadUnquotedString();
        }

        private void ReadQuotedString()
        {
            int startPos = (int)_stream.Position;
            char open = (char)_reader.ReadByte();
            if (open != '"')
                throw new InvalidDataException();

            while (true)
            {
                byte b = _reader.ReadByte();
                if ((char)b == '\\')
                    _reader.ReadByte();
                else if (StringUtil.IsShiftJisLeadByte(b))
                    _reader.ReadByte();
                else if ((char)b == '"')
                    break;
            }
            int endPos = (int)_stream.Position;

            if (!IsInFunctionCall() || IsCurrentFunctionOneOf(MessageFunctions))
                TextEncountered?.Invoke(new Range(startPos, endPos - startPos, ScriptStringType.Message));
        }

        private void ReadUnquotedString()
        {
            int startPos = (int)_stream.Position;
            const string specialChars = "\0\n!@,?#$\\a(){}[]";
            while (true)
            {
                byte b = _reader.ReadByte();
                if (specialChars.IndexOf((char)b) >= 0)
                {
                    _stream.Position--;
                    break;
                }
                if (StringUtil.IsShiftJisLeadByte(b))
                    _reader.ReadByte();
            }
            int endPos = (int)_stream.Position;
            if ((!IsInFunctionCall() || IsCurrentFunctionOneOf(MessageFunctions)) && !RangeEquals(startPos, endPos - startPos, SceneEndMarker))
                TextEncountered?.Invoke(new Range(startPos, endPos - startPos, ScriptStringType.Message));
        }

        private void SkipDebugMarkers()
        {
            while ((char)_reader.PeekByte() == '\n')
            {
                Read();
            }
        }

        private bool RangeEquals(int offset, int length, byte[] compareTo)
        {
            if (length != compareTo.Length)
                return false;

            long pos = _stream.Position;

            _stream.Position = offset;
            for (int i = 0; i < length; i++)
            {
                if (_reader.ReadByte() != compareTo[i])
                {
                    _stream.Position = pos;
                    return false;
                }
            }
            _stream.Position = pos;
            return true;
        }
    }
}
