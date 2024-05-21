using Gw2Sharp.WebApi.Exceptions;
using System;
using Gw2Sharp.WebApi;

namespace ApiParser
{
    /// <summary>
    /// Provides utility functions for <see cref="Gw2Sharp.WebApi.Exceptions"/>.
    /// </summary>
    public static class RequestExceptionUtil
    {
        /// <summary>
        /// Determines whether an <see cref="Exception"/> thrown by the <see cref="IGw2WebApiClient"/> 
        /// is recoverable.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns>True, if the <paramref name="exception"/> is either a 
        /// <see cref="TooManyRequestsException"/> or a
        /// <see cref="ServerErrorException"/> or a 
        /// <see cref="ServiceUnavailableException"/>.</returns>
        public static bool IsRecoverable(Exception exception)
        {
            return exception is TooManyRequestsException
                || exception is ServerErrorException
                || exception is ServiceUnavailableException;
        }
    }
}
