using System;
using System.IO;
using VNTextPatch.Shared.Scripts;

namespace VNTextPatch.Shared
{
    public struct ScriptLocation
    {
        public ScriptLocation(IScriptCollection collection, string scriptName)
        {
            Collection = collection;
            ScriptName = scriptName;
        }

        public IScriptCollection Collection;
        public string ScriptName;

        public static ScriptLocation FromFilePath(string filePath, string format = null)
        {
            IScriptCollection collection = new FolderScriptCollection(Path.GetDirectoryName(filePath), Path.GetExtension(filePath), format);
            return new ScriptLocation(collection, Path.GetFileName(filePath));
        }

        public string ToFilePath()
        {
            if (!(Collection is FolderScriptCollection folder))
                throw new InvalidOperationException();

            return Path.Combine(folder.FolderPath, ScriptName);
        }

        public override string ToString()
        {
            return $"{Collection.Name}\\{ScriptName}";
        }
    }
}
