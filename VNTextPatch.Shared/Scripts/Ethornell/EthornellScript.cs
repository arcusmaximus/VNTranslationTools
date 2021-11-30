using System.Collections.Generic;
using System.IO;
using System.Linq;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Ethornell
{
    public class EthornellScript : IScript
    {
        private byte[] _scenarioData;
        private readonly List<EthornellScriptString> _strings = new List<EthornellScriptString>();
        private int _codeOffset;
        private int _codeLength;

        public string Extension => ".bgi";

        public void Load(ScriptLocation location)
        {
            _scenarioData = File.ReadAllBytes(location.ToFilePath());
            _strings.Clear();

            using (Stream stream = new MemoryStream(_scenarioData))
            {
                EthornellDisassembler disassembler = EthornellDisassembler.Create(stream);
                _codeOffset = disassembler.CodeOffset;
                disassembler.StringAddressEncountered += (offset, address, type) => _strings.Add(new EthornellScriptString(offset, _codeOffset + address, type));
                disassembler.Disassemble();
                _codeLength = (int)stream.Position - _codeOffset;
            }
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            using (Stream stream = new MemoryStream(_scenarioData))
            {
                BinaryReader reader = new BinaryReader(stream);
                foreach (EthornellScriptString str in _strings.Where(s => s.Type != ScriptStringType.Internal))
                {
                    string text = ReadString(reader, str.TextOffset);
                    yield return new ScriptString(text, str.Type);
                }
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            using (Stream inputStream = new MemoryStream(_scenarioData))
            using (Stream outputStream = File.Open(location.ToFilePath(), FileMode.Create, FileAccess.Write))
            {
                BinaryPatcher patcher = new BinaryPatcher(inputStream, outputStream, a => _codeOffset + a, o => o - _codeOffset);
                patcher.CopyUpTo(_codeOffset + _codeLength);

                SerializeStrings(inputStream, strings, out MemoryStream stringStream, out List<EthornellScriptString> newStrings);
                foreach (EthornellScriptString newString in newStrings)
                {
                    patcher.PatchInt32(newString.OperandOffset, newString.TextOffset - _codeOffset);
                }

                stringStream.CopyTo(outputStream);
            }
        }

        private string ReadString(BinaryReader reader, int offset)
        {
            long position = reader.BaseStream.Position;
            reader.BaseStream.Position = offset;
            string str = reader.ReadZeroTerminatedSjisString();
            reader.BaseStream.Position = position;
            return str;
        }

        private void SerializeStrings(Stream inputStream, IEnumerable<ScriptString> scriptStrings, out MemoryStream stringStream, out List<EthornellScriptString> newStrings)
        {
            BinaryReader inputReader = new BinaryReader(inputStream);

            stringStream = new MemoryStream();
            BinaryWriter stringWriter = new BinaryWriter(stringStream);
            Dictionary<string, int> stringOffsets = new Dictionary<string, int>();
            newStrings = new List<EthornellScriptString>();

            using (IEnumerator<ScriptString> scriptStringEnumerator = scriptStrings.GetEnumerator())
            {
                foreach (EthornellScriptString ethString in _strings)
                {
                    string text;
                    if (ethString.Type == ScriptStringType.Internal)
                    {
                        text = ReadString(inputReader, ethString.TextOffset);
                    }
                    else
                    {
                        if (!scriptStringEnumerator.MoveNext())
                            throw new InvalidDataException("Not enough strings in script file");

                        text = scriptStringEnumerator.Current.Text;
                        text = ProportionalWordWrapper.Default.Wrap(text);
                    }

                    int offset;
                    if (!stringOffsets.TryGetValue(text, out offset))
                    {
                        offset = _codeOffset + _codeLength + (int)stringStream.Length;
                        stringWriter.WriteZeroTerminatedSjisString(text);
                        stringOffsets.Add(text, offset);
                    }

                    newStrings.Add(new EthornellScriptString(ethString.OperandOffset, offset, ethString.Type));
                }
                if (scriptStringEnumerator.MoveNext())
                    throw new InvalidDataException("Too many strings in script file");
            }

            stringStream.Position = 0;
        }

        private struct EthornellScriptString
        {
            public EthornellScriptString(int operandOffset, int textOffset, ScriptStringType type)
            {
                OperandOffset = operandOffset;
                TextOffset = textOffset;
                Type = type;
            }

            public int OperandOffset { get; }
            public int TextOffset { get; }
            public ScriptStringType Type { get; }
        }
    }
}
