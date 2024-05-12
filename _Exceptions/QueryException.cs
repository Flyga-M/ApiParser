using System;
using System.Runtime.Serialization;

namespace ApiParser
{
    /// <summary>
    /// The <see cref="ApiParserException"/> that is thrown when an <see cref="Exception"/> regarding 
    /// a query occured.
    /// </summary>
    [Serializable]
    public class QueryException : ApiParserException
    {
        /// <summary>
        /// The name of the query that triggered the 
        /// <see cref="QueryException"/>.
        /// </summary>
        public string QueryName { get; }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(QueryName))
                {
                    return base.Message + $" Endpoint: {QueryName}";
                }
                return base.Message;
            }
        }

        /// <inheritdoc/>
        public QueryException()
        {

        }

        /// <inheritdoc/>
        public QueryException(string message)
            : base(message)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryException"/> with the 
        /// given <paramref name="message"/> and <paramref name="queryName"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="queryName"></param>
        public QueryException(string message, string queryName)
            : base(message)
        {
            QueryName = queryName;
            base.Data.Add("QueryName", QueryName);
        }

        /// <inheritdoc/>
        public QueryException(string message, Exception inner)
            : base(message, inner)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryException"/> with the 
        /// given <paramref name="message"/>, <paramref name="queryName"/> and a reference to 
        /// the inner <see cref="Exception"/>, that triggered this <see cref="QueryException"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="queryName"></param>
        /// <param name="inner"></param>
        public QueryException(string message, string queryName, Exception inner)
            : base(message, inner)
        {
            QueryName = queryName;
            base.Data.Add("QueryName", QueryName);
        }

        /// <inheritdoc/>
        protected QueryException(SerializationInfo info, StreamingContext ctxt) : base(info, ctxt)
        {

        }
    }
}
