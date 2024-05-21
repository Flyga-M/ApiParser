namespace ApiParser.V2.Settings.Default
{
    /// <summary>
    /// Contains the constants used by the default <see cref="ParseSettings"/>.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The default identifier for a variable.
        /// </summary>
        public const string INDEX_VAR_IDENTIFIER = "$";

        /// <summary>
        /// The default separator for indices.
        /// </summary>
        public const char INDEX_SEPARATOR = ':';

        /// <summary>
        /// The default identifier for an optional index.
        /// </summary>
        public const string INDEX_OPTIONAL_IDENTIFIER = "OPTIONAL";

        /// <summary>
        /// The default open bracket to indicate the start of an index.
        /// </summary>
        public const char INDEX_OPEN = '[';

        /// <summary>
        /// The default close bracket to indicate the end of an index.
        /// </summary>
        public const char INDEX_CLOSE = ']';

        /// <summary>
        /// The default separator for endpoint parts.
        /// </summary>
        public const char ENDPOINT_SEPARATOR = '.';
    }
}
