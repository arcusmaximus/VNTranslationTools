using System;
using System.Collections.Generic;
using System.IO;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Ethornell
{
    public class EthornellV0Disassembler : EthornellDisassembler
    {
        /* Script version 0 has no header whatsoever. */

        private static readonly Dictionary<ushort, string> OperandTemplates =
            new Dictionary<ushort, string>
            {
                // h: short
                // i: int
                // c: code offset
                // m: message offset
                // z: inline zero-terminated string
                { 0x0010, "iim" },
                { 0x0011, "" },
                { 0x0012, "zz" },
                { 0x0013, "z" },
                { 0x0014, "z" },
                { 0x0015, "" },
                { 0x0018, "iiiii" },
                { 0x0019, "iiii" },     // untested
                { 0x001A, "iii" },
                { 0x001B, "ziii" },     // untested
                { 0x001F, "i" },        // untested
                { 0x0020, "" },
                { 0x0021, "" },
                { 0x0022, "i" },        // untested
                { 0x0024, "iiiii" },
                { 0x0025, "ii" },
                { 0x0028, "zi" },
                { 0x0029, "zzi" },
                { 0x002A, "i" },
                { 0x002B, "zi" },
                { 0x002C, "ziiiiiiii" },
                { 0x002D, "ziiiiiiii" },
                { 0x002E, "iiiii" },
                { 0x0030, "zi" },       // untested
                { 0x0031, "zii" },      // untested
                { 0x0032, "i" },        // untested
                { 0x0033, "i" },        // untested
                { 0x0034, "ii" },
                { 0x0035, "i" },
                { 0x0036, "i" },
                { 0x0037, "" },
                { 0x0038, "iziiiii" },
                { 0x0039, "ii" },
                { 0x003A, "iziiiiiiii" },   // untested
                { 0x003B, "iiiiii" },
                { 0x003C, "iiiiiiiiii" },
                { 0x003D, "iiiiiiiiiii" },
                { 0x003F, "i" },
                { 0x0040, "iizii" },
                { 0x0041, "iizii" },
                { 0x0042, "iizi" },
                { 0x0043, "iizi" },
                { 0x0044, "iizi" },
                { 0x0045, "iizi" },
                { 0x0046, "izi" },
                { 0x0047, "izi" },
                { 0x0048, "ii" },
                { 0x0049, "ii" },
                { 0x004A, "izi" },
                { 0x004B, "" },
                { 0x004C, "zi" },       // untested
                { 0x004D, "zi" },       // untested
                { 0x004E, "i" },        // untested
                { 0x004F, "i" },        // untested
                { 0x0050, "zi" },
                { 0x0051, "zzi" },
                { 0x0052, "i" },
                { 0x0053, "zi" },
                { 0x0054, "zii" },
                { 0x0060, "iiiii" },
                { 0x0061, "ii" },       // untested
                { 0x0062, "iiiiii" },
                { 0x0065, "i" },
                { 0x0066, "ii" },
                { 0x0067, "i" },
                { 0x0068, "i" },
                { 0x0069, "i" },
                { 0x006A, "i" },
                { 0x006B, "i" },        // untested
                { 0x006C, "i" },        // untested
                { 0x006E, "iii" },
                { 0x006F, "i" },
                { 0x0070, "izi" },
                { 0x0071, "i" },
                { 0x0072, "iii" },
                { 0x0073, "iii" },
                { 0x0074, "izi" },
                { 0x0075, "i" },
                { 0x0076, "iii" },
                { 0x0078, "izi" },      // untested
                { 0x0079, "i" },        // untested
                { 0x007A, "iii" },      // untested
                { 0x0080, "izii" },
                { 0x0081, "z" },
                { 0x0082, "i" },
                { 0x0083, "i" },
                { 0x0084, "izi" },      // untested
                { 0x0085, "z" },
                { 0x0086, "i" },
                { 0x0087, "i" },
                { 0x0088, "z" },
                { 0x008C, "i" },
                { 0x008D, "i" },        // untested
                { 0x008E, "i" },        // untested
                { 0x0090, "i" },        // untested
                { 0x0091, "i" },        // untested
                { 0x0092, "i" },
                { 0x0093, "i" },
                { 0x0094, "i" },
                { 0x0098, "ii" },
                { 0x0099, "ii" },
                { 0x009A, "ii" },       // untested
                { 0x009B, "ii" },       // untested
                { 0x009C, "ii" },       // untested
                { 0x009D, "ii" },       // untested
                { 0x00A0, "c" },
                { 0x00A1, "ic" },       // untested
                { 0x00A2, "ic" },       // untested
                { 0x00A3, "iic" },      // untested
                { 0x00A4, "iic" },
                { 0x00A5, "iic" },
                { 0x00A6, "iic" },
                { 0x00A7, "iic" },      // untested
                { 0x00A8, "iic" },
                { 0x00AC, "c" },        // untested
                { 0x00AD, "" },         // untested
                { 0x00AE, "i" },
                { 0x00AF, "" },         // untested
                { 0x00B8, "" },
                { 0x00B9, "i" },        // untested
                { 0x00BA, "i" },        // untested
                { 0x00C0, "z" },
                { 0x00C1, "z" },
                { 0x00C2, "" },
                { 0x00C4, "i" },
                { 0x00C8, "z" },
                { 0x00C9, "" },
                { 0x00CA, "i" },        // untested
                { 0x00D0, "" },         // untested
                { 0x00D4, "i" },        // untested
                { 0x00D8, "i" },
                { 0x00D9, "i" },
                { 0x00DA, "i" },
                { 0x00DB, "i" },
                { 0x00DC, "i" },
                { 0x00F8, "z" },        // untested
                { 0x00F9, "zi" },       // untested
                { 0x00FE, "h" },
                { 0x0110, "zz" },
                { 0x0111, "i" },
                { 0x0120, "i" },
                { 0x0121, "i" },
                { 0x0128, "zii" },
                { 0x012A, "ii" },
                { 0x0134, "ii" },       // untested
                { 0x0135, "i" },        // untested
                { 0x0136, "i" },        // untested
                { 0x0138, "iziiiiziii" },
                { 0x013B, "iiiiiiii" },
                { 0x0140, "iiziiii" },  // untested
                { 0x0141, "iiziiii" },  // untested
                { 0x0142, "iiziii" },   // untested
                { 0x0143, "iiziii" },   // untested
                { 0x0144, "iiziii" },   // untested
                { 0x0145, "iiziii" },   // untested
                { 0x0146, "iziii" },    // untested
                { 0x0147, "iziii" },    // untested
                { 0x0148, "ii" },
                { 0x0149, "ii" },
                { 0x014B, "ziiz" },
                { 0x0150, "zii" },
                { 0x0151, "ziii" },     // untested
                { 0x0152, "ii" },
                { 0x0153, "iii" },      // untested
                { 0x016E, "iiiiii" },
                { 0x016F, "iiiiiii" },  // untested
                { 0x0170, "izzii" },
                { 0x01C0, "zz" },
                { 0x01C1, "zz" },       // untested
                { 0x0249, "z" },        // untested
                { 0x024C, "zziii" },    // untested
                { 0x024D, "z" },        // untested
                { 0x024E, "zz" },       // untested
                { 0x024F, "z" }         // untested
            };

        private readonly Dictionary<ushort, Action> _operandReaders;

        public EthornellV0Disassembler(Stream stream)
            : base(stream)
        {
            _operandReaders =
                new Dictionary<ushort, Action>
                {
                    { 0x00A9, ReadOperands00A9 },
                    { 0x00B0, ReadOperands00B0 },
                    { 0x00B4, ReadOperands00B4 },
                    { 0x00FD, ReadOperands00FD },
                    { 0x0248, ReadOperands0248 }
                };
        }

        public override int CodeOffset
        {
            get { return 0; }
        }

        public override void Disassemble()
        {
            _reader.BaseStream.Position = CodeOffset;
            while (true)
            {
                ushort opcode = _reader.ReadUInt16();
                Action specializedReader = _operandReaders.GetOrDefault(opcode);
                if (specializedReader != null)
                    specializedReader();
                else
                    ReadOperands(OperandTemplates[opcode]);

                if (opcode == 0x00C2 && _largestCodeAddressOperandEncountered < (int)_reader.BaseStream.Position - CodeOffset)
                    break;
            }
        }

        private void ReadOperands00A9()
        {
            int count = _reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                ReadCodeAddress();
            }
        }

        private void ReadOperands00B0()
        {
            int count = _reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                SkipInlineStringOperand();
            }
        }

        private void ReadOperands00B4()
        {
            // Untested
            int count = _reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                SkipInlineStringOperand();
            }
        }

        private void ReadOperands00FD()
        {
            int count = _reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                SkipInlineStringOperand();
                ReadCodeAddress();
            }
        }

        private void ReadOperands0248()
        {
            throw new NotImplementedException();
        }
    }
}
