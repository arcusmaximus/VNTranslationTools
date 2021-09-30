using System;

namespace VNTextPatch.Shared.Util
{
    internal class TrackingStringReader : IDisposable
    {
        private string _str;
        private int _pos;
        private readonly int _length;

        public TrackingStringReader(string str)
        {
            _str = str;
            _length = str.Length;
        }

        public string ReadLine()
        {
            if (_str == null)
                throw new ObjectDisposedException(nameof(TrackingStringReader));

            int i;
            for (i = _pos; i < _length; i++)
            {
                char c = _str[i];
                if (c == '\r' || c == '\n')
                {
                    string line = _str.Substring(_pos, i - _pos);
                    _pos = i + 1;
                    if (c == '\r' && _pos < _length && _str[_pos] == '\n')
                        _pos++;

                    return line;
                }
            }

            if (i > _pos)
            {
                string line = _str.Substring(_pos, i - _pos);
                _pos = i;
                return line;
            }

            return null;
        }

        public int Position
        {
            get { return _pos; }
        }

        public void Dispose()
        {
            _str = null;
        }
    }
}
