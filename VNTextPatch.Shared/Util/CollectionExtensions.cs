using System;
using System.Collections.Generic;
using System.Linq;

namespace VNTextPatch.Shared.Util
{
    internal static class CollectionExtensions
    {
        public static T Get<T>(this ArraySegment<T> segment, int index)
        {
            return segment.Array[segment.Offset + index];
        }

        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict.ContainsKey(key))
                return false;

            dict.Add(key, value);
            return true;
        }

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default(TValue))
        {
            return dict.TryGetValue(key, out TValue value) ? value : defaultValue;
        }

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> getDefault)
        {
            return dict.TryGetValue(key, out TValue value) ? value : getDefault();
        }

        public static TValue FetchValue<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> getValue)
        {
            TValue value;
            if (!dict.TryGetValue(key, out value))
            {
                value = getValue();
                dict.Add(key, value);
            }
            return value;
        }

        public static TValue FetchValue<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> getValue)
        {
            TValue value;
            if (!dict.TryGetValue(key, out value))
            {
                value = getValue(key);
                dict.Add(key, value);
            }
            return value;
        }

        public static int IndexOf<T>(this IEnumerable<T> items, T item)
        {
            int index = 0;
            foreach (T i in items)
            {
                if (item.Equals(i))
                    return index;

                index++;
            }
            return -1;
        }

        public static int IndexOf(this byte[] data, byte[] toSearch, int startIndex = 0)
        {
            for (int i = startIndex; i <= data.Length - toSearch.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < toSearch.Length; j++)
                {
                    if (data[i + j] != toSearch[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                    return i;
            }

            return -1;
        }

        public static IList<T> AsList<T>(this IEnumerable<T> items)
        {
            return items as IList<T> ?? items.ToList();
        }

        public static int BinaryLastLessOrEqual<TValue>(this IList<TValue> list, TValue value)
        {
            IComparer<TValue> comparer = Comparer<TValue>.Default;

            int start = 0;
            int end = list.Count;
            while (start < end)
            {
                int pivot = start + (end - start) / 2;
                int comparison = comparer.Compare(value, list[pivot]);
                if (comparison < 0)
                    end = pivot;
                else if (comparison == 0)
                    return pivot;
                else
                    start = pivot + 1;
            }

            return start - 1;
        }

        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
        {
            key = pair.Key;
            value = pair.Value;
        }
    }
}
