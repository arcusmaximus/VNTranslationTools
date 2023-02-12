using System.IO;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Artemis
{
    internal static class LuaParser
    {
        public static ILuaNode Read(string text, ref int pos)
        {
            SkipWhitespace(text, ref pos);
            if (pos == text.Length)
                return null;

            char c = text[pos];
            if ((c >= '0' && c <= '9') || c == '+' || c == '-' || c == '.')
                return ReadNumber(text, ref pos);

            if (c == '"' || c == '\'')
                return ReadQuotedString(text, ref pos);

            if (c == '[' && pos + 1 < text.Length && (text[pos + 1] == '[' || text[pos + 1] == '='))
                return ReadBracketedString(text, ref pos);

            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || c == '[')
                return ReadAttribute(text, ref pos);

            if (c == '{')
                return ReadTable(text, ref pos);

            throw new InvalidDataException("Invalid token found");
        }

        private static void SkipWhitespace(string text, ref int pos)
        {
            while (pos < text.Length && char.IsWhiteSpace(text[pos]))
            {
                pos++;
            }
        }

        private static LuaNumber ReadNumber(string text, ref int pos)
        {
            int startPos = pos;
            while (pos < text.Length)
            {
                char c = text[pos];
                if ((c >= '0' && c <= '9') || c == '+' || c == '-' || c == '.')
                    pos++;
                else
                    break;
            }
            return new LuaNumber(text.Substring(startPos, pos - startPos));
        }

        private static LuaString ReadQuotedString(string text, ref int pos)
        {
            int startPos = pos;
            char quote = text[pos++];
            while (pos < text.Length)
            {
                char c = text[pos++];
                if (c == '\\')
                    pos++;
                else if (c == quote)
                    break;
            }
            return new LuaString(StringUtil.UnescapeC(text.Substring(startPos + 1, pos - startPos - 2)));
        }

        private static LuaString ReadBracketedString(string text, ref int pos)
        {
            pos++;
            int openBracketLevel = 0;
            while (pos < text.Length && text[pos] == '=')
            {
                openBracketLevel++;
                pos++;
            }

            if (pos == text.Length || text[pos] != '[')
                throw new InvalidDataException("No second [ found in literal string");

            pos++;
            int startPos = pos;
            int endPos = 0;
            bool inClosingBracket = false;
            int closeBracketLevel = 0;
            while (true)
            {
                if (pos == text.Length)
                    throw new InvalidDataException("Unclosed literal string encountered");

                char c = text[pos++];
                if (c == ']')
                {
                    if (!inClosingBracket || closeBracketLevel != openBracketLevel)
                    {
                        endPos = pos - 1;
                        inClosingBracket = true;
                        closeBracketLevel = 0;
                    }
                    else
                    {
                        break;
                    }
                }
                else if (c == '=')
                {
                    if (inClosingBracket)
                        closeBracketLevel++;
                }
                else
                {
                    inClosingBracket = false;
                }
            }

            return new LuaString(text.Substring(startPos, endPos - startPos));
        }

        private static LuaAttribute ReadAttribute(string text, ref int pos)
        {
            int nameStartPos = pos;
            string name;
            if (text[pos] == '[')
            {
                pos++;
                ILuaNode nameNode = Read(text, ref pos);
                name = nameNode switch
                       {
                           LuaNumber num => num.Value,
                           LuaString str => str.Value,
                           _ => throw new InvalidDataException("Invalid data type in attribute name")
                       };
                SkipWhitespace(text, ref pos);
                if (pos == text.Length || text[pos] != ']')
                    throw new InvalidDataException("No \"]\" found after attribute name");

                pos++;
            }
            else
            {
                while (pos < text.Length)
                {
                    char c = text[pos];
                    if (char.IsLetterOrDigit(c) || c == '_')
                        pos++;
                    else
                        break;
                }
                name = text.Substring(nameStartPos, pos - nameStartPos);
            }

            SkipWhitespace(text, ref pos);
            if (pos == text.Length || text[pos++] != '=')
                throw new InvalidDataException("No \"=\" found after attribute name");

            ILuaNode value = Read(text, ref pos);
            if (value == null)
                throw new InvalidDataException("Attribute without value encountered");

            return new LuaAttribute(name, value);
        }

        private static LuaTable ReadTable(string text, ref int pos)
        {
            LuaTable table = new LuaTable();
            pos++;
            while (pos < text.Length)
            {
                SkipWhitespace(text, ref pos);
                if (pos == text.Length)
                    throw new InvalidDataException("Incomplete table encountered");

                if (text[pos] == '}')
                {
                    pos++;
                    break;
                }

                ILuaNode item = Read(text, ref pos);
                if (item == null)
                    throw new InvalidDataException("Incomplete table encountered");

                table.Add(item);

                SkipWhitespace(text, ref pos);
                if (pos == text.Length)
                    throw new InvalidDataException("Incomplete table encountered");

                if (text[pos] == ',')
                    pos++;
            }
            return table;
        }
    }
}
