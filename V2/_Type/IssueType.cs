using Gw2Sharp.WebApi.Exceptions;

namespace ApiParser.V2
{
    /// <summary>
    /// Contains information on the issue that occured with the gw2 api.
    /// </summary>
    public enum IssueType
    {
        /// <summary>
        /// No issue occured.
        /// </summary>
        WithoutIssue,
        /// <summary>
        /// A <see cref="TooManyRequestsException"/> occured.
        /// </summary>
        RateLimit,
        /// <summary>
        /// A <see cref="ServerErrorException"/> occured.
        /// </summary>
        ServerError,
        /// <summary>
        /// A <see cref="ServiceUnavailableException"/> occured.
        /// </summary>
        ServiceUnavailable
    }
}
