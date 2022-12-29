using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VNTextPatch.Shared.Scripts.Artemis
{
    internal class LuaTable : List<ILuaNode>, ILuaNode
    {
        public ILuaNode this[string key]
        {
            get { return this.OfType<LuaAttribute>().FirstOrDefault(n => n.Name == key)?.Value; }
            set
            {
                LuaAttribute node = this.OfType<LuaAttribute>().FirstOrDefault(n => n.Name == key);
                if (node != null)
                    node.Value = value;
                else
                    Add(new LuaAttribute(key, value));
            }
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            ToString(result, 0);
            return result.ToString();
        }

        public void ToString(StringBuilder result, int indentLevel)
        {
            bool hasChildTables = this.Any(i => i is LuaTable || (i is LuaAttribute attr && attr.Value is LuaTable));

            result.Append("{");
            if (hasChildTables)
                result.AppendLine();

            indentLevel++;
            string indent = new string(' ', indentLevel * 4);

            for (int i = 0; i < Count; i++)
            {
                if (hasChildTables)
                    result.Append(indent);

                this[i].ToString(result, indentLevel);

                if (i < Count - 1)
                    result.Append(hasChildTables ? "," : ", ");

                if (hasChildTables)
                    result.AppendLine();
            }

            indentLevel--;
            indent = new string(' ', indentLevel * 4);
            if (hasChildTables)
                result.Append(indent);

            result.Append("}");
        }
    }
}
