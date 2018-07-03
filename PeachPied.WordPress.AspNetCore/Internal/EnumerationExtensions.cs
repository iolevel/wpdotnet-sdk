using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PeachPied.WordPress.AspNetCore.Internal
{
    internal static class EnumerationExtensions
    {
        /// <summary>
        /// Gets value indicating whether the enumeration is null or empty for sure.
        /// </summary>
        public static bool IsEmpty<T>(this IEnumerable<T> collection)
        {
            return collection == null || ((collection is ICollection col) && col.Count == 0);
        }

        /// <summary>
        /// Safely concatenates two enumerations. They both can be <c>null</c>.
        /// The method always returns non-null object.
        /// </summary>
        public static IEnumerable<T>/*!*/ConcatSafe<T>(this IEnumerable<T> collection1, IEnumerable<T> collection2)
        {
            if (IsEmpty(collection2))
            {
                return collection1 ?? Array.Empty<T>();
            }

            if (IsEmpty(collection1))
            {
                return collection2;
            }

            //
            return collection1.Concat(collection2);
        }

        /// <summary>
        /// Creates array with concatenation of given item and an enumeration.
        /// </summary>
        public static T[]/*!*/ArrayConcat<T>(this T item, IEnumerable<T> enumeration)
        {
            if (IsEmpty(enumeration))
            {
                return new[] { item };
            }

            //
            T[] result;

            if (enumeration is ICollection collection)
            {
                result = new T[1 + collection.Count];
                result[0] = item;
                collection.CopyTo(result, 1);
            }
            else
            {
                var list = new List<T>() { item };
                list.AddRange(enumeration);
                result = list.ToArray();
            }

            //
            return result;
        }
    }

}
