using System.Text;

namespace VNTextPatch.Shared.Scripts.Artemis
{
    internal class LuaNumber : ILuaNode
    {
        public LuaNumber(string value)
        {
            Value = value;
        }

        public string Value
        {
            get;
            set;
        }

        public override string ToString()
        {
            return Value;
        }

        public void ToString(StringBuilder result, int indentLevel)
        {
            result.Append(ToString());
        }
    }
}
