using ApiParser.V2.Settings;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ApiParser.V2.Endpoint
{
    public struct EndpointQueryIndex
    {
        public ParseSettings Settings;
        
        public string Index;

        public bool IsVariable;

        public Type IndexType;

        public string Variable;

        public object Value;

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="QueryParsingException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="QueryNotSupportedException"></exception>
        /// <exception cref="SettingsException"></exception>
        public EndpointQueryIndex(string index, ParseSettings settings)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                throw new ArgumentNullException("index");
            }

            Settings = settings;

            Index = index;

            IsVariable = false;
            IndexType = null;
            Variable = string.Empty;
            Value = null;

            try
            {
                ResolveIndex(settings);
            }
            catch (InvalidOperationException ex)
            {
                throw new QueryParsingException("Unable to resolve index.", ex);
            }
            catch (ApiParserInternalException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (QueryNotSupportedException ex)
            {
                throw new QueryNotSupportedException($"Index type for index {index} not supported.", ex);
            }
            catch (SettingsException ex)
            {
                throw new SettingsException($"Unable to parse index {index}.", ex);
            }
            catch (QueryParsingException ex)
            {
                throw new QueryParsingException($"Unable to parse index {index}.", ex);
            }
        }

        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="QueryNotSupportedException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="SettingsException"></exception>
        /// <exception cref="QueryParsingException"></exception>
        private void ResolveIndex(ParseSettings settings)
        {
            string[] parts = Index.Split(settings.IndexSeparator);

            if (parts.Length != 2)
            {
                throw new InvalidOperationException($"Index {Index} must consist of two parts, separated by \"{settings.IndexSeparator}\".");
            }

            string type = parts[0];
            string value = parts[1];

            if (!TryResolveType(type, settings))
            {
                throw new QueryNotSupportedException($"Index {Index} type {type} is not supported.");
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Index {Index} must have a non empty value.");
            }

            if (value.StartsWith(settings.IndexVariableIdentifier)) // is variable
            {
                try
                {
                    ResolveVariable(value, settings);
                }
                catch (ArgumentNullException ex)
                {
                    throw new ApiParserInternalException(ex);
                }
                catch (ApiParserInternalException ex)
                {
                    throw new ApiParserInternalException(ex);
                }

                return;
            }

            try
            {
                Value = ResolveValue(value, settings);
            }
            catch (InvalidOperationException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (ArgumentException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (SettingsException ex)
            {
                throw new SettingsException($"Unable to resolve value {value}.", ex);
            }
            catch (QueryParsingException ex)
            {
                throw new QueryParsingException($"Unable to resolve value {value}.", ex);
            }
            catch (Exception ex)
            {
                throw new ApiParserInternalException("Unhandled exception occured.", ex);
            }
        }

        private bool TryResolveType(string identifier, ParseSettings settings)
        {
            IndexType = settings.GetType(identifier);

            return IndexType != null;
        }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        private void ResolveVariable(string variable, ParseSettings settings)
        {
            if (string.IsNullOrWhiteSpace(variable))
            {
                throw new ArgumentNullException(nameof(variable));
            }

            if (!variable.StartsWith(settings.IndexVariableIdentifier))
            {
                throw new ApiParserInternalException($"Variable {variable} passed to ResolveVariable(..) does not start " +
                    $"with variable identifier. Expected identifier: \"{settings.IndexVariableIdentifier}\".");
            }

            if (IndexType == null)
            {
                throw new ApiParserInternalException($"Variable {variable} was passed to ResolveVariable(..), but the " +
                    $"index type was not yet resolved.");
            }

            Variable = variable.Substring(settings.IndexVariableIdentifier.Length);
            IsVariable = true;
            Value = null;
        }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="SettingsException"></exception>
        /// <exception cref="QueryParsingException"></exception>
        private object ResolveValue(string value, ParseSettings settings)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (IndexType == null)
            {
                throw new InvalidOperationException($"Value {value} was passed to ResolveValue(..), but the " +
                    $"index type was not yet resolved.");
            }

            Type indexType = IndexType;

            IIndexConverter converter = settings.IndexConverters.FirstOrDefault(conv => conv.Type == indexType);

            if (converter == null)
            {
                throw new InvalidOperationException($"No converter for the type " +
                    $"{indexType} could be found.");
            }

            if (!converter.TryConvert(value, out object result))
            {
                throw new QueryParsingException($"Value {value} could not be converted to index type {IndexType}.");
            }

            if (result.GetType() != IndexType)
            {
                throw new SettingsException($"IndexConverter for type {IndexType} did successfully convert value " +
                    $"{value}, but value is not of promised type {IndexType}.");
            }

            return result;
        }
        /// <summary>
        /// Returns a <see cref="Task"/> that represents the resolved variable, or <see langword="null"/> 
        /// if the variable can't be resolved by the <paramref name="resolver"/>.
        /// </summary>
        /// <param name="resolver"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If the <see cref="EndpointQueryIndex"/> is not a variable.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="resolver"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="resolver"/> returns a value, that fails to 
        /// be converted to the <see cref="IndexType"/>.</exception>
        /// <exception cref="ApiParserInternalException">If an internal exception occurs.</exception>
        /// <exception cref="SettingsException">If an exception related to the <see cref="Settings"/> 
        /// occurs.</exception>
        public async Task<object> ResolveVariable(IQueryVariableResolver resolver)
        {
            if (!IsVariable)
            {
                throw new InvalidOperationException($"{typeof(EndpointQueryIndex)} is not a variable.");
            }
            
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            string value = await resolver.Resolve(Variable);

            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            object result;

            try
            {
                result = ResolveValue(value, Settings);
            }
            catch (InvalidOperationException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (ArgumentNullException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (SettingsException ex)
            {
                throw new SettingsException($"Unable to resolve value {value}.", ex);
            }
            catch (QueryParsingException ex) // value could not be resolved (the returned string fails to be converted)
            {
                throw new ArgumentException($"Returned value {value} from {nameof(resolver)} could not be " +
                    $"converted to the provided variable index type {IndexType}.", ex);
            }
            catch (Exception ex)
            {
                throw new ApiParserInternalException("Unhandled exception occured.", ex);
            }

            return result;
        }
    }
}
