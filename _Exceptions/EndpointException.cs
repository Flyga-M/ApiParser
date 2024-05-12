using System;
using System.Runtime.Serialization;

namespace ApiParser
{
    /// <summary>
    /// The <see cref="ApiParserException"/> that is thrown when an <see cref="Exception"/> regarding 
    /// an endpoint occured.
    /// </summary>
    [Serializable]
    public class EndpointException : ApiParserException
    {
        /// <summary>
        /// The name of the endpoint that triggered the 
        /// <see cref="EndpointException"/>.
        /// </summary>
        public string EndpointName { get; }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(EndpointName))
                {
                    return base.Message + $" Endpoint: {EndpointName}";
                }
                return base.Message;
            }
        }

        /// <inheritdoc/>
        public EndpointException()
        {

        }

        /// <inheritdoc/>
        public EndpointException(string message)
            : base(message)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EndpointException"/> with the 
        /// given <paramref name="message"/> and <paramref name="endpointName"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="endpointName"></param>
        public EndpointException(string message, string endpointName)
            : base(message)
        {
            EndpointName = endpointName;
            base.Data.Add("EndpointName", EndpointName);
        }

        /// <inheritdoc/>
        public EndpointException(string message, Exception inner)
            : base(message, inner)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EndpointException"/> with the 
        /// given <paramref name="message"/>, <paramref name="endpointName"/> and a reference to 
        /// the inner <see cref="Exception"/>, that triggered this <see cref="EndpointException"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="endpointName"></param>
        /// <param name="inner"></param>
        public EndpointException(string message, string endpointName, Exception inner)
            : base(message, inner)
        {
            EndpointName = endpointName;
            base.Data.Add("EndpointName", EndpointName);
        }

        /// <inheritdoc/>
        protected EndpointException(SerializationInfo info, StreamingContext ctxt) : base(info, ctxt)
        {

        }
    }
}
