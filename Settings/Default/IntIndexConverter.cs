using System;
using System.Collections.Generic;

namespace ApiParser.Settings.Default
{
    /// <summary>
    /// A <see cref="int"/> <see cref="IIndexConverter"/>.
    /// </summary>
    public class IntIndexConverter : IIndexConverter
    {
        /// <inheritdoc/>
        public IEnumerable<string> IndexIdentifiers => new string[] { "INTEGER", "INT" };

        /// <inheritdoc/>
        public Type Type => typeof(int);

        /// <inheritdoc/>
        public bool TryConvert(string value, out object result)
        {
            if (value == null)
            {
                result = null;
                return false;
            }
            
            bool success = int.TryParse(value, out int @int);

            result = @int;

            return success;
        }
    }
}
