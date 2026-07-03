using System;
using System.Collections.Generic;

namespace UIToolkitMVVM.Utils
{
    public static class DictExt
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
