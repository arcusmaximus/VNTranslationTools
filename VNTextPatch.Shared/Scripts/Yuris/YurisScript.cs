using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VNTextPatch.Shared.Scripts.Yuris
{
    public class YurisScript : IScript
    {
        public string Extension => ".ybn";

        private IScript _innerScript;

        public void Load(ScriptLocation location)
        {
            _innerScript = null;

            string magic = ReadMagic(location);
            _innerScript = magic switch
                           {
                               "YSTB" => new YurisScenarioScript(),
                               "YSCF" => new YurisConfigScript(),
                               _ => null
                           };
            _innerScript?.Load(location);
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            return _innerScript?.GetStrings() ?? Enumerable.Empty<ScriptString>();
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            _innerScript?.WritePatched(strings, location);
        }

        private static string ReadMagic(ScriptLocation location)
        {
            using Stream stream = File.OpenRead(location.ToFilePath());
            byte[] magic = new byte[4];
            if (stream.Read(magic, 0, magic.Length) < magic.Length)
                return null;

            return Encoding.ASCII.GetString(magic);
        }
    }
}
