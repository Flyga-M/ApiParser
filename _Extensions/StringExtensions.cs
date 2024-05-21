namespace ApiParser
{
    /// <summary>
    /// Provides extension methods for the <see cref="string"/> class.
    /// </summary>
    public static class StringExtensions
    {
        /// <inheritdoc cref="string.EndsWith(string)"/>
        public static bool EndsWith(this string @string, char value)
        {
            return @string.EndsWith(value.ToString());
        }
    }
}
