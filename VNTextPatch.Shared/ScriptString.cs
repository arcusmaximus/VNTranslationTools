namespace VNTextPatch.Shared
{
    public struct ScriptString
    {
        public ScriptString(string text, ScriptStringType type)
        {
            Text = text;
            Type = type;
        }

        public string Text;
        public ScriptStringType Type;

        public override string ToString()
        {
            return Text;
        }
    }
}
