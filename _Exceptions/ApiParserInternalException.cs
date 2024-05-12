using System;
using System.Runtime.Serialization;

namespace ApiParser
{
    /// <summary>
    /// The <see cref="ApiParserException"/> that is thrown, when an internal error occurs 
    /// in this library. If this <see cref="ApiParserException"/> get's ever thrown, 
    /// there is an error in the internal logic of this library. Please report the issue to me!
    /// </summary>
    public class ApiParserInternalException : ApiParserException
    {
        /// <inheritdoc/>
        public ApiParserInternalException()
        {

        }

        /// <inheritdoc/>
        public ApiParserInternalException(string message)
            : base(message)
        {

        }

        /// <summary>
        /// Inializes a new instace of the <see cref="ApiParserInternalException"/> with the default message and a reference to 
        /// the inner <see cref="Exception"/>, that triggered this <see cref="ApiParserInternalException"/>.
        /// </summary>
        /// <param name="inner"></param>
        public ApiParserInternalException(Exception inner)
            : base("An internal exception occured.", inner)
        {

        }

        /// <inheritdoc/>
        public ApiParserInternalException(string message, Exception inner)
            : base(message, inner)
        {

        }

        /// <inheritdoc/>
        protected ApiParserInternalException(SerializationInfo info, StreamingContext ctxt) : base(info, ctxt)
        {

        }
    }
}
