using ApiParser.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiParser.Endpoint
{
    /// <summary>
    /// Builds <see cref="EndpointQuery"/>.
    /// </summary>
    public class EndpointQueryBuilder
    {
        private List<EndpointQueryPart> _parts = new List<EndpointQueryPart>();

        private ParseSettings _settings;

        /// <inheritdoc/>
        public EndpointQueryBuilder(ParseSettings? settings = null)
        {
            if (!settings.HasValue)
            {
                settings = ParseSettings.Default;
            }

            _settings = settings.Value;
        }

        /// <summary>
        /// Applies the <paramref name="settings"/> to the resulting <see cref="EndpointQuery"/>.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns>Itself.</returns>
        public EndpointQueryBuilder WithSettings(ParseSettings settings)
        {
            _settings = settings;
            return this;
        }

        /// <summary>
        /// Adds the <paramref name="part"/> to the resulting <see cref="EndpointQuery"/>.
        /// </summary>
        /// <param name="part"></param>
        /// <returns>Itself.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="part"/> is <see langword="null"/>.</exception>
        public EndpointQueryBuilder AddPart(EndpointQueryPart part)
        {
            if (part == null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            _parts.Add(part);
            return this;
        }

        /// <summary>
        /// Builds the <see cref="EndpointQuery"/>.
        /// </summary>
        /// <returns>The built <see cref="EndpointQuery"/>.</returns>
        /// <exception cref="InvalidOperationException">If no <see cref="EndpointQueryPart"/>s were added.</exception>
        public EndpointQuery Build()
        {
            if (!_parts.Any())
            {
                throw new InvalidOperationException($"An {typeof(EndpointQuery)} must have at least one part.");
            }

            return new EndpointQuery(_parts, _settings);
        }
    }
}
