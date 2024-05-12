using System;
using System.Collections.Generic;

namespace ApiParser.V2.Settings.Default
{
    /// <summary>
    /// A <see cref="Guid"/> <see cref="IIndexConverter"/>.
    /// </summary>
    public class GuidIndexConverter : IIndexConverter
    {
        /// <inheritdoc/>
        public IEnumerable<string> IndexIdentifiers => new string[] { "GUID" };

        /// <inheritdoc/>
        public Type Type => typeof(Guid);

        /// <inheritdoc/>
        public bool TryConvert(string value, out object result)
        {
            if (value == null)
            {
                result = null;
                return false;
            }

            bool success = Guid.TryParseExact(value, "D", out Guid guid);

            result = guid;

            return success;
        }
    }
}
