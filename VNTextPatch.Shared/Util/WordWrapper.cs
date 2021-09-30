using System.Collections.Generic;
using System.Text;

namespace VNTextPatch.Shared.Util
{
    internal abstract class WordWrapper
    {
        private static readonly char[] LineBreakChars = { ' ', '-' };

        public string Wrap(string text, string lineBreak = "\r\n")
        {
            StringBuilder result = new StringBuilder();

            foreach (string line in text.Split(new[] { lineBreak }, System.StringSplitOptions.None))
            {
                int lineStartPos = 0;
                foreach (int lineEndPos in GetWrapPositions(line))
                {
                    if (result.Length > 0)
                        result.Append(lineBreak);

                    result.Append(line, lineStartPos, lineEndPos - lineStartPos);

                    lineStartPos = lineEndPos;
                    while (lineStartPos < line.Length && line[lineStartPos] == ' ')
                    {
                        lineStartPos++;
                    }
                }
            }

            return result.ToString();
        }

        public IEnumerable<int> GetWrapPositions(string text)
        {
            int lineStartPos = 0;
            int lineEndPos;
            while (lineStartPos < text.Length)
            {
                lineEndPos = lineStartPos;
                while (lineEndPos < text.Length)
                {
                    int searchPos = text.IndexOfAny(LineBreakChars, lineEndPos + 1);
                    if (searchPos >= 0)
                    {
                        if (text[searchPos] != ' ')
                            searchPos++;
                    }
                    else
                    {
                        searchPos = text.Length;
                    }

                    if (GetTextWidth(text, lineStartPos, searchPos - lineStartPos) > LineWidth)
                        break;

                    lineEndPos = searchPos;
                }

                if (lineEndPos == lineStartPos)
                {
                    int searchPos = lineEndPos;
                    while (lineEndPos < text.Length)
                    {
                        searchPos++;
                        if (GetTextWidth(text, lineStartPos, searchPos - lineStartPos) > LineWidth)
                            break;

                        lineEndPos = searchPos;
                    }
                }

                yield return lineEndPos;

                lineStartPos = lineEndPos;
                while (lineStartPos < text.Length && text[lineStartPos] == ' ')
                {
                    lineStartPos++;
                }
            }
        }

        protected abstract int GetTextWidth(string text, int offset, int length);

        protected abstract int LineWidth { get; }
    }
}
