using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts
{
    internal class SoftpalScript : IScript
    {
        private byte[] _data;

        public string Extension => ".dat";

        public void Load(ScriptLocation location)
        {
            _data = File.ReadAllBytes(location.ToFilePath());
            if (_data.Length < 0x10 || Encoding.ASCII.GetString(_data, 0, 0xC) != "$TEXT_LIST__")
                throw new InvalidDataException();
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            MemoryStream stream = new MemoryStream(_data);
            BinaryReader reader = new BinaryReader(stream);

            stream.Position = 0xC;
            int numStrings = reader.ReadInt32();

            string pendingDialogue = null;
            for (int i = 0; i < numStrings; i++)
            {
                reader.ReadInt32();
                string text = reader.ReadZeroTerminatedSjisString().Replace("<br>", "\r\n");
                if (pendingDialogue != null)
                {
                    yield return new ScriptString(text, ScriptStringType.CharacterName);
                    yield return new ScriptString(pendingDialogue, ScriptStringType.Message);
                    pendingDialogue = null;
                }
                else if (text.StartsWith("「") ||
                         text.StartsWith("（"))
                {
                    pendingDialogue = text;
                }
                else
                {
                    yield return new ScriptString(text, ScriptStringType.Message);
                }
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            using Stream stream = File.Open(location.ToFilePath(), FileMode.Create);
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(Encoding.ASCII.GetBytes("$TEXT_LIST__"));

            int numStrings = BitConverter.ToInt32(_data, 0xC);
            writer.Write(numStrings);

            using IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator();
            string pendingName = null;
            for (int i = 0; i < numStrings; i++)
            {
                if (!stringEnumerator.MoveNext())
                    throw new Exception("Too few strings in translation");

                string text = MonospaceWordWrapper.Default.Wrap(stringEnumerator.Current.Text).Replace("\r\n", "<br>");
                if (pendingName != null)
                {
                    writer.Write(i - 1);
                    writer.WriteZeroTerminatedSjisString(text);

                    writer.Write(i);
                    writer.WriteZeroTerminatedSjisString(pendingName);

                    pendingName = null;
                }
                else if (stringEnumerator.Current.Type == ScriptStringType.CharacterName)
                {
                    pendingName = text;
                }
                else
                {
                    writer.Write(i);
                    writer.WriteZeroTerminatedSjisString(text);
                }
            }

            if (stringEnumerator.MoveNext())
                throw new Exception("Too many strings in translation");
        }
    }
}
