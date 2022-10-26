using System.Collections.Generic;
using System.IO;

namespace VNTextPatch.Shared.Scripts.ArcGameEngine
{
    internal class AgeAssembler
    {
        public static void Assemble(IEnumerable<AgeInstruction> instrs, BinaryWriter writer)
        {
            foreach (AgeInstruction instr in instrs)
            {
                Assemble(instr, writer);
            }
        }

        public static void Assemble(AgeInstruction instr, BinaryWriter writer)
        {
            instr.Offset = (int)writer.BaseStream.Position;
            writer.Write(instr.Opcode);
            foreach (AgeOperand operand in instr.Operands)
            {
                writer.Write((int)operand.Type);
                operand.ValueOffset = (int)writer.BaseStream.Position;
                writer.Write(operand.Value);
            }
        }
    }
}
