namespace VNTextPatch.Shared.Scripts.ArcGameEngine
{
    internal static class AgeOpcode
    {
        public const int Exit = 0x0002;
        public const int Ret = 0x0005;
        public const int Sub = 0x0051;
        public const int MovInt = 0x0055;
        public const int LoadArray = 0x0064;
        public const int Print = 0x006E;
        public const int PrintNewline = 0x006F;
        public const int Call = 0x008F;
        public const int GetArrayItem = 0x012C;
        public const int MovString = 0x0192;
        public const int PrintFurigana = 0x0196;
    }

    internal enum AgeOperandType
    {
        IntLiteral,
        FloatLiteral,
        StringLiteral,

        GlobalIntVar,
        GlobalFloatVar,
        GlobalStringVar,

        GlobalIntVarRef,
        GlobalFloatVarRef,
        GlobalStringVarRef,

        LocalIntVar,
        LocalFloatVar,
        LocalStringVar,

        LocalIntVarRef,
        LocalFloatVarRef,
        LocalStringVarRef
    }
}
