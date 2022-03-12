namespace VNTextPatch.Shared.Scripts.Silkys
{
    internal class SilkysOpcodes
    {
        public byte Yield;
        public byte Add;
        public byte EscapeSequence;
        public byte Message1;
        public byte Message2;
        public byte PushInt;
        public byte PushString;
        public byte Syscall;
        public byte LineNumber;
        public byte Nop1;
        public byte Nop2;

        public bool IsMessage1Obfuscated;
    }
}
