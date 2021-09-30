using System.IO;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.RealLive
{
    public class RealLiveAssembler
    {
        private readonly BinaryWriter _writer;
        private readonly byte[] _textBuffer = new byte[1024];

        public RealLiveAssembler(Stream stream)
        {
            _writer = new BinaryWriter(stream);
        }

        public void WriteString(string str, bool quote)
        {
            WriteString(str, 0, str.Length, quote);
        }

        public void WriteString(string str, int offset, int length, bool quote)
        {
            if (length == 0)
                return;

            int encodedLength = StringUtil.SjisEncoding.GetBytes(str, offset, length, _textBuffer, 0);
            if (!quote)
            {
                _writer.Write(_textBuffer, 0, encodedLength);
                return;
            }

            _writer.Write((byte)'"');
            int i = 0;
            while (i < encodedLength)
            {
                byte c = _textBuffer[i++];
                if (c == (byte)'"')
                {
                    _writer.Write((byte)'\\');
                    _writer.Write((byte)'"');
                }
                else
                {
                    _writer.Write(c);
                    if (StringUtil.IsShiftJisLeadByte(c))
                        _writer.Write(_textBuffer[i++]);
                }
            }
            _writer.Write((byte)'"');
        }

        public void WriteSelectTextWindow(int window)
        {
            WriteFunctionCall(0, 3, 102, 1, 0);
            WriteArgumentListStart();
            WriteInt(window);
            WriteArgumentListEnd();
        }

        public void WriteSelectTextWindow0()
        {
            WriteFunctionCall(0, 3, 102, 0, 1);
        }

        public void WriteTextPosX(int x)
        {
            WriteFunctionCall(0, 3, 311, 1, 0);
            WriteArgumentListStart();
            WriteInt(x);
            WriteArgumentListEnd();
        }

        public void WriteTextPosY(int y)
        {
            WriteFunctionCall(0, 3, 312, 1, 0);
            WriteArgumentListStart();
            WriteInt(y);
            WriteArgumentListEnd();
        }

        public void WriteTextOffsetX(int x)
        {
            WriteFunctionCall(0, 3, 321, 1, 0);
            WriteArgumentListStart();
            WriteInt(x);
            WriteArgumentListEnd();
        }

        public void WriteTextOffsetY(int x)
        {
            WriteFunctionCall(0, 3, 322, 1, 0);
            WriteArgumentListStart();
            WriteInt(x);
            WriteArgumentListEnd();
        }

        public void WriteLineBreak()
        {
            WriteFunctionCall(0, 3, 201, 0, 0);
        }

        private void WriteFunctionCall(byte type, byte module, ushort function, ushort numArgs, byte overload)
        {
            _writer.Write((byte)'#');
            _writer.Write(type);
            _writer.Write(module);
            _writer.Write(function);
            _writer.Write(numArgs);
            _writer.Write(overload);
        }

        private void WriteArgumentListStart()
        {
            _writer.Write((byte)'(');
        }

        private void WriteArgumentListEnd()
        {
            _writer.Write((byte)')');
        }

        private void WriteInt(int value)
        {
            _writer.Write((byte)'$');
            _writer.Write((byte)0xFF);
            _writer.Write(value);
        }
    }
}
