using System;
using System.Collections.Generic;
using System.IO;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.ArcGameEngine
{
    internal class AgeStringPoolBuilder
    {
        private readonly Dictionary<string, int> _relativeAddrs = new Dictionary<string, int>();
        private readonly MemoryStream _content = new MemoryStream();
        private readonly byte[] _textBuffer = new byte[0x200];

        public int Add(string str)
        {
            if (_relativeAddrs.TryGetValue(str, out int relativeAddr))
                return relativeAddr;

            relativeAddr = (int)_content.Length / 4;
            _relativeAddrs.Add(str, relativeAddr);

            int length = StringUtil.SjisTunnelEncoding.GetBytes(str, 0, str.Length, _textBuffer, 0);
            _textBuffer[length++] = 0x00;
            while ((length & 3) != 0)
            {
                _textBuffer[length++] = 0x00;
            }

            for (int i = 0; i < length; i++)
            {
                _textBuffer[i] ^= 0xFF;
            }

            _content.Write(_textBuffer, 0, length);
            
            return relativeAddr;
        }

        public ArraySegment<byte> Content
        {
            get
            {
                _content.TryGetBuffer(out ArraySegment<byte> content);
                return content;
            }
        }
    }
}
