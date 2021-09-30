using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts
{
    internal class CatSystemScript : IScript
    {
        private static readonly byte[] Magic = { 0x43, 0x61, 0x74, 0x53, 0x63, 0x65, 0x6E, 0x65 };

        private byte[] _script;
        private int _stringAddressesOffset;
        private int _stringsOffset;
        private int _numStrings;

        public string Extension => ".cst";

        public void Load(ScriptLocation location)
        {
            byte[] compressedData = File.ReadAllBytes(location.ToFilePath());
            _script = Decompress(compressedData);

            int dataLength = BitConverter.ToInt32(_script, 0);
            if (0x10 + dataLength != _script.Length)
                throw new InvalidDataException("Invalid data length");

            int clearScreenCount = BitConverter.ToInt32(_script, 4);
            _stringAddressesOffset = 0x10 + BitConverter.ToInt32(_script, 8);
            _stringsOffset = 0x10 + BitConverter.ToInt32(_script, 0xC);
            _numStrings = (_stringsOffset - _stringAddressesOffset) / 4;
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            StringBuilder currentMessage = new StringBuilder();
            foreach (CstString cstString in GetCstStrings())
            {
                if (cstString.Type == CstStringType.Message && !cstString.IsEmpty)
                {
                    currentMessage.Append(cstString.Text);
                }
                else
                {
                    if (currentMessage.Length > 0)
                    {
                        yield return new ScriptString(CstTextToScriptText(currentMessage.ToString()), ScriptStringType.Message);
                        currentMessage.Clear();
                    }

                    switch (cstString.Type)
                    {
                        case CstStringType.Character:
                            yield return new ScriptString(cstString.Text, ScriptStringType.CharacterName);
                            break;

                        case CstStringType.Command:
                            Match match = Regex.Match(cstString.Text, @"^\d+\s+\w+\s+(.+)");
                            if (match.Success)
                                yield return new ScriptString(match.Groups[1].Value, ScriptStringType.Message);
                            
                            break;
                    }
                }
            }

            if (currentMessage.Length > 0)
                yield return new ScriptString(CstTextToScriptText(currentMessage.ToString()), ScriptStringType.Message);
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            MemoryStream patchedStream = PatchScript(strings);

            using (Stream outputStream = File.Open(location.ToFilePath(), FileMode.Create, FileAccess.Write))
            {
                Compress(patchedStream, outputStream);
            }
        }

        private MemoryStream PatchScript(IEnumerable<ScriptString> strings)
        {
            MemoryStream originalStream = new MemoryStream(_script);
            MemoryStream patchedStream = new MemoryStream(_script.Length);
            BinaryPatcher patcher = new BinaryPatcher(originalStream, patchedStream, a => _stringsOffset + a, o => o - _stringsOffset);
            patcher.CopyUpTo(_stringsOffset);

            using (IEnumerator<ScriptString> scriptStringEnumerator = strings.GetEnumerator())
            {
                bool inMessageContinuation = false;
                foreach (CstString cstString in GetCstStrings())
                {
                    switch (cstString.Type)
                    {
                        case CstStringType.Character:
                            if (!scriptStringEnumerator.MoveNext())
                                throw new InvalidDataException("Not enough lines in translation file");

                            patcher.CopyUpTo(_stringsOffset + cstString.Address + 2);
                            patcher.ReplaceZeroTerminatedSjisString(scriptStringEnumerator.Current.Text);

                            inMessageContinuation = false;
                            break;

                        case CstStringType.Message when !cstString.IsEmpty:
                            patcher.CopyUpTo(_stringsOffset + cstString.Address + 2);
                            if (!inMessageContinuation)
                            {
                                if (!scriptStringEnumerator.MoveNext())
                                    throw new InvalidDataException("Not enough lines in translation file");

                                string text = ScriptTextToCstText(cstString, scriptStringEnumerator.Current.Text);
                                patcher.ReplaceZeroTerminatedSjisString(text);
                            }
                            else
                            {
                                patcher.ReplaceZeroTerminatedSjisString("");
                            }

                            inMessageContinuation = true;
                            break;

                        case CstStringType.Command:
                            Match match = Regex.Match(cstString.Text, @"^(\d+\s+\w+\s+).+");
                            if (match.Success)
                            {
                                if (!scriptStringEnumerator.MoveNext())
                                    throw new InvalidDataException("Not enough lines in translation file");

                                string text = scriptStringEnumerator.Current.Text.Replace(" ", "　");
                                patcher.CopyUpTo(_stringsOffset + cstString.Address + 2);
                                patcher.ReplaceZeroTerminatedSjisString(match.Groups[1].Value + text);
                            }

                            inMessageContinuation = false;
                            break;

                        default:
                            inMessageContinuation = false;
                            break;
                    }
                }

                if (scriptStringEnumerator.MoveNext())
                    throw new InvalidDataException("Too many lines in translation file");
            }

            patcher.CopyUpTo(_script.Length);
            for (int addressOffset = _stringAddressesOffset; addressOffset < _stringsOffset; addressOffset += 4)
            {
                patcher.PatchAddress(addressOffset);
            }

            patcher.PatchInt32(0, (int)patchedStream.Length - 0x10);

            patchedStream.Position = 0;
            return patchedStream;
        }

        private static byte[] Decompress(byte[] inputData)
        {
            MemoryStream inputStream = new MemoryStream(inputData);
            BinaryReader reader = new BinaryReader(inputStream);
            byte[] magic = reader.ReadBytes(8);
            if (!magic.SequenceEqual(Magic))
                throw new InvalidDataException("Invalid magic");

            int compressedLength = reader.ReadInt32();
            int uncompressedLength = reader.ReadInt32();
            if (compressedLength == 0)
            {
                if (uncompressedLength != inputStream.Length - 0x10)
                    throw new InvalidDataException("Invalid size");

                return reader.ReadBytes(uncompressedLength);
            }
            else
            {
                if (compressedLength != inputStream.Length - 0x10)
                    throw new InvalidDataException("Invalid compressed size");

                ushort zlibHeader = reader.ReadUInt16();
                byte[] uncompressedData = new byte[uncompressedLength];
                using (DeflateStream decompressionStream = new DeflateStream(inputStream, CompressionMode.Decompress))
                {
                    decompressionStream.Read(uncompressedData, 0, uncompressedData.Length);
                }
                return uncompressedData;
            }
        }

        private static void Compress(Stream uncompressedStream, Stream compressedStream)
        {
            BinaryWriter writer = new BinaryWriter(compressedStream);
            writer.Write(Magic);
            writer.Write(0);
            writer.Write((int)uncompressedStream.Length);

            using (ZlibStream zlibStream = new ZlibStream(compressedStream))
            {
                uncompressedStream.CopyTo(zlibStream);
            }

            int compressedLength = (int)compressedStream.Length - 0x10;
            compressedStream.Position = 8;
            writer.Write(compressedLength);
        }

        private IEnumerable<CstString> GetCstStrings()
        {
            MemoryStream stream = new MemoryStream(_script);
            BinaryReader reader = new BinaryReader(stream);

            for (int i = 0; i < _numStrings; i++)
            {
                int address = BitConverter.ToInt32(_script, _stringAddressesOffset + 4 * i);
                stream.Position = _stringsOffset + address;

                byte startMarker = reader.ReadByte();
                if (startMarker != 0x01)
                    throw new InvalidDataException($"Unexpected start marker byte {startMarker:X02}");

                CstStringType type = (CstStringType)reader.ReadByte();
                if (!Enum.IsDefined(typeof(CstStringType), type))
                    throw new InvalidDataException($"Unknown string type {(int)type:X02}");

                string text = reader.ReadZeroTerminatedSjisString();
                yield return new CstString(address, type, text);
            }
        }

        private static string CstTextToScriptText(string text)
        {
            text = text.Replace("\\n", "\r\n");
            text = text.Replace("\\@", "");
            return text;
        }

        private static string ScriptTextToCstText(CstString cstString, string text)
        {
            if (!StringUtil.ContainsJapaneseText(text))
            {
                // Prevent wordwrapping in the middle of words: CS2 "[word]" syntax
                text = Regex.Replace(text, @"\S+", m => Regex.IsMatch(m.Value, @"\\[^@]") ? m.Value : $"[{m.Value}]");

                // Smaller font size so everything fits in the message window.
                // Delimit with dummy tag so it doesn't become "\fss" due to a line starting with "s"
                text = "\\fs\\@" + text;
            }

            text = ReplaceSpecialCharsByEscapeSequence(text);
            text = text.Replace("\r\n", "\\n");

            if (cstString.Text.EndsWith("\\@"))
                text += "\\@";

            return text;
        }

        private static string ReplaceSpecialCharsByEscapeSequence(string str)
        {
            StringBuilder result = new StringBuilder(str.Length);
            byte[] bytes = new byte[2];
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (c == '\'' || c == '"')
                {
                    result.Append($@"\${(int)c};");
                }
                else
                {
                    try
                    {
                        StringUtil.SjisEncoding.GetBytes(str, i, 1, bytes, 0);
                        result.Append(str[i]);
                    }
                    catch
                    {
                        result.Append($@"\${(int)c};");
                    }
                }
            }
            return result.ToString();
        }

        private enum CstStringType
        {
            EmptyLine = 0x02,
            Paragraph = 0x03,
            Message = 0x20,
            Character = 0x21,
            Command = 0x30,
            FileName = 0xF0,
            LineNumber = 0xF1
        }

        private struct CstString
        {
            public CstString(int address, CstStringType type, string text)
            {
                Address = address;
                Type = type;
                Text = text;
            }

            public int Address
            {
                get;
            }

            public CstStringType Type
            {
                get;
            }

            public string Text
            {
                get;
            }

            public bool IsEmpty
            {
                get { return string.IsNullOrEmpty(Text) || Text == "\\n" || Text == "\\p"; }
            }
        }
    }
}
