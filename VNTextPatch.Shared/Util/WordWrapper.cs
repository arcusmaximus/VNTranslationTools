using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Scripts;

namespace VNTextPatch.Shared.Util
{
    internal abstract class WordWrapper
    {
        private static readonly char[] LineBreakChars = { ' ', '-' };

        public string Wrap(string text, Regex controlCodePattern = null, string lineBreak = "\r\n")
        {
            StringBuilder result = new StringBuilder();

            foreach (string line in text.Split(new[] { lineBreak }, StringSplitOptions.None))
            {
                int lineStartPos = 0;
                foreach (int lineEndPos in GetWrapPositions(line, controlCodePattern))
                {
                    if (lineEndPos == lineStartPos)
                        continue;

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

        public IEnumerable<int> GetWrapPositions(string text, Regex controlCodePattern)
        {
            if (controlCodePattern == null)
            {
                foreach (int position in GetWrapPositions(text))
                {
                    yield return position;
                }
                yield break;
            }

            StringBuilder cleanedText = new StringBuilder();
            SortedList<int, int> positionMapping = new SortedList<int, int>();
            foreach ((Range range, bool isControlCode) in StringUtil.GetMatchingAndSurroundingRanges(text, controlCodePattern))
            {
                if (isControlCode)
                    continue;

                positionMapping.Add(range.Offset, cleanedText.Length);
                cleanedText.Append(text, range.Offset, range.Length);
            }

            foreach (int position in GetWrapPositions(cleanedText.ToString()))
            {
                if (position == cleanedText.Length)
                {
                    yield return text.Length;
                }
                else
                {
                    int mappingIdx = positionMapping.Values.BinaryLastLessOrEqual(position);
                    yield return position - positionMapping.Values[mappingIdx] + positionMapping.Keys[mappingIdx];
                }
            }
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

                if (lineEndPos < text.Length && "，。？！」』】）’”".IndexOf(text[lineEndPos]) >= 0)
                    lineEndPos++;

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
