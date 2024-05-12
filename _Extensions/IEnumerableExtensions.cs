using Gw2Sharp.WebApi.V2;
using System.Collections.Generic;

namespace ApiParser
{
    /// <summary>
    /// Provides extension methods for the <see cref="IEnumerable{T}"/> interface.
    /// </summary>
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Creates an <see cref="ApiV2BaseObjectList{T}"/> from an <see cref="IEnumerable{T}"/>.
        /// </summary>
        public static ApiV2BaseObjectList<T> ToApiV2BaseObjectList<T>(this IEnumerable<T> enumerable)
        {
            ApiV2BaseObjectList<T> result = new ApiV2BaseObjectList<T>();
            foreach (T item in enumerable)
            {
                result.Add(item);
            }

            return result;
        }
    }
}
