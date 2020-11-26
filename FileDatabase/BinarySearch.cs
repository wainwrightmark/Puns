using System;
using System.Collections.Generic;

namespace FileDatabase
{
    //https://stackoverflow.com/questions/967047/how-to-perform-a-binary-search-on-ilistt
    public static class BinarySearchHelper
    {
        /// <summary>
        /// Performs a binary search on the specified collection.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <typeparam name="TSearch">The type of the searched item.</typeparam>
        /// <param name="list">The list to be searched.</param>
        /// <param name="value">The value to search for.</param>
        /// <param name="comparer">The comparer that is used to compare the value
        /// with the list items.</param>
        /// <returns></returns>
        public static int BinarySearch<TItem, TSearch>(this IList<TItem> list,
            TSearch value, Func<TSearch, TItem, int> comparer)
        {
            var lower = 0;
            var upper = list.Count - 1;

            while (lower <= upper)
            {
                var middle = lower + (upper - lower) / 2;
                var comparisonResult = comparer(value, list[middle]);
                if (comparisonResult < 0)
                    upper = middle - 1;
                else if (comparisonResult > 0)
                    lower = middle + 1;
                else
                    return middle;
            }

            //var bitwise =  ~lower;

            return lower - 1;
        }

        /// <summary>
        /// Performs a binary search on the specified collection.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="list">The list to be searched.</param>
        /// <param name="value">The value to search for.</param>
        /// <returns></returns>
        public static int BinarySearch<TItem>(this IList<TItem> list, TItem value) => BinarySearch(list, value, Comparer<TItem>.Default);

        /// <summary>
        /// Performs a binary search on the specified collection.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="list">The list to be searched.</param>
        /// <param name="value">The value to search for.</param>
        /// <param name="comparer">The comparer that is used to compare the value
        /// with the list items.</param>
        /// <returns></returns>
        public static int BinarySearch<TItem>(this IList<TItem> list, TItem value,
            IComparer<TItem> comparer) =>
            list.BinarySearch(value, comparer.Compare!);
    }
}