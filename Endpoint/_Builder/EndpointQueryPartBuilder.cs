using ApiParser.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiParser.Endpoint
{
    /// <summary>
    /// Builds <see cref="EndpointQueryPart"/>.
    /// </summary>
    public class EndpointQueryPartBuilder
    {
        private string _endpoint;

        private List<EndpointQueryIndex> _indices = new List<EndpointQueryIndex>();

        private ParseSettings _settings;

        /// <inheritdoc/>
        public EndpointQueryPartBuilder(ParseSettings? settings = null)
        {
            if (!settings.HasValue)
            {
                settings = ParseSettings.Default;
            }
            
            _settings = settings.Value;
        }

        /// <summary>
        /// Applies the <paramref name="settings"/> to the resulting <see cref="EndpointQueryPart"/>.
        /// </summary>
        /// <remarks>
        /// If you use custom settings, make sure to apply them before adding any indices with <see cref="AddIndex(object)"/> 
        /// or <see cref="AddIndex(Type, string)"/>, because otherwise the default settings will be applied to the indices.
        /// </remarks>
        /// <param name="settings"></param>
        /// <returns>Itself.</returns>
        public EndpointQueryPartBuilder WithSettings(ParseSettings settings)
        {
            _settings = settings;
            return this;
        }

        /// <summary>
        /// Applies the <paramref name="endpointName"/> to the resulting <see cref="EndpointQueryPart"/>.
        /// </summary>
        /// <param name="endpointName"></param>
        /// <returns>Itself.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="endpointName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If the <paramref name="endpointName"/> is empty or whitespace.</exception>
        public EndpointQueryPartBuilder WithEndpointName(string endpointName)
        {
            if (endpointName == null)
            {
                throw new ArgumentNullException(nameof(endpointName));
            }

            if (string.IsNullOrWhiteSpace(endpointName))
            {
                throw new ArgumentException($"{nameof(endpointName)} can't be empty or whitespace.", nameof(endpointName));
            }

            _endpoint = endpointName;
            return this;
        }

        /// <summary>
        /// Adds the <paramref name="value"/> as an <see cref="EndpointQueryIndex"/> to the resulting <see cref="EndpointQueryPart"/>.
        /// </summary>
        /// <remarks>
        /// If you use custom settings, make sure to apply them via <see cref="WithSettings(ParseSettings)"/> before adding 
        /// any indices, because otherwise the default settings will be applied to the indices.
        /// </remarks>
        /// <param name="value"></param>
        /// <returns>Itself.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If the <see cref="Type"/> of <paramref name="value"/> can't be 
        /// converted by the previously applied <see cref="ParseSettings"/>.</exception>
        public EndpointQueryPartBuilder AddIndex(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Type indexType = value.GetType();

            if (!_settings.IndexTypes.Contains(indexType))
            {
                throw new ArgumentException($"{nameof(indexType)} must be convertable by the {nameof(_settings)}.", nameof(indexType));
            }

            _indices.Add(new EndpointQueryIndex(value, _settings));
            return this;
        }

        /// <summary>
        /// Adds the <paramref name="variable"/> as an <see cref="EndpointQueryIndex"/> to the 
        /// resulting <see cref="EndpointQueryPart"/>.
        /// </summary>
        /// <remarks>
        /// If you use custom settings, make sure to apply them via <see cref="WithSettings(ParseSettings)"/> before adding 
        /// any indices, because otherwise the default settings will be applied to the indices.
        /// </remarks>
        /// <param name="indexType"></param>
        /// <param name="variable"></param>
        /// <returns>Itself.</returns>
        /// <exception cref="ArgumentNullException">If either <paramref name="indexType"/> or <paramref name="variable"/> 
        /// is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="variable"/> is empty or whitespace, or if 
        /// the <paramref name="indexType"/> can't be converted by the previously applied <see cref="ParseSettings"/>.</exception>
        public EndpointQueryPartBuilder AddIndex(Type indexType, string variable)
        {
            if (indexType == null)
            {
                throw new ArgumentNullException(nameof(indexType));
            }

            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            if (string.IsNullOrWhiteSpace(variable))
            {
                throw new ArgumentException($"{nameof(variable)} can't be empty or whitespace.", nameof(variable));
            }

            if (!_settings.IndexTypes.Contains(indexType))
            {
                throw new ArgumentException($"{nameof(indexType)} must be convertable by the {nameof(_settings)}.", nameof(indexType));
            }

            _indices.Add(new EndpointQueryIndex(indexType, variable, _settings));
            return this;
        }

        /// <summary>
        /// Builds the <see cref="EndpointQueryPart"/>.
        /// </summary>
        /// <returns>The built <see cref="EndpointQueryPart"/>.</returns>
        /// <exception cref="InvalidOperationException">If no endpoint name was set.</exception>
        public EndpointQueryPart Build()
        {
            if (string.IsNullOrWhiteSpace(_endpoint))
            {
                throw new InvalidOperationException($"An {typeof(EndpointQueryPart)} must have an endpoint name.");
            }

            return new EndpointQueryPart(_endpoint, _indices, _settings);
        }
    }
}
