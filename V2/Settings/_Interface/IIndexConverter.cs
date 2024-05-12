using System;
using System.Collections.Generic;

namespace ApiParser.V2.Settings
{
    /// <summary>
    /// Represents a class that can convert a string to a specific index type.
    /// </summary>
    public interface IIndexConverter
    {
        /// <summary>
        /// The identifier and it's aliases of the index type.
        /// </summary>
        IEnumerable<string> IndexIdentifiers { get; }

        /// <summary>
        /// The <see cref="Type"/> that the <see cref="IIndexConverter"/> converts to.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Attempts to convert the given <paramref name="value"/> to the <see cref="Type"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="result"></param>
        /// <returns>True, if the <paramref name="value"/> was successfully converted to 
        /// the <see cref="Type"/>. Otherwise false.</returns>
        bool TryConvert(string value, out object result);
    }
}
