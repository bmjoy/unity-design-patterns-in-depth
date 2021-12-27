#pragma warning disable IDE0029 // Use coalesce expression
#pragma warning disable IDE0051 // Remove unused private members

using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvancedSceneManager.Utility
{

    public static class DictionaryUtility
    {

        static readonly Dictionary<Type, object> nullKeys = new Dictionary<Type, object>();
        static T GetNullKey<T>()
        {
            if (nullKeys.ContainsKey(typeof(T)))
                return (T)nullKeys[typeof(T)];
            return default;
        }

        public static Dictionary<TKey, TValue> WithNullKey<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey nullKey)
        {
            if (nullKeys.ContainsKey(typeof(TKey)))
                nullKeys[typeof(TKey)] = nullKey;
            else
                nullKeys.Add(typeof(TKey), nullKey);
            return d;
        }

        public static TValue Set<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey key, TValue value)
        {
            Add(d, key, value);
            return value;
        }

        public static void EnsureAdded<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey key)
        {
            if (!d.ContainsKey(key))
                d.Add(key, default);
        }

        /// <summary>Adds or sets the value of a key.</summary>
        public static void Add<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey key, TValue value)
        {

            if (d == null)
                return;

            if (key == null) key = GetNullKey<TKey>();
            if (key == null) return;

            if (d.ContainsKey(key))
                d[key] = value;
            else
                d.Add(key, value);

        }

        /// <summary>Adds the value to the list with the specified key. Creates list automatically if null and adds key if necessary.</summary>
        public static void Add<TKey, TList, TItem>(this Dictionary<TKey, TList> d, TKey key, TItem item) where TList : IList<TItem>, new() =>
            AddRange(d, key, item);

        /// <summary>Adds the values to the list with the specified key. Creates list automatically if null and adds key if necessary.</summary>
        public static void AddRange<TKey, TList, TItem>(this Dictionary<TKey, TList> d, TKey key, IEnumerable<TItem> items) where TList : IList<TItem>, new() =>
            AddRange(d, key, items.ToArray());

        /// <summary>Adds the values to the list with the specified key. Creates list automatically if null and adds key if necessary.</summary>
        public static void AddRange<TKey, TList, TItem>(this Dictionary<TKey, TList> d, TKey key, params TItem[] items) where TList : IList<TItem>, new()
        {

            if (d == null)
                return;

            if (key == null) key = GetNullKey<TKey>();
            if (key == null) return;

            if (!d.ContainsKey(key))
                d.Add(key, new TList());
            else if (d[key] == null)
                d[key] = new TList();

            foreach (var item in items)
                d[key].Add(item);

        }

        /// <summary>Removes the value to the list with the specified key.</summary>
        public static void Remove<TKey, TList, TItem>(this Dictionary<TKey, TList> d, TKey key, TItem value) where TList : IList<TItem>, new()
        {

            if (d == null)
                return;

            if (key == null) key = GetNullKey<TKey>();
            if (key == null) return;

            if (d.ContainsKey(key))
                d[key]?.Remove(value);

        }

        /// <summary>Gets the value of the specified key, returns default if it does not exist.</summary>
        public static TValue GetValue<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey key, TValue defaultValue = default)
        {

            if (d == null)
                return defaultValue;

            if (key == null) key = GetNullKey<TKey>();
            if (key == null) return defaultValue;

            if (d.ContainsKey(key))
                return d[key];
            else
                return defaultValue;
        }

    }

}
