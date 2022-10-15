using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Mware
{
    public class MwareScript : IScript
    {
        public string Extension => ".nut";

        private byte[] _data;
        private bool _hasHeader;
        private List<SquirrelLiteralPool> _literalPools;
        private List<SquirrelLiteralReference> _literalRefs;
        private GuessedEncoding _encoding;

        public void Load(ScriptLocation location)
        {
            _data = File.ReadAllBytes(location.ToFilePath());
            _literalPools = new List<SquirrelLiteralPool>();
            _literalRefs = new List<SquirrelLiteralReference>();
            _encoding = new GuessedEncoding();

            MemoryStream stream = new MemoryStream(_data);
            using StreamWriter writer = null; //new StreamWriter(Path.ChangeExtension(location.ToFilePath(), ".txt"));
            SquirrelV2Disassembler disassembler = new SquirrelV2Disassembler(stream, _encoding, writer);
            disassembler.LiteralPoolEncountered += p => _literalPools.Add(p);
            disassembler.TextReferenceEncountered += r => _literalRefs.Add(r);
            disassembler.Disassemble();

            _hasHeader = disassembler.HasHeader;
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            foreach (SquirrelLiteralReference reference in _literalRefs)
            {
                string value = (string)reference.Value;
                foreach (Range range in GetTextRanges(value, reference.Type))
                {
                    yield return new ScriptString(value.Substring(range.Offset, range.Length), range.Type);
                }
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            List<SquirrelLiteralReference> referencesToPatch = MergeIntoLiteralPools(strings);

            using Stream inputStream = new MemoryStream(_data);
            using Stream outputStream = File.Open(location.ToFilePath(), FileMode.Create, FileAccess.Write);
            BinaryPatcher patcher = new BinaryPatcher(inputStream, outputStream);

            PatchLiteralPools(patcher);
            
            patcher.CopyUpTo((int)inputStream.Length);
            if (_hasHeader)
            {
                patcher.PatchInt32(8, (int)outputStream.Length - 0x14);
                patcher.PatchInt32(0xC, (int)outputStream.Length - 4);
            }

            PatchLiteralReferences(patcher, referencesToPatch);
        }

        private List<SquirrelLiteralReference> MergeIntoLiteralPools(IEnumerable<ScriptString> strings)
        {
            SquirrelLiteralPool currentPool = null;
            int lastLiteralIndex = -1;
            List<SquirrelLiteralReference> referencesToPatch = new List<SquirrelLiteralReference>();

            using IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator();
            foreach (SquirrelLiteralReference reference in _literalRefs)
            {
                string newText = MergeIntoText((string)reference.Value, reference.Type, stringEnumerator);
                if (reference.Pool != currentPool)
                {
                    currentPool = reference.Pool;
                    lastLiteralIndex = -1;
                }

                if (reference.Index > lastLiteralIndex)
                {
                    reference.Pool.Values[reference.Index] = newText;
                    lastLiteralIndex = reference.Index;
                }
                else
                {
                    reference.Pool.Values.Add(newText);
                    reference.Index = reference.Pool.Values.Count - 1;
                    referencesToPatch.Add(reference);
                }
            }

            if (stringEnumerator.MoveNext())
                throw new Exception("Too many lines in translation");

            return referencesToPatch;
        }

        private static string MergeIntoText(string origValue, ScriptStringType origType, IEnumerator<ScriptString> stringEnumerator)
        {
            StringBuilder newValue = new StringBuilder();
            int origStart = 0;
            foreach (Range range in GetTextRanges(origValue, origType))
            {
                if (!stringEnumerator.MoveNext())
                    throw new Exception("Too few lines in translation");

                if (origStart < range.Offset)
                    newValue.Append(origValue, origStart, range.Offset - origStart);

                string newText = ProportionalWordWrapper.Default.Wrap(stringEnumerator.Current.Text);
                newValue.Append(newText);
                origStart = range.Offset + range.Length;
            }

            if (origStart < origValue.Length)
                newValue.Append(origValue, origStart, origValue.Length - origStart);

            return newValue.ToString();
        }

        private void PatchLiteralPools(BinaryPatcher patcher)
        {
            foreach (SquirrelLiteralPool pool in _literalPools.Where(p => p.Length > 0)
                                                              .OrderBy(p => p.Offset))
            {
                patcher.CopyUpTo(pool.Offset);
                patcher.PatchInt32(pool.CountOffset, pool.Values.Count);
                patcher.ReplaceBytes(
                    pool.Length,
                    writer =>
                    {
                        foreach (object value in pool.Values)
                        {
                            SquirrelObject.Write(writer, value, _encoding);
                        }
                    }
                );
            }
        }

        private static void PatchLiteralReferences(BinaryPatcher patcher, List<SquirrelLiteralReference> references)
        {
            foreach (SquirrelLiteralReference reference in references)
            {
                switch (reference.Length)
                {
                    case 1:
                        patcher.PatchByte(reference.Offset, (byte)reference.Index);
                        break;

                    case 4:
                        patcher.PatchInt32(reference.Offset, reference.Index);
                        break;
                }
            }
        }

        private static IEnumerable<Range> GetTextRanges(string value, ScriptStringType type)
        {
            if (type == ScriptStringType.CharacterName)
            {
                yield return new Range(0, value.Length, ScriptStringType.CharacterName);
                yield break;
            }

            TrackingStringReader reader = new TrackingStringReader(value);
            int paragraphStart = -1;
            while (true)
            {
                int lineStartOffset = reader.Position;
                string line = reader.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                {
                    if (paragraphStart >= 0)
                    {
                        int length = lineStartOffset - paragraphStart;
                        if (length >= 2 &&
                            value[paragraphStart + length - 2] == '\r' &&
                            value[paragraphStart + length - 1] == '\n')
                        {
                            length -= 2;
                        }

                        yield return new Range(paragraphStart, length, ScriptStringType.Message);
                    }

                    if (line == null)
                        yield break;

                    paragraphStart = -1;
                    continue;
                }

                if (line.StartsWith("//"))
                    continue;

                if (line.StartsWith("<voice"))
                {
                    Match match = Regex.Match(line, @" name='([^']+)'");
                    if (match.Success)
                        yield return new Range(lineStartOffset + match.Groups[1].Index, match.Groups[1].Length, ScriptStringType.CharacterName);

                    continue;
                }

                if (paragraphStart < 0)
                    paragraphStart = lineStartOffset;
            }
        }
    }
}
