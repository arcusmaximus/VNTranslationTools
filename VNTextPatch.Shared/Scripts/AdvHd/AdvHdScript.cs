using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.AdvHd
{
    public class AdvHdScript : IScript
    {
        public string Extension => ".ws2";

        private static readonly string[] NameControlCodes = { "%LC", "%LF", "%LR" };

        private byte[] _data;
        private readonly List<int> _addressOffsets = new List<int>();
        private readonly List<Range> _textRanges = new List<Range>();

        public void Load(ScriptLocation location)
        {
            _data = File.ReadAllBytes(location.ToFilePath());

            Stream stream = new MemoryStream(_data);
            AdvHdDisassemblerBase[] disassemblers =
                {
                    new AdvHdDisassemblerV1(stream),
                    new AdvHdDisassemblerV2(stream),
                    new AdvHdDisassemblerV3(stream)
                };
            foreach (AdvHdDisassemblerBase disassembler in disassemblers)
            {
                stream.Position = 0;
                _addressOffsets.Clear();
                _textRanges.Clear();

                disassembler.AddressEncountered += o => _addressOffsets.Add(o);
                disassembler.TextEncountered += r => _textRanges.Add(r);
                try
                {
                    disassembler.Disassemble();
                }
                catch
                {
                    continue;
                }
                return;
            }

            throw new InvalidDataException("Failed to read file");
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            foreach (Range range in _textRanges)
            {
                string text = StringUtil.SjisEncoding.GetString(_data, range.Offset, range.Length - 1);
                text = RemoveControlCodes(text, range.Type);
                if (!string.IsNullOrWhiteSpace(text))
                    yield return new ScriptString(text, range.Type);
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            using Stream inputStream = new MemoryStream(_data);
            using Stream outputStream = File.Open(location.ToFilePath(), FileMode.Create, FileAccess.Write);
            BinaryPatcher patcher = new BinaryPatcher(inputStream, outputStream);

            using IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator();
            foreach (Range range in _textRanges)
            {
                string origText = StringUtil.SjisEncoding.GetString(_data, range.Offset, range.Length - 1);
                if (string.IsNullOrWhiteSpace(RemoveControlCodes(origText, range.Type)))
                    continue;

                if (!stringEnumerator.MoveNext())
                    throw new InvalidDataException("Not enough strings in translation");

                string newText = stringEnumerator.Current.Text;
                if (range.Type == ScriptStringType.Message)
                    newText = MonospaceWordWrapper.Default.Wrap(newText);

                newText = AddControlCodes(origText, newText, range.Type);

                patcher.CopyUpTo(range.Offset);
                patcher.ReplaceZeroTerminatedSjisString(newText);
            }

            if (stringEnumerator.MoveNext())
                throw new InvalidDataException("Too many strings in translation");

            patcher.CopyUpTo((int)inputStream.Length);

            foreach (int offset in _addressOffsets)
            {
                patcher.PatchAddress(offset);
            }
        }

        private static string RemoveControlCodes(string text, ScriptStringType type)
        {
            switch (type)
            {
                case ScriptStringType.CharacterName:
                    foreach (string controlCode in NameControlCodes)
                    {
                        text = text.Replace(controlCode, "");
                    }
                    break;

                case ScriptStringType.Message:
                    text = Regex.Replace(text, @"(?:%\w+)+$", "");
                    text = text.Replace("\\n", "\r\n");
                    break;
            }
            return text;
        }

        private static string AddControlCodes(string origText, string newText, ScriptStringType type)
        {
            switch (type)
            {
                case ScriptStringType.CharacterName:
                    foreach (string controlCode in NameControlCodes)
                    {
                        if (origText.StartsWith(controlCode))
                        {
                            newText = controlCode + newText;
                            break;
                        }
                    }
                    break;

                case ScriptStringType.Message:
                    Match match = Regex.Match(origText, @"(?:%\w+)+$");
                    if (match.Success)
                        newText += match.Value;

                    newText = newText.Replace("\r\n", " \\n");      // Add space to avoid end-of-line characters getting cut off while message builds up
                    break;
            }
            return newText;
        }
    }
}
