using System.Text;

namespace VNTextPatch.Shared.Scripts.Artemis
{
    internal class LuaAttribute : ILuaNode
    {
        public LuaAttribute(string name, ILuaNode value)
        {
            Name = name;
            Value = value;
        }

        public string Name
        {
            get;
            set;
        }

        public ILuaNode Value
        {
            get;
            set;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            ToString(result, 0);
            return result.ToString();
        }

        public void ToString(StringBuilder result, int indentLevel)
        {
            if (char.IsDigit(Name[0]))
            {
                result.Append('[');
                result.Append(Name);
                result.Append(']');
            }
            else
            {
                result.Append(Name);
            }

            result.Append(" = ");
            Value.ToString(result, indentLevel);
        }
    }
}
