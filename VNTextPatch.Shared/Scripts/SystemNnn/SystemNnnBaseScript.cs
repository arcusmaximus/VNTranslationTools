using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.SystemNnn
{
    internal abstract class SystemNnnBaseScript
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

        protected virtual IEnumerable<ScriptString> ParseFileText(string text)
        {
            text = Regex.Replace(text, @"(?<=^|\r\n)//.+?($|\r\n)", "");
            text = text.TrimEnd('\r', '\n');
            Match match = Regex.Match(text, @"^(?<name>[^「『（）』」\r\n]+)\r\n(?<message>[「『（].+[）』」])\s*$", RegexOptions.Singleline);
            if (match.Success)
            {
                yield return new ScriptString(match.Groups["name"].Value, ScriptStringType.CharacterName);
                yield return new ScriptString(match.Groups["message"].Value, ScriptStringType.Message);
            }
            else
            {
                yield return new ScriptString(text, ScriptStringType.Message);
            }
        }

        protected virtual byte[] FormatFileText(IEnumerator<ScriptString> stringEnumerator, NnnMessageType type)
        {
            WordWrapper wrapper = type == NnnMessageType.LPrint ? ProportionalWordWrapper.Secondary : ProportionalWordWrapper.Default;

            if (!stringEnumerator.MoveNext())
                throw new Exception("Too few lines in translation file.");

            string text = stringEnumerator.Current.Text;
            if (stringEnumerator.Current.Type == ScriptStringType.CharacterName)
            {
                if (!stringEnumerator.MoveNext() || stringEnumerator.Current.Type != ScriptStringType.Message)
                    throw new Exception("No message found after character name.");

                text = text.Replace(" ", "　") + "\r\n" + wrapper.Wrap(stringEnumerator.Current.Text);
            }
            else
            {
                text = wrapper.Wrap(text);
            }

            text = text.Replace("#", "＃");

            text = StringUtil.FancifyQuotes(text);
            byte[] textBytes = StringUtil.SjisTunnelEncoding.GetBytes(text);
            textBytes = BinaryUtil.Replace(textBytes, FormattingReplacements);
            return textBytes;
        }

        protected enum NnnMessageType
        {
            Print,
            LPrint,
            Append,
            Draw
        }
    }
}
