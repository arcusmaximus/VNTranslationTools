using System;
using System.IO;
using System.Text;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Mware
{
    internal class SquirrelV2Disassembler
    {
        private readonly Stream _stream;
        private readonly BinaryReader _reader;
        private readonly Encoding _encoding;

        private readonly StreamWriter _writer;

        public SquirrelV2Disassembler(Stream stream, Encoding encoding, StreamWriter writer = null)
        {
            _stream = stream;
            _reader = new BinaryReader(stream);
            _encoding = encoding;

            _writer = writer;
        }

        public event Action<SquirrelLiteralPool> LiteralPoolEncountered;
        public event Action<SquirrelLiteralReference> TextReferenceEncountered;

        public void Disassemble()
        {
            HasHeader = TryReadHeader();

            ushort bytecodeTag = _reader.ReadUInt16();
            if (bytecodeTag != 0xFAFA)
                throw new InvalidDataException();

            ReadTag("SQIR");
            int charSize = _reader.ReadInt32();
            if (charSize != 1)
                throw new InvalidDataException();

            ReadFunction();

            ReadTag("TAIL");
        }

        public bool HasHeader
        {
            get;
            private set;
        }

        private bool TryReadHeader()
        {
            if (!TryReadTag("PRCS"))
                return false;

            int headerSize = _reader.ReadInt32();
            if (headerSize != 0x10)
                throw new InvalidDataException();

            int fileSize1 = _reader.ReadInt32();
            if (fileSize1 != _stream.Length - 0x14)
                throw new InvalidDataException();

            int fileSize2 = _reader.ReadInt32();
            if (fileSize2 != _stream.Length - 4)
                throw new InvalidDataException();

            return true;
        }

        private void ReadFunction()
        {
            ReadTag("PART");
            string sourceName = (string)ReadObject();
            string funcName = (string)ReadObject();

            _writer?.WriteLine();
            _writer?.WriteLine($"function {funcName}()");

            FunctionCounts counts = ReadCounts();
            SquirrelLiteralPool literals = ReadLiterals(counts);
            ReadParameters(counts);
            ReadOuterValues(counts);
            ReadLocals(counts);
            ReadLines(counts);
            ReadDefaultParams(counts);
            ReadInstructions(counts, literals);
            ReadChildFunctions(counts);

            int stackSize = _reader.ReadInt32();
            byte generator = _reader.ReadByte();
            byte varParams = _reader.ReadByte();
        }

        private FunctionCounts ReadCounts()
        {
            ReadTag("PART");
            return new FunctionCounts
                   {
                       LiteralCountOffset = (int)_stream.Position,
                       NumLiterals = _reader.ReadInt32(),
                       NumParameters = _reader.ReadInt32(),
                       NumOuterValues = _reader.ReadInt32(),
                       NumLocals = _reader.ReadInt32(),
                       NumLines = _reader.ReadInt32(),
                       NumDefaultParams = _reader.ReadInt32(),
                       NumInstructions = _reader.ReadInt32(),
                       NumFunctions = _reader.ReadInt32()
                   };
        }

        private SquirrelLiteralPool ReadLiterals(in FunctionCounts counts)
        {
            ReadTag("PART");
            SquirrelLiteralPool literals = new SquirrelLiteralPool
                                           {
                                               CountOffset = counts.LiteralCountOffset,
                                               Offset = (int)_stream.Position
                                           };
            for (int i = 0; i < counts.NumLiterals; i++)
            {
                literals.Values.Add(ReadObject());
            }
            literals.Length = (int)_stream.Position - literals.Offset;

            LiteralPoolEncountered?.Invoke(literals);

            return literals;
        }

        private void ReadParameters(in FunctionCounts counts)
        {
            ReadTag("PART");
            for (int i = 0; i < counts.NumParameters; i++)
            {
                object name = ReadObject();
            }
        }

        private void ReadOuterValues(in FunctionCounts counts)
        {
            ReadTag("PART");
            for (int i = 0; i < counts.NumOuterValues; i++)
            {
                int type = _reader.ReadInt32();
                object value = ReadObject();
                string name = (string)ReadObject();
            }
        }

        private void ReadLocals(in FunctionCounts counts)
        {
            ReadTag("PART");
            for (int i = 0; i < counts.NumLocals; i++)
            {
                string name = (string)ReadObject();
                int pos = _reader.ReadInt32();
                int startOp = _reader.ReadInt32();
                int endOp = _reader.ReadInt32();
            }
        }

        private void ReadLines(in FunctionCounts counts)
        {
            ReadTag("PART");
            _reader.Skip(counts.NumLines * 8);
        }

        private void ReadDefaultParams(in FunctionCounts counts)
        {
            ReadTag("PART");
            _reader.Skip(counts.NumDefaultParams * 4);
        }

        private void ReadInstructions(in FunctionCounts counts, SquirrelLiteralPool literals)
        {
            ScriptStringType? emitType = null;

            ReadTag("PART");
            for (int i = 0; i < counts.NumInstructions; i++)
            {
                Instruction instr = ReadInstruction();
                switch (instr.Opcode)
                {
                    case Opcode.GETK:
                        string fieldName = (string)literals.Values[instr.Arg1];
                        switch (fieldName)
                        {
                            case "TransText":
                            case "TransLog":
                            case "TransChoice":
                                emitType = ScriptStringType.Message;
                                break;
                        }

                        _writer?.WriteLine($"GETK({instr.Arg0}, \"{literals.Values[instr.Arg1]}\", {instr.Arg2})");
                        break;

                    case Opcode.PREPCALLK:
                        string funcName = (string)literals.Values[instr.Arg1];
                        switch (funcName)
                        {
                            case "Print":
                            case "SetChoice":
                            case "maintxt_print":
                                emitType = ScriptStringType.Message;
                                break;

                            case "speaker_name":
                                emitType = ScriptStringType.CharacterName;
                                break;
                        }

                        _writer?.WriteLine($"PREPCALLK({instr.Arg0}, \"{literals.Values[instr.Arg1]}\", {instr.Arg2}, {instr.Arg3})");
                        break;

                    case Opcode.LOAD:
                        if (emitType != null)
                            EmitArg1(instr, literals, emitType.Value);

                        _writer?.WriteLine($"LOAD({instr.Arg0}, \"{literals.Values[instr.Arg1]}\")");
                        break;

                    case Opcode.DLOAD:
                        if (emitType != null)
                        {
                            EmitArg1(instr, literals, emitType.Value);
                            EmitArg3(instr, literals, emitType.Value);
                        }

                        _writer?.WriteLine($"DLOAD({instr.Arg0}, \"{literals.Values[instr.Arg1]}\", {instr.Arg2}, \"{literals.Values[instr.Arg3]}\")");
                        break;

                    case Opcode.NEWSLOT:
                    case Opcode.CALL:
                        emitType = null;
                        _writer?.WriteLine($"{instr.Opcode}({instr.Arg0}, {instr.Arg1}, {instr.Arg2}, {instr.Arg3})");
                        break;

                    default:
                        _writer?.WriteLine($"{instr.Opcode}({instr.Arg0}, {instr.Arg1}, {instr.Arg2}, {instr.Arg3})");
                        break;
                }
            }
        }

        private void EmitArg1(Instruction instr, SquirrelLiteralPool literals, ScriptStringType type)
        {
            TextReferenceEncountered?.Invoke(new SquirrelLiteralReference(instr.Offset + 0, 4, literals, instr.Arg1, type));
        }

        private void EmitArg3(Instruction instr, SquirrelLiteralPool literals, ScriptStringType type)
        {
            TextReferenceEncountered?.Invoke(new SquirrelLiteralReference(instr.Offset + 7, 1, literals, instr.Arg3, type));
        }

        private Instruction ReadInstruction()
        {
            return new Instruction
                   {
                       Offset = (int)_stream.Position,
                       Arg1 = _reader.ReadInt32(),
                       Opcode = (Opcode)_reader.ReadByte(),
                       Arg0 = _reader.ReadByte(),
                       Arg2 = _reader.ReadByte(),
                       Arg3 = _reader.ReadByte()
                   };
        }

        private void ReadChildFunctions(in FunctionCounts counts)
        {
            ReadTag("PART");
            for (int i = 0; i < counts.NumFunctions; i++)
            {
                ReadFunction();
            }
        }

        private void ReadTag(string tag)
        {
            if (!TryReadTag(tag))
                throw new InvalidDataException();
        }

        private bool TryReadTag(string tag)
        {
            for (int i = 0; i < 4; i++)
            {
                if (_reader.ReadByte() != tag[3 - i])
                {
                    _reader.BaseStream.Position -= i + 1;
                    return false;
                }
            }
            return true;
        }

        private object ReadObject()
        {
            return SquirrelObject.Read(_reader, _encoding);
        }

        private struct FunctionCounts
        {
            public int LiteralCountOffset;
            public int NumLiterals;

            public int NumParameters;
            public int NumOuterValues;
            public int NumLocals;
            public int NumLines;
            public int NumDefaultParams;
            public int NumInstructions;
            public int NumFunctions;
        }

        private struct Instruction
        {
            public int Offset;

            public Opcode Opcode;
            public byte Arg0;
            public int Arg1;
            public byte Arg2;
            public byte Arg3;
        }

        private enum Opcode
        {
            LINE = 0x00,
            LOAD = 0x01,
            LOADINT = 0x02,
            LOADFLOAT = 0x03,
            DLOAD = 0x04,
            TAILCALL = 0x05,
            CALL = 0x06,
            PREPCALL = 0x07,
            PREPCALLK = 0x08,
            GETK = 0x09,
            MOVE = 0x0A,
            NEWSLOT = 0x0B,
            DELETE = 0x0C,
            SET = 0x0D,
            GET = 0x0E,
            EQ = 0x0F,
            NE = 0x10,
            ARITH = 0x11,
            BITW = 0x12,
            RETURN = 0x13,
            LOADNULLS = 0x14,
            LOADROOTTABLE = 0x15,
            LOADBOOL = 0x16,
            DMOVE = 0x17,
            JMP = 0x18,
            JNZ = 0x19,
            JZ = 0x1A,
            LOADFREEVAR = 0x1B,
            VARGC = 0x1C,
            GETVARGV = 0x1D,
            NEWTABLE = 0x1E,
            NEWARRAY = 0x1F,
            APPENDARRAY = 0x20,
            GETPARENT = 0x21,
            COMPARITH = 0x22,
            COMPARITHL = 0x23,
            INC = 0x24,
            INCL = 0x25,
            PINC = 0x26,
            PINCL = 0x27,
            CMP = 0x28,
            EXISTS = 0x29,
            INSTANCEOF = 0x2A,
            AND = 0x2B,
            OR = 0x2C,
            NEG = 0x2D,
            NOT = 0x2E,
            BWNOT = 0x2F,
            CLOSURE = 0x30,
            YIELD = 0x31,
            RESUME = 0x32,
            FOREACH = 0x33,
            POSTFOREACH = 0x34,
            DELEGATE = 0x35,
            CLONE = 0x36,
            TYPEOF = 0x37,
            PUSHTRAP = 0x38,
            POPTRAP = 0x39,
            THROW = 0x3A,
            CLASS = 0x3B,
            NEWSLOTA = 0x3C
        }
    }
}
