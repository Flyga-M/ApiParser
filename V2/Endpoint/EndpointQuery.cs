using ApiParser.V2.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ApiParser.V2.Endpoint
{
    public class EndpointQuery
    {
        public ParseSettings Settings { get; }
        
        public string Query => ToString(); // TODO: remove

        public EndpointQueryPart[] QueryParts { get; }

        public bool ContainsVariable => QueryParts?.Any(part => part.ContainsVariable) ?? false;

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public EndpointQuery(IEnumerable<EndpointQueryPart> queryParts, ParseSettings? settings = null)
        {
            if (queryParts == null)
            {
                throw new ArgumentNullException(nameof(queryParts));
            }

            if (!queryParts.Any())
            {
                throw new ArgumentException($"{nameof(queryParts)} must have at least one element.", nameof(queryParts));
            }

            if (queryParts.Any(part => part == null))
            {
                throw new ArgumentException($"{nameof(queryParts)} can't have elements that are null.", nameof(queryParts));
            }

            QueryParts = queryParts.ToArray();
            Settings = settings ?? ParseSettings.Default;
        }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="QueryParsingException">When the given <paramref name="query"/> can't be parsed 
        /// correctly.</exception>
        /// <exception cref="SettingsException">When the converted value of any index of any part is not of the type that the 
        /// <paramref name="settings"/> IndexConverter promised.</exception>
        public static EndpointQuery FromString(string query, ParseSettings? settings = null)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException($"{nameof(query)} can't be empty or whitespace.", nameof(query));
            }

            if (!settings.HasValue)
            {
                settings = ParseSettings.Default;
            }

            string[] parts = query.Split(settings.Value.EndpointSeparator);

            List<EndpointQueryPart> queryParts = new List<EndpointQueryPart>();

            foreach (string part in parts)
            {
                if (string.IsNullOrWhiteSpace(part))
                {
                    throw new QueryParsingException($"Unable to parse {nameof(query)} {query} to {typeof(EndpointQuery)}. " +
                        $"Parts are not valid.");
                }

                queryParts.Add(EndpointQueryPart.FromString(part, settings.Value));
            }

            return new EndpointQuery(queryParts, settings);
        }

        /// <exception cref="QueryNotSupportedException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        internal EndpointQuery GetSubQuery(Endpoint endpoint)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (!endpoint.SupportsQuery(this))
            {
                throw new QueryNotSupportedException($"The given endpoint {endpoint.Name} does not support this query " +
                    $"{Query}.");
            }

            EndpointQueryPart[] subQueryParts = QueryParts.Skip(endpoint.Parts.Length - 1).ToArray();

            return new EndpointQuery(subQueryParts, Settings);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Join(Settings.EndpointSeparator.ToString(), QueryParts.Select(part => part.ToString()));
        }
    }
}
