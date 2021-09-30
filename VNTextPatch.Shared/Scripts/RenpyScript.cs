using System.Collections.Generic;

namespace VNTextPatch.Shared.Scripts
{
    public class RenpyScript : PlainTextScript
    {
        public override string Extension => ".rpy";

        protected override IEnumerable<Range> GetRanges(string script)
        {
            bool inSingleQuotes = false;
            bool inDoubleQuotes = false;
            int position = 0;
            int length = script.Length;
            int currentStringStart = 0;
            while (position < length)
            {
                char c = script[position];
                if (c == '#')
                {
                    while (position < length && script[position] != '\r' && script[position] != '\n')
                    {
                        position++;
                    }
                }
                else if (c == '\'' && !inDoubleQuotes)
                {
                    if (inSingleQuotes)
                        yield return new Range(currentStringStart + 1, position - (currentStringStart + 1), ScriptStringType.Message);
                    else
                        currentStringStart = position;

                    inSingleQuotes = !inSingleQuotes;
                    position++;
                }
                else if (c == '"' && !inSingleQuotes)
                {
                    if (inDoubleQuotes)
                        yield return new Range(currentStringStart + 1, position - (currentStringStart + 1), ScriptStringType.Message);
                    else
                        currentStringStart = position;

                    inDoubleQuotes = !inDoubleQuotes;
                    position++;
                }
                else if (c == '\\' && (inSingleQuotes || inDoubleQuotes))
                {
                    position += 2;
                }
                else
                {
                    position++;
                }
            }
        }
    }
}
