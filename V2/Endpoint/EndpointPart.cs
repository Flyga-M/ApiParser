using ApiParser.V2.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiParser.V2.Endpoint
{
    public class EndpointPart
    {
        public string EndpointName { get; private set; }

        public ParseSettings Settings { get; }

        public bool IsEnumerable { get; private set; } = false;
        public bool IsDirectlyAccessible { get; private set; } = false;

        public Type[] PossibleIndexTypes { get; private set; } = Array.Empty<Type>();

        public Endpoint[] SubEndpoints { get; private set; } = Array.Empty<Endpoint>();

        public Endpoint[] SubEndpointsForIndex { get; private set; } = Array.Empty<Endpoint>();

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="EndpointException"></exception>
        public EndpointPart(string endpointName, ParseSettings settings, bool isDirectlyAccessible = true, IEnumerable<Type> possibleIndexTypes = null, IEnumerable<Endpoint> subEndpoints = null, IEnumerable<Endpoint> subEndpointsForIndex = null)
        {
            if (string.IsNullOrWhiteSpace(endpointName))
            {
                throw new ArgumentNullException(nameof(endpointName));
            }
            
            EndpointName = endpointName;
            Settings = settings;

            IsEnumerable = possibleIndexTypes != null && possibleIndexTypes.Any();
            IsDirectlyAccessible = isDirectlyAccessible;

            if (possibleIndexTypes != null)
            {
                PossibleIndexTypes = possibleIndexTypes.ToArray();
            }

            if (subEndpoints != null)
            {
                SubEndpoints = subEndpoints.ToArray();
            }

            if (subEndpointsForIndex != null)
            {
                SubEndpointsForIndex = subEndpointsForIndex.ToArray();
            }

            try
            {
                Validate();
            }
            catch (InvalidOperationException ex)
            {
                throw new EndpointException($"{typeof(EndpointPart)} could not be validated.", ex);
            }
        }

        /// <exception cref="InvalidOperationException"></exception>
        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(EndpointName))
            {
                throw new InvalidOperationException($"{nameof(EndpointName)} can't be null, whitespace or empty.");
            }

            if (!IsDirectlyAccessible && !IsEnumerable)
            {
                throw new InvalidOperationException($"{typeof(EndpointPart)} must be either directly accessible or enumerable.");
            }

            if (PossibleIndexTypes.Any(type => !Settings.IndexTypes.Contains(type)))
            {
                throw new InvalidOperationException($"{nameof(Settings)} must include an {typeof(IIndexConverter)} for " +
                    $"every type in {nameof(PossibleIndexTypes)}.");
            }
        }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="EndpointParsingException"></exception>
        public static EndpointPart FromString(string id, ParseSettings settings)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            string endpointName = GetEndpointName(id, settings, out string indices);

            if (indices == null)
            {
                return new EndpointPart(endpointName, settings, true, null, null, null);
            }

            if (string.IsNullOrWhiteSpace(indices))
            {
                throw new EndpointParsingException($"Unable to parse {nameof(id)} {id} to {typeof(EndpointPart)}. " +
                    $"Index contents are invalid.");
            }

            Type[] possibleIndices = GetTypes(indices, settings, out bool isOptional);

            return new EndpointPart(endpointName, settings, isOptional, possibleIndices, null, null);
        }

        /// <exception cref="EndpointParsingException"></exception>
        private static string GetEndpointName(string id, ParseSettings settings, out string indices)
        {
            indices = null;
            string[] parts = id.Split(new char[] { settings.IndexOpen }, 2);

            string name = parts[0];

            if (parts.Length == 1)
            {
                return name;
            }

            string rest = settings.IndexOpen + parts[1];

            if (!BracketUtil.TryGetInnerContent(rest, settings.IndexOpen, settings.IndexClose, out indices))
            {
                throw new EndpointParsingException($"Unable to parse {nameof(id)} {id} to {typeof(EndpointPart)}. " +
                    $"Index brackets are not well formed.");
            }

            return name;
        }

        /// <exception cref="EndpointParsingException"></exception>
        private static Type[] GetTypes(string indices, ParseSettings settings, out bool isOptional)
        {
            List<Type> types = new List<Type>();
            
            string[] parts = indices.Split(settings.IndexSeparator);

            isOptional = false;

            foreach (string index in parts)
            {
                if (string.IsNullOrWhiteSpace(index))
                {
                    throw new EndpointParsingException($"Unable to parse {nameof(indices)} {indices} for {typeof(EndpointPart)}. " +
                        $"Indices are invalid.");
                }

                if (index == settings.IndexOptionalIdentifier)
                {
                    isOptional = true;
                    continue;
                }

                Type type = settings.GetType(index);

                if (type == null)
                {
                    throw new EndpointParsingException($"Unable to parse {nameof(index)} {index} for {typeof(EndpointPart)}. " +
                        $"No such type found in {nameof(settings)}.");
                }

                types.Add(type);
            }

            return types.ToArray();
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

        // TODO: add sub endpoints?
        /// <inheritdoc/>
        public override string ToString()
        {
            string result = EndpointName;
            
            if (IsEnumerable)
            {
                result += Settings.IndexOpen;

                List<string> indexIdentifiers = new List<string>();

                if (IsDirectlyAccessible)
                {
                    indexIdentifiers.Add(Settings.IndexOptionalIdentifier);
                }

                foreach (Type type in PossibleIndexTypes)
                {
                    indexIdentifiers.Add(Settings.GetIdentifier(type));
                }

                result += string.Join(Settings.IndexSeparator.ToString(), indexIdentifiers);

                result += Settings.IndexClose;
            }

            return result;
        }
    }
}
