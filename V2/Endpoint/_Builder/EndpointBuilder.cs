using ApiParser.V2.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiParser.V2.Endpoint
{
    public class EndpointBuilder
    {
        private List<EndpointPart> _parts = new List<EndpointPart>();

        public EndpointBuilder AddPart(EndpointPart part)
        {
            if (part == null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            if (_parts.Contains(part))
            {
                throw new InvalidOperationException($"Part {part} can't be added twice.");
            }

            _parts.Add(part);

            return this;
        }

        /// <exception cref="InvalidOperationException"></exception>
        public Endpoint Build(ParseSettings settings)
        {
            if (!_parts.Any())
            {
                throw new InvalidOperationException($"{nameof(_parts)} must at least have one element.");
            }

            return new Endpoint(_parts, settings);
        }
    }
}
