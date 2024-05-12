namespace ApiParser.V2.Settings
{
    /// <summary>
    /// Determines how to continue, after a retriable api error.
    /// </summary>
    public enum ResolveMode
    {
        /// <summary>
        /// Just throw an exception.
        /// </summary>
        None,
        /// <summary>
        /// Retry the same query again.
        /// </summary>
        Retry,
        /// <summary>
        /// Retry the same query again. If it still fails, use the data of the previous call, even if it is null.
        /// </summary>
        RetryOrUsePrevious,
        /// <summary>
        /// Use the data of the previous call to the endpoint, even if it is null.
        /// </summary>
        UsePrevious
    }
}
