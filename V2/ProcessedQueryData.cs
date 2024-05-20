using ApiParser.V2.Endpoint;
using Gw2Sharp.WebApi.V2.Clients;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiParser.V2
{
    public class ProcessedQueryData
    {
        public readonly IEndpointClient Client;

        public readonly EndpointQuery Path;

        public readonly EndpointQuery SubQuery; // may be null

        public readonly EndpointQueryIndex[] RemainingIndices;

        /// <summary>
        /// Determines whether the <see cref="SubQuery"/> or the <see cref="RemainingIndices"/> contain any variables.
        /// </summary>
        public bool ContainsVariable
        {
            get
            {
                if (SubQuery != null && SubQuery.ContainsVariable)
                {
                    return true;
                }

                return RemainingIndices.Any(index => index.IsVariable);
            }
        }

        public ProcessedQueryData(IEndpointClient client, EndpointQuery path, EndpointQuery subQuery, IEnumerable<EndpointQueryIndex> remainingIndices)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (remainingIndices == null)
            {
                remainingIndices = Array.Empty<EndpointQueryIndex>();
            }

            Client = client;
            Path = path;
            SubQuery = subQuery;
            RemainingIndices = remainingIndices.ToArray();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string indices = "";
            
            if (RemainingIndices.Any())
            {
                indices = $"{Path.Settings.IndexOpen}{string.Join<EndpointQueryIndex>($"{Path.Settings.IndexClose}{Path.Settings.IndexOpen}", RemainingIndices)}{Path.Settings.IndexClose}";
            }

            string subQuery = "";

            if (SubQuery != null)
            {
                subQuery = $"{Path.Settings.EndpointSeparator}{SubQuery}";
            }

            return $"{Path}///{indices}{subQuery}";
        }
    }
}
