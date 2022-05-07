using System;
using System.Collections.Generic;
using System.IO;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.TmrHiroAdvSystem
{
    public class TmrHiroAdvSystemTextScript : IScript
    {
        public string Extension => null;

        private byte[] _data;

        public void Load(ScriptLocation location)
        {
            _data = File.ReadAllBytes(location.ToFilePath());
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            int pos = 0;
            while (pos < _data.Length)
            {
                int length = BitConverter.ToInt16(_data, pos);
                pos += 2;

                string text = StringUtil.SjisEncoding.GetString(_data, pos, length);
                pos += length;

                yield return new ScriptString(text, ScriptStringType.Message);
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            using Stream stream = File.Open(location.ToFilePath(), FileMode.Create);
            BinaryWriter writer = new BinaryWriter(stream);
            foreach (ScriptString str in strings)
            {
                byte[] textBytes = StringUtil.SjisTunnelEncoding.GetBytes(str.Text);
                writer.Write((short)textBytes.Length);
                writer.Write(textBytes);
            }
        }
    }
}
