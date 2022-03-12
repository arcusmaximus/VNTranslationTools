using System.Collections.Generic;
using System.IO;

namespace VNTextPatch.Shared.Scripts.Silkys
{
    internal class SilkysPlusDisassembler : SilkysDisassemblerBase
    {
        private static readonly SilkysOpcodes SilkysPlusOpcodes =
            new SilkysOpcodes
            {
                Yield = 0x00,
                Add = 0x34,
                EscapeSequence = 0x1C,
                Message1 = 0x0A,
                Message2 = 0x0B,
                PushInt = 0x32,
                PushString = 0x33,
                Syscall = 0x18,
                LineNumber = 0xFF,
                Nop1 = 0xFC,
                Nop2 = 0xFD,

                IsMessage1Obfuscated = true
            };

        private static readonly Dictionary<byte, string> SilkysPlusOperandTemplates =
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
                { 0x0B, "t" },      // message
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
                { 0x1A, "i" },      // msgid2
                { 0x1B, "a" },      // choice
                { 0x1C, "b" },      // escape sequence
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
                { 0xFA, "" },
                { 0xFB, "" },
                { 0xFC, "" },
                { 0xFD, "" },
                { 0xFE, "" },
                { 0xFF, "" }
            };

        private static readonly SilkysSyscalls[] SilkysPlusSyscalls =
        {
            new SilkysSyscalls
            {
                Exec = 29,
                ExecSetCharacterName = 11
            },
            new SilkysSyscalls
            {
                Exec = 29,
                ExecSetCharacterName = 15
            }
        };

        private readonly int _numMessages;
        private readonly int _numSpecialMessages;

        public SilkysPlusDisassembler(Stream stream)
            : base(stream)
        {
            _numMessages = _reader.ReadInt32();
            _numSpecialMessages = _reader.ReadInt32();
            CodeOffset = 8 + 4 * (_numMessages + _numSpecialMessages);
        }

        public override SilkysOpcodes Opcodes => SilkysPlusOpcodes;

        protected override Dictionary<byte, string> OperandTemplates => SilkysPlusOperandTemplates;

        public override SilkysSyscalls[] Syscalls => SilkysPlusSyscalls;

        public override int CodeOffset
        {
            get;
        }

        public override void ReadHeader()
        {
            for (int i = 0; i < _numMessages + _numSpecialMessages; i++)
            {
                RaiseLittleEndianAddressEncountered(8 + 4 * i);
            }
            Stream.Position = CodeOffset;
        }
    }
}
