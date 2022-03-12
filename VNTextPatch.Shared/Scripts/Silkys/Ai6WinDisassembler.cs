using System.Collections.Generic;
using System.IO;

namespace VNTextPatch.Shared.Scripts.Silkys
{
    internal class Ai6WinDisassembler : SilkysDisassemblerBase
    {
        private static readonly SilkysOpcodes Ai6WinOpcodes =
            new SilkysOpcodes
            {
                Yield = 0x00,
                Add = 0x34,
                EscapeSequence = 0x1B,
                Message1 = 0x0A,
                Message2 = 0x0B,
                PushInt = 0x32,
                PushString = 0x33,
                Syscall = 0x18,
                LineNumber = 0xFF,
                Nop1 = 0xFC,
                Nop2 = 0xFD,

                IsMessage1Obfuscated = false
            };

        private static readonly Dictionary<byte, string> Ai6WinOperandTemplates =
            new Dictionary<byte, string>
            {
                { 0x00, "" },       // yield
                { 0x01, "" },       // ret
                { 0x02, "" },       // ldglob1.i8
                { 0x03, "" },       // ldglob2.i16
                { 0x04, "" },       // ldglob3.var
                { 0x05, "" },       // ldglob4.var
                { 0x06, "" },       // ldloc.var
                { 0x07, "" },       // ldglob5.i8
                { 0x08, "" },       // ldglob5.i16
                { 0x09, "" },       // ldglob5.i32
                { 0x0A, "s" },      // message
                { 0x0B, "s" },      // message
                { 0x0C, "" },       // stglob1.i8
                { 0x0D, "" },       // stglob2.i16
                { 0x0E, "" },       // stglob3.var
                { 0x0F, "" },       // stglob4.var
                { 0x10, "" },       // stloc.var
                { 0x11, "" },       // stglob5.i8
                { 0x12, "" },       // stglob5.i16
                { 0x13, "" },       // stglob5.i32
                { 0x14, "a" },      // jz
                { 0x15, "a" },      // jmp
                { 0x16, "a" },      // libreg
                { 0x17, "" },       // libcall
                { 0x18, "" },       // syscall
                { 0x19, "i" },      // msgid
                { 0x1A, "a" },      // choice
                { 0x1B, "b" },      // escape sequence
                { 0x32, "i" },      // ldc.i4
                { 0x33, "s" },      // ldstr
                { 0x34, "" },       // add
                { 0x35, "" },       // sub
                { 0x36, "" },       // mul
                { 0x37, "" },       // div
                { 0x38, "" },       // mod
                { 0x39, "" },       // rand
                { 0x3A, "" },       // logand
                { 0x3B, "" },       // logor
                { 0x3C, "" },       // binand
                { 0x3D, "" },       // binor
                { 0x3E, "" },       // lt
                { 0x3F, "" },       // gt
                { 0x40, "" },       // le
                { 0x41, "" },       // ge
                { 0x42, "" },       // eq
                { 0x43, "" },       // neq
                { 0xFE, "" },
                { 0xFF, "" }
            };

        private static readonly SilkysSyscalls[] Ai6WinSyscalls =
        {
            new SilkysSyscalls
            {
                Exec = 31,
                ExecSetCharacterName = 15
            }
        };

        private readonly int _numMessages;

        public Ai6WinDisassembler(Stream stream)
            : base(stream)
        {
            _numMessages = _reader.ReadInt32();
            CodeOffset = 4 + _numMessages * 4;
        }

        public override SilkysOpcodes Opcodes => Ai6WinOpcodes;

        public override SilkysSyscalls[] Syscalls => Ai6WinSyscalls;

        protected override Dictionary<byte, string> OperandTemplates => Ai6WinOperandTemplates;

        public override int CodeOffset
        {
            get;
        }

        public override void ReadHeader()
        {
            for (int i = 0; i < _numMessages; i++)
            {
                RaiseLittleEndianAddressEncountered(4 + 4 * i);
            }
            Stream.Position = CodeOffset;
        }
    }
}
