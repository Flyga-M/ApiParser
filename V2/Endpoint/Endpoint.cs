using ApiParser.V2.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ApiParser.V2.Endpoint
{
    public class Endpoint
    {
        public string Name => ToString(); // TODO: remove

        public EndpointPart[] Parts { get; internal set; } = Array.Empty<EndpointPart>();

        public ParseSettings Settings { get; }

        public bool IsDirectlyAccessible => Parts.Last().IsDirectlyAccessible;

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public Endpoint(IEnumerable<EndpointPart> parts, ParseSettings settings)
        {
            if (parts == null)
            {
                throw new ArgumentNullException(nameof(parts));
            }

            if (!parts.Any())
            {
                throw new ArgumentException($"{nameof(parts)} must at least have one element.", nameof(parts));
            }
            
            Parts = parts.ToArray();
            Settings = settings;
        }

        public static Endpoint FromString(string id, ParseSettings settings)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(id);
            }

            string[] parts = id.Split(settings.EndpointSeparator);

            List <EndpointPart> endpointParts = new List<EndpointPart>();

            foreach(string part in parts)
            {
                if (string.IsNullOrWhiteSpace(part))
                {
                    throw new EndpointParsingException($"Unable to parse {nameof(id)} {id} to {typeof(Endpoint)}. " +
                        $"Parts are not valid.");
                }

                endpointParts.Add(EndpointPart.FromString(part, settings));
            }

            return new Endpoint(endpointParts, settings);
        }

        /// <summary>
        /// Determines whether the <see cref="Endpoint"/> supports the given <paramref name="query"/>.
        /// </summary>
        /// <param name="query"></param>
        /// <returns><see langword="true"/>, if the given <paramref name="query"/> is supported by the <see cref="Endpoint"/>. 
        /// Otherwise <see langword="false"/>.</returns>
        public bool SupportsQuery(EndpointQuery query)
        {
            if (query.QueryParts.Length < Parts.Length)
            {
                return false;
            }

            bool supportsParts = true;

            for (int i = 0; i < Parts.Length; i++)
            {
                supportsParts = Parts[i].SupportsQueryPart(query.QueryParts[i]);
                if (!supportsParts)
                {
                    break;
                }
            }

            return supportsParts;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Join(Settings.EndpointSeparator.ToString(), Parts.Select(part => part.ToString()));
        }
    }
}
