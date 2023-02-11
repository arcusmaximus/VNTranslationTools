using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Scripts;

namespace VNTextPatch.Shared.Util
{
    public static class StringUtil
    {
        public static readonly Encoding SjisEncoding = Encoding.GetEncoding(932, EncoderFallback.ExceptionFallback, DecoderFallback.ReplacementFallback);
        public static readonly SjisTunnelEncoding SjisTunnelEncoding = new SjisTunnelEncoding();

        private static readonly char[] ControlChars = "\a\b\f\n\r\t\v".ToCharArray();
        private static readonly char[] EscapeChars = "abfnrtv".ToCharArray();

        public static string QuoteC(string str)
        {
            return "\"" + EscapeC(str).Replace("\"", "\\\"") + "\"";
        }

        public static string UnquoteC(string str)
        {
            if (str.Length < 2 || !str.StartsWith("\"") || !str.EndsWith("\""))
                throw new ArgumentException("String is not quoted");

            return UnescapeC(str.Substring(1, str.Length - 2));
        }

        public static string EscapeC(string str)
        {
            StringBuilder result = null;
            int startOffset = 0;
            while (true)
            {
                int controlCharOffset = str.IndexOfAny(ControlChars, startOffset);
                if (controlCharOffset < 0)
                    break;

                result ??= new StringBuilder();
                result.Append(str, startOffset, controlCharOffset - startOffset);
                result.Append('\\');
                result.Append(MapChar(str[controlCharOffset], ControlChars, EscapeChars));
                startOffset = controlCharOffset + 1;
            }
            if (result == null)
                return str;

            result.Append(str, startOffset, str.Length - startOffset);
            return result.ToString();
        }

        public static string UnescapeC(string str)
        {
            StringBuilder result = null;
            int startOffset = 0;
            while (startOffset < str.Length)
            {
                int backslashOffset = str.IndexOf('\\', startOffset);
                if (backslashOffset < 0)
                    break;

                result ??= new StringBuilder();
                result.Append(str, startOffset, backslashOffset - startOffset);
                startOffset = backslashOffset + 2;
                if (backslashOffset < str.Length - 1)
                    result.Append(MapChar(str[backslashOffset + 1], EscapeChars, ControlChars));
                else
                    result.Append('\\');
            }
            if (result == null)
                return str;

            result.Append(str, startOffset, str.Length - startOffset);
            return result.ToString();
        }

        private static char MapChar(char c, char[] from, char[] to)
        {
            int index = from.IndexOf(c);
            return index >= 0 ? to[index] : c;
        }

        public static bool IsShiftJisLeadByte(byte b)
        {
            return (b >= 0x81 && b < 0xA0) || (b >= 0xE0 && b < 0xFD);
        }

        public static bool ContainsJapaneseText(string str)
        {
            return str.Any(IsJapaneseCharacter);
        }

        public static bool IsJapaneseCharacter(char c)
        {
            return c >= 0x3000;
        }

        public static string ToHalfWidth(string str)
        {
            StringBuilder result = new StringBuilder(str.Length);
            result.Length = NativeMethods.LCMapStringEx("ja-JP", NativeMethods.LCMAP_HALFWIDTH, str, str.Length, result, result.Capacity + 1, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            return result.ToString();
        }

        public static string ToFullWidth(string str)
        {
            StringBuilder result = new StringBuilder(str.Length);
            result.Length = NativeMethods.LCMapStringEx("ja-JP", NativeMethods.LCMAP_FULLWIDTH, str, str.Length, result, result.Capacity + 1, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            return result.ToString();
        }

        public static string NullIf(string str, string compareTo)
        {
            return str == compareTo ? null : str;
        }

        public static string NullIfEmpty(string str)
        {
            return NullIf(str, string.Empty);
        }

        public static string ReplaceMatchSurroundings(string input, Regex regex, Func<int, string, string> replace)
        {
            return ReplaceMatchesAndSurroundings(input, regex, m => m.Value, replace);
        }

        public static string ReplaceMatchesAndSurroundings(string input, Regex regex, Func<Match, string> replaceMatch, Func<int, string, string> replaceSurrounding)
        {
            StringBuilder result = new StringBuilder();
            int startPos = 0;
            foreach (Match match in regex.Matches(input))
            {
                if (startPos < match.Index)
                    result.Append(replaceSurrounding(startPos, input.Substring(startPos, match.Index - startPos)));

                result.Append(replaceMatch(match));
                startPos = match.Index + match.Length;
            }

            if (startPos < input.Length)
                result.Append(replaceSurrounding(startPos, input.Substring(startPos)));

            return result.ToString();
        }

        public static IEnumerable<(Range, bool)> GetMatchingAndSurroundingRanges(string input, Regex regex)
        {
            int startPos = 0;
            foreach (Match match in regex.Matches(input))
            {
                if (startPos < match.Index)
                    yield return (new Range(startPos, match.Index - startPos, ScriptStringType.Message), false);

                if (match.Length > 0)
                    yield return (new Range(match.Index, match.Length, ScriptStringType.Message), true);

                startPos = match.Index + match.Length;
            }

            if (startPos < input.Length)
                yield return (new Range(startPos, input.Length - startPos, ScriptStringType.Message), false);
        }

        public static IEnumerable<(string, Match)> GetMatchingAndSurroundingTexts(string input, Regex regex)
        {
            int startPos = 0;
            foreach (Match match in regex.Matches(input))
            {
                if (startPos < match.Index)
                    yield return (input.Substring(startPos, match.Index - startPos), null);

                if (match.Length > 0)
                    yield return (null, match);

                startPos = match.Index + match.Length;
            }

            if (startPos < input.Length)
                yield return (input.Substring(startPos), null);
        }

        public static string FancifyQuotes(string str, Regex tagRegex = null)
        {
            MatchCollection tagMatches = tagRegex?.Matches(str);
            str = Regex.Replace(
                str,
                @"(?<=^|\s|"")'",
                m => IsInTag(m.Index) ? m.Value : "‘"
            );
            str = Regex.Replace(
                str,
                @"'",
                m => IsInTag(m.Index) ? m.Value : "’"
            );
            str = Regex.Replace(
                str,
                @"(?<=^|\s)""",
                m => IsInTag(m.Index) ? m.Value : "“"
            );
            str = Regex.Replace(
                str,
                @"""",
                m => IsInTag(m.Index) ? m.Value : "”"
            );
            return str;

            bool IsInTag(int index)
            {
                if (tagMatches == null)
                    return false;

                return tagMatches.Cast<Match>().Any(m => index >= m.Index && index < m.Index + m.Length);
            }
        }
    }
}
