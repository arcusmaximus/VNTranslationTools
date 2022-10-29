using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Majiro
{
    public class MajiroScript : IScript
    {
        public string Extension => ".mjo";

        private byte[] _data;
        private int _codeOffset;
        private int _codeSize;
        private int _entryPointAddr;
        private readonly List<int> _functionAddrs = new List<int>();
        private readonly List<int> _absoluteAddressOffsets = new List<int>();
        private readonly List<int> _relativeAddressOffsets = new List<int>();
        private readonly List<MajiroTextCodeRange> _textCodeRanges = new List<MajiroTextCodeRange>();
        private int _numInlineLineMarkers;

        public void Load(ScriptLocation location)
        {
            _data = File.ReadAllBytes(location.ToFilePath());
            _functionAddrs.Clear();
            _absoluteAddressOffsets.Clear();
            _relativeAddressOffsets.Clear();
            _textCodeRanges.Clear();
            _numInlineLineMarkers = 0;

            Stream stream = new MemoryStream(_data);
            ReadHeader(stream, out bool encrypted);
            if (encrypted)
                Decrypt();

            ReadCode(stream);
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            return _textCodeRanges.SelectMany(GetScriptStrings);
        }

        private static IEnumerable<ScriptString> GetScriptStrings(MajiroTextCodeRange range)
        {
            switch (range.Type)
            {
                case MajiroTextCodeType.Ldstr:
                    yield return new ScriptString(range.Text, ScriptStringType.Message);
                    break;

                case MajiroTextCodeType.Text:
                    Match match = Regex.Match(range.Text, @"^(?:(?<name>[^「」\r\n]+)「(?<message>.+?)」?(?:\r\n|$))+$", RegexOptions.Singleline);
                    if (!match.Success)
                        yield return new ScriptString(range.Text, ScriptStringType.Message);

                    for (int i = 0; i < match.Groups["name"].Captures.Count; i++)
                    {
                        yield return new ScriptString(match.Groups["name"].Captures[i].Value, ScriptStringType.CharacterName);
                        yield return new ScriptString(match.Groups["message"].Captures[i].Value, ScriptStringType.Message);
                    }
                    break;
            }
        }

        private void ReadHeader(Stream stream, out bool encrypted)
        {
            BinaryReader reader = new BinaryReader(stream);

            string signature = Encoding.ASCII.GetString(reader.ReadBytes(0x10));

            _absoluteAddressOffsets.Add((int)stream.Position);
            _entryPointAddr = reader.ReadInt32();

            int numLines = reader.ReadInt32();

            int numFunctions = reader.ReadInt32();
            for (int i = 0; i < numFunctions; i++)
            {
                int nameHash = reader.ReadInt32();
                _absoluteAddressOffsets.Add((int)stream.Position);
                int addr = reader.ReadInt32();
                _functionAddrs.Add(addr);
            }

            _codeSize = reader.ReadInt32();
            _codeOffset = (int)stream.Position;
            if (_codeOffset + _codeSize != stream.Length)
                throw new InvalidDataException();

            switch (signature)
            {
                case "MajiroObjV1.000\0":
                    encrypted = false;
                    break;

                case "MajiroObjX1.000\0":
                    encrypted = true;
                    break;

                default:
                    throw new InvalidDataException("Invalid Majiro signature in .mjo file");
            }
        }

        private void ReadCode(Stream stream)
        {
            List<int> remainingAddresses = new List<int>();
            remainingAddresses.Add(_entryPointAddr);
            remainingAddresses.AddRange(_functionAddrs);
            while (true)
            {
                int currentAddr = (int)stream.Position - _codeOffset;
                remainingAddresses.RemoveAll(a => a < currentAddr);
                if (remainingAddresses.Count == 0)
                    break;

                ReadCodeUntilRet(stream, remainingAddresses.Min(), remainingAddresses);
            }
        }

        private void ReadCodeUntilRet(Stream stream, int addr, List<int> remainingAddresses)
        {
            stream.Position = _codeOffset + addr;
            MajiroDisassembler disassembler = new MajiroDisassembler(stream);
            disassembler.RelativeAddressEncountered +=
                offset =>
                {
                    _relativeAddressOffsets.Add(offset);

                    int distance = BitConverter.ToInt32(_data, offset);
                    int targetAddr = (offset - _codeOffset) + 4 + distance;
                    remainingAddresses.Add(targetAddr);
                };

            Stack<MajiroTextCodeRange> ldstrRanges = new Stack<MajiroTextCodeRange>();
            int textStartOffset = -1;
            string currentText = null;
            while (stream.Position < stream.Length)
            {
                int instrOffset = (int)stream.Position;
                (short opcode, List<object> operands) = disassembler.ReadInstruction();

                switch (opcode)
                {
                    case MajiroOpcodes.LdcI:
                        break;

                    case MajiroOpcodes.Ldstr:
                        ldstrRanges.Push(new MajiroTextCodeRange(instrOffset, (int)stream.Position - instrOffset, (string)operands[0], MajiroTextCodeType.Ldstr));
                        break;

                    case MajiroOpcodes.Text:
                        if (textStartOffset < 0)
                            textStartOffset = instrOffset;

                        currentText += operands[0];
                        break;

                    case MajiroOpcodes.Proc:
                        break;

                    case MajiroOpcodes.Line:
                        if (currentText != null)
                            _numInlineLineMarkers++;

                        break;

                    case MajiroOpcodes.Ctrl when (string)operands[0] == "n":
                        if (textStartOffset < 0)
                            textStartOffset = instrOffset;

                        currentText += "\r\n";
                        break;

                    case MajiroOpcodes.Ctrl when (string)operands[0] == "d" && ldstrRanges.Count > 0:
                        if (textStartOffset < 0)
                            textStartOffset = ldstrRanges.Peek().Offset;

                        string append = ldstrRanges.Pop().Text;
                        currentText += append;
                        break;

                    case MajiroOpcodes.Callp when (int)operands[0] == MajiroSyscalls.Ruby:
                        string rubyText = ldstrRanges.Pop().Text;

                        if (textStartOffset < 0)
                            textStartOffset = ldstrRanges.Peek().Offset;
                        
                        string baseText = ldstrRanges.Pop().Text;
                        currentText += $"[{baseText}/{rubyText}]";
                        break;

                    case MajiroOpcodes.Ret:
                        return;

                    default:
                        if (!string.IsNullOrWhiteSpace(currentText))
                        {
                            int textEndOffset = instrOffset;
                            while (ldstrRanges.Count > 0)
                            {
                                MajiroTextCodeRange prevRange = ldstrRanges.Pop();
                                textEndOffset = prevRange.Offset;
                            }

                            _textCodeRanges.Add(new MajiroTextCodeRange(textStartOffset, textEndOffset - textStartOffset, currentText.Trim(), MajiroTextCodeType.Text));
                        }

                        if (opcode == MajiroOpcodes.Call || opcode == MajiroOpcodes.Callp)
                            ReadSyscall((int)operands[0], (int)operands[2], ldstrRanges);

                        textStartOffset = -1;
                        currentText = null;
                        ldstrRanges.Clear();
                        break;
                }
            }
        }

        private void ReadSyscall(int nameHash, int numArgs, Stack<MajiroTextCodeRange> ldstrRanges)
        {
            switch (nameHash)
            {
                case MajiroSyscalls.Select1:
                {
                    List<MajiroTextCodeRange> choices = new List<MajiroTextCodeRange>();
                    for (int i = 0; i < numArgs; i++)
                    {
                        choices.Add(ldstrRanges.Pop());
                    }
                    choices.Reverse();
                    _textCodeRanges.AddRange(choices);
                    break;
                }

                case MajiroSyscalls.Select2:
                {
                    ldstrRanges.Pop();
                    ldstrRanges.Pop();

                    int numChoices = numArgs - 2;
                    List<MajiroTextCodeRange> choices = new List<MajiroTextCodeRange>();
                    for (int i = 0; i < numChoices; i++)
                    {
                        choices.Add(ldstrRanges.Pop());
                    }
                    choices.Reverse();
                    _textCodeRanges.AddRange(choices);
                    break;
                }

                case MajiroSyscalls.SelectMenu:
                {
                    List<MajiroTextCodeRange> choices = new List<MajiroTextCodeRange>();
                    for (int i = 0; i < numArgs / 2; i++)
                    {
                        choices.Add(ldstrRanges.Pop());
                    }
                    choices.Reverse();
                    _textCodeRanges.AddRange(choices);
                    break;
                }

                case MajiroSyscalls.OkMessageBox:
                case MajiroSyscalls.YesNoMessageBox:
                    _textCodeRanges.Add(ldstrRanges.Pop());
                    break;
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            using Stream inputStream = new MemoryStream(_data);
            using Stream outputStream = File.Open(location.ToFilePath(), FileMode.Create, FileAccess.Write);
            BinaryPatcher patcher = new BinaryPatcher(inputStream, outputStream, a => _codeOffset + a, o => o - _codeOffset);

            WriteStrings(patcher, strings);
            PatchHeader(patcher);
            PatchAddresses(patcher);
        }

        private void WriteStrings(BinaryPatcher patcher, IEnumerable<ScriptString> strings)
        {
            Regex rubyRegex = new Regex(@"\[(.+?)\]");
            using IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator();
            foreach (MajiroTextCodeRange range in _textCodeRanges)
            {
                string newText = null;
                foreach (ScriptString origString in GetScriptStrings(range))
                {
                    if (!stringEnumerator.MoveNext())
                        throw new InvalidDataException("Not enough strings in translation");

                    ScriptString newString = stringEnumerator.Current;
                    if (newString.Type != origString.Type)
                        throw new InvalidDataException("Translation string type doesn't match original");

                    switch (newString.Type)
                    {
                        case ScriptStringType.CharacterName:
                            if (newText != null)
                                newText += "\r\n";

                            newText += newString.Text;
                            break;

                        case ScriptStringType.Message:
                            if (newText == null)
                                newText = MonospaceWordWrapper.Default.Wrap(newString.Text, rubyRegex);
                            else
                                newText += $"「{MonospaceWordWrapper.Default.Wrap(stringEnumerator.Current.Text, rubyRegex)}」";

                            break;
                    }
                }

                patcher.CopyUpTo(range.Offset);

                byte[] newCode = range.Type switch
                                 {
                                     MajiroTextCodeType.Ldstr => AssembleLdstr(newText),
                                     MajiroTextCodeType.Text => AssembleText(newText)
                                 };
                patcher.ReplaceBytes(range.Length, newCode);
            }

            if (stringEnumerator.MoveNext())
                throw new InvalidDataException("Too many strings in translation");

            patcher.CopyUpTo(_data.Length);
        }

        private static byte[] AssembleLdstr(string text)
        {
            MajiroAssembler assembler = new MajiroAssembler();
            assembler.Write(MajiroOpcodes.Ldstr, text);
            return assembler.GetResult();
        }

        private static byte[] AssembleText(string text)
        {
            MajiroAssembler assembler = new MajiroAssembler();

            string[] lines = text.Split(new[] { "\r\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int startIdx = 0;
                foreach (Match rubyMatch in Regex.Matches(line, @"\[([^\[\]/]+)/([^\[\]/]+)\]"))
                {
                    if (startIdx < rubyMatch.Index)
                    {
                        assembler.Write(MajiroOpcodes.Text, line.Substring(startIdx, rubyMatch.Index - startIdx));
                        assembler.Write(MajiroOpcodes.Proc);
                    }

                    assembler.Write(MajiroOpcodes.Ldstr, rubyMatch.Groups[1].Value);
                    assembler.Write(MajiroOpcodes.Ldstr, rubyMatch.Groups[2].Value);
                    assembler.Write(MajiroOpcodes.Callp, MajiroSyscalls.Ruby, 0, 2);

                    startIdx = rubyMatch.Index + rubyMatch.Length;
                }

                if (startIdx < line.Length)
                {
                    assembler.Write(MajiroOpcodes.Text, line.Substring(startIdx));
                    assembler.Write(MajiroOpcodes.Proc);
                }

                if (i < lines.Length - 1)
                    assembler.Write(MajiroOpcodes.Ctrl, "n");
            }

            return assembler.GetResult();
        }

        private void PatchHeader(BinaryPatcher patcher)
        {
            int origNumLines = BitConverter.ToInt32(_data, 0x14);
            int newNumLines = origNumLines - _numInlineLineMarkers;
            patcher.PatchInt32(0x14, newNumLines);

            int newCodeSize = (int)patcher.OutputStream.Position - _codeOffset;
            patcher.PatchInt32(_codeOffset - 4, newCodeSize);
        }

        private void PatchAddresses(BinaryPatcher patcher)
        {
            foreach (int offset in _absoluteAddressOffsets)
            {
                patcher.PatchAddress(offset);
            }

            foreach (int offset in _relativeAddressOffsets)
            {
                int origDistance = BitConverter.ToInt32(_data, offset);
                int origTarget = offset + 4 + origDistance;

                int newTarget = patcher.MapOffset(origTarget);
                int newDistance = newTarget - patcher.MapOffset(offset + 4);

                patcher.PatchInt32(offset, newDistance);
            }
        }

        private void Decrypt()
        {
            _data[9] = (byte)'V';
            BinaryUtil.Xor(_data, _codeOffset, _codeSize, GetEncryptionTable());
        }

        private static byte[] GetEncryptionTable()
        {
            MemoryStream stream = new MemoryStream(256 * 4);
            BinaryWriter writer = new BinaryWriter(stream);
            for (int i = 0; i < 256; i++)
            {
                writer.Write(GetEncryptionValue(i));
            }
            return stream.ToArray();
        }

        private static uint GetEncryptionValue(int seed)
        {
            const uint poly = 0xEDB88320;
            uint value = (uint)seed;
            for (int i = 0; i < 8; i++)
            {
                value = (value & 1) != 0 ? (value >> 1) ^ poly : value >> 1;
            }
            return value;
        }

        private readonly struct MajiroTextCodeRange
        {
            public MajiroTextCodeRange(int offset, int length, string text, MajiroTextCodeType type)
            {
                Offset = offset;
                Length = length;
                Text = text;
                Type = type;
            }

            public readonly int Offset;
            public readonly int Length;
            public readonly string Text;
            public readonly MajiroTextCodeType Type;
        }

        private enum MajiroTextCodeType
        {
            Ldstr,
            Text
        }
    }
}
