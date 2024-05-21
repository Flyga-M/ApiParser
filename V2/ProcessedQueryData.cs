using ApiParser.V2.Endpoint;
using Gw2Sharp.WebApi.V2.Clients;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiParser.V2
{
    /// <summary>
    /// Contains data of a processed <see cref="EndpointQuery"/>.
    /// </summary>
    public class ProcessedQueryData
    {
        /// <summary>
        /// The resolved <see cref="IEndpointClient"/>.
        /// </summary>
        public readonly IEndpointClient Client;

        /// <summary>
        /// Contains the <see cref="EndpointQueryPart"/>s that were traversed, to get the <see cref="Client"/>.
        /// </summary>
        public readonly EndpointQuery Path;

        /// <summary>
        /// Contains the <see cref="EndpointQueryPart"/>s that can't be resolved directly on the <see cref="Client"/>.
        /// </summary>
        public readonly EndpointQuery SubQuery; // may be null

        /// <summary>
        /// The indices that can't be resolved directly on the <see cref="Client"/> object.
        /// </summary>
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

        /// <exception cref="ArgumentNullException">If either <paramref name="client"/> or <paramref name="path"/> 
        /// is null.</exception>
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
