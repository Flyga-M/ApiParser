using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gw2Sharp.WebApi.Exceptions;

namespace ApiParser.V2
{
    /// <summary>
    /// Provides information on the assumed current state of the gw2 api.
    /// </summary>
    public enum ApiState
    {
        /// <summary>
        /// No request has been made in some time, so not enough information to make an assessment.
        /// </summary>
        Unknown,
        /// <summary>
        /// The latest requests experienced little to no server issues.
        /// </summary>
        Reliable,
        /// <summary>
        /// The latest request experienced more than a few server issues.
        /// </summary>
        Unreliable,
        /// <summary>
        /// The latest requests threw a <see cref="TooManyRequestsException"/>.
        /// </summary>
        RateLimited
    }
}
