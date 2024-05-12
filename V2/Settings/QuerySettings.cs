namespace ApiParser.V2.Settings
{
    /// <summary>
    /// Determines how a query is resolved.
    /// </summary>
    public struct QuerySettings
    {
        public static readonly QuerySettings Default = new QuerySettings(null, null, null, null);
        
        /// <summary>
        /// Determines how to continue after a retriable api error.
        /// </summary>
        public ResolveMode ResolveMode;

        /// <summary>
        /// The amount of milliseconds until a retry attempt is made if <see cref="ResolveMode"/> 
        /// is set to <see cref="ResolveMode.Retry"/>, <see cref="ResolveMode.RetryOrUsePrevious"/> or 
        /// <see cref="ResolveMode.UsePreviousOrRetry"/>.
        /// </summary>
        public int RetryDelay;

        /// <summary>
        /// The amount of times a retry attempt is made if <see cref="ResolveMode"/> 
        /// is set to <see cref="ResolveMode.Retry"/>, <see cref="ResolveMode.RetryOrUsePrevious"/> or 
        /// <see cref="ResolveMode.UsePreviousOrRetry"/>.
        /// </summary>
        public int RetryAmount;

        /// <summary>
        /// The <see cref="IQueryVariableResolver"/> that is used to resolve variables inside the query. Optional, if 
        /// the queries that are resolved with this do not contain any variables.
        /// </summary>
        public IQueryVariableResolver VariableResolver;

        public QuerySettings(ResolveMode? resolve = null, int? retryDelay = null, int? retryAmount = null, IQueryVariableResolver variableResolver = null)
        {
            if (!resolve.HasValue)
            {
                resolve = ResolveMode.Retry;
            }

            if (!retryDelay.HasValue)
            {
                retryDelay = 1000;
            }

            if (!retryAmount.HasValue)
            {
                retryAmount = 3;
            }

            if (variableResolver == null)
            {
                variableResolver = new Default.QueryVariableResolver();
            }

            ResolveMode = resolve.Value;
            RetryDelay = retryDelay.Value;
            RetryAmount = retryAmount.Value;
            VariableResolver = variableResolver;
        }
    }
}
