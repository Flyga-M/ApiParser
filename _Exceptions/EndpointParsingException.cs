using System;
using System.Runtime.Serialization;

namespace ApiParser
{
    /// <summary>
    /// The <see cref="EndpointException"/> that is thrown, when an endpoint could not be parsed correctly.
    /// </summary>
    public class EndpointParsingException : EndpointException
    {
        /// <inheritdoc/>
        public EndpointParsingException()
        {

        }

        /// <inheritdoc/>
        public EndpointParsingException(string message)
            : base(message)
        {

        }

        /// <inheritdoc/>
        public EndpointParsingException(string message, string endpointName)
            : base(message, endpointName)
        {

        }

        /// <inheritdoc/>
        public EndpointParsingException(string message, Exception inner)
            : base(message, inner)
        {

        }

        /// <inheritdoc/>
        public EndpointParsingException(string message, string endpointName, Exception inner)
            : base(message, endpointName, inner)
        {

        }

        /// <inheritdoc/>
        protected EndpointParsingException(SerializationInfo info, StreamingContext ctxt) : base(info, ctxt)
        {

        }
    }
}
