using System;
using System.Globalization;

namespace VNTextPatch.Shared.Scripts.Mware
{
    internal class SquirrelLiteralReference
    {
        public SquirrelLiteralReference(int offset, int length, SquirrelLiteralPool pool, int index)
        {
            Offset = offset;
            Length = length;
            Pool = pool;
            Index = index;
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

        public override string ToString() => Convert.ToString(Value, CultureInfo.InvariantCulture) ?? "null";
    }
}
