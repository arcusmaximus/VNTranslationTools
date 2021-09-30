namespace VNTextPatch.Shared.Scripts
{
    public struct Range
    {
        public Range(int offset, int length, ScriptStringType type)
        {
            Offset = offset;
            Length = length;
            Type = type;
        }

        public int Offset;
        public int Length;
        public ScriptStringType Type;
    }
}
