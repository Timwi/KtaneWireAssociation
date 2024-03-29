﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Rnd = UnityEngine.Random;

namespace WireAssociation
{
    static class Ut
    {
        public static T[] NewArray<T>(params T[] array) { return array; }

        public static T[] NewArray<T>(int size, Func<int, T> initialiser = null)
        {
            var result = new T[size];
            if (initialiser != null)
                for (int i = 0; i < size; i++)
                    result[i] = initialiser(i);
            return result;
        }

        /// <summary>
        ///     Turns all elements in the enumerable to strings and joins them using the specified <paramref
        ///     name="separator"/> and the specified <paramref name="prefix"/> and <paramref name="suffix"/> for each string.</summary>
        /// <param name="values">
        ///     The sequence of elements to join into a string.</param>
        /// <param name="separator">
        ///     Optionally, a separator to insert between each element and the next.</param>
        /// <param name="prefix">
        ///     Optionally, a string to insert in front of each element.</param>
        /// <param name="suffix">
        ///     Optionally, a string to insert after each element.</param>
        /// <param name="lastSeparator">
        ///     Optionally, a separator to use between the second-to-last and the last element.</param>
        /// <example>
        ///     <code>
        ///         // Returns "[Paris], [London], [Tokyo]"
        ///         (new[] { "Paris", "London", "Tokyo" }).JoinString(", ", "[", "]")
        ///         
        ///         // Returns "[Paris], [London] and [Tokyo]"
        ///         (new[] { "Paris", "London", "Tokyo" }).JoinString(", ", "[", "]", " and ");</code></example>
        public static string JoinString<T>(this IEnumerable<T> values, string separator = null, string prefix = null, string suffix = null, string lastSeparator = null)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            if (lastSeparator == null)
                lastSeparator = separator;

            using (var enumerator = values.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    return "";

                // Optimise the case where there is only one element
                var one = enumerator.Current;
                if (!enumerator.MoveNext())
                    return prefix + one + suffix;

                // Optimise the case where there are only two elements
                var two = enumerator.Current;
                if (!enumerator.MoveNext())
                {
                    // Optimise the (common) case where there is no prefix/suffix; this prevents an array allocation when calling string.Concat()
                    if (prefix == null && suffix == null)
                        return one + lastSeparator + two;
                    return prefix + one + suffix + lastSeparator + prefix + two + suffix;
                }

                StringBuilder sb = new StringBuilder()
                    .Append(prefix).Append(one).Append(suffix).Append(separator)
                    .Append(prefix).Append(two).Append(suffix);
                var prev = enumerator.Current;
                while (enumerator.MoveNext())
                {
                    sb.Append(separator).Append(prefix).Append(prev).Append(suffix);
                    prev = enumerator.Current;
                }
                sb.Append(lastSeparator).Append(prefix).Append(prev).Append(suffix);
                return sb.ToString();
            }
        }

        public static T PickRandom<T>(this IEnumerable<T> src)
        {
            if (src == null)
                throw new ArgumentNullException("src");

            var arr = src.ToArray();
            if (arr.Length == 0)
                throw new InvalidOperationException("Cannot pick a random element from an empty set.");
            return arr[Rnd.Range(0, arr.Length)];
        }

        /// <summary>
        ///     Enumerates all consecutive pairs of the elements.</summary>
        /// <param name="source">
        ///     The input enumerable.</param>
        /// <param name="closed">
        ///     If true, an additional pair containing the last and first element is included. For example, if the source
        ///     collection contains { 1, 2, 3, 4 } then the enumeration contains { (1, 2), (2, 3), (3, 4) } if <paramref
        ///     name="closed"/> is <c>false</c>, and { (1, 2), (2, 3), (3, 4), (4, 1) } if <paramref name="closed"/> is
        ///     <c>true</c>.</param>
        /// <param name="selector">
        ///     The selector function to run each consecutive pair through.</param>
        public static IEnumerable<TResult> SelectConsecutivePairs<T, TResult>(this IEnumerable<T> source, bool closed, Func<T, T, TResult> selector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");
            return selectConsecutivePairsIterator(source, closed, selector);
        }
        private static IEnumerable<TResult> selectConsecutivePairsIterator<T, TResult>(IEnumerable<T> source, bool closed, Func<T, T, TResult> selector)
        {
            using (var enumer = source.GetEnumerator())
            {
                bool any = enumer.MoveNext();
                if (!any)
                    yield break;
                T first = enumer.Current;
                T last = enumer.Current;
                while (enumer.MoveNext())
                {
                    yield return selector(last, enumer.Current);
                    last = enumer.Current;
                }
                if (closed)
                    yield return selector(last, first);
            }
        }

        public static IEnumerable<TResult> SelectManyConsecutivePairs<T, TResult>(this IEnumerable<T> source, bool closed, Func<T, T, IEnumerable<TResult>> selector)
        {
            return source.SelectConsecutivePairs(closed, selector).SelectMany(x => x);
        }

        /// <summary>
        ///     Enumerates the items of this collection, skipping the last <paramref name="count"/> items. Note that the
        ///     memory usage of this method is proportional to <paramref name="count"/>, but the source collection is only
        ///     enumerated once, and in a lazy fashion. Also, enumerating the first item will take longer than enumerating
        ///     subsequent items.</summary>
        /// <param name="source">
        ///     Source collection.</param>
        /// <param name="count">
        ///     Number of items to skip from the end of the collection.</param>
        /// <param name="throwIfNotEnough">
        ///     If <c>true</c>, the enumerator throws at the end of the enumeration if the source collection contained fewer
        ///     than <paramref name="count"/> elements.</param>
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int count, bool throwIfNotEnough = false)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "count cannot be negative.");
            if (count == 0)
                return source;

            var collection = source as ICollection<T>;
            if (collection != null)
            {
                if (throwIfNotEnough && collection.Count < count)
                    throw new InvalidOperationException("The collection does not contain enough elements.");
                return collection.Take(Math.Max(0, collection.Count - count));
            }

            return skipLastIterator(source, count, throwIfNotEnough);
        }
        private static IEnumerable<T> skipLastIterator<T>(IEnumerable<T> source, int count, bool throwIfNotEnough)
        {
            var queue = new T[count];
            int headtail = 0; // tail while we're still collecting, both head & tail afterwards because the queue becomes completely full
            int collected = 0;

            foreach (var item in source)
            {
                if (collected < count)
                {
                    queue[headtail] = item;
                    headtail++;
                    collected++;
                }
                else
                {
                    if (headtail == count)
                        headtail = 0;
                    yield return queue[headtail];
                    queue[headtail] = item;
                    headtail++;
                }
            }

            if (throwIfNotEnough && collected < count)
                throw new InvalidOperationException("The collection does not contain enough elements.");
        }

        /// <summary>
        ///     Returns a collection containing only the last <paramref name="count"/> items of the input collection. This
        ///     method enumerates the entire collection to the end once before returning. Note also that the memory usage of
        ///     this method is proportional to <paramref name="count"/>.</summary>
        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "count cannot be negative.");
            if (count == 0)
                return new T[0];

            var collection = source as ICollection<T>;
            if (collection != null)
                return collection.Skip(Math.Max(0, collection.Count - count));

            var queue = new Queue<T>(count + 1);
            foreach (var item in source)
            {
                if (queue.Count == count)
                    queue.Dequeue();
                queue.Enqueue(item);
            }
            return queue.AsEnumerable();
        }

        /// <summary>
        ///     Similar to <see cref="string.Substring(int)"/>, but for arrays. Returns a new array containing all items from
        ///     the specified <paramref name="startIndex"/> onwards.</summary>
        /// <remarks>
        ///     Returns a new copy of the array even if <paramref name="startIndex"/> is 0.</remarks>
        public static T[] Subarray<T>(this T[] array, int startIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            return Subarray(array, startIndex, array.Length - startIndex);
        }

        /// <summary>
        ///     Similar to <see cref="string.Substring(int,int)"/>, but for arrays. Returns a new array containing <paramref
        ///     name="length"/> items from the specified <paramref name="startIndex"/> onwards.</summary>
        /// <remarks>
        ///     Returns a new copy of the array even if <paramref name="startIndex"/> is 0 and <paramref name="length"/> is
        ///     the length of the input array.</remarks>
        public static T[] Subarray<T>(this T[] array, int startIndex, int length)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex", "startIndex cannot be negative.");
            if (length < 0 || startIndex + length > array.Length)
                throw new ArgumentOutOfRangeException("length", "length cannot be negative or extend beyond the end of the array.");
            T[] result = new T[length];
            Array.Copy(array, startIndex, result, 0, length);
            return result;
        }
    }
}
