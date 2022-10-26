namespace VNTextPatch.Shared.Scripts.ArcGameEngine
{
    internal class AgeOperand
    {
        public AgeOperand(AgeOperandType type, int value)
        {
            Type = type;
            ValueOffset = -1;
            Value = value;
        }

        public AgeOperand(AgeOperandType type, int valueOffset, int value)
        {
            Type = type;
            ValueOffset = valueOffset;
            Value = value;
        }

        public AgeOperandType Type
        {
            get;
        }

        public int ValueOffset
        {
            get;
            set;
        }

        public int Value
        {
            get;
            set;
        }

        public bool Matches(AgeOperandType type, int value)
        {
            return Type == type && Value == value;
        }
    }
}
