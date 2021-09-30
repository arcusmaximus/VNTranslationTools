using System;
using System.Collections.Generic;
using System.IO;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.ShSystem
{
    internal class ShSystemScript : IScript
    {
        public string Extension => ".hst";

        private byte[] _data;
        private List<int> _addressOffsets;
        private List<int> _stringOffsets;

        public void Load(ScriptLocation location)
        {
            _data = File.ReadAllBytes(location.ToFilePath());
            _addressOffsets = new List<int>();
            _stringOffsets = new List<int>();

            MemoryStream stream = new MemoryStream(_data);
            ShSystemDisassembler disassembler = new ShSystemDisassembler(stream);
            disassembler.AddressEncountered += offset => _addressOffsets.Add(offset);
            disassembler.ScriptCallEncountered += HandleScriptCall;
            disassembler.Disassemble();
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            MemoryStream stream = new MemoryStream(_data);
            BinaryReader reader = new BinaryReader(stream);
            foreach (int offset in _stringOffsets)
            {
                stream.Position = offset;
                string text = reader.ReadZeroTerminatedSjisString().Replace("\\n", "\r\n");
                int nameLength = text.IndexOf("\r\n");
                if (nameLength > 0)
                {
                    string name = text.Substring(0, nameLength);
                    yield return new ScriptString(name, ScriptStringType.CharacterName);

                    text = text.Substring(nameLength + 2);
                }

                yield return new ScriptString(text.Trim(), ScriptStringType.Message);
            }
        }

        private void HandleScriptCall(int scriptId, List<ShSystemDisassembler.ShValueRange> args)
        {
            switch (scriptId)
            {
                case 0x33:
                    if (args.Count == 5 && args[4].Type == ShSystemDisassembler.ShValueType.StringLiteral)
                        _stringOffsets.Add(args[4].Offset);

                    break;
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            throw new NotImplementedException();
        }
    }
}
