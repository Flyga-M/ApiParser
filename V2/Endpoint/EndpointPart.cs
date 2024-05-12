using ApiParser.V2.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiParser.V2.Endpoint
{
    public struct EndpointPart
    {
        public string Id;

        public string EndpointName;

        public bool IsEnumerable;
        public bool IsDirectlyAccessible;

        public Type[] PossibleIndexTypes;

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="EndpointParsingException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        public EndpointPart(string id, ParseSettings settings)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }

            Id = id;
            EndpointName = string.Empty;
            IsEnumerable = false;
            IsDirectlyAccessible = false;
            PossibleIndexTypes = Array.Empty<Type>();

            try
            {
                ResolveId(settings);
            }
            catch (InvalidOperationException ex)
            {
                throw new EndpointParsingException($"EndpointPart id {id} could not be parsed.", nameof(id), ex);
            }
            catch (ApiParserInternalException ex)
            {
                throw new ApiParserInternalException($"EndpointPart id {id} could not be parsed.", ex);
            }

            IsEnumerable = PossibleIndexTypes.Length > 0;
        }

        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        private void ResolveId(ParseSettings settings)
        {   
            if (!Id.Contains(settings.IndexOpen) && !Id.Contains(settings.IndexClose))
            {
                EndpointName = Id;
                IsDirectlyAccessible = true;
                return;
            }

            if (!Id.EndsWith(settings.IndexClose))
            {
                throw new InvalidOperationException($"EndpointPart id {Id} could not be resolved. The part contains " +
                    $"{settings.IndexOpen} but not {settings.IndexClose}.");
            }

            string[] parts = Id.Split(settings.IndexOpen);

            if (parts.Length != 2)
            {
                throw new InvalidOperationException($"EndpointPart id {Id} could not be resolved. The part may only " +
                    $"contain one index.");
            }

            string name = parts[0];
            string indices = parts[1];

            if (!indices.EndsWith(settings.IndexClose))
            {
                throw new InvalidOperationException($"EndpointPart id {Id} could not be resolved. An index that is opened " +
                    $"with {settings.IndexOpen} must be closed with {settings.IndexClose}.");
            }

            indices = indices.Remove(indices.Length - 1);

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException($"EndpointPart id {Id} could not be resolved. The name can't be null, " +
                    $"empty or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(indices))
            {
                throw new InvalidOperationException($"EndpointPart id {Id} could not be resolved. The index can't be null, " +
                    $"empty or whitespace.");
            }

            EndpointName = name;

            string[] indicesParts = indices.Split(settings.IndexSeparator);
            List<Type> types = new List<Type>();

            try
            {
                foreach(string index in indicesParts)
                {
                    if (string.IsNullOrWhiteSpace(index))
                    {
                        throw new InvalidOperationException($"Index part {index} can't be null, empty or whitespace.");
                    }
                    
                    ResolveIndex(index, settings, ref types);
                }
            }
            catch (ArgumentNullException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException($"EndpointPart id {Id} could not be resolved. An index could not " +
                    $"be resolved.", ex);
            }

            PossibleIndexTypes = types.ToArray();
        }

        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        private void ResolveIndex(string identifier, ParseSettings settings, ref List<Type> indices)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            if (indices == null)
            {
                throw new ArgumentNullException(nameof(indices));
            }

            if (identifier == settings.IndexOptionalIdentifier)
            {
                IsDirectlyAccessible = true;
                return;
            }

            Type type = settings.GetType(identifier);

            if (type == null)
            {
                throw new ArgumentException($"Index {identifier} could not be resolved.", nameof(identifier));
            }

            indices.Add(type);
        }

        public bool SupportsQueryPart(EndpointQueryPart queryPart)
        {
            if (queryPart.EndpointName != EndpointName)
            {
                return false;
            }

            if (queryPart.Enumerate)
            {
                if (!IsEnumerable)
                {
                    return false;
                }

                if (!PossibleIndexTypes.Contains(queryPart.Indices.First().IndexType))
                {
                    return false;
                }
            }
            else
            {
                return IsDirectlyAccessible;
            }

            return true;
        }
    }
}
