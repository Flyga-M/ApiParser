using System;
using System.Runtime.Serialization;

namespace ApiParser
{
    /// <summary>
    /// The <see cref="EndpointException"/> that is thrown when an endpoint fails to receive data from the 
    /// api.
    /// </summary>
    public class EndpointRequestException : EndpointException
    {
        /// <inheritdoc/>
        public EndpointRequestException()
        {

        }

        /// <inheritdoc/>
        public EndpointRequestException(string message)
            : base(message)
        {

        }

        /// <inheritdoc/>
        public EndpointRequestException(string message, string endpointName)
            : base(message, endpointName)
        {

        }

        /// <inheritdoc/>
        public EndpointRequestException(string message, Exception inner)
            : base(message, inner)
        {

        }

        /// <inheritdoc/>
        public EndpointRequestException(string message, string endpointName, Exception inner)
            : base(message, endpointName, inner)
        {

        }

        /// <inheritdoc/>
        protected EndpointRequestException(SerializationInfo info, StreamingContext ctxt) : base(info, ctxt)
        {

        }
    }
}
