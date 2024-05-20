﻿using ApiParser.V2.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiParser.V2.Endpoint
{
    public class EndpointQueryPart
    {
        public ParseSettings Settings { get; }

        public string EndpointName { get; }

        public bool Enumerate => Indices?.Any() ?? false;

        public EndpointQueryIndex[] Indices { get; }

        public bool ContainsVariable => Indices?.Any(index => index.IsVariable) ?? false;

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public EndpointQueryPart(string endpointName, IEnumerable<EndpointQueryIndex> indices, ParseSettings settings)
        {
            if (endpointName == null)
            {
                throw new ArgumentNullException(nameof(endpointName));
            }

            if (string.IsNullOrWhiteSpace(endpointName))
            {
                throw new ArgumentException($"{nameof(endpointName)} can't be empty or whitespace.", nameof(endpointName));
            }

            if (indices == null)
            {
                indices = Array.Empty<EndpointQueryIndex>();
            }

            if (indices.Any(index => index == null))
            {
                throw new ArgumentException($"{nameof(indices)} can't have elements that are null.", nameof(indices));
            }

            EndpointName = endpointName;
            Indices = indices.ToArray();
            Settings = settings;
        }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="QueryParsingException">When the given <paramref name="id"/> can't be parsed 
        /// correctly.</exception>
        /// <exception cref="SettingsException">When the converted value of any index is not of the type that the 
        /// <paramref name="settings"/> IndexConverter promised.</exception>
        public static EndpointQueryPart FromString(string id, ParseSettings? settings = null)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException($"{nameof(id)} can't be empty or whitespace.", nameof(id));
            }

            if (!settings.HasValue)
            {
                settings = ParseSettings.Default;
            }

            string endpointName = GetEndpointName(id, settings.Value, out string[] indices);

            if (indices == null || !indices.Any())
            {
                return new EndpointQueryPart(endpointName, null, settings.Value);
            }

            EndpointQueryIndex[] queryIndices = GetIndices(indices, settings.Value);

            return new EndpointQueryPart(endpointName, queryIndices, settings.Value);
        }

        /// <exception cref="QueryParsingException"></exception>
        private static string GetEndpointName(string id, ParseSettings settings, out string[] indices)
        {
            indices = null;
            string[] parts = id.Split(new char[] { settings.IndexOpen }, 2);

            string name = parts[0];

            if (parts.Length == 1)
            {
                return name;
            }

            string rest = settings.IndexOpen + parts[1];

            if (!BracketUtil.TryGetInnerContents(rest, settings.IndexOpen, settings.IndexClose, out indices))
            {
                throw new QueryParsingException($"Unable to parse {nameof(id)} {id} to {typeof(EndpointQueryPart)}. " +
                    $"Index brackets are not well formed.");
            }

            return name;
        }

        /// <exception cref="QueryParsingException"></exception>
        /// <exception cref="SettingsException">When the converted value of any index is not of the type that the 
        /// <paramref name="settings"/> IndexConverter promised.</exception>
        private static EndpointQueryIndex[] GetIndices(string[] indices, ParseSettings settings)
        {
            List<EndpointQueryIndex> result = new List<EndpointQueryIndex>();
            
            foreach (string index in indices)
            {
                if (string.IsNullOrWhiteSpace(index))
                {
                    throw new QueryParsingException($"Unable to parse {nameof(index)} {index} to {typeof(EndpointQueryIndex)}. " +
                        $"Index contents are invalid.");
                }

                result.Add(EndpointQueryIndex.FromString(index, settings));
            }

            return result.ToArray();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string result = EndpointName;

            if (Enumerate)
            {
                result += Settings.IndexOpen;

                result += string.Join($"{Settings.IndexClose}{Settings.IndexOpen}", Indices.Select(index => index.ToString()));

                result += Settings.IndexClose;
            }

            return result;
        }
    }
}
