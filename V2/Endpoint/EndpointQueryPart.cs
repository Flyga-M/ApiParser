using ApiParser.V2.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiParser.V2.Endpoint
{
    public struct EndpointQueryPart
    {
        public string Id;

        public string EndpointName;

        public bool Enumerate;

        public EndpointQueryIndex[] Indices;

        public bool ContainsVariable => Indices?.Any(index => index.IsVariable) ?? false;

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="QueryParsingException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="QueryNotSupportedException"></exception>
        /// <exception cref="SettingsException"></exception>
        public EndpointQueryPart(string id, ParseSettings settings)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            Id = id;

            EndpointName = string.Empty;
            Enumerate = false;
            Indices = Array.Empty<EndpointQueryIndex>();

            try
            {
                ResolveId(settings);
            }
            catch (InvalidOperationException ex)
            {
                throw new QueryParsingException($"EndpointQueryPart id {Id} could not be parsed.", ex);
            }
            catch (ApiParserInternalException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (QueryParsingException ex)
            {
                throw new QueryParsingException($"EndpointQueryPart id {Id} could not be parsed.", ex);
            }
            catch (QueryNotSupportedException ex)
            {
                throw new QueryNotSupportedException($"EndpointQueryPart id {id} is not supported.", ex);
            }
            catch (SettingsException ex)
            {
                throw new SettingsException($"EndpointQueryPart id {id} could not be parsed.", ex);
            }

            Enumerate = Indices.Length > 0;
        }

        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="QueryParsingException"></exception>
        /// <exception cref="QueryNotSupportedException"></exception>
        /// <exception cref="SettingsException"></exception>
        private void ResolveId(ParseSettings settings)
        {
            if (!Id.Contains(settings.IndexOpen) && !Id.Contains(settings.IndexClose))
            {
                EndpointName = Id;
                return;
            }

            if (!Id.EndsWith(settings.IndexClose))
            {
                throw new InvalidOperationException($"The part contains " +
                    $"{settings.IndexOpen} but not {settings.IndexClose}.");
            }

            string[] parts = Id.Split(settings.IndexOpen);

            string endpointName = parts[0];

            if (string.IsNullOrWhiteSpace(endpointName))
            {
                throw new InvalidOperationException($"The endpointName can't be null, " +
                    $"empty or whitespace.");
            }

            EndpointName = endpointName;

            List<EndpointQueryIndex> indices = new List<EndpointQueryIndex>();

            foreach (string part in parts.Skip(1))
            {
                if (!part.EndsWith(settings.IndexClose))
                {
                    throw new InvalidOperationException($"An index that is opened " +
                        $"with {settings.IndexOpen} must be closed with {settings.IndexClose}.");
                }

                string index = part.Remove(part.Length - 1);

                try
                {
                    ResolveIndex(index, settings, ref indices);
                }
                catch (ArgumentNullException ex)
                {
                    throw new InvalidOperationException($"The provided index value \"{index}\" can't be null or empty.", ex);
                }
                catch (ApiParserInternalException ex)
                {
                    throw new ApiParserInternalException(ex);
                }
                catch (QueryParsingException ex)
                {
                    throw new QueryParsingException($"The provided index value \"{index}\" could not be parsed correctly.", ex);
                }
                catch (QueryNotSupportedException ex)
                {
                    throw new QueryNotSupportedException($"Index {index} is not supported.", ex);
                }
                catch (SettingsException ex)
                {
                    throw new SettingsException($"Index {index} could not be parsed.", ex);
                }
            }

            Indices = indices.ToArray();
        }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="QueryParsingException"></exception>
        /// <exception cref="QueryNotSupportedException"></exception>
        /// <exception cref="SettingsException"></exception>
        private void ResolveIndex(string index, ParseSettings settings, ref List<EndpointQueryIndex> indices)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                throw new ArgumentNullException($"Value \"{index}\" can't be null or empty.", nameof(index));
            }

            EndpointQueryIndex Index;

            try
            {
                Index = new EndpointQueryIndex(index, settings);
            }
            catch (ArgumentNullException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (QueryParsingException ex)
            {
                throw new QueryParsingException($"Index of query part {Id} could not be parsed.", ex);
            }
            catch (ApiParserInternalException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (QueryNotSupportedException ex)
            {
                throw new QueryNotSupportedException($"Index is not supported.", ex);
            }
            catch (SettingsException ex)
            {
                throw new SettingsException($"Index could not be parsed.", ex);
            }

            indices.Add(Index);
        }
    }
}
