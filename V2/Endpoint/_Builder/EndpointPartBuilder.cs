using ApiParser.V2.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiParser.V2.Endpoint
{
    public class EndpointPartBuilder
    {
        private string _endpointName;
        private List<Type> _indexTypes = new List<Type>();
        private bool _optionallyEnumerable = false;

        private List<Endpoint> _subEndpoints = new List<Endpoint>();
        private List<Endpoint> _subEndpointsForIndex = new List<Endpoint>();

        /// <summary>
        /// Sets the endpoint name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public EndpointPartBuilder WithEndpointName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            
            _endpointName = name;
            return this;
        }

        /// <summary>
        /// Makes the endpoint part directly accessible, even if it is enumerable.
        /// </summary>
        /// <returns></returns>
        public EndpointPartBuilder WithOptionalIndex()
        {
            _optionallyEnumerable = true;

            return this;
        }

        /// <summary>
        /// Adds a possible index type to the endpoint part.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public EndpointPartBuilder AddIndexType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (_indexTypes.Contains(type))
            {
                throw new InvalidOperationException($"IndexType {type} can't be added twice.");
            }

            _indexTypes.Add(type);

            return this;
        }

        /// <summary>
        /// Adds a sub endpoint to the core endpoint part.
        /// </summary>
        /// <param name="subEndpoint"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public EndpointPartBuilder AddSubEndpoint(Endpoint subEndpoint)
        {
            if (subEndpoint == null)
            {
                throw new ArgumentNullException(nameof(subEndpoint));
            }

            if (_subEndpoints.Contains(subEndpoint))
            {
                throw new InvalidOperationException($"SubEndpoint {subEndpoint} can't be added twice.");
            }

            _subEndpoints.Add(subEndpoint);

            return this;
        }

        /// <summary>
        /// Adds a sub endpoint to the indexed endpoint part.
        /// </summary>
        /// <param name="subEndpoint"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public EndpointPartBuilder AddSubEndpointForIndex(Endpoint subEndpoint)
        {
            if (subEndpoint == null)
            {
                throw new ArgumentNullException(nameof(subEndpoint));
            }

            if (_subEndpointsForIndex.Contains(subEndpoint))
            {
                throw new InvalidOperationException($"SubEndpoint {subEndpoint} can't be added twice.");
            }

            _subEndpointsForIndex.Add(subEndpoint);

            return this;
        }

        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="EndpointException"></exception>
        public EndpointPart Build(ParseSettings settings)
        {
            bool isDirectlyAccessible = _optionallyEnumerable || !_indexTypes.Any();

            if (string.IsNullOrWhiteSpace(_endpointName))
            {
                throw new InvalidOperationException($"{typeof(EndpointPart)} must have a {nameof(_endpointName)}.");
            }

            return new EndpointPart(_endpointName, settings, isDirectlyAccessible, _indexTypes, _subEndpoints, _subEndpointsForIndex);
        }
    }
}
