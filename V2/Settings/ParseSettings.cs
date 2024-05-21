using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiParser.V2.Settings
{
    /// <summary>
    /// Determines how a query is parsed.
    /// </summary>
    public struct ParseSettings
    {
        /// <summary>
        /// The default <see cref="ParseSettings"/>.
        /// </summary>
        public static readonly ParseSettings Default = new ParseSettings(null, null, null, null, null, null, null);
        
        /// <summary>
        /// The <see cref="IIndexConverter">IINdexConverters</see>.
        /// </summary>
        public IIndexConverter[] IndexConverters;

        /// <summary>
        /// The <see cref="Type">Types</see> that the <see cref="IndexConverters"/> can convert to.
        /// </summary>
        public Type[] IndexTypes;

        /// <summary>
        /// The type identifiers, that are used to convert indices into their respective <see cref="Type"/>.
        /// </summary>
        public string[] IndexTypeIdentifiers;

        /// <summary>
        /// Indicates the start of a variable.
        /// </summary>
        /// <remarks>
        /// May not contain the <see cref="IndexSeparator"/>, <see cref="IndexOpen"/>, <see cref="IndexClose"/> or 
        /// <see cref="EndpointSeparator"/>.
        /// </remarks>
        public string IndexVariableIdentifier;

        /// <summary>
        /// Indicates that an endpoint can be enumerated, but is also directly accessible. Needs to be used with at least 
        /// one other index identifier.
        /// </summary>
        /// <remarks>
        /// May not contain the <see cref="IndexSeparator"/>, <see cref="IndexOpen"/>, <see cref="IndexClose"/> or 
        /// <see cref="EndpointSeparator"/>. May not be the same as any <see cref="IIndexConverter.IndexIdentifier"/>.
        /// </remarks>
        public string IndexOptionalIdentifier;

        /// <summary>
        /// Indicates the end of an index.
        /// </summary>
        /// <remarks>
        /// May not be the same as <see cref="IndexOpen"/>, <see cref="IndexSeparator"/> or <see cref="EndpointSeparator"/>.
        /// </remarks>
        public char IndexClose;

        /// <summary>
        /// Indicates the start of an index.
        /// </summary>
        /// <remarks>
        /// May not be the same as <see cref="IndexSeparator"/> or <see cref="EndpointSeparator"/>.
        /// </remarks>
        public char IndexOpen;

        /// <summary>
        /// Separates the index type from the index value in a query, or the different types of indices in an endpoint.
        /// </summary>
        /// <remarks>
        /// May not be the same as <see cref="EndpointSeparator"/>.
        /// </remarks>
        public char IndexSeparator;

        /// <summary>
        /// Separates endpoint parts from each other.
        /// </summary>
        public char EndpointSeparator;

        /// <summary>
        /// Initializes a new <see cref="ParseSettings"/> instance with the given parameters. Will use default values 
        /// for parameters that are null.
        /// </summary>
        /// <param name="indexConverters"></param>
        /// <param name="indexVariableIdentifier"></param>
        /// <param name="indexOptionalIdentifier"></param>
        /// <param name="indexClose"></param>
        /// <param name="indexOpen"></param>
        /// <param name="indexSeparator"></param>
        /// <param name="endpointSeparator"></param>
        /// <exception cref="SettingsException">If any of the parameters are in conflict with each other.</exception>
        public ParseSettings(IIndexConverter[] indexConverters = null, string indexVariableIdentifier = null,
            string indexOptionalIdentifier = null, char? indexClose = null, char? indexOpen = null, char? indexSeparator = null,
            char? endpointSeparator = null)
        {   
            if (indexConverters == null)
            {
                indexConverters = new IIndexConverter[] { new Default.GuidIndexConverter (), new Default.IntIndexConverter(), new Default.StringIndexConverter() };
            }

            if (indexVariableIdentifier == null)
            {
                indexVariableIdentifier = Settings.Default.Constants.INDEX_VAR_IDENTIFIER;
            }

            if (indexOptionalIdentifier == null)
            {
                indexOptionalIdentifier = Settings.Default.Constants.INDEX_OPTIONAL_IDENTIFIER;
            }

            if (!indexClose.HasValue)
            {
                indexClose = Settings.Default.Constants.INDEX_CLOSE;
            }

            if (!indexOpen.HasValue)
            {
                indexOpen = Settings.Default.Constants.INDEX_OPEN;
            }

            if (!indexSeparator.HasValue)
            {
                indexSeparator = Settings.Default.Constants.INDEX_SEPARATOR;
            }

            if (!endpointSeparator.HasValue)
            {
                endpointSeparator = Settings.Default.Constants.ENDPOINT_SEPARATOR;
            }

            IndexConverters = indexConverters;
            IndexVariableIdentifier = indexVariableIdentifier;
            IndexOptionalIdentifier = indexOptionalIdentifier;
            IndexClose = indexClose.Value;
            IndexOpen = indexOpen.Value;
            IndexSeparator = indexSeparator.Value;
            EndpointSeparator = endpointSeparator.Value;

            IndexTypes = IndexConverters.Select(converter => converter.Type).ToArray();
            IndexTypeIdentifiers = IndexConverters.SelectMany(converter => converter.IndexIdentifiers).ToArray();

            try
            {
                ValidateSettings();
            }
            catch (SettingsException ex)
            {
                throw new SettingsException($"Settings are not valid.", ex);
            }
        }

        /// <exception cref="SettingsException"></exception>
        private void ValidateSettings()
        {
            try
            {
                ValidateIndexConverters();
                ValidateIndexVariableIdentifier();
                ValidateIndexOptionalIdentifier();
                ValidateIndexClose();
                ValidateIndexOpen();
                ValidateIndexSeperator();
            }
            catch (InvalidOperationException ex)
            {
                throw new SettingsException($"An exception while attempting to validate {typeof(ParseSettings)} occured.", ex);
            }
        }

        /// <exception cref="InvalidOperationException"></exception>
        private void ValidateIndexConverters()
        {
            if (IndexConverters.Any(converter => converter.IndexIdentifiers == null || !converter.IndexIdentifiers.Any()))
            {
                throw new InvalidOperationException("Every index converter must have at least on index identifier.");
            }
            
            foreach (IEnumerable<string> indexTypeIdentifiers in IndexConverters.Select(converter => converter.IndexIdentifiers))
            {
                foreach (string identifier in indexTypeIdentifiers)
                {
                    ValidateIndexTypeIdentifier(identifier);
                }
            }

            if (IndexTypes.Length != IndexTypes.Distinct().Count())
            {
                throw new InvalidOperationException("For every type there may only be one IndexConverter.");
            }

            if (IndexTypeIdentifiers.Length != IndexTypeIdentifiers.Distinct().Count())
            {
                throw new InvalidOperationException("Every type identifier may only be used once.");
            }
        }

        /// <exception cref="InvalidOperationException"></exception>
        private void ValidateIndexTypeIdentifier(string indexTypeIdentifier)
        {
            if (indexTypeIdentifier == null)
            {
                throw new InvalidOperationException($"{nameof(indexTypeIdentifier)} can't be null.");
            }
            
            if (indexTypeIdentifier.Contains(IndexSeparator))
            {
                throw new InvalidOperationException($"{nameof(indexTypeIdentifier)} \"{indexTypeIdentifier}\" may " +
                    $"not contain {nameof(IndexSeparator)} \"{IndexSeparator}\".");
            }

            if (indexTypeIdentifier.Contains(IndexOpen))
            {
                throw new InvalidOperationException($"{nameof(indexTypeIdentifier)} \"{indexTypeIdentifier}\" may " +
                    $"not contain {nameof(IndexOpen)} \"{IndexOpen}\".");
            }

            if (indexTypeIdentifier.Contains(IndexClose))
            {
                throw new InvalidOperationException($"{nameof(indexTypeIdentifier)} \"{indexTypeIdentifier}\" may " +
                    $"not contain {nameof(IndexClose)} \"{IndexClose}\".");
            }

            if (indexTypeIdentifier.Contains(EndpointSeparator))
            {
                throw new InvalidOperationException($"{nameof(indexTypeIdentifier)} \"{indexTypeIdentifier}\" may " +
                    $"not contain {nameof(EndpointSeparator)} \"{EndpointSeparator}\".");
            }
        }

        /// <exception cref="InvalidOperationException"></exception>
        private void ValidateIndexVariableIdentifier()
        {
            if (IndexVariableIdentifier == null)
            {
                throw new InvalidOperationException($"{nameof(IndexVariableIdentifier)} \"{IndexVariableIdentifier}\" may " +
                    $"not be null.");
            }
            
            if (IndexVariableIdentifier.Contains(IndexSeparator))
            {
                throw new InvalidOperationException($"{nameof(IndexVariableIdentifier)} \"{IndexVariableIdentifier}\" may " +
                    $"not contain {nameof(IndexSeparator)} \"{IndexSeparator}\".");
            }

            if (IndexVariableIdentifier.Contains(IndexOpen))
            {
                throw new InvalidOperationException($"{nameof(IndexVariableIdentifier)} \"{IndexVariableIdentifier}\" may " +
                    $"not contain {nameof(IndexOpen)} \"{IndexOpen}\".");
            }

            if (IndexVariableIdentifier.Contains(IndexClose))
            {
                throw new InvalidOperationException($"{nameof(IndexVariableIdentifier)} \"{IndexVariableIdentifier}\" may " +
                    $"not contain {nameof(IndexClose)} \"{IndexClose}\".");
            }

            if (IndexVariableIdentifier.Contains(EndpointSeparator))
            {
                throw new InvalidOperationException($"{nameof(IndexVariableIdentifier)} \"{IndexVariableIdentifier}\" may " +
                    $"not contain {nameof(EndpointSeparator)} \"{EndpointSeparator}\".");
            }
        }

        /// <exception cref="InvalidOperationException"></exception>
        private void ValidateIndexOptionalIdentifier()
        {
            if (IndexOptionalIdentifier == null)
            {
                throw new InvalidOperationException($"{nameof(IndexOptionalIdentifier)} \"{IndexOptionalIdentifier}\" may " +
                    $"not be null.");
            }

            string optionalIdentifier = IndexOptionalIdentifier;

            if (IndexTypeIdentifiers.Any(identifier => identifier == optionalIdentifier))
            {
                throw new InvalidOperationException($"{nameof(IndexOptionalIdentifier)} \"{IndexOptionalIdentifier}\" may " +
                    $"not be the same as any index type identifier \"{optionalIdentifier}\".");
            }
            
            if (IndexOptionalIdentifier.Contains(IndexSeparator))
            {
                throw new InvalidOperationException($"{nameof(IndexOptionalIdentifier)} \"{IndexOptionalIdentifier}\" may " +
                    $"not contain {nameof(IndexSeparator)} \"{IndexSeparator}\".");
            }

            if (IndexOptionalIdentifier.Contains(IndexOpen))
            {
                throw new InvalidOperationException($"{nameof(IndexOptionalIdentifier)} \"{IndexOptionalIdentifier}\" may " +
                    $"not contain {nameof(IndexOpen)} \"{IndexOpen}\".");
            }

            if (IndexOptionalIdentifier.Contains(IndexClose))
            {
                throw new InvalidOperationException($"{nameof(IndexOptionalIdentifier)} \"{IndexOptionalIdentifier}\" may " +
                    $"not contain {nameof(IndexClose)} \"{IndexClose}\".");
            }

            if (IndexOptionalIdentifier.Contains(EndpointSeparator))
            {
                throw new InvalidOperationException($"{nameof(IndexOptionalIdentifier)} \"{IndexOptionalIdentifier}\" may " +
                    $"not contain {nameof(EndpointSeparator)} \"{EndpointSeparator}\".");
            }
        }

        /// <exception cref="InvalidOperationException"></exception>
        private void ValidateIndexClose()
        {
            if (IndexClose == IndexOpen)
            {
                throw new InvalidOperationException($"{nameof(IndexClose)} \"{IndexClose}\" may " +
                    $"not be the same as {nameof(IndexOpen)} \"{IndexOpen}\".");
            }

            if (IndexClose == IndexSeparator)
            {
                throw new InvalidOperationException($"{nameof(IndexClose)} \"{IndexClose}\" may " +
                    $"not be the same as {nameof(IndexSeparator)} \"{IndexSeparator}\".");
            }

            if (IndexClose == EndpointSeparator)
            {
                throw new InvalidOperationException($"{nameof(IndexClose)} \"{IndexClose}\" may " +
                    $"not be the same as {nameof(EndpointSeparator)} \"{EndpointSeparator}\".");
            }
        }
        
        /// <exception cref="InvalidOperationException"></exception>
        private void ValidateIndexOpen()
        {
            if (IndexOpen == IndexSeparator)
            {
                throw new InvalidOperationException($"{nameof(IndexOpen)} \"{IndexOpen}\" may " +
                    $"not be the same as {nameof(IndexSeparator)} \"{IndexSeparator}\".");
            }

            if (IndexOpen == EndpointSeparator)
            {
                throw new InvalidOperationException($"{nameof(IndexOpen)} \"{IndexOpen}\" may " +
                    $"not be the same as {nameof(EndpointSeparator)} \"{EndpointSeparator}\".");
            }
        }

        /// <exception cref="InvalidOperationException"></exception>
        private void ValidateIndexSeperator()
        {
            if (IndexSeparator == EndpointSeparator)
            {
                throw new InvalidOperationException($"{nameof(IndexSeparator)} \"{IndexSeparator}\" may " +
                    $"not be the same as {nameof(EndpointSeparator)} \"{EndpointSeparator}\".");
            }
        }

        /// <summary>
        /// Returns the combined identifier for the given <paramref name="types"/>. Will also add the 
        /// <see cref="IndexOptionalIdentifier"/> if <paramref name="isOptional"/> is <see langword="true"/>.
        /// </summary>
        /// <param name="types"></param>
        /// <param name="isOptional"></param>
        /// <returns>The combined identifier for the given <paramref name="types"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="types"/> is null.</exception>
        /// <exception cref="ArgumentException">If <paramref name="types"/> is empty.</exception>
        /// <exception cref="SettingsException">If any of the <paramref name="types"/> is supported by the 
        /// <see cref="IndexConverters"/>, but the <see cref="IIndexConverter"/> does not supply a valid 
        /// index identifier.</exception>
        public string GetCombinedIdentifier(Type[] types, bool isOptional = false)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            if (types.Length == 0)
            {
                throw new ArgumentException("Types must at least have one element." ,nameof(types));
            }

            List<string> identifiers = new List<string>();

            foreach (Type type in types)
            {
                try
                {
                    identifiers.Add(GetIdentifier(type));
                }
                catch (SettingsException ex)
                {
                    throw new SettingsException($"Unable to get combined identifiers for types {string.Join(", ", types.Select(t => t.ToString()))}.", ex);
                }
            }

            if (isOptional)
            {
                identifiers.Add(IndexOptionalIdentifier);
            }

            return string.Join(IndexSeparator.ToString(), identifiers);
        }

        /// <summary>
        /// Returns the identifier for the given <paramref name="type"/>, or <see langword="null"/>, 
        /// if none is found.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>The identifier for the given <paramref name="type"/>, or <see langword="null"/>, 
        /// if none is found.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="type"/> is null.</exception>
        /// <exception cref="SettingsException">If the <paramref name="type"/> is supported by the <see cref="IndexConverters"/> 
        /// and therefore should not return null, but the <see cref="IIndexConverter"/> does not supply a valid 
        /// index identifier.</exception>
        public string GetIdentifier(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            IIndexConverter converter = IndexConverters.FirstOrDefault(conv => conv.Type == type);

            if (converter == null)
            {
                return null;
            }

            string result = converter.IndexIdentifiers?.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new SettingsException($"Type {type} is supported, but {converter.GetType()} does not supply " +
                    $"a valid index identifier.");
            }

            return result;
        }

        /// <summary>
        /// Returns the <see cref="Type"/> that corresponds with the given <paramref name="identifier"/>, or 
        /// <see langword="null"/> if there is none.
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns>The <see cref="Type"/> that corresponds with the given <paramref name="identifier"/>, or 
        /// <see langword="null"/> if there is none.</returns>
        public Type GetType(string identifier)
        {
            return IndexConverters.FirstOrDefault(converter => converter.IndexIdentifiers.Contains(identifier))?.Type;
        }
    }
}
