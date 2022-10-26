using System.Collections.Generic;

namespace VNTextPatch.Shared.Scripts.ArcGameEngine
{
    internal class AgeInstruction
    {
        public AgeInstruction(int opcode)
        {
            Offset = -1;
            Opcode = opcode;
        }

        public AgeInstruction(int offset, int opcode)
        {
            Offset = offset;
            Opcode = opcode;
        }

        public int Address => AgeDisassembler.OffsetToAddress(Offset);

        public int Offset
        {
            get;
            set;
        }

        public int Length => 4 + Operands.Count * 8;

        public int Opcode
        {
            get;
        }

        public List<AgeOperand> Operands
        {
            get;
        } = new List<AgeOperand>();
    }
}
