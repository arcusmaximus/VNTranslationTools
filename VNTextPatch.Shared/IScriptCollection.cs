using System.Collections.Generic;

namespace VNTextPatch.Shared
{
    public interface IScriptCollection
    {
        string Name { get; }

        IScript GetTemporaryScript();

        IEnumerable<string> Scripts { get; }

        bool Exists(string scriptName);

        void Add(string scriptName);
        void Add(string scriptName, ScriptLocation copyFrom);
    }
}
