using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Silkys
{
    internal class SilkysMesScript : IScript
    {
        public string Extension => ".mes";

        private static class EscapeCode
        {
            public const byte LineBreak = 0x00;
            public const byte Ruby = 0x01;
        }

        private byte[] _data;
        private SilkysDisassemblerBase _disassembler;
        private SilkysOpcodes _opcodes;
        private int _codeOffset;
        private readonly List<int> _littleEndianAddressOffsets = new List<int>();
        private readonly List<int> _bigEndianAddressOffsets = new List<int>();
        private readonly List<Range> _textCodeRanges = new List<Range>();

        public void Load(ScriptLocation location)
        {
            _data = File.ReadAllBytes(location.ToFilePath());
            _littleEndianAddressOffsets.Clear();
            _bigEndianAddressOffsets.Clear();
            _textCodeRanges.Clear();

            _disassembler = GetDisassembler();
            if (_disassembler == null)
                return;

            _opcodes = _disassembler.Opcodes;
            _codeOffset = _disassembler.CodeOffset;
            _disassembler.LittleEndianAddressEncountered += o => _littleEndianAddressOffsets.Add(o);
            _disassembler.BigEndianAddressEncountered += o => _bigEndianAddressOffsets.Add(o);
            
            _disassembler.ReadHeader();
            ReadCode();
        }

        private void ReadCode()
        {
            Stack<object> stack = new Stack<object>();
            int messageStartOffset = -1;
            bool inRuby = false;

            _disassembler.Stream.Position = _codeOffset;
            while (_disassembler.Stream.Position < _disassembler.Stream.Length)
            {
                int instrOffset = (int)_disassembler.Stream.Position;
                (byte opcode, List<object> operands) = _disassembler.ReadInstruction();

                HandleMessageInstructions(instrOffset, opcode, operands, ref messageStartOffset, ref inRuby);
                HandleCharacterNameInstructions(opcode, operands, stack);
            }
        }

        private void HandleMessageInstructions(int instrOffset, byte opcode, List<object> operands, ref int messageStartOffset, ref bool inRuby)
        {
            if (opcode == _opcodes.Message1 ||
                opcode == _opcodes.Message2)
            {
                if (messageStartOffset < 0)
                    messageStartOffset = instrOffset;
            }
            else if (opcode == _opcodes.EscapeSequence)
            {
                if ((byte)operands[0] == EscapeCode.Ruby)
                    inRuby = true;
            }
            else if (opcode == _opcodes.Yield && inRuby)
            {
                inRuby = false;
            }
            else if (opcode == _opcodes.PushInt && _data[instrOffset + 5] == _opcodes.LineNumber)
            {
            }
            else if (opcode == _opcodes.LineNumber ||
                     opcode == _opcodes.Nop1 ||
                     opcode == _opcodes.Nop2)
            {
            }
            else
            {
                if (messageStartOffset >= 0)
                    _textCodeRanges.Add(new Range(messageStartOffset, instrOffset - messageStartOffset, ScriptStringType.Message));

                messageStartOffset = -1;
                inRuby = false;
            }
        }

        private void HandleCharacterNameInstructions(byte opcode, List<object> operands, Stack<object> stack)
        {
            if (opcode == _opcodes.PushInt ||
                opcode == _opcodes.PushString)
            {
                stack.Push(operands[0]);
            }
            else if (opcode == _opcodes.Add && stack.Count >= 2)
            {
                object value1 = stack.Pop();
                object value2 = stack.Pop();
                if (value1 is int int1 && value2 is int int2)
                    stack.Push(int1 + int2);
            }
            else if (opcode == _opcodes.Syscall &&
                     stack.Count == 3 &&
                     stack.Pop() is int funcId &&
                     stack.Pop() is int execId &&
                     _disassembler.Syscalls.Any(s => funcId == s.Exec && execId == s.ExecSetCharacterName) &&
                     stack.Pop() is Range name)
            {
                _textCodeRanges.Add(new Range(name.Offset - 1, name.Length + 1, ScriptStringType.CharacterName));
                stack.Clear();
            }
            else
            {
                stack.Clear();
            }
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            foreach (Range codeRange in _textCodeRanges)
            {
                string text = CodeToText(codeRange);
                if (codeRange.Type == ScriptStringType.Message)
                {
                    Match match = Regex.Match(text, @"^〈(?<name>.+?)〉：(?<message>.+)", RegexOptions.Singleline);
                    if (match.Success)
                    {
                        yield return new ScriptString(match.Groups["name"].Value, ScriptStringType.CharacterName);
                        text = match.Groups["message"].Value;
                    }
                }

                yield return new ScriptString(text, codeRange.Type);
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            using Stream inputStream = new MemoryStream(_data);
            using Stream outputStream = File.Open(location.ToFilePath(), FileMode.Create, FileAccess.Write);
            BinaryPatcher patcher = new BinaryPatcher(inputStream, outputStream);

            using IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator();
            foreach (Range range in _textCodeRanges)
            {
                patcher.CopyUpTo(range.Offset);

                if (!stringEnumerator.MoveNext())
                    throw new InvalidDataException("Not enough strings in translation");

                string newText = stringEnumerator.Current.Text;
                if (stringEnumerator.Current.Type == ScriptStringType.CharacterName && range.Type == ScriptStringType.Message)
                {
                    newText = $"〈{newText}〉：";

                    if (!stringEnumerator.MoveNext())
                        throw new InvalidDataException("Not enough strings in translation");

                    newText += stringEnumerator.Current.Text;
                }

                newText = MonospaceWordWrapper.Default.Wrap(newText, new Regex(@"\[.+?\]"));
                byte[] newCode = TextToCode(newText, range.Type);
                patcher.ReplaceBytes(range.Length, newCode);
            }

            if (stringEnumerator.MoveNext())
                throw new InvalidDataException("Too many strings in translation");

            patcher.CopyUpTo(_data.Length);

            foreach (int addressOffset in _littleEndianAddressOffsets)
            {
                PatchAddress(patcher, addressOffset, false);
            }

            foreach (int addressOffset in _bigEndianAddressOffsets)
            {
                PatchAddress(patcher, addressOffset, true);
            }
        }

        private void PatchAddress(BinaryPatcher patcher, int addressOffset, bool bigEndian)
        {
            int origAddress = BitConverter.ToInt32(_data, addressOffset);
            if (bigEndian)
                origAddress = BinaryUtil.FlipEndianness(origAddress);

            int origOffset = _codeOffset + origAddress;
            
            int newOffset = patcher.MapOffset(origOffset);
            int newAddress = newOffset - _codeOffset;
            if (bigEndian)
                newAddress = BinaryUtil.FlipEndianness(newAddress);

            patcher.PatchInt32(addressOffset, newAddress);
        }

        private string CodeToText(Range codeRange)
        {
            StringBuilder result = new StringBuilder();
            _disassembler.Stream.Position = codeRange.Offset;
            while (_disassembler.Stream.Position < codeRange.Offset + codeRange.Length)
            {
                (byte opcode, List<object> operands) = _disassembler.ReadInstruction();

                if (opcode == _opcodes.PushString ||
                    opcode == _opcodes.Message1 && !_opcodes.IsMessage1Obfuscated ||
                    opcode == _opcodes.Message2)
                {
                    Range textRange = (Range)operands[0];
                    result.Append(StringUtil.SjisEncoding.GetString(_data, textRange.Offset, textRange.Length - 1));
                }
                else if (opcode == _opcodes.Message1 && _opcodes.IsMessage1Obfuscated)
                {
                    Range textRange = (Range)operands[0];
                    byte[] deobfuscated = new byte[(textRange.Length - 1) * 2];
                    int inputIdx = 0;
                    int outputIdx = 0;
                    while (inputIdx < textRange.Length - 1)
                    {
                        byte b = _data[textRange.Offset + inputIdx++];
                        if ((b >= 0x81 && b < 0xA0) || (b >= 0xE0 && b < 0xF0))
                        {
                            deobfuscated[outputIdx++] = b;
                            deobfuscated[outputIdx++] = _data[textRange.Offset + inputIdx++];
                        }
                        else
                        {
                            int c =  b - 0x7D62;
                            deobfuscated[outputIdx++] = (byte)(c >> 8);
                            deobfuscated[outputIdx++] = (byte)c;
                        }
                    }
                    result.Append(StringUtil.SjisEncoding.GetString(deobfuscated, 0, outputIdx));
                }
                else if (opcode == _opcodes.EscapeSequence)
                {
                    result.Append(
                        (byte)operands[0] switch
                        {
                            EscapeCode.LineBreak => "\r\n",
                            EscapeCode.Ruby => "[",
                            _ => throw new InvalidDataException($"Encountered invalid escape sequence {operands[0]}")
                        }
                    );
                }
                else if (opcode == _opcodes.Yield)
                {
                    result.Append("]");
                }
            }
            return result.ToString();
        }

        private byte[] TextToCode(string text, ScriptStringType type)
        {
            MemoryStream result = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(result);

            switch (type)
            {
                case ScriptStringType.CharacterName:
                    writer.Write(_opcodes.PushString);
                    writer.Write(StringUtil.SjisTunnelEncoding.GetBytes(text));
                    writer.Write((byte)0);
                    break;

                case ScriptStringType.Message:
                    foreach (string line in text.Split(new[] { "\r\n" }, StringSplitOptions.None))
                    {
                        if (result.Length > 0)
                        {
                            writer.Write(_opcodes.EscapeSequence);
                            writer.Write(EscapeCode.LineBreak);
                        }

                        foreach ((Range range, bool isRuby) in StringUtil.GetMatchingAndSurroundingRanges(line, new Regex(@"\[.+?\]")))
                        {
                            if (isRuby)
                            {
                                writer.Write(_opcodes.EscapeSequence);
                                writer.Write(EscapeCode.Ruby);

                                writer.Write(_opcodes.Message2);
                                writer.Write(StringUtil.SjisTunnelEncoding.GetBytes(line.Substring(range.Offset + 1, range.Length - 2)));
                                writer.Write((byte)0);

                                writer.Write(_opcodes.Yield);
                            }
                            else
                            {
                                writer.Write(_opcodes.Message2);
                                writer.Write(StringUtil.SjisTunnelEncoding.GetBytes(line.Substring(range.Offset, range.Length)));
                                writer.Write((byte)0);
                            }
                        }
                    }
                    break;
            }

            return result.ToArray();
        }

        private SilkysDisassemblerBase GetDisassembler()
        {
            int numMessages = BitConverter.ToInt32(_data, 0);
            
            // If there are no messages, it's harder to tell what version of the format we're working with, and we have nothing to extract anyway
            if (numMessages == 0)
                return null;

            MemoryStream stream = new MemoryStream(_data);

            // In the AI6WIN version of the format, the message count is immediately followed by the address of the first message marker
            int codeOffset = 4 + numMessages * 4;
            int firstLineOffset = codeOffset + BitConverter.ToInt32(_data, 4);
            if (firstLineOffset <= _data.Length - 5 &&
                _data[firstLineOffset + 0] == 0x19 &&
                _data[firstLineOffset + 1] == 0x00 &&
                _data[firstLineOffset + 2] == 0x00 &&
                _data[firstLineOffset + 3] == 0x00 &&
                _data[firstLineOffset + 4] == 0x00)
            {
                return new Ai6WinDisassembler(stream);
            }
            // In the Silky's+ version of the format, the message count is followed by the special message count, which is (hopefully)
            // not an address of a message marker
            else
            {
                return new SilkysPlusDisassembler(stream);
            }
        }
    }
}
