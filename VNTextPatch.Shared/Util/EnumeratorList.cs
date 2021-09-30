using System.Collections.Generic;

namespace VNTextPatch.Shared.Util
{
    internal class EnumeratorList<T>
    {
        private readonly List<IEnumerator<T>> _enumerators = new List<IEnumerator<T>>();

        public void Add(IEnumerable<T> enumerable)
        {
            _enumerators.Add(enumerable.GetEnumerator());
        }

        public int Count
        {
            get { return _enumerators.Count; }
        }

        public bool MoveNext()
        {
            bool success = true;
            for (int i = 0; i < _enumerators.Count; i++)
            {
                if (!MoveNext(i))
                    success = false;
            }
            
            return success;
        }

        public bool MoveNext(int index)
        {
            if (_enumerators[index] == null)
                return false;

            if (!_enumerators[index].MoveNext())
            {
                _enumerators[index].Dispose();
                _enumerators[index] = null;
                return false;
            }

            return true;
        }

        public bool IsOpen(int index)
        {
            return _enumerators[index] != null;
        }

        public T GetCurrent(int index)
        {
            return _enumerators[index].Current;
        }

        public T GetCurrentOrDefault(int index, T defaultValue)
        {
            return _enumerators[index] != null ? _enumerators[index].Current : defaultValue;
        }
    }
}
