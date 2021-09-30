using System;
using System.Runtime.InteropServices;
using System.Text;

namespace VNTextPatch.Shared.Util
{
    internal static class NativeMethods
    {
        [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
        public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr CreateFontW(
            int height,
            int width,
            int escapement,
            int orientation,
            int weight,
            bool italic,
            bool underline,
            bool strikeout,
            int charset,
            int outputPrecision,
            int clipPrecision,
            int quality,
            int pitchAndFamily,
            [MarshalAs(UnmanagedType.LPWStr)] string face
        );

        [DllImport("gdi32", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

        [DllImport("gdi32", CallingConvention = CallingConvention.StdCall)]
        public static extern bool GetCharABCWidthsFloatW(IntPtr hdc, int iFirst, int iLast, [MarshalAs(UnmanagedType.LPArray), Out] ABCFLOAT[] lpABC);

        [DllImport("gdi32", CallingConvention = CallingConvention.StdCall)]
        public static extern int GetKerningPairsW(IntPtr hdc, int nPairs, [MarshalAs(UnmanagedType.LPArray), Out] KERNINGPAIR[] lpKernPair);

        [DllImport("gdi32", CallingConvention = CallingConvention.StdCall)]
        public static extern bool DeleteObject(IntPtr h);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        public const int FW_NORMAL = 400;
        public const int FW_BOLD = 700;

        public const int ANSI_CHARSET = 0;
        public const int DEFAULT_CHARSET = 1;
        public const int SHIFTJIS_CHARSET = 128;

        public const int OUT_DEFAULT_PRECIS = 0;
        public const int CLIP_DEFAULT_PRECIS = 0;

        public const int DEFAULT_QUALITY = 0;
        public const int ANTIALIASED_QUALITY = 4;

        public const int DEFAULT_PITCH = 0;
        public const int FF_DONTCARE = 0 << 4;

        [StructLayout(LayoutKind.Sequential)]
        public struct KERNINGPAIR
        {
            public ushort wFirst;
            public ushort wSecond;
            public int iKernAmount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ABCFLOAT
        {
            public float abcfA;
            public float abcfB;
            public float abcfC;
        }

        public const int LCMAP_HALFWIDTH = 0x00400000;
        public const int LCMAP_FULLWIDTH = 0x00800000;

        [DllImport("kernel32", CallingConvention = CallingConvention.StdCall)]
        public static extern int LCMapStringEx(
            [MarshalAs(UnmanagedType.LPWStr)] string localeName,
            int mapFlags,
            [MarshalAs(UnmanagedType.LPWStr)] string sourceString,
            int sourceStringLength,
            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder destStr,
            int destStringLength,
            IntPtr pVersionInformation,
            IntPtr reserved,
            IntPtr sortHandle
        );
    }
}
