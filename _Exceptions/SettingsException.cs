using System;
using System.Runtime.Serialization;

namespace ApiParser
{
    /// <summary>
    /// The <see cref="ApiParserException"/> that is thrown, when a settings related exception occurs.
    /// </summary>
    public class SettingsException : ApiParserException
    {
        /// <inheritdoc/>
        public SettingsException()
        {

        }

        /// <inheritdoc/>
        public SettingsException(string message)
            : base(message)
        {

        }

        /// <inheritdoc/>
        public SettingsException(string message, Exception inner)
            : base(message, inner)
        {

        }

        /// <inheritdoc/>
        protected SettingsException(SerializationInfo info, StreamingContext ctxt) : base(info, ctxt)
        {

        }
    }
}
