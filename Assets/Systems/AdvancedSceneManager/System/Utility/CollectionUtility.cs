#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace AdvancedSceneManager.Editor.Utility
{

    public static class CollectionUtility
    {

        /// <summary>Moves the <paramref name="item"/> up in the <paramref name="array"/>. Returns false if index is 0 or <paramref name="item"/> does not exist in <paramref name="array"/>.</summary>
        public static bool MoveUp<T>(ref T[] array, T item)
        {

            var i = ArrayUtility.IndexOf(array, item);
            if (i == -1 || i == 0)
                return false;

            ArrayUtility.RemoveAt(ref array, i);
            ArrayUtility.Insert(ref array, i - 1, item);

            return true;

        }

        /// <summary>Moves the <paramref name="item"/> up in the <paramref name="array"/>. Returns false if index is last or <paramref name="item"/> does not exist in <paramref name="array"/>.</summary>
        public static bool MoveDown<T>(ref T[] array, T item)
        {

            var i = ArrayUtility.IndexOf(array, item);
            if (i == -1 || i == array.Length - 1)
                return false;

            ArrayUtility.RemoveAt(ref array, i);
            ArrayUtility.Insert(ref array, i + 1, item);

            return true;

        }

        /// <summary>Runs <paramref name="action"/> on each item in <paramref name="list"/>.</summary>
        public static void ForEach<T>(this T[] list, Action<T, int> action)
        {
            for (int i = 0; i < list.Length; i++)
                action?.Invoke(list[i], i);
        }

        /// <summary>Runs <paramref name="action"/> on each item in <paramref name="list"/>.</summary>
        public static void ForEach<T>(this IEnumerable<T> list, Action<T, int> action)
        {
            for (int i = 0; i < list.Count(); i++)
                action?.Invoke(list.ElementAt(i), i);
        }

        /// <summary>
        /// <para>Flattens a multidimensional list.</para>
        /// <para>Usage: list.Flatten(item => item.subItems);</para>
        /// </summary>
        public static IEnumerable<T> Flatten<T>(this IEnumerable<T> list, Func<T, IEnumerable<T>> getSubList)
        {

            foreach (var item in list)
            {
                yield return item;
                foreach (var subItem in Flatten(getSubList?.Invoke(item), getSubList))
                    yield return subItem;
            }

        }

        /// <summary>Excludes the items from the list.</summary>
        public static IEnumerable<T> Except<T>(this IEnumerable<T> list, T item) =>
            list.Except(new[] { item });

        /// <summary>Groups consecutive items together.</summary>
        public static IEnumerable<IEnumerable<T>> GroupConsecutive<T>(this IEnumerable<T> list, Func<T, T, bool> compare)
        {
            if (list.Count() > 1)
            {

                var prevItem = list.First();
                var l = new List<T>();
                l.Add(prevItem);

                foreach (var item in list.Skip(1))
                {

                    if (!compare.Invoke(prevItem, item))
                    {
                        yield return l;
                        l = new List<T>();
                        l.Add(item);
                    }
                    else
                        l.Add(item);

                    prevItem = item;

                }

                yield return l;

            }
        }

    }

}
#endif
