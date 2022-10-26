using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.ArcGameEngine
{
    public class AgeScript : IScript
    {
        private byte[] _data;
        private List<int> _addrOffsets;
        private AgeDisassembler _disassembler;
        private List<AgeInstruction> _instructions;
        private Range _stringPoolRange;

        private string _chInitFilePath;
        private SortedList<int, string> _names;

        private NameOutputInfo _nameOutputInfo;

        public string Extension => ".bin";

        public void Load(ScriptLocation location)
        {
            string filePath = location.ToFilePath();
            _data = File.ReadAllBytes(filePath);
            _addrOffsets = new List<int>();

            MemoryStream stream = new MemoryStream(_data);
            _disassembler = new AgeDisassembler(stream);

            using TextWriter writer = GetDisassemblyWriter(filePath);
            AgeDisassembler disassembler = new AgeDisassembler(stream, writer);
            disassembler.AddressEncountered +=
                (addrOffset, isStringAddr) =>
                {
                    if (!isStringAddr && BitConverter.ToInt32(_data, addrOffset) >= 0)
                        _addrOffsets.Add(addrOffset);
                };
            _instructions = disassembler.Disassemble();
            _stringPoolRange = disassembler.StringPoolRange;

            string chInitFilePath = Path.Combine(Path.GetDirectoryName(filePath), "CHINIT.BIN");
            if (chInitFilePath != _chInitFilePath)
            {
                _chInitFilePath = chInitFilePath;
                _names = File.Exists(chInitFilePath) ? ReadNamesFromChInit(chInitFilePath) : new SortedList<int, string>();
            }

            _nameOutputInfo = GetNameOutputInfo();
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            return GetStringInstructionGroups().SelectMany(ParseStringInstructionGroup);
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            using Stream inputStream = new MemoryStream(_data);
            using Stream outputStream = File.Open(location.ToFilePath(), FileMode.Create, FileAccess.ReadWrite);
            BinaryPatcher patcher = new BinaryPatcher(inputStream, outputStream, AgeDisassembler.AddressToOffset, AgeDisassembler.OffsetToAddress);

            PatchStrings(strings, patcher);

            foreach (int addrOffset in _addrOffsets)
            {
                patcher.PatchAddress(addrOffset);
            }
        }

        private void PatchStrings(IEnumerable<ScriptString> strings, BinaryPatcher patcher)
        {
            AgeStringPoolBuilder stringPool = new AgeStringPoolBuilder();

            using IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator();
            List<int> stringAddrOffsets = new List<int>();
            foreach (AgeInstructionGroup origInstrGroup in GetStringInstructionGroups())
            {
                patcher.CopyUpTo(origInstrGroup.Instructions[0].Offset);
                AgeInstructionGroup newInstrGroup = CreateStringInstructionGroup(origInstrGroup, stringEnumerator, stringPool);
                patcher.ReplaceBytes(
                    origInstrGroup.Instructions.Sum(i => i.Length),
                    writer => AgeAssembler.Assemble(newInstrGroup.Instructions, writer)
                );
                stringAddrOffsets.AddRange(newInstrGroup.Instructions
                                                        .SelectMany(i => i.Operands)
                                                        .Where(o => o.Type == AgeOperandType.StringLiteral)
                                                        .Select(o => o.ValueOffset));
            }

            if (stringEnumerator.MoveNext())
                throw new Exception("Too many strings in translation");

            patcher.CopyUpTo(_stringPoolRange.Offset);
            int newStringPoolBaseAddr = AgeDisassembler.OffsetToAddress((int)patcher.OutputStream.Position);
            patcher.ReplaceBytes(_stringPoolRange.Length, stringPool.Content);

            patcher.CopyUpTo((int)patcher.InputStream.Length);

            BinaryReader outputReader = new BinaryReader(patcher.OutputStream);
            BinaryWriter outputWriter = new BinaryWriter(patcher.OutputStream);
            foreach (int addrOffset in stringAddrOffsets)
            {
                outputReader.BaseStream.Position = addrOffset;
                int relativeAddr = outputReader.ReadInt32();

                int absoluteAddr = newStringPoolBaseAddr + relativeAddr;

                outputWriter.BaseStream.Position = addrOffset;
                outputWriter.Write(absoluteAddr);
            }
        }

        private IEnumerable<AgeInstructionGroup> GetStringInstructionGroups()
        {
            AgeInstructionGroup? group = null;
            foreach (AgeInstruction instr in _instructions)
            {
                if (SetsMajorNameIndex(instr))
                {
                    if (group != null && IsUsefulStringInstructionGroup(group.Value))
                        yield return group.Value;

                    group = new AgeInstructionGroup(AgeInstructionGroupType.IndirectName, new List<AgeInstruction> { instr });
                }
                else if (SetsMinorNameIndex(instr))
                {
                    if (group == null)
                        continue;

                    group.Value.Instructions.Add(instr);
                    yield return group.Value;

                    group = null;
                }
                else if ((instr.Opcode == AgeOpcode.Print && instr.Operands[0].Matches(AgeOperandType.IntLiteral, 0)) ||
                         instr.Opcode == AgeOpcode.PrintNewline ||
                         instr.Opcode == AgeOpcode.PrintFurigana)
                {
                    group ??= new AgeInstructionGroup(AgeInstructionGroupType.Message, new List<AgeInstruction>());
                    group.Value.Instructions.Add(instr);
                }
                else if (instr.Operands.Any(o => o.Type == AgeOperandType.StringLiteral))
                {
                    if (group != null && IsUsefulStringInstructionGroup(group.Value))
                        yield return group.Value;

                    group = null;

                    AgeInstructionGroupType type;
                    if (instr.Opcode == AgeOpcode.Print && instr.Operands[0].Matches(AgeOperandType.IntLiteral, 2) && _nameOutputInfo == null)
                        type = AgeInstructionGroupType.DirectName;
                    else
                        type = AgeInstructionGroupType.Other;

                    yield return new AgeInstructionGroup(type, new List<AgeInstruction> { instr });
                }
                else if (group != null)
                {
                    if (IsUsefulStringInstructionGroup(group.Value))
                        yield return group.Value;

                    group = null;
                }
            }
        }

        private static bool IsUsefulStringInstructionGroup(AgeInstructionGroup group)
        {
            return group.Instructions.Any(i => i.Opcode != AgeOpcode.PrintNewline);
        }

        private IEnumerable<ScriptString> ParseStringInstructionGroup(AgeInstructionGroup group)
        {
            return group.Type switch
                   {
                       AgeInstructionGroupType.DirectName => ParseDirectNameInstructionGroup(group),
                       AgeInstructionGroupType.IndirectName => ParseIndirectNameInstructionGroup(group),
                       AgeInstructionGroupType.Message => ParseMessageInstructionGroup(group),
                       _ => ParseOtherInstructionGroup(group)
                   };
        }

        private IEnumerable<ScriptString> ParseDirectNameInstructionGroup(AgeInstructionGroup group)
        {
            string name = _disassembler.GetStringAtAddress(group.Instructions[0].Operands[1].Value);
            yield return new ScriptString(name, ScriptStringType.CharacterName);
        }

        private IEnumerable<ScriptString> ParseIndirectNameInstructionGroup(AgeInstructionGroup group)
        {
            int index = GetIndirectNameIndex(group);
            string name = _names.GetOrDefault(index) ?? "？？？";
            yield return new ScriptString(name, ScriptStringType.CharacterName);
        }

        private int GetIndirectNameIndex(AgeInstructionGroup group)
        {
            List<AgeInstruction> instrs = group.Instructions;
            int majorIndex = instrs[0].Operands[1].Value;
            int minorIndex = instrs[1].Opcode switch
                             {
                                 AgeOpcode.MovInt => instrs[1].Operands[1].Value,
                                 AgeOpcode.Sub => instrs[1].Operands[1].Value - instrs[1].Operands[2].Value,
                                 _ => throw new NotSupportedException()
                             };
            return _nameOutputInfo.NameArrayBase + majorIndex * _nameOutputInfo.MajorIndexFactor + minorIndex;
        }

        private IEnumerable<ScriptString> ParseMessageInstructionGroup(AgeInstructionGroup group)
        {
            StringBuilder message = new StringBuilder();
            foreach (AgeInstruction instr in group.Instructions)
            {
                switch (instr.Opcode)
                {
                    case AgeOpcode.Print when instr.Operands[0].Matches(AgeOperandType.IntLiteral, 0):
                        message.Append(_disassembler.GetStringAtAddress(instr.Operands[1].Value));
                        break;

                    case AgeOpcode.PrintNewline:
                        message.AppendLine();
                        break;

                    case AgeOpcode.PrintFurigana:
                    {
                        string text = _disassembler.GetStringAtAddress(instr.Operands[1].Value);
                        string ruby = _disassembler.GetStringAtAddress(instr.Operands[2].Value);
                        message.Append($"[{text}/{ruby}]");
                        break;
                    }
                }
            }

            yield return new ScriptString(message.ToString(), ScriptStringType.Message);
        }

        private IEnumerable<ScriptString> ParseOtherInstructionGroup(AgeInstructionGroup group)
        {
            foreach (AgeInstruction instr in group.Instructions)
            {
                foreach (AgeOperand operand in instr.Operands.Where(o => o.Type == AgeOperandType.StringLiteral))
                {
                    string text = _disassembler.GetStringAtAddress(operand.Value);
                    yield return new ScriptString(text, ScriptStringType.Message);
                }
            }
        }

        private AgeInstructionGroup CreateStringInstructionGroup(AgeInstructionGroup origInstrGroup, IEnumerator<ScriptString> stringEnumerator, AgeStringPoolBuilder stringPool)
        {
            switch (origInstrGroup.Type)
            {
                case AgeInstructionGroupType.DirectName:
                    return CreateDirectNameInstructionGroup(stringEnumerator, stringPool);

                case AgeInstructionGroupType.IndirectName:
                    return CreateIndirectNameInstructionGroup(origInstrGroup, stringEnumerator, stringPool);

                case AgeInstructionGroupType.Message:
                    return CreateMessageInstructionGroup(stringEnumerator, stringPool);

                default:
                    PatchOtherInstructionGroup(origInstrGroup, stringEnumerator, stringPool);
                    return origInstrGroup;
            }
        }

        private AgeInstructionGroup CreateDirectNameInstructionGroup(IEnumerator<ScriptString> stringEnumerator, AgeStringPoolBuilder stringPool)
        {
            if (!stringEnumerator.MoveNext())
                throw new Exception("Too few strings in translation");

            string name = stringEnumerator.Current.Text;
            int relativeNameAddr = stringPool.Add(name);

            AgeInstruction instr = new AgeInstruction(AgeOpcode.Print);
            instr.Operands.Add(new AgeOperand(AgeOperandType.IntLiteral, 2));
            instr.Operands.Add(new AgeOperand(AgeOperandType.StringLiteral, relativeNameAddr));
            return new AgeInstructionGroup(AgeInstructionGroupType.DirectName, new List<AgeInstruction> { instr });
        }

        private AgeInstructionGroup CreateIndirectNameInstructionGroup(AgeInstructionGroup origInstrGroup, IEnumerator<ScriptString> stringEnumerator, AgeStringPoolBuilder stringPool)
        {
            if (!stringEnumerator.MoveNext())
                throw new Exception("Too few strings in translation");

            List<AgeInstruction> newInstrs = origInstrGroup.Instructions.ToList();

            string name = stringEnumerator.Current.Text;
            int index = GetIndirectNameIndex(origInstrGroup);
            
            AgeInstruction instr = new AgeInstruction(AgeOpcode.MovString);
            instr.Operands.Add(new AgeOperand(AgeOperandType.GlobalStringVar, index));
            instr.Operands.Add(new AgeOperand(AgeOperandType.StringLiteral, stringPool.Add(name)));
            newInstrs.Add(instr);

            return new AgeInstructionGroup(AgeInstructionGroupType.IndirectName, newInstrs);
        }

        private AgeInstructionGroup CreateMessageInstructionGroup(IEnumerator<ScriptString> stringEnumerator, AgeStringPoolBuilder stringPool)
        {
            if (!stringEnumerator.MoveNext())
                throw new Exception("Too few strings in translation");

            AgeInstructionGroup instrGroup = new AgeInstructionGroup(AgeInstructionGroupType.Message, new List<AgeInstruction>());
            string message = MonospaceWordWrapper.Default.Wrap(stringEnumerator.Current.Text);
            foreach ((string text, Match match) in StringUtil.GetMatchingAndSurroundingTexts(message, new Regex(@"\r\n|\[([^\[\]/]+)/([^\[\]/]+)\]")))
            {
                if (text != null)
                {
                    int relativeAddr = stringPool.Add(text);

                    AgeInstruction instr = new AgeInstruction(AgeOpcode.Print);
                    instr.Operands.Add(new AgeOperand(AgeOperandType.IntLiteral, 0));
                    instr.Operands.Add(new AgeOperand(AgeOperandType.StringLiteral, relativeAddr));
                    instrGroup.Instructions.Add(instr);
                }
                else if (match.Value == "\r\n")
                {
                    AgeInstruction instr = new AgeInstruction(AgeOpcode.PrintNewline);
                    instr.Operands.Add(new AgeOperand(AgeOperandType.IntLiteral, 0));
                    instrGroup.Instructions.Add(instr);
                }
                else
                {
                    int relativeTextAddr = stringPool.Add(match.Groups[1].Value);
                    int relativeRubyAddr = stringPool.Add(match.Groups[2].Value);

                    AgeInstruction instr = new AgeInstruction(AgeOpcode.PrintFurigana);
                    instr.Operands.Add(new AgeOperand(AgeOperandType.IntLiteral, 0));
                    instr.Operands.Add(new AgeOperand(AgeOperandType.StringLiteral, relativeTextAddr));
                    instr.Operands.Add(new AgeOperand(AgeOperandType.StringLiteral, relativeRubyAddr));
                    instrGroup.Instructions.Add(instr);
                }
            }
            return instrGroup;
        }

        private void PatchOtherInstructionGroup(AgeInstructionGroup instrGroup, IEnumerator<ScriptString> stringEnumerator, AgeStringPoolBuilder stringPool)
        {
            foreach (AgeOperand operand in instrGroup.Instructions
                                                     .SelectMany(i => i.Operands)
                                                     .Where(o => o.Type == AgeOperandType.StringLiteral))
            {
                if (!stringEnumerator.MoveNext())
                    throw new Exception("Too few strings in translation");

                operand.Value = stringPool.Add(stringEnumerator.Current.Text);
            }
        }

        private bool SetsMajorNameIndex(AgeInstruction instr)
        {
            if (_nameOutputInfo == null)
                return false;

            return instr.Opcode == AgeOpcode.MovInt &&
                   instr.Operands[0].Matches(AgeOperandType.GlobalIntVar, _nameOutputInfo.MajorIndexVariable) &
                   instr.Operands[1].Type == AgeOperandType.IntLiteral;
        }

        private bool SetsMinorNameIndex(AgeInstruction instr)
        {
            if (_nameOutputInfo == null)
                return false;

            switch (instr.Opcode)
            {
                case AgeOpcode.MovInt:
                    return instr.Operands[0].Matches(AgeOperandType.GlobalIntVar, _nameOutputInfo.MinorIndexVariable) &&
                           instr.Operands[1].Type == AgeOperandType.IntLiteral;

                case AgeOpcode.Sub:
                    return instr.Operands[0].Matches(AgeOperandType.GlobalIntVar, _nameOutputInfo.MinorIndexVariable) &&
                           instr.Operands[1].Type == AgeOperandType.IntLiteral &&
                           instr.Operands[2].Type == AgeOperandType.IntLiteral;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Reads the global name array from CHINIT.BIN
        /// </summary>
        private static SortedList<int, string> ReadNamesFromChInit(string filePath)
        {
            using Stream stream = File.OpenRead(filePath);
            AgeDisassembler disassembler = new AgeDisassembler(stream);
            List<AgeInstruction> instrs = disassembler.Disassemble();

            SortedList<int, string> names = new SortedList<int, string>();
            foreach (AgeInstruction instr in instrs)
            {
                if (instr.Opcode == AgeOpcode.MovString &&
                    instr.Operands[0].Type == AgeOperandType.GlobalStringVar &&
                    instr.Operands[1].Type == AgeOperandType.StringLiteral)
                {
                    int id = instr.Operands[0].Value;
                    string name = disassembler.GetStringAtAddress(instr.Operands[1].Value);
                    names.Add(id, name);
                }
            }
            return names;
        }

        /// <summary>
        /// Gets information about the AGE script function that outputs character names to the message window.
        /// First finds the innermost function (which sits at the very end of the script file),
        /// then follows any wrapper functions upwards until the top-level function is reached.
        /// </summary>
        private NameOutputInfo GetNameOutputInfo()
        {
            NameOutputInfo info = GetInnerNameOutputInfo();
            if (!info.Valid)
                return null;

            while (true)
            {
                int wrapperFuncOffset = -1;
                int currentFuncOffset = -1;
                for (int i = 0; i < _instructions.Count; i++)
                {
                    AgeInstruction instr = _instructions[i];
                    if (instr.Opcode == AgeOpcode.Exit ||
                        instr.Opcode == AgeOpcode.Ret)
                    {
                        currentFuncOffset = i < _instructions.Count - 1 ? _instructions[i + 1].Offset : -1;
                    }
                    else if (instr.Opcode == AgeOpcode.Call &&
                             instr.Operands[0].Matches(AgeOperandType.IntLiteral, info.FuncAddr))
                    {
                        if (wrapperFuncOffset < 0)
                        {
                            wrapperFuncOffset = currentFuncOffset;
                        }
                        else
                        {
                            // Multiple calls to inner func -> inner func is actually top-level func
                            wrapperFuncOffset = -1;
                            break;
                        }
                    }
                }

                if (wrapperFuncOffset >= 0)
                    info.FuncAddr = AgeDisassembler.OffsetToAddress(wrapperFuncOffset);
                else
                    break;
            }

            return info;
        }

        /// <summary>
        /// 
        /// This function does the following (simplified):
        ///     intermediateName = GlobalNameArray[majorIndex*MAJOR_INDEX_FACTOR + minorIndex];
        ///     name = intermediateName;
        ///     OutputName(name);
        ///
        /// Each time a name needs to be displayed, the script first sets majorIndex/minorIndex and then calls the function.
        /// </summary>
        private NameOutputInfo GetInnerNameOutputInfo()
        {
            NameOutputInfo info = new NameOutputInfo();
            int intermediateNameVariable = -1;

            int i = _instructions.Count - 1;

            // Skip exits and returns at the very end
            while (i >= 0 && (_instructions[i].Opcode == AgeOpcode.Exit || _instructions[i].Opcode == AgeOpcode.Ret))
            {
                i--;
            }

            for (; i >= Math.Max(_instructions.Count - 50, 0); i--)
            {
                AgeInstruction instr = _instructions[i];
                switch (instr.Opcode)
                {
                    // Output name variable
                    case AgeOpcode.Print
                        when instr.Operands[0].Matches(AgeOperandType.IntLiteral, 2) &&
                             instr.Operands[1].Type == AgeOperandType.GlobalStringVar:
                    {
                        info.NameVariable = instr.Operands[1].Value;
                        break;
                    }

                    // Set name variable to intermediate name variable
                    case AgeOpcode.MovString
                        when instr.Operands[0].Matches(AgeOperandType.GlobalStringVar, info.NameVariable) &&
                             instr.Operands[1].Type == AgeOperandType.LocalStringVarRef:
                    {
                        intermediateNameVariable = instr.Operands[1].Value;
                        break;
                    }

                    // Set intermediate name variable to item in global name array
                    case AgeOpcode.GetArrayItem                         // op0 = op1[op2 * op3 + op4]
                        when instr.Operands[0].Matches(AgeOperandType.LocalStringVarRef, intermediateNameVariable) &&
                             instr.Operands[1].Type == AgeOperandType.GlobalStringVar &&
                             instr.Operands[2].Type == AgeOperandType.GlobalIntVar &&
                             instr.Operands[3].Type == AgeOperandType.IntLiteral &&
                             instr.Operands[4].Type == AgeOperandType.GlobalIntVar:
                    {
                        info.NameArrayBase = instr.Operands[1].Value;
                        info.MajorIndexVariable = instr.Operands[2].Value;
                        info.MajorIndexFactor = instr.Operands[3].Value;
                        info.MinorIndexVariable = instr.Operands[4].Value;
                        break;
                    }

                    // Exit/return of preceding function
                    case AgeOpcode.Exit:
                    case AgeOpcode.Ret:
                        info.FuncAddr = _instructions[i + 1].Address;
                        break;
                }
                if (info.Valid)
                    break;
            }
            return info;
        }

        private static TextWriter GetDisassemblyWriter(string binFilePath)
        {
            //return null;

            Stream stream = File.Open(Path.ChangeExtension(binFilePath, ".txt"), FileMode.Create, FileAccess.Write);
            return new StreamWriter(stream);
        }

        private class NameOutputInfo
        {
            public int FuncAddr = -1;
            public int NameVariable = -1;
            public int NameArrayBase = -1;
            public int MajorIndexVariable = -1;
            public int MajorIndexFactor = -1;
            public int MinorIndexVariable = -1;

            public bool Valid => FuncAddr >= 0 &&
                                 NameVariable >= 0 &&
                                 NameArrayBase >= 0 &&
                                 MajorIndexVariable >= 0 &&
                                 MajorIndexFactor >= 0 &&
                                 MinorIndexVariable >= 0;
        }

        private enum AgeInstructionGroupType
        {
            DirectName,
            IndirectName,
            Message,
            Other
        }

        private readonly struct AgeInstructionGroup
        {
            public AgeInstructionGroup(AgeInstructionGroupType type, List<AgeInstruction> instrs)
            {
                Type = type;
                Instructions = instrs;
            }

            public readonly AgeInstructionGroupType Type;
            public readonly List<AgeInstruction> Instructions;
        }
    }
}
