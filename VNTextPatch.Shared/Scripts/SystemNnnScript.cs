using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts
{
    internal class SystemNnnScript : IScript
    {
        private static readonly byte[] MessageDataHeader =
            { 0x2D, 0x2D, 0x4D, 0x45, 0x53, 0x53, 0x41, 0x47, 0x45, 0x44, 0x41, 0x54, 0x41, 0x20, 0x20, 0x00 };

        private static readonly byte[] CommandDataHeader =
            { 0x2D, 0x43, 0x4F, 0x4D, 0x4D, 0x41, 0x4E, 0x44, 0x44, 0x41, 0x54, 0x41, 0x20, 0x20, 0x20, 0x00 };

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
        
        public string Extension => ".nnn";

        private byte[] _data;

        public void Load(ScriptLocation location)
        {
            _data = File.ReadAllBytes(location.ToFilePath());
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            MemoryStream stream = new MemoryStream(_data);
            BinaryReader reader = new BinaryReader(stream);
            foreach (NnnRange range in GetTextRanges())
            {
                stream.Position = range.Offset;
                string message = reader.ReadZeroTerminatedSjisString();
                message = Regex.Replace(message, @"(?<=^|\r\n)//.+?($|\r\n)", "");
                message = message.TrimEnd('\r', '\n');
                Match match = Regex.Match(message, @"^(?<name>[^「『』」\r\n]+)\r\n(?<message>[「『].+[』」])\s*$", RegexOptions.Singleline);
                if (match.Success)
                {
                    yield return new ScriptString(match.Groups["name"].Value, ScriptStringType.CharacterName);
                    yield return new ScriptString(match.Groups["message"].Value, ScriptStringType.Message);
                }
                else
                {
                    yield return new ScriptString(message, ScriptStringType.Message);
                }
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            using IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator();
            foreach (NnnRange range in GetTextRanges())
            {
                WordWrapper wrapper = range.Type == NnnMessageType.LPrint ? ProportionalWordWrapper.Secondary : ProportionalWordWrapper.Default;

                if (!stringEnumerator.MoveNext())
                    throw new Exception("Too few lines in translation file.");

                string text = stringEnumerator.Current.Text;
                text = wrapper.Wrap(text);
                if (stringEnumerator.Current.Type == ScriptStringType.CharacterName)
                {
                    if (!stringEnumerator.MoveNext() || stringEnumerator.Current.Type != ScriptStringType.Message)
                        throw new Exception("No message found after character name.");

                    text = text.Replace(" ", "　") + "\r\n" + wrapper.Wrap(stringEnumerator.Current.Text);
                }

                text = StringUtil.FancifyQuotes(text);
                byte[] textBytes = StringUtil.SjisTunnelEncoding.GetBytes(text);
                textBytes = BinaryUtil.Replace(textBytes, FormattingReplacements);
                if (textBytes.Length + 1 > range.Length)
                    throw new ArgumentException($"Message {text} is too long (can be {range.Length - 1} SJIS-encoded bytes at most)");

                Array.Copy(textBytes, 0, _data, range.Offset, textBytes.Length);
                _data[range.Offset + textBytes.Length] = 0;
            }

            if (stringEnumerator.MoveNext())
                throw new Exception("Too many strings in translation file.");

            File.WriteAllBytes(location.ToFilePath(), _data);
        }

        private IEnumerable<NnnRange> GetTextRanges()
        {
            int textBufferOffset = 0;
            int textBufferLength = 0;

            // Messages
            while (true)
            {
                int headerOffset = _data.IndexOf(MessageDataHeader, textBufferOffset + textBufferLength);
                if (headerOffset < 0)
                    break;

                NnnMessageType messageType = (NnnMessageType)BitConverter.ToInt32(_data, headerOffset + 0x10);
                textBufferOffset = headerOffset + 0x50 + 4 * BitConverter.ToInt32(_data, headerOffset + 0x4C);
                textBufferLength = BitConverter.ToInt32(_data, headerOffset + 0x3C);
                if (messageType != NnnMessageType.Draw)
                    yield return new NnnRange(textBufferOffset, textBufferLength, messageType);
            }

            // Commands
            while (true)
            {
                int headerOffset = _data.IndexOf(CommandDataHeader, textBufferOffset + textBufferLength);
                if (headerOffset < 0)
                    break;

                NnnCommandType commandType = (NnnCommandType)BitConverter.ToInt32(_data, headerOffset + 0x20);
                textBufferOffset = headerOffset + 0x60;
                textBufferLength = BitConverter.ToInt32(_data, headerOffset + 0x24);
                if (commandType == NnnCommandType.Case)
                    yield return new NnnRange(textBufferOffset, textBufferLength, NnnMessageType.Print);
            }
        }

        private struct NnnRange
        {
            public NnnRange(int offset, int length, NnnMessageType type)
            {
                Offset = offset;
                Length = length;
                Type = type;
            }

            public int Offset;
            public int Length;
            public NnnMessageType Type;
        }

        private enum NnnMessageType
        {
            Print,
            LPrint,
            Append,
            Draw
        }

        private enum NnnCommandType
        {
            Nop,
            If,
            Elsif,
            Else,
            Case,
            Story,
            Film,
            BgmMidi,
            BgmCd,
            System,
            Calcu,
            Ret,
            Next,
            End,
            Script,
            While = 17,
            EndIf = 19,
            Debug,
            Jump,
            Subscript
        }
    }
}
