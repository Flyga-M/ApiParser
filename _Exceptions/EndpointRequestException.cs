using System;
using Gw2Sharp.WebApi.Exceptions;

namespace ApiParser
{
    /// <summary>
    /// The <see cref="EndpointException"/> that is thrown when an endpoint fails to receive data from the 
    /// api. The inner exception contains the <see cref="RequestException"/> or <see cref="RequestException{TResponse}"/>.
    /// </summary>
    public class EndpointRequestException : EndpointException
    {
        /// <summary>
        /// Determines whether the inner <see cref="RequestException"/> is recoverable.
        /// </summary>
        public bool Recoverable
        {
            get
            {
                if (InnerException == null)
                {
                    return false;
                }

                return RequestExceptionUtil.IsRecoverable(InnerException);
            }
        }

        /// <inheritdoc/>
        public EndpointRequestException(string message, Exception inner)
            : base(message, inner)
        {
            
        }
    }
}
