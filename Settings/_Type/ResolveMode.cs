namespace ApiParser.Settings
{
    /// <summary>
    /// Determines how to continue, after a recoverable api error.
    /// </summary>
    public enum ResolveMode
    {
        /// <summary>
        /// Try once. If it fails, throw an exception.
        /// </summary>
        None,
        /// <summary>
        /// Try for x amount of times if a recoverable error occurs. Throw if the error is not recoverable 
        /// or if all retries fail.
        /// </summary>
        Retry,
        /// <summary>
        /// Try for x amount of times if a recoverable error occurs. Use the data from the previous call if all retries 
        /// fail. Throw if the error is not recoverable or if all retries fail and the previous data is null.
        /// </summary>
        RetryOrUsePrevious,
        /// <summary>
        /// Try once. Use the data from the previous call if a recoverable error occurs. 
        /// Throw if the error is not recoverable or the previous data is null.
        /// </summary>
        UsePrevious
    }
}
