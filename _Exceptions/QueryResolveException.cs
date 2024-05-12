using System;
using System.Runtime.Serialization;

namespace ApiParser
{
    /// <summary>
    /// The <see cref="QueryException"/> that is thrown, when a query could not be resolved.
    /// </summary>
    public class QueryResolveException : QueryException
    {
        /// <inheritdoc/>
        public QueryResolveException()
        {

        }

        /// <inheritdoc/>
        public QueryResolveException(string message)
            : base(message)
        {

        }

        /// <inheritdoc/>
        public QueryResolveException(string message, string queryName)
            : base(message, queryName)
        {

        }

        /// <inheritdoc/>
        public QueryResolveException(string message, Exception inner)
            : base(message, inner)
        {

        }

        /// <inheritdoc/>
        public QueryResolveException(string message, string queryName, Exception inner)
            : base(message, queryName, inner)
        {

        }

        /// <inheritdoc/>
        protected QueryResolveException(SerializationInfo info, StreamingContext ctxt) : base(info, ctxt)
        {

        }
    }
}
