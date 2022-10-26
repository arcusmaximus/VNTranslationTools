using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.ArcGameEngine
{
    internal class AgeDisassembler
    {
        private static readonly Dictionary<int, int> OperandCounts =
            new Dictionary<int, int>
            {
                { 0x0001, 0 },
                { 0x0002, 0 },
                { 0x0003, 1 },
                { 0x0004, 2 },
                { 0x0005, 0 },
                { 0x0006, 2 },
                { 0x0007, 1 },
                { 0x0008, 1 },
                { 0x0009, 0 },
                { 0x000A, 2 },
                { 0x000B, 11 },
                { 0x000C, 0 },
                { 0x000D, 4 },
                { 0x000E, 12 },
                { 0x000F, 1 },
                { 0x0010, 4 },
                { 0x0011, 9 },
                { 0x0012, 1 },
                { 0x0013, 4 },
                { 0x0014, 0 },
                { 0x0015, 5 },
                { 0x0016, 2 },
                { 0x0017, 2 },
                { 0x001E, 8 },
                { 0x001F, 12 },
                { 0x0020, 6 },
                { 0x0021, 2 },
                { 0x0022, 2 },
                { 0x0023, 2 },
                { 0x0024, 2 },
                { 0x0025, 3 },
                { 0x0026, 4 },
                { 0x0027, 4 },
                { 0x0028, 4 },
                { 0x002A, 4 },
                { 0x002B, 5 },
                { 0x002C, 5 },
                { 0x002D, 12 },
                { 0x002E, 5 },
                { 0x002F, 4 },
                { 0x0030, 5 },
                { 0x0031, 4 },
                { 0x0032, 10 },
                { 0x0033, 6 },
                { 0x0034, 12 },
                { 0x0035, 11 },
                { 0x0036, 3 },
                { 0x0037, 11 },
                { 0x0038, 12 },
                { 0x0050, 3 },
                { 0x0051, 3 },
                { 0x0052, 3 },
                { 0x0053, 3 },
                { 0x0054, 3 },
                { 0x0055, 2 },
                { 0x0056, 3 },
                { 0x0057, 3 },
                { 0x0058, 3 },
                { 0x0059, 3 },
                { 0x005A, 3 },
                { 0x005B, 3 },
                { 0x005C, 3 },
                { 0x005D, 3 },
                { 0x005E, 3 },
                { 0x005F, 3 },
                { 0x0060, 2 },
                { 0x0061, 3 },
                { 0x0062, 3 },
                { 0x0063, 2 },
                { 0x0064, 2 },
                { 0x0065, 2 },
                { 0x0066, 3 },
                { 0x0067, 3 },
                { 0x0068, 3 },
                { 0x0069, 3 },
                { 0x006A, 3 },
                { 0x006B, 3 },
                { 0x006C, 2 },
                { 0x006D, 0 },
                { 0x006E, 2 },
                { 0x006F, 1 },
                { 0x0070, 5 },
                { 0x0071, 1 },
                { 0x0072, 1 },
                { 0x0073, 10 },
                { 0x0074, 1 },
                { 0x0075, 1 },
                { 0x0076, 1 },
                { 0x0077, 1 },
                { 0x0078, 1 },
                { 0x0079, 3 },
                { 0x007A, 3 },
                { 0x007B, 2 },
                { 0x007C, 0 },
                { 0x007D, 2 },
                { 0x007E, 1 },
                { 0x007F, 1 },
                { 0x0080, 1 },
                { 0x0081, 1 },
                { 0x0082, 5 },
                { 0x0083, 3 },
                { 0x0084, 1 },
                { 0x0085, 0 },
                { 0x0086, 1 },
                { 0x0087, 0 },
                { 0x0088, 1 },
                { 0x0089, 4 },
                { 0x008A, 6 },
                { 0x008B, 1 },
                { 0x008C, 1 },
                { 0x008D, 2 },
                { 0x008E, 1 },
                { 0x008F, 1 },
                { 0x0090, 7 },
                { 0x0091, 1 },
                { 0x0092, 2 },
                { 0x0093, 0 },
                { 0x0094, 0 },
                { 0x0095, 2 },
                { 0x0096, 0 },
                { 0x0097, 5 },
                { 0x00A0, 3 },
                { 0x00A1, 0 },
                { 0x00A2, 2 },
                { 0x00A3, 2 },
                { 0x00AA, 2 },
                { 0x00AB, 2 },
                { 0x00AC, 9 },
                { 0x00AD, 0 },
                { 0x00AE, 0 },
                { 0x00AF, 0 },
                { 0x00B0, 1 },
                { 0x00B1, 1 },
                { 0x00B2, 2 },
                { 0x00B3, 0 },
                { 0x00B4, 2 },
                { 0x00B5, 1 },
                { 0x00B6, 1 },
                { 0x00B7, 1 },
                { 0x00B8, 0 },
                { 0x00B9, 1 },
                { 0x00BA, 1 },
                { 0x00BB, 1 },
                { 0x00BC, 1 },
                { 0x00BD, 1 },
                { 0x00BE, 1 },
                { 0x00BF, 1 },
                { 0x00C0, 1 },
                { 0x00C1, 0 },
                { 0x00C2, 2 },
                { 0x00C3, 1 },
                { 0x00C4, 1 },
                { 0x00C5, 2 },
                { 0x00C6, 2 },
                { 0x00C7, 2 },
                { 0x00C8, 1 },
                { 0x00C9, 0 },
                { 0x00CA, 0 },
                { 0x00CB, 1 },
                { 0x00CC, 2 },
                { 0x00CD, 0 },
                { 0x00CE, 3 },
                { 0x00CF, 0 },
                { 0x00D0, 1 },
                { 0x00D1, 0 },
                { 0x00D2, 1 },
                { 0x00D3, 0 },
                { 0x00D4, 4 },
                { 0x00D5, 1 },
                { 0x00D6, 6 },
                { 0x00D7, 1 },
                { 0x00D8, 2 },
                { 0x00D9, 0 },
                { 0x00DA, 6 },
                { 0x00FA, 0 },
                { 0x00FB, 2 },
                { 0x00FC, 0 },
                { 0x00FD, 2 },
                { 0x00FE, 1 },
                { 0x00FF, 0 },
                { 0x0100, 0 },
                { 0x0101, 0 },
                { 0x0102, 3 },
                { 0x0103, 1 },
                { 0x0104, 0 },
                { 0x0105, 1 },
                { 0x0106, 1 },
                { 0x0107, 2 },
                { 0x0108, 1 },
                { 0x0109, 2 },
                { 0x010A, 2 },
                { 0x010B, 2 },
                { 0x010C, 2 },
                { 0x010D, 1 },
                { 0x010E, 2 },
                { 0x010F, 1 },
                { 0x012C, 5 },
                { 0x012D, 7 },
                { 0x012E, 8 },
                { 0x012F, 4 },
                { 0x0130, 1 },
                { 0x0131, 1 },
                { 0x0132, 1 },
                { 0x0133, 2 },
                { 0x0134, 3 },
                { 0x0135, 2 },
                { 0x0136, 2 },
                { 0x0137, 1 },
                { 0x0138, 2 },
                { 0x0139, 3 },
                { 0x013A, 6 },
                { 0x013B, 7 },
                { 0x013C, 1 },
                { 0x013D, 3 },
                { 0x013E, 2 },
                { 0x013F, 3 },
                { 0x0140, 4 },
                { 0x0141, 1 },
                { 0x0142, 1 },
                { 0x0143, 0 },
                { 0x0144, 2 },
                { 0x0145, 1 },
                { 0x0146, 1 },
                { 0x0147, 6 },
                { 0x0148, 1 },
                { 0x0149, 1 },
                { 0x014A, 7 },
                { 0x014B, 1 },
                { 0x014C, 2 },
                { 0x014D, 6 },
                { 0x0190, 2 },
                { 0x0191, 2 },
                { 0x0192, 2 },
                { 0x0193, 3 },
                { 0x0194, 3 },
                { 0x0195, 3 },
                { 0x0196, 3 },
                { 0x0197, 1 },
                { 0x0198, 3 },
                { 0x0199, 0 },
                { 0x019A, 1 },
                { 0x019B, 0 },
                { 0x019C, 0 },
                { 0x019D, 2 },
                { 0x019E, 2 },
                { 0x019F, 2 },
                { 0x01A0, 9 },
                { 0x01A1, 2 },
                { 0x01A2, 1 },
                { 0x01A3, 1 },
                { 0x01A4, 2 },
                { 0x01A5, 1 },
                { 0x01A6, 2 },
                { 0x01A7, 1 },
                { 0x01A8, 0 },
                { 0x01A9, 1 },
                { 0x01AA, 1 },
                { 0x01AB, 2 },
                { 0x01AC, 3 },
                { 0x01AD, 0 },
                { 0x01AE, 3 },
                { 0x01AF, 3 },
                { 0x01B0, 3 },
                { 0x01B1, 1 },
                { 0x01B2, 1 },
                { 0x01B3, 0 },
                { 0x01B4, 0 },
                { 0x01B5, 1 },
                { 0x01B6, 1 },
                { 0x01B7, 1 },
                { 0x01B8, 2 },
                { 0x01B9, 2 },
                { 0x01BA, 2 },
                { 0x01BB, 1 },
                { 0x01BC, 0 },
                { 0x01BD, 1 },
                { 0x01BE, 2 },
                { 0x01BF, 0 },
                { 0x01C0, 1 },
                { 0x01C1, 3 },
                { 0x01C2, 2 },
                { 0x01C3, 2 },
                { 0x01C4, 1 },
                { 0x01C5, 4 },
                { 0x01C6, 2 },
                { 0x01C7, 1 },
                { 0x01C8, 2 },
                { 0x01C9, 3 },
                { 0x01CA, 1 },
                { 0x01CB, 1 },
                { 0x01CC, 1 },
                { 0x01CD, 2 },
                { 0x01CE, 1 },
                { 0x01CF, 1 },
                { 0x01D0, 3 },
                { 0x01D1, 5 },
                { 0x01D2, 2 },
                { 0x01D3, 5 },
                { 0x01D4, 4 },
                { 0x01D5, 0 },
                { 0x01D6, 2 },
                { 0x01D7, 2 },
                { 0x01D8, 3 },
                { 0x01D9, 2 },
                { 0x01F4, 0 },
                { 0x01F5, 0 },
                { 0x01F6, 0 },
                { 0x01F7, 2 },
                { 0x01F8, 4 },
                { 0x01F9, 3 },
                { 0x01FA, 1 },
                { 0x01FB, 8 },
                { 0x01FC, 1 },
                { 0x01FD, 4 },
                { 0x01FE, 5 },
                { 0x01FF, 4 },
                { 0x0200, 1 },
                { 0x0201, 1 },
                { 0x0202, 5 },
                { 0x0203, 4 },
                { 0x0204, 4 },
                { 0x0205, 6 },
                { 0x0206, 7 },
                { 0x0207, 8 },
                { 0x0208, 3 },
                { 0x0209, 5 },
                { 0x020A, 1 },
                { 0x020B, 7 },
                { 0x020C, 0 },
                { 0x020D, 1 },
                { 0x020E, 0 },
                { 0x020F, 3 },
                { 0x0210, 1 },
                { 0x0211, 1 },
                { 0x0212, 2 },
                { 0x0213, 3 },
                { 0x0214, 2 },
                { 0x0215, 2 },
                { 0x0216, 2 },
                { 0x0217, 4 },
                { 0x0218, 4 },
                { 0x0219, 4 },
                { 0x021A, 4 },
                { 0x021B, 1 },
                { 0x021C, 0 },
                { 0x021D, 2 },
                { 0x021E, 6 },
                { 0x021F, 7 },
                { 0x0220, 6 },
                { 0x0221, 4 },
                { 0x0222, 2 },
                { 0x0223, 8 },
                { 0x0224, 0 },
                { 0x0225, 2 },
                { 0x0226, 5 },
                { 0x0227, 6 },
                { 0x0228, 5 },
                { 0x0229, 5 },
                { 0x022A, 3 },
                { 0x022B, 4 },
                { 0x022C, 3 },
                { 0x022D, 5 },
                { 0x022E, 6 },
                { 0x022F, 5 },
                { 0x0230, 1 },
                { 0x0231, 4 },
                { 0x0232, 4 },
                { 0x0233, 5 },
                { 0x0234, 5 },
                { 0x0235, 5 },
                { 0x0236, 4 },
                { 0x0237, 2 },
                { 0x0238, 1 },
                { 0x0239, 6 },
                { 0x023A, 2 },
                { 0x023B, 7 },
                { 0x023C, 0 },
                { 0x023D, 0 },
                { 0x023E, 2 },
                { 0x023F, 2 },
                { 0x0240, 4 },
                { 0x0241, 5 },
                { 0x0242, 2 },
                { 0x0243, 0 },
                { 0x0244, 0 },
                { 0x0245, 2 },
                { 0x0246, 2 },
                { 0x0247, 1 },
                { 0x0248, 1 },
                { 0x0249, 3 },
                { 0x024A, 3 },
                { 0x024D, 12 },
                { 0x024E, 1 },
                { 0x024F, 10 },
                { 0x0250, 10 },
                { 0x0251, 12 },
                { 0x0252, 1 },
                { 0x0253, 2 },
                { 0x0254, 5 },
                { 0x0256, 5 },
                { 0x0257, 5 },
                { 0x0258, 2 },
                { 0x0259, 0 },
                { 0x025A, 1 },
                { 0x025B, 1 },
                { 0x025C, 8 },
                { 0x025D, 3 },
                { 0x025E, 5 },
                { 0x025F, 4 },
                { 0x0260, 4 },
                { 0x0261, 1 },
                { 0x02BC, 11 },
                { 0x02BD, 1 },
                { 0x02BE, 1 },
                { 0x02BF, 3 },
                { 0x02C0, 3 },
                { 0x02C1, 1 },
                { 0x02C2, 6 },
                { 0x02C3, 2 },
                { 0x02C4, 0 },
                { 0x02C5, 2 },
                { 0x02C6, 2 },
                { 0x02C7, 4 },
                { 0x02C8, 4 },
                { 0x02C9, 3 },
                { 0x02CC, 1 },
                { 0x02CD, 1 },
                { 0x02CE, 1 },
                { 0x02CF, 1 },
                { 0x02D0, 3 },
                { 0x02D1, 3 },
                { 0x02D2, 3 },
                { 0x02D3, 3 },
                { 0x02D5, 2 },
                { 0x02D7, 2 },
                { 0x02D8, 3 },
                { 0x02D9, 2 },
                { 0x02DA, 8 },
                { 0x02DB, 1 },
                { 0x02DC, 1 },
                { 0x02DD, 2 },
                { 0x02DE, 2 },
                { 0x02DF, 3 },
                { 0x02E0, 3 },
                { 0x02E1, 3 },
                { 0x02E2, 3 },
                { 0x02E3, 3 },
                { 0x02E4, 3 },
                { 0x02E5, 1 },
                { 0x02E6, 2 },
                { 0x02E7, 2 },
                { 0x02E8, 1 },
                { 0x02E9, 1 },
                { 0x02EA, 1 },
                { 0x02EB, 1 },
                { 0x02EC, 2 },
                { 0x02EE, 1 },
                { 0x02EF, 11 },
                { 0x02F0, 9 },
                { 0x02F1, 7 },
                { 0x02F2, 6 },
                { 0x02F3, 6 },
                { 0x02F4, 3 },
                { 0x02F5, 4 },
                { 0x02F6, 1 },
                { 0x02F7, 1 },
                { 0x02F8, 2 },
                { 0x02F9, 7 },
                { 0x02FA, 1 },
                { 0x02FB, 1 },
                { 0x02FC, 5 },
                { 0x02FD, 6 },
                { 0x02FE, 1 },
                { 0x02FF, 2 },
                { 0x0300, 3 },
                { 0x0301, 1 },
                { 0x0302, 2 },
                { 0x0303, 3 },
                { 0x0304, 0 },
                { 0x0305, 0 },
                { 0x0306, 1 },
                { 0x0307, 1 },
                { 0x0308, 1 },
                { 0x030A, 2 },
                { 0x0320, 10 },
                { 0x0321, 3 },
                { 0x0322, 4 },
                { 0x0323, 5 },
                { 0x0324, 0 },
                { 0x0325, 2 },
                { 0x0326, 4 },
                { 0x0327, 1 },
                { 0x0328, 3 },
                { 0x0329, 2 },
                { 0x032A, 1 },
                { 0x032B, 0 },
                { 0x032C, 6 },
                { 0x032D, 2 },
                { 0x032E, 11 },
                { 0x032F, 1 },
                { 0x0330, 2 },
                { 0x0332, 4 },
                { 0x0334, 1 },
                { 0x0335, 4 },
                { 0x0337, 4 },
                { 0x033B, 4 },
                { 0x033D, 3 },
                { 0x033E, 5 },
                { 0x033F, 3 },
                { 0x0340, 1 },
                { 0x0341, 2 },
                { 0x0342, 1 },
                { 0x0344, 2 },
                { 0x0345, 3 },
                { 0x0349, 4 },
                { 0x034D, 6 },
                { 0x034E, 4 },
                { 0x0352, 3 }
            };

        private static readonly Dictionary<int, int[]> AddressOperands =
            new Dictionary<int, int[]>
            {
                { 0x0064, new[] { 1 } },        // Array
                { 0x007B, new[] { 0, 1 } },
                { 0x008C, new[] { 0 } },        // Unconditional jump
                { 0x008D, new[] { 1 } },        // Register choice
                { 0x008F, new[] { 0 } },        // Call
                { 0x0090, new[] { 4, 5, 6 } },  // Register choice
                { 0x0092, new[] { 1 } },
                { 0x0095, new[] { 1 } },
                { 0x00A0, new[] { 1, 2 } },     // Conditional jump
                { 0x00A2, new[] { 1 } },        // Register named label?
                { 0x00A3, new[] { 1 } },        // Jump to named label?
                { 0x00CC, new[] { 1 } },
                { 0x00CE, new[] { 1, 2 } },
                { 0x00D4, new[] { 2 } },
                { 0x00D5, new[] { 0 } },
                { 0x00D6, new[] { 4 } },
                { 0x00FB, new[] { 1 } },
                { 0x0102, new[] { 2 } }
            };

        private static readonly Dictionary<byte[], byte[]> TextReplacements =
            new Dictionary<byte[], byte[]>
            {
                { new byte[] { 0xF0, 0x40 }, new byte[] { 0x81, 0x63 } },
                { new byte[] { 0xF0, 0x41 }, new byte[] { 0x81, 0x5C } },
                { new byte[] { 0xF0, 0x42 }, new byte[] { 0x81, 0x5C } },
                { new byte[] { 0xF0, 0x43 }, new byte[] { 0x81, 0x5C } }
            };

        private readonly Stream _stream;
        private readonly BinaryReader _reader;
        private readonly TextWriter _writer;
        private readonly byte[] _textBuffer = new byte[0x200];

        public AgeDisassembler(Stream stream, TextWriter writer = null)
        {
            _stream = stream;
            _reader = new BinaryReader(stream);
            _writer = writer;
        }

        public delegate void AddressHandler(int addrOffset, bool isStringAddr);

        public event AddressHandler AddressEncountered;

        public Range StringPoolRange
        {
            get;
            private set;
        }

        public List<AgeInstruction> Disassemble()
        {
            string magic = Encoding.ASCII.GetString(_reader.ReadBytes(4));
            if (magic != "SYS4")
                throw new InvalidDataException("Invalid AGE header");

            _reader.Skip(4);            // Version number
            _reader.Skip(6 * 4);        // Runtime variables counts

            int addrTableHeaderSize = _reader.ReadInt32();
            if (addrTableHeaderSize != 0x1C)
                throw new InvalidDataException("Invalid address table header size");

            List<AddressTable> addressTables = new List<AddressTable>();
            for (int i = 0; i < 3; i++)
            {
                int count = _reader.ReadInt32();
                AddressEncountered?.Invoke((int)_stream.Position, false);
                int addr = _reader.ReadInt32();
                addressTables.Add(new AddressTable(AddressToOffset(addr), count));
            }

            StringPoolRange = new Range(addressTables[0].Offset, 0, ScriptStringType.Internal);
            List<AgeInstruction> instrs = ReadInstructions();

            foreach (AddressTable addressTable in addressTables)
            {
                for (int i = 0; i < addressTable.Count; i++)
                {
                    AddressEncountered?.Invoke(addressTable.Offset + 4 * i, false);
                }
            }

            return instrs;
        }

        private List<AgeInstruction> ReadInstructions()
        {
            List<AgeInstruction> instrs = new List<AgeInstruction>();
            while (_stream.Position < StringPoolRange.Offset)
            {
                AgeInstruction instr = ReadInstruction();
                int[] addressOperands = AddressOperands.GetOrDefault(instr.Opcode);

                for (int i = 0; i < instr.Operands.Count; i++)
                {
                    AgeOperand operand = instr.Operands[i];
                    if (operand.Type == AgeOperandType.StringLiteral)
                        HandleStringOperand(operand);
                    else if (instr.Opcode == AgeOpcode.LoadArray && i == 1)
                        HandleArrayOperand(operand);
                    else if (addressOperands != null && addressOperands.Contains(i))
                        AddressEncountered?.Invoke(operand.ValueOffset, false);
                }

                if (_writer != null)
                    WriteInstruction(instr);

                instrs.Add(instr);
            }
            return instrs;
        }

        private void HandleStringOperand(AgeOperand operand)
        {
            AddressEncountered?.Invoke(operand.ValueOffset, true);

            int offset = AddressToOffset(operand.Value);
            if (offset < StringPoolRange.Offset)
                StringPoolRange = new Range(offset, StringPoolRange.Offset + StringPoolRange.Length - offset, ScriptStringType.Internal);
        }

        private void HandleArrayOperand(AgeOperand operand)
        {
            AddressEncountered?.Invoke(operand.ValueOffset, false);

            int offset = AddressToOffset(operand.Value);
            if (offset < StringPoolRange.Offset)
                StringPoolRange = new Range(offset, 0, ScriptStringType.Internal);

            if (offset < StringPoolRange.Offset + StringPoolRange.Length)
                StringPoolRange = new Range(StringPoolRange.Offset, offset - StringPoolRange.Offset, ScriptStringType.Internal);
        }

        private AgeInstruction ReadInstruction()
        {
            int offset = (int)_stream.Position;
            int opcode = _reader.ReadInt32();
            AgeInstruction instr = new AgeInstruction(offset, opcode);

            int numOperands = OperandCounts[opcode];
            for (int i = 0; i < numOperands; i++)
            {
                AgeOperandType type = (AgeOperandType)_reader.ReadInt32();
                int valueOffset = (int)_stream.Position;
                int value = _reader.ReadInt32();
                instr.Operands.Add(new AgeOperand(type, valueOffset, value));
            }
            return instr;
        }

        private void WriteInstruction(AgeInstruction instr)
        {
            _writer.Write($"{instr.Offset:X08} {instr.Opcode:X04}");

            int[] addressOperands = AddressOperands.GetOrDefault(instr.Opcode);
            for (int i = 0; i < instr.Operands.Count; i++)
            {
                _writer.Write(i == 0 ? " " : ", ");

                AgeOperand operand = instr.Operands[i];

                if (operand.Type == AgeOperandType.StringLiteral)
                {
                    string text = GetStringAtAddress(operand.Value);
                    _writer.Write("\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"");
                }
                else if (instr.Opcode == AgeOpcode.LoadArray && i == 1)
                {
                    List<int> array = GetArrayAtAddress(operand.Value);
                    _writer.Write("[ " + string.Join(", ", array.Select(n => n.ToString("X"))) + " ]");
                }
                else if (addressOperands != null && addressOperands.Contains(i))
                {
                    _writer.Write(operand.Value < 0 ? "#none" : $"#{AddressToOffset(operand.Value):X08}");
                }
                else
                {
                    _writer.Write($"{(int)operand.Type:X}:{operand.Value:X08}");
                }
            }

            _writer.WriteLine();
        }

        public string GetStringAtAddress(int addr)
        {
            return GetStringAtOffset(AddressToOffset(addr));
        }

        public string GetStringAtOffset(int offset)
        {
            int origPos = (int)_stream.Position;

            _stream.Position = offset;
            int i = 0;
            while (true)
            {
                byte b = (byte)(_reader.ReadByte() ^ 0xFF);
                if (b == 0)
                    break;

                _textBuffer[i++] = b;
            }
            BinaryUtil.ReplaceInPlace(_textBuffer, 0, i, TextReplacements);
            string text = StringUtil.SjisEncoding.GetString(_textBuffer, 0, i);
            _stream.Position = origPos;
            return text;
        }

        public List<int> GetArrayAtAddress(int addr)
        {
            return GetArrayAtOffset(AddressToOffset(addr));
        }

        public List<int> GetArrayAtOffset(int offset)
        {
            int origPos = (int)_stream.Position;

            _stream.Position = offset;
            int count = _reader.ReadInt32();

            List<int> values = new List<int>(count);
            for (int i = 0; i < count; i++)
            {
                values.Add(_reader.ReadInt32());
            }

            _stream.Position = origPos;
            return values;
        }

        public static int AddressToOffset(int addr)
        {
            if (addr < 0)
                return -1;

            return 0x3C + 4 * addr;
        }

        public static int OffsetToAddress(int offset)
        {
            if (offset < 0)
                return -1;

            return (offset - 0x3C) / 4;
        }

        private readonly struct AddressTable
        {
            public AddressTable(int offset, int count)
            {
                Offset = offset;
                Count = count;
            }

            public readonly int Offset;
            public readonly int Count;
        }
    }
}
