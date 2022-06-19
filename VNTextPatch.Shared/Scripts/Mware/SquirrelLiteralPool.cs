using System.Collections.Generic;

namespace VNTextPatch.Shared.Scripts.Mware
{
    internal class SquirrelLiteralPool
    {
        public int CountOffset
        {
            get;
            set;
        }

        public int Offset
        {
            get;
            set;
        }

        public int Length
        {
            get;
            set;
        }

        public List<object> Values
        {
            get;
        } = new List<object>();
    }
}
