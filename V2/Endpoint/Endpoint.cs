using ApiParser.V2.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiParser.V2.Endpoint
{
    public struct Endpoint
    {
        public string Name;

        public EndpointPart[] Parts;

        public bool IsDirectlyAccessible => Parts.Last().IsDirectlyAccessible;

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="EndpointParsingException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        public Endpoint(string name, ParseSettings settings)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            Name = name;
            Parts = Array.Empty<EndpointPart>();

            try
            {
                ResolveName(settings);
            }
            catch (InvalidOperationException ex)
            {
                throw new EndpointParsingException($"Endpoint {name} could not be parsed.", nameof(name), ex);
            }
            catch (ApiParserInternalException ex)
            {
                throw new ApiParserInternalException($"Endpoint {name} could not be parsed.", ex);
            }
            catch (EndpointParsingException ex)
            {
                throw new EndpointParsingException($"Endpoint {name} could not be parsed.", nameof(name), ex);
            }
        }

        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="EndpointParsingException"></exception>
        private void ResolveName(ParseSettings settings)
        {
            List<EndpointPart> endpointParts = new List<EndpointPart>();

            string[] parts = Name.Split(settings.EndpointSeparator);

            foreach (string part in parts)
            {
                EndpointPart endpointPart;

                if (string.IsNullOrWhiteSpace(part))
                {
                    throw new InvalidOperationException($"Part {part} can't be null, empty or whitespace.");
                }

                try
                {
                    endpointPart = new EndpointPart(part, settings);
                }
                catch (ArgumentNullException ex)
                {
                    throw new ApiParserInternalException(ex);
                }
                catch (EndpointParsingException ex)
                {
                    throw new EndpointParsingException($"The " +
                    $"part {part} could not be parsed.", ex);
                }
                catch (ApiParserInternalException ex)
                {
                    throw new ApiParserInternalException($"The part {part} could not be parsed.", ex);
                }

                endpointParts.Add(endpointPart);
            }

            Parts = endpointParts.ToArray();
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
    }
}
