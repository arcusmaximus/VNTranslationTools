using System;
using System.Collections.Generic;

namespace VNTextPatch.Shared.Util
{
    internal static class EnumerableExtensions
    {
        public static void ZipStrict<T1, T2>(this IEnumerable<T1> list1, IEnumerable<T2> list2, Action<T1, T2> action)
        {
            using (IEnumerator<T1> enumerator1 = list1.GetEnumerator())
            using (IEnumerator<T2> enumerator2 = list2.GetEnumerator())
            {
                while (true)
                {
                    bool move1Succeeded = enumerator1.MoveNext();
                    bool move2Succeeded = enumerator2.MoveNext();
                    if (move1Succeeded != move2Succeeded)
                        throw new ArgumentException("Lists don't have equal number of items");

                    if (!move1Succeeded)
                        break;

                    action(enumerator1.Current, enumerator2.Current);
                }
            }
        }
    }
}
