using ApiParser.V2.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiParser.V2.Endpoint
{
    /// <summary>
    /// A gw2 api query, that can be resolved via the <see cref="ApiManager"/>.
    /// </summary>
    public class EndpointQuery
    {
        /// <summary>
        /// Determines how the <see cref="EndpointQuery"/> is parsed either to or from a string.
        /// </summary>
        public ParseSettings Settings { get; }

        /// <summary>
        /// The <see cref="EndpointQueryPart"/>s that the <see cref="EndpointQuery"/> is made of.
        /// </summary>
        public EndpointQueryPart[] QueryParts { get; }

        /// <summary>
        /// Determines whether any of the <see cref="QueryParts"/> contain a variable.
        /// </summary>
        public bool ContainsVariable => QueryParts?.Any(part => part.ContainsVariable) ?? false;

        /// <exception cref="ArgumentNullException">If <paramref name="queryParts"/> is null.</exception>
        /// <exception cref="ArgumentException">If <paramref name="queryParts"/> does not have any elements, or 
        /// if any element in <paramref name="queryParts"/> is null.</exception>
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

        /// <summary>
        /// Parses an <see cref="EndpointQuery"/> from the given <paramref name="query"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="ArgumentException">If <paramref name="query"/> is empty or whitespace.</exception>
        /// <exception cref="QueryParsingException">If the given <paramref name="query"/> can't be parsed 
        /// correctly.</exception>
        /// <exception cref="SettingsException">If the converted value of any index of any part is not of the type that the 
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

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Join(Settings.EndpointSeparator.ToString(), QueryParts.Select(part => part.ToString()));
        }
    }
}
