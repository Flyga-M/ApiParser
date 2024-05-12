using System;
using System.Runtime.Serialization;

namespace ApiParser
{
    /// <summary>
    /// A general <see cref="Exception"/> that is thrown inside this library. Every other 
    /// exception inherits from the <see cref="ApiParserException"/>.
    /// </summary>
    public class ApiParserException : Exception
    {
        /// <inheritdoc/>
        public ApiParserException()
        {

        }

        /// <inheritdoc/>
        public ApiParserException(string message)
            : base(message)
        {

        }

        /// <inheritdoc/>
        public ApiParserException(string message, Exception inner)
            : base(message, inner)
        {

        }

        /// <inheritdoc/>
        protected ApiParserException(SerializationInfo info, StreamingContext ctxt) : base(info, ctxt)
        {

        }
    }
}
