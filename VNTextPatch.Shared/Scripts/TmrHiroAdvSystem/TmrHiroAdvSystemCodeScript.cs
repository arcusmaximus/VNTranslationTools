using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.TmrHiroAdvSystem
{
    public class TmrHiroAdvSystemCodeScript : IScript
    {
        public string Extension => ".srp";

        private byte[] _data;
        private List<HiroTextInstruction> _textInstrs;

        public void Load(ScriptLocation location)
        {
            _data = File.ReadAllBytes(location.ToFilePath());
            _textInstrs = GetTextInstructions().ToList();
        }

        private IEnumerable<HiroTextInstruction> GetTextInstructions()
        {
            MemoryStream stream = new MemoryStream(_data);
            BinaryReader reader = new BinaryReader(stream);
            int numInstrs = reader.ReadInt32();
            for (int i = 0; i < numInstrs; i++)
            {
                int instrOffset = (int)stream.Position;
                int instrLength = 2 + reader.ReadInt16();
                int opcode = reader.ReadInt32();
                if (opcode == 0x00150050 || (opcode & 0x0000FFFF) == 0)
                {
                    yield return new HiroTextInstruction(instrOffset, instrLength, HiroStringType.Message);
                }
                else if (opcode == 0x00140010)
                {
                    yield return new HiroTextInstruction(instrOffset, instrLength, HiroStringType.Select);
                }

                stream.Position = instrOffset + instrLength;
            }
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            foreach (HiroTextInstruction instr in _textInstrs)
            {
                string text = StringUtil.SjisEncoding.GetString(_data, instr.TextOffset, instr.TextLength);
                IEnumerable<ScriptString> strings = instr.Type switch
                                                    {
                                                        HiroStringType.Message => SplitMessage(text),
                                                        HiroStringType.Select => SplitSelect(text)
                                                    };
                foreach (ScriptString str in strings)
                {
                    yield return str;
                }
            }
        }

        private static IEnumerable<ScriptString> SplitMessage(string text)
        {
            string[] parts = text.Split(',');
            if (parts.Length > 1)
            {
                yield return new ScriptString(parts[0], ScriptStringType.CharacterName);
                yield return new ScriptString(parts[1].Replace("\\n", "\r\n"), ScriptStringType.Message);
            }
            else
            {
                yield return new ScriptString(text.Replace("\\n", "\r\n"), ScriptStringType.Message);
            }
        }

        private static IEnumerable<ScriptString> SplitSelect(string text)
        {
            foreach (Match match in Regex.Matches(text, @" d(?<choice>.+?),(?<label>\w+)"))
            {
                yield return new ScriptString(match.Groups["choice"].Value, ScriptStringType.Message);
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            using Stream inputStream = new MemoryStream(_data);
            using Stream outputStream = File.Open(location.ToFilePath(), FileMode.Create);
            BinaryPatcher patcher = new BinaryPatcher(inputStream, outputStream);

            using IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator();

            foreach (HiroTextInstruction instr in _textInstrs)
            {
                patcher.CopyUpTo(instr.InstructionOffset);
                int newInstrOffset = (int)outputStream.Length;

                patcher.CopyUpTo(instr.TextOffset);
                string origText = StringUtil.SjisEncoding.GetString(_data, instr.TextOffset, instr.TextLength);
                string newText = instr.Type switch
                                 {
                                     HiroStringType.Message => JoinMessage(stringEnumerator, origText),
                                     HiroStringType.Select => JoinSelect(stringEnumerator, origText)
                                 };
                patcher.ReplaceBytes(instr.TextLength, StringUtil.SjisTunnelEncoding.GetBytes(newText));

                int newInstrLength = (int)outputStream.Length - newInstrOffset;
                patcher.PatchInt16(instr.InstructionOffset, (short)(newInstrLength - 2));
            }

            if (stringEnumerator.MoveNext())
                throw new Exception("Too many lines in translation");

            patcher.CopyUpTo((int)inputStream.Length);
        }

        private static string JoinMessage(IEnumerator<ScriptString> stringEnumerator, string origText)
        {
            string[] parts = origText.Split(',');
            if (parts.Length > 1)
            {
                parts[0] = GetNextString(stringEnumerator, ScriptStringType.CharacterName);
                parts[1] = MonospaceWordWrapper.Default.Wrap(GetNextString(stringEnumerator, ScriptStringType.Message)).Replace("\r\n", "\\n");
                return string.Join(",", parts);
            }
            else
            {
                return MonospaceWordWrapper.Default.Wrap(GetNextString(stringEnumerator, ScriptStringType.Message)).Replace("\r\n", "\\n");
            }
        }

        private static string JoinSelect(IEnumerator<ScriptString> stringEnumerator, string origText)
        {
            StringBuilder result = new StringBuilder();
            foreach (Match match in Regex.Matches(origText, @" d(?<choice>.+?),(?<label>\w+)"))
            {
                result.Append(" d");
                result.Append(GetNextString(stringEnumerator, ScriptStringType.Message).Replace(" ", "　"));
                result.Append(",");
                result.Append(match.Groups["label"].Value);
            }
            return result.ToString();
        }

        private static string GetNextString(IEnumerator<ScriptString> stringEnumerator, ScriptStringType type)
        {
            if (!stringEnumerator.MoveNext())
                throw new Exception("Too few strings in translation");

            if (stringEnumerator.Current.Type != type)
                throw new Exception("Translation string doesn't have expected type");

            return stringEnumerator.Current.Text.Replace(",", "，");
        }

        private struct HiroTextInstruction
        {
            public HiroTextInstruction(int instrOffset, int instrLength, HiroStringType type)
            {
                InstructionOffset = instrOffset;
                InstructionLength = instrLength;
                Type = type;
            }

            public int InstructionOffset
            {
                get;
            }

            public int InstructionLength
            {
                get;
            }

            public int TextOffset
            {
                get
                {
                    return Type switch
                           {
                               HiroStringType.Message => InstructionOffset + 2 + 4,
                               HiroStringType.Select => InstructionOffset + 2 + 4 + 3
                           };
                }
            }

            public int TextLength
            {
                get
                {
                    return InstructionOffset + InstructionLength - TextOffset;
                }
            }

            public HiroStringType Type
            {
                get;
            }
        }

        private enum HiroStringType
        {
            Message,
            Select
        }
    }
}
