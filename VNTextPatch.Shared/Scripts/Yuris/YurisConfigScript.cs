using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Yuris
{
    public class YurisConfigScript : IScript
    {
        private const int CaptionOffset = 0x4C;

        public string Extension => ".ybn";

        private byte[] _data;

        public void Load(ScriptLocation location)
        {
            _data = null;

            byte[] data = File.ReadAllBytes(location.ToFilePath());
            if (data.Length < CaptionOffset + 2)
                return;

            int captionLength = BitConverter.ToInt16(data, CaptionOffset);
            if (CaptionOffset + 2 + captionLength != data.Length)
                return;

            _data = data;
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            if (_data == null)
                yield break;

            int captionLength = BitConverter.ToInt16(_data, CaptionOffset);
            string caption = StringUtil.SjisEncoding.GetString(_data, CaptionOffset + 2, captionLength);
            yield return new ScriptString(caption, ScriptStringType.Message);
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            if (_data == null)
                return;

            string caption = strings.SingleOrDefault().Text;
            if (caption == null)
                throw new Exception("Exactly one translation line expected for yscfg.ybn");

            byte[] captionBytes = StringUtil.SjisTunnelEncoding.GetBytes(caption);

            using Stream outputStream = File.Open(location.ToFilePath(), FileMode.Create, FileAccess.Write);
            BinaryWriter writer = new BinaryWriter(outputStream);
            writer.Write(_data, 0, CaptionOffset);
            writer.Write((short)captionBytes.Length);
            writer.Write(captionBytes);
        }
    }
}
