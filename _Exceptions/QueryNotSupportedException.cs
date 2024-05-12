using System;
using System.Runtime.Serialization;

namespace ApiParser
{
    /// <summary>
    /// The <see cref="QueryException"/> that is thrown, when a query could be parsed, but is not supported by a certain endpoint 
    /// or this library.
    /// </summary>
    public class QueryNotSupportedException : QueryException
    {
        /// <inheritdoc/>
        public QueryNotSupportedException()
        {

        }

        /// <inheritdoc/>
        public QueryNotSupportedException(string message)
            : base(message)
        {

        }

        /// <inheritdoc/>
        public QueryNotSupportedException(string message, string queryName)
            : base(message, queryName)
        {

        }

        /// <inheritdoc/>
        public QueryNotSupportedException(string message, Exception inner)
            : base(message, inner)
        {

        }

        /// <inheritdoc/>
        public QueryNotSupportedException(string message, string queryName, Exception inner)
            : base(message, queryName, inner)
        {

        }

        /// <inheritdoc/>
        protected QueryNotSupportedException(SerializationInfo info, StreamingContext ctxt) : base(info, ctxt)
        {

        }
    }
}
