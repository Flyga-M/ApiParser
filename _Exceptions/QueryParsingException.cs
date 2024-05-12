using System;
using System.Runtime.Serialization;

namespace ApiParser
{
    /// <summary>
    /// The <see cref="QueryException"/> that is thrown, when a query could not be parsed correctly.
    /// </summary>
    public class QueryParsingException : QueryException
    {
        /// <inheritdoc/>
        public QueryParsingException()
        {

        }

        /// <inheritdoc/>
        public QueryParsingException(string message)
            : base(message)
        {

        }

        /// <inheritdoc/>
        public QueryParsingException(string message, string queryName)
            : base(message, queryName)
        {

        }

        /// <inheritdoc/>
        public QueryParsingException(string message, Exception inner)
            : base(message, inner)
        {

        }

        /// <inheritdoc/>
        public QueryParsingException(string message, string queryName, Exception inner)
            : base(message, queryName, inner)
        {

        }

        /// <inheritdoc/>
        protected QueryParsingException(SerializationInfo info, StreamingContext ctxt) : base(info, ctxt)
        {

        }
    }
}
