using System;
using System.Collections.Generic;

namespace ApiParser.V2.Settings.Default
{
    /// <summary>
    /// A <see cref="string"/> <see cref="IIndexConverter"/>.
    /// </summary>
    public class StringIndexConverter : IIndexConverter
    {
        /// <inheritdoc/>
        public IEnumerable<string> IndexIdentifiers => new string[] { "STRING", "STR" };

        /// <inheritdoc/>
        public Type Type => typeof(string);

        /// <inheritdoc/>
        public bool TryConvert(string value, out object result)
        {
            result = value;
            
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return true;
        }
    }
}
