using System;
using System.Collections.Generic;

namespace UIKit.Runtime.Utils
{
    public static class DictionaryExtensions
    {
        public static Dictionary<TKey, TVal> ToDictionaryChecked<TKey, TVal>(this IEnumerable<TVal> source, Func<TVal, TKey> keySelector)
        {
            var d = new Dictionary<TKey, TVal>();
            foreach (var value in source) {
                var key = keySelector(value);
                if (!d.TryAdd(key, value)) {
                    throw new ArgumentException($"Duplicate key: '{key}'");
                }
            }
            return d;
        }
    }
}
