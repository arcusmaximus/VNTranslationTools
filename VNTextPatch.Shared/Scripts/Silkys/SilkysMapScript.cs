using System;
using System.Collections.Generic;
using System.IO;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Silkys
{
    internal class SilkysMapScript : IScript
    {
        public string Extension => ".map";

        private byte[] _data;
        private List<int> _messageOffsets;

        public void Load(ScriptLocation location)
        {
            _data = File.ReadAllBytes(location.ToFilePath());
            _messageOffsets = new List<int>();

            MemoryStream stream = new MemoryStream(_data);
            BinaryReader reader = new BinaryReader(stream);
            int numMessages = reader.ReadInt32();
            for (int i = 0; i < numMessages; i++)
            {
                int messageIndex = reader.ReadInt32();
                int messageOffset = reader.ReadInt32();
                if (BitConverter.ToInt16(_data, messageOffset) != 0)
                    _messageOffsets.Add(messageOffset);
            }
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            MemoryStream stream = new MemoryStream(_data);
            BinaryReader reader = new BinaryReader(stream);

            foreach (int messageOffset in _messageOffsets)
            {
                stream.Position = messageOffset;
                string text = reader.ReadZeroTerminatedUtf16String();
                yield return new ScriptString(text, ScriptStringType.Message);
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            using Stream inputStream = new MemoryStream(_data);
            using Stream outputStream = File.Open(location.ToFilePath(), FileMode.Create, FileAccess.Write);
            BinaryPatcher patcher = new BinaryPatcher(inputStream, outputStream);

            using IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator();
            foreach (int messageOffset in _messageOffsets)
            {
                if (!stringEnumerator.MoveNext())
                    throw new Exception("Not enough lines in translation");

                patcher.CopyUpTo(messageOffset);
                patcher.ReplaceZeroTerminatedUtf16String(stringEnumerator.Current.Text);
            }

            if (stringEnumerator.MoveNext())
                throw new Exception("Too many lines in translation");

            patcher.CopyUpTo((int)inputStream.Length);

            for (int i = 0; i < _messageOffsets.Count; i++)
            {
                patcher.PatchAddress(4 + 8 * i + 4);
            }
        }
    }
}
