using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Propeller
{
    internal class PropellerScript : IScript
    {
        private static readonly Dictionary<byte[], byte[]> FormattingReplacements =
            new Dictionary<byte[], byte[]>
            {
                {  new[] { (byte)'<', (byte)'b', (byte)'>' }, new byte[] { 0xFC, 0xFD } },
                {  new[] { (byte)'<', (byte)'i', (byte)'>' }, new byte[] { 0xFC, 0xFE } },
                {  new[] { (byte)'<', (byte)'u', (byte)'>' }, new byte[] { 0xFC, 0xFF } },

                {  new[] { (byte)'<', (byte)'/', (byte)'b', (byte)'>' }, new byte[] { 0xFC, 0xFD } },
                {  new[] { (byte)'<', (byte)'/', (byte)'i', (byte)'>' }, new byte[] { 0xFC, 0xFE } },
                {  new[] { (byte)'<', (byte)'/', (byte)'u', (byte)'>' }, new byte[] { 0xFC, 0xFF } },
            };

        public string Extension => ".msc";

        private byte[] _data;
        private int _codeOffset;
        private readonly List<int> _addressOffsets = new List<int>();
        private readonly List<Range> _textRanges = new List<Range>();

        public void Load(ScriptLocation location)
        {
            _data = File.ReadAllBytes(location.ToFilePath());
            _addressOffsets.Clear();
            _textRanges.Clear();

            using Stream stream = new MemoryStream(_data);
            PropellerV1Disassembler disassembler = new PropellerV1Disassembler(stream);
            disassembler.AddressEncountered += offset => _addressOffsets.Add(offset);
            disassembler.TextEncountered += range => _textRanges.Add(range);
            disassembler.Disassemble();

            _codeOffset = disassembler.CodeOffset;
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            foreach (Range range in _textRanges)
            {
                Match match = GetParsedString(range);
                foreach (Capture nameCapture in match.Groups["name"].Captures)
                {
                    yield return new ScriptString(nameCapture.Value, ScriptStringType.CharacterName);
                }
                yield return new ScriptString(match.Groups["text"].Value, ScriptStringType.Message);
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            using Stream inputStream = new MemoryStream(_data);
            using Stream outputStream = File.Open(location.ToFilePath(), FileMode.Create, FileAccess.Write);
            BinaryPatcher patcher = new BinaryPatcher(inputStream, outputStream, a => _codeOffset + a, o => o - _codeOffset);

            using IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator();
            foreach (Range range in _textRanges)
            {
                if (!stringEnumerator.MoveNext())
                    throw new Exception("Not enough strings in translation");

                string names = null;
                while (stringEnumerator.Current.Type == ScriptStringType.CharacterName)
                {
                    names ??= $"一{stringEnumerator.Current.Text}一";
                    if (!stringEnumerator.MoveNext())
                        throw new Exception("Not enough strings in translation");
                }

                if (names != null)
                {
                    Match match = GetParsedString(range);
                    names += $"/一{match.Groups["name"].Captures.Cast<Capture>().Last().Value}一";
                }

                string text = ProportionalWordWrapper.Default.Wrap(stringEnumerator.Current.Text);
                text = text.Replace("\r\n", "_r");

                if (text.Contains(","))
                    text = "<,>" + text;

                byte[] textBytes = StringUtil.SjisTunnelEncoding.GetBytes(names + text);
                textBytes = BinaryUtil.Replace(textBytes, FormattingReplacements);

                patcher.CopyUpTo(range.Offset + 4);
                patcher.PatchInt32(range.Offset, textBytes.Length);
                patcher.ReplaceBytes(range.Length - 4, textBytes);
            }

            if (stringEnumerator.MoveNext())
                throw new Exception("Too many strings in translation");

            patcher.CopyUpTo((int)inputStream.Length);

            foreach (int offset in _addressOffsets)
            {
                patcher.PatchAddress(offset);
            }
        }

        private Match GetParsedString(Range range)
        {
            string text = StringUtil.SjisEncoding.GetString(_data, range.Offset + 4, range.Length - 4);
            text = text.Replace("_r", "\r\n");
            return Regex.Match(text, @"^(?:【(?<name>.+?)】/?)*(?<text>.+)", RegexOptions.Singleline);
        }
    }
}
