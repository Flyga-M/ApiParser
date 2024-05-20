using ApiParser.V2.Settings;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ApiParser.V2.Endpoint
{
    public class EndpointQueryIndex
    {
        public ParseSettings Settings { get; }

        public bool IsVariable => Variable != null;

        public Type IndexType { get; }

        public string Variable { get; }

        public object Value { get; }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public EndpointQueryIndex(object value, ParseSettings settings)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Type indexType = value.GetType();

            if (!settings.IndexTypes.Contains(indexType))
            {
                throw new ArgumentException($"{nameof(indexType)} must be convertable by the {nameof(settings)}.", nameof(indexType));
            }

            IndexType = indexType;
            Value = value;
            Variable = null;
            Settings = settings;
        }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public EndpointQueryIndex(Type indexType, string variable, ParseSettings settings)
        {
            if (indexType == null)
            {
                throw new ArgumentNullException(nameof(indexType));
            }

            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            if (string.IsNullOrWhiteSpace(variable))
            {
                throw new ArgumentException($"{nameof(variable)} can't be empty or whitespace.", nameof(variable));
            }

            if (!settings.IndexTypes.Contains(indexType))
            {
                throw new ArgumentException($"{nameof(indexType)} must be convertable by the {nameof(settings)}.", nameof(indexType));
            }

            IndexType = indexType;
            Value = null;
            Variable = variable;
            Settings = settings;
        }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="QueryParsingException">When the given <paramref name="index"/> can't be parsed 
        /// correctly.</exception>
        /// <exception cref="SettingsException">When the converted value is not of the type that the 
        /// <paramref name="settings"/> IndexConverter promised.</exception>
        public static EndpointQueryIndex FromString(string index, ParseSettings? settings = null)
        {
            if (index == null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            if (string.IsNullOrWhiteSpace(index))
            {
                throw new ArgumentException($"{nameof(index)} can't be empty or whitespace.", nameof(index));
            }

            if (!settings.HasValue)
            {
                settings = ParseSettings.Default;
            }

            Type indexType = GetType(index, settings.Value, out string valueString);

            if (indexType == null)
            {
                throw new QueryParsingException($"{nameof(settings)} don't provide an appropriate indexConverter for the given " +
                    $"identifier in the {nameof(index)} {index}.");
            }

            string variableString = GetVariable(valueString, settings.Value);

            if (variableString != null)
            {
                return new EndpointQueryIndex(indexType, variableString, settings.Value);
            }

            object value = GetValue(valueString, indexType, settings.Value);

            return new EndpointQueryIndex(value, settings.Value);
        }

        /// <exception cref="QueryParsingException"></exception>
        private static Type GetType(string id, ParseSettings settings, out string valueString)
        {
            string[] parts = id.Split(settings.IndexSeparator);

            if (parts.Length != 2)
            {
                throw new QueryParsingException($"{nameof(id)} {id} must consist of two parts, separated by \"{settings.IndexSeparator}\".");
            }

            string identifier = parts[0];
            valueString = parts[1];

            if (string.IsNullOrWhiteSpace(identifier))
            {
                throw new QueryParsingException($"Type {nameof(identifier)} for {nameof(id)} {id} can't be null, " +
                    $"empty or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(valueString))
            {
                throw new QueryParsingException($"Type {nameof(valueString)} for {nameof(id)} {id} can't be null, " +
                    $"empty or whitespace.");
            }

            return settings.GetType(identifier);
        }

        private static string GetVariable(string valueString, ParseSettings settings)
        {
            if (!valueString.StartsWith(settings.IndexVariableIdentifier))
            {
                return null;
            }

            return valueString.Substring(settings.IndexVariableIdentifier.Length);
        }

        /// <exception cref="QueryParsingException"></exception>
        /// <exception cref="SettingsException"></exception>
        private static object GetValue(string valueString, Type valueType, ParseSettings settings)
        {
            IIndexConverter converter = settings.IndexConverters.FirstOrDefault(conv => conv.Type == valueType);

            if (converter == null) // TODO: maybe remove? should not occur, because we checked if the type exists in the settings earlier
            {
                throw new QueryParsingException($"No index converter for the type " +
                    $"{valueType} could be found.");
            }

            if (!converter.TryConvert(valueString, out object result))
            {
                throw new QueryParsingException($"Value {valueString} could not be converted to {nameof(valueType)} {valueType}.");
            }

            if (result.GetType() != valueType)
            {
                throw new SettingsException($"IndexConverter for type {valueType} did successfully convert value " +
                    $"{valueString}, but value is not of promised type {valueType}. Given type: {result.GetType()}");
            }

            return result;
        }

        /// <summary>
        /// Returns a <see cref="Task"/> that represents the resolved variable, or <see langword="null"/> 
        /// if the variable can't be resolved by the <paramref name="resolver"/>.
        /// </summary>
        /// <param name="resolver"></param>
        /// <returns>A <see cref="Task"/> that represents the resolved variable, or <see langword="null"/> 
        /// if the variable can't be resolved by the <paramref name="resolver"/>.</returns>
        /// <exception cref="InvalidOperationException">If the <see cref="EndpointQueryIndex"/> is not a variable.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="resolver"/> is <see langword="null"/>.</exception>
        /// <inheritdoc cref="GetValue(string, Type, ParseSettings)"/>
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

            return GetValue(value, IndexType, Settings);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string identifier = Settings.GetIdentifier(IndexType);

            if (IsVariable)
            {
                return $"{identifier}{Settings.IndexSeparator}{Variable}";
            }

            return $"{identifier}{Settings.IndexSeparator}{Value}";
        }
    }
}
