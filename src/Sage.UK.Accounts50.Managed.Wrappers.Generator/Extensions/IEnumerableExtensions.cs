using System;
using System.Collections.Generic;
using System.Linq;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.Extensions
{
    /// <summary>
    /// Provides extended functionality on <see cref="IEnumerable{T}"/>.
    /// </summary>
    internal static class IEnumerableExtensions
    {
        /// <summary>
        /// Flattens a list of items, where each item in the list can have the same list of items itself, into one single list with all items combined.
        /// </summary>
        /// <typeparam name="T">Type of item</typeparam>
        /// <param name="e">Current list of items where we want to flatten a list for each item present in it.</param>
        /// <param name="produceDescendents">A callback that gives us the next list of items.</param>
        /// <returns>One list containing all items extracted from every list we received.</returns>
        public static IEnumerable<T> Flatten<T>(this IEnumerable<T> e, Func<T, IEnumerable<T>> produceDescendents)
        {
            if (e != null && produceDescendents != null)
            {
                return e.SelectMany(c => produceDescendents(c).Flatten(produceDescendents)).Concat(e);
            }

            return Enumerable.Empty<T>();
        }
    }
}
