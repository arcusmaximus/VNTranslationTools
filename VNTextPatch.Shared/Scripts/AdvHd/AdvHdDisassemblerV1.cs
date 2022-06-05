using System.Collections.Generic;
using System.IO;

namespace VNTextPatch.Shared.Scripts.AdvHd
{
    internal class AdvHdDisassemblerV1 : AdvHdDisassemblerBase
    {
        private static readonly Dictionary<byte, string> OperandTemplates =
            new Dictionary<byte, string>
            {
                { 0x00, "" },
                { 0x01, "bhfaa" },  // Conditional jmp
                { 0x02, "a" },      // Unconditional jmp
                { 0x04, "s" },
                { 0x05, "" },
                { 0x06, "a" },      // Unconditional jmp
                { 0x07, "s" },
                { 0x08, "b" },
                { 0x09, "bhf" },
                { 0x0A, "hf" },
                { 0x0B, "hb" },
                { 0x0C, "hbh*" },
                { 0x0D, "hhf" },
                { 0x0E, "hhb" },
                { 0x0F, "b" },      // Choice screen
                { 0x11, "sf" },
                { 0x12, "sbs" },
                { 0x13, "" },
                { 0x14, "iss" },    // Message
                { 0x15, "s" },      // Character name
                { 0x16, "b" },
                { 0x17, "" },
                { 0x18, "bs" },
                { 0x19, "" },
                { 0x1A, "s" },
                { 0x1B, "b" },
                { 0x1C, "ssh" },
                { 0x1D, "h" },
                { 0x1E, "ssffhhb" },
                { 0x1F, "sf" },
                { 0x20, "sfh" },
                { 0x21, "shhh" },
                { 0x22, "sb" },
                { 0x28, "ssffhhbhhb" },
                { 0x29, "sf" },
                { 0x2A, "sfh" },
                { 0x2B, "s" },
                { 0x2C, "s" },
                { 0x2D, "sb" },
                { 0x2E, "" },
                { 0x2F, "shf" },
                { 0x32, "s" },
                { 0x33, "ssbb" },
                { 0x34, "ssbb" },
                { 0x35, "ssbbb" },
                { 0x36, "sfffffffbb" },
                { 0x37, "s" },
                { 0x38, "sb" },
                { 0x39, "sbbh*" },
                { 0x3A, "sbb" },
                { 0x3B, "sshhhffffffff" },
                { 0x3C, "s" },
                { 0x3D, "h" },
                { 0x3E, "" },
                { 0x3F, "s*" },
                { 0x40, "ssb" },
                { 0x41, "sb" },
                { 0x42, "sh" },
                { 0x43, "s" },
                { 0x44, "ssb" },
                { 0x45, "shffff" },
                { 0x46, "shbffff" },
                { 0x47, "sshbbfffffhf" },
                { 0x48, "sshbbs" },
                { 0x49, "sss" },
                { 0x4A, "ss" },
                { 0x4B, "shhffff" },
                { 0x4C, "shhbffff" },
                { 0x4D, "sshhbbfffffhf" },
                { 0x4E, "sshhbbs" },
                { 0x4F, "sshs" },
                { 0x50, "ssh" },
                { 0x51, "sshfb" },
                { 0x52, "ssfhfbs" },
                { 0x53, "ss" },
                { 0x54, "sss" },
                { 0x55, "ss" },
                { 0x56, "sbhfffffffffffbffffbhshssf" },
                { 0x57, "sh" },
                { 0x58, "ss" },
                { 0x59, "ssh" },
                { 0x5A, "sh*" },
                { 0x5B, "shb" },
                { 0x5C, "s" },
                { 0x5D, "ssb" },
                { 0x5E, "sff" },
                { 0x64, "b" },
                { 0x65, "hbffbs" },
                { 0x66, "s" },
                { 0x67, "bbhfffffb" },
                { 0x68, "b" },
                { 0x6E, "ss" },
                { 0x6F, "s" },
                { 0x70, "sh" },
                { 0x71, "" },
                { 0x72, "shhs" },
                { 0x73, "ssh" },
                { 0xFA, "" },
                { 0xFB, "b" },
                { 0xFC, "h" },
                { 0xFD, "" },
                { 0xFE, "s" },
                { 0xFF, "" }
        };

        public AdvHdDisassemblerV1(Stream stream)
            : base(stream, OperandTemplates)
        {
        }
    }
}
