using System;
using System.Collections.Generic;
using System.Configuration;

namespace VNTextPatch.Shared.Util
{
    internal class ProportionalWordWrapper : WordWrapper, IDisposable
    {
        public static readonly ProportionalWordWrapper Default =
            new ProportionalWordWrapper(
                ConfigurationManager.AppSettings["ProportionalFontName"],
                Convert.ToInt32(ConfigurationManager.AppSettings["ProportionalFontSize"]),
                Convert.ToBoolean(ConfigurationManager.AppSettings["ProportionalFontBold"]),
                Convert.ToInt32(ConfigurationManager.AppSettings["ProportionalLineWidth"])
            );

        public static readonly ProportionalWordWrapper Secondary =
            new ProportionalWordWrapper(
                ConfigurationManager.AppSettings["ProportionalFontName"],
                Convert.ToInt32(ConfigurationManager.AppSettings["ProportionalFontSize"]),
                Convert.ToBoolean(ConfigurationManager.AppSettings["ProportionalFontBold"]),
                Convert.ToInt32(ConfigurationManager.AppSettings["SecondaryProportionalLineWidth"])
            );

        private readonly IntPtr _dc;
        private readonly IntPtr _font;

        private readonly byte[] _charWidths;
        private readonly Dictionary<int, int> _kernAmounts = new Dictionary<int, int>();

        public ProportionalWordWrapper(string fontName, int fontSize, bool bold, int lineWidth)
        {
            _dc = NativeMethods.GetDC(IntPtr.Zero);
            _font = NativeMethods.CreateFontW(
                fontSize,
                0,
                0,
                0,
                bold ? NativeMethods.FW_BOLD : NativeMethods.FW_NORMAL,
                false,
                false,
                false,
                NativeMethods.ANSI_CHARSET,
                NativeMethods.OUT_DEFAULT_PRECIS,
                NativeMethods.CLIP_DEFAULT_PRECIS,
                NativeMethods.DEFAULT_QUALITY,
                NativeMethods.DEFAULT_PITCH | NativeMethods.FF_DONTCARE,
                fontName
            );
            NativeMethods.SelectObject(_dc, _font);

            LineWidth = lineWidth;

            _charWidths = MeasureCharWidths((char)0, (char)0xFF);

            int numKerningPairs = NativeMethods.GetKerningPairsW(_dc, 0, null);
            NativeMethods.KERNINGPAIR[] kerningPairs = new NativeMethods.KERNINGPAIR[numKerningPairs];
            NativeMethods.GetKerningPairsW(_dc, kerningPairs.Length, kerningPairs);
            foreach (NativeMethods.KERNINGPAIR pair in kerningPairs)
            {
                _kernAmounts[pair.wFirst | (pair.wSecond << 16)] = pair.iKernAmount;
            }
        }

        protected override int GetTextWidth(string text, int offset, int length)
        {
            if (length <= 0)
                return 0;

            int width = 0;
            for (int i = offset; i < offset + length; i++)
            {
                width += GetCharWidth(text[i]);
                if (i > offset)
                    width += GetKernAmount(text[i - 1], text[i]);
            }
            return width;
        }

        private byte GetCharWidth(char c)
        {
            return c < _charWidths.Length ? _charWidths[c] : MeasureCharWidths(c, c)[0];
        }

        private byte[] MeasureCharWidths(char from, char to)
        {
            NativeMethods.ABCFLOAT[] abcs = new NativeMethods.ABCFLOAT[to - from + 1];
            NativeMethods.GetCharABCWidthsFloatW(_dc, from, to, abcs);

            byte[] widths = new byte[abcs.Length];
            for (int i = 0; i < abcs.Length; i++)
            {
                widths[i] = (byte)(abcs[i].abcfA + abcs[i].abcfB + abcs[i].abcfC);
            }
            return widths;
        }

        private int GetKernAmount(char first, char second)
        {
            return _kernAmounts.GetOrDefault(first | (second << 16));
        }

        protected override int LineWidth
        {
            get;
        }

        public void Dispose()
        {
            NativeMethods.ReleaseDC(IntPtr.Zero, _dc);
            NativeMethods.DeleteObject(_font);
        }
    }
}
