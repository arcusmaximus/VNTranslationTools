using System;
using System.Globalization;

namespace VNTextPatch.Shared.Scripts.Mware
{
    internal class SquirrelLiteralReference
    {
        public SquirrelLiteralReference(int offset, int length, SquirrelLiteralPool pool, int index, ScriptStringType type)
        {
            Offset = offset;
            Length = length;
            Pool = pool;
            Index = index;
            Type = type;
        }

        public int Offset
        {
            get;
        }

        public int Length
        {
            get;
        }

        public SquirrelLiteralPool Pool
        {
            get;
        }

        public int Index
        {
            get;
            set;
        }

        public object Value => Pool.Values[Index];

        public ScriptStringType Type
        {
            get;
        }

        public override string ToString() => Convert.ToString(Value, CultureInfo.InvariantCulture) ?? "null";
    }
}
