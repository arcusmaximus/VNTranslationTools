using System.Text;

namespace VNTextPatch.Shared.Scripts.Artemis
{
    internal interface ILuaNode
    {
        void ToString(StringBuilder result, int indentLevel);
    }
}
