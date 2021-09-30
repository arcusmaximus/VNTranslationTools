using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.RealLive
{
    public class RealLiveScript : IScript
    {
        private byte[] _scenario;
        private int _codeOffset;
        private readonly List<int> _addressOffsets = new List<int>();
        private readonly List<Range> _textRanges = new List<Range>();

        public void Load(ScriptLocation location)
        {
            _scenario = File.ReadAllBytes(location.ToFilePath());
            _addressOffsets.Clear();
            _textRanges.Clear();
            using (Stream stream = new MemoryStream(_scenario))
            {
                RealLiveDisassembler disassembler = new RealLiveDisassembler(stream);
                disassembler.AddressEncountered += offset => _addressOffsets.Add(offset);
                disassembler.TextEncountered += range => _textRanges.Add(range);
                disassembler.Disassemble();
                _codeOffset = disassembler.CodeOffset;
            }
        }

        public string Extension => ".rl";

        public IEnumerable<ScriptString> GetStrings()
        {
            Regex dialogueRegex = new Regex(@"^【(.+?)】(.+)$");
            foreach (Range range in _textRanges)
            {
                string text = StringUtil.SjisEncoding.GetString(_scenario, range.Offset, range.Length);
                text = Unquote(text).Replace("「", "").Replace("」", "");
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                Match match = dialogueRegex.Match(text);
                if (match.Success)
                {
                    yield return new ScriptString(match.Groups[1].Value, ScriptStringType.CharacterName);
                    yield return new ScriptString(match.Groups[2].Value, ScriptStringType.Message);
                }
                else
                {
                    yield return new ScriptString(text, ScriptStringType.Message);
                }
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            using (Stream inputStream = new MemoryStream(_scenario))
            using (Stream outputStream = File.Open(location.ToFilePath(), FileMode.Create, FileAccess.ReadWrite))
            using (IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator())
            {
                BinaryPatcher patcher = new BinaryPatcher(inputStream, outputStream, addr => _codeOffset + addr, offset => offset - _codeOffset);

                foreach (Range range in _textRanges)
                {
                    string origText = StringUtil.SjisEncoding.GetString(_scenario, range.Offset, range.Length);
                    origText = Unquote(origText).Replace("「", "").Replace("」", "");
                    if (string.IsNullOrWhiteSpace(origText))
                        continue;

                    if (!stringEnumerator.MoveNext())
                        throw new InvalidDataException("Not enough strings in translation file");

                    string name = null;
                    if (stringEnumerator.Current.Type == ScriptStringType.CharacterName)
                    {
                        name = stringEnumerator.Current.Text;
                        if (!stringEnumerator.MoveNext())
                            throw new InvalidDataException("Not enough strings in translation file");
                    }

                    string text = stringEnumerator.Current.Text;
                    ArraySegment<byte> outputData = EncodeMessage(name, text);
                    patcher.CopyUpTo(range.Offset);
                    patcher.ReplaceBytes(range.Length, outputData);
                }

                if (stringEnumerator.MoveNext())
                    throw new InvalidDataException("Too many strings in translation file");

                patcher.CopyUpTo((int)inputStream.Length);

                foreach (int offset in _addressOffsets)
                {
                    patcher.PatchAddress(offset);
                }
            }
        }

        private static ArraySegment<byte> EncodeMessage(string name, string message)
        {
            MemoryStream stream = new MemoryStream();
            RealLiveAssembler assembler = new RealLiveAssembler(stream);

            if (name != null)
                assembler.WriteString("【" + name + "】", false);

            foreach (string line in message.Split(new[] { "\r\n" }, StringSplitOptions.None))
            {
                int start = 0;
                foreach (int breakPos in MonospaceWordWrapper.Default.GetWrapPositions(line))
                {
                    assembler.WriteString(line, start, breakPos - start, true);
                    assembler.WriteLineBreak();

                    start = breakPos;
                    while (start < line.Length && line[start] == ' ')
                    {
                        start++;
                    }
                }

                if (start < line.Length)
                {
                    assembler.WriteString(line, start, line.Length - start, true);
                    assembler.WriteLineBreak();
                }
            }

            ArraySegment<byte> code;
            stream.TryGetBuffer(out code);
            return code;
        }

        private static string Unquote(string text)
        {
            if (!text.StartsWith("\"") || !text.EndsWith("\""))
                return text;

            text = text.Substring(1, text.Length - 2);
            text = text.Replace("\\\"", "\"");
            return text;
        }
    }
}
