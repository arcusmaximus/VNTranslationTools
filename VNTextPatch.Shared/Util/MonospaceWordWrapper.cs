using System;
using System.Configuration;

namespace VNTextPatch.Shared.Util
{
    internal class MonospaceWordWrapper : WordWrapper
    {
        public static readonly MonospaceWordWrapper Default = new MonospaceWordWrapper();

        private MonospaceWordWrapper()
            : this(Convert.ToInt32(ConfigurationManager.AppSettings["MonospaceCharactersPerLine"]))
        {
        }

        public MonospaceWordWrapper(int charactersPerLine)
        {
            LineWidth = charactersPerLine;
        }

        protected override int GetTextWidth(string text, int offset, int length)
        {
            int width = 0;
            for (int i = offset; i < offset + length; i++)
            {
                if (StringUtil.IsJapaneseCharacter(text[i]))
                    width += 2;
                else
                    width++;
            }
            return width;
        }

        protected override int LineWidth
        {
            get;
        }
    }
}
