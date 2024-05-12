using ApiParser.V2.Endpoint;
using Gw2Sharp.WebApi.V2;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiParser.V2.Settings;

namespace ApiParser.V2
{
    public class ApiManager
    {
        private List<EndpointManager> _endpoints = new List<EndpointManager>();

        public ApiManagerSettings Settings { get; }

        public ApiManager(IGw2WebApiV2Client client, ApiManagerSettings settings)
        {
            Settings = settings;

            foreach (Endpoint.Endpoint endpoint in V2.Settings.Default.Endpoints.Default)
            {
                InitializeEndpoint(client, endpoint, settings);
            }
        }

        // TODO: decide if there should be a cooldown setting per endpoint
        // TODO: set up correctly
        private void InitializeEndpoint(IGw2WebApiV2Client client, Endpoint.Endpoint endpoint, ApiManagerSettings settings)
        {
            _endpoints.Add(new EndpointManager(client, endpoint, settings.Cooldown));
        }

        // TODO: document and add exceptions
        public async Task<object> Query(EndpointQuery query, QuerySettings? settings = null)
        {
            if (!settings.HasValue)
            {
                settings = QuerySettings.Default;
            }
            
            EndpointManager manager = _endpoints.Where(endpoint => endpoint.Endpoint.SupportsQuery(query)).OrderByDescending(endpoint => endpoint.Endpoint.Parts.Length).FirstOrDefault();

            if (manager == null)
            {
                throw new QueryNotSupportedException($"No endpoint with the signature of the query {query.Query} could " +
                    $"be found.");
            }

            // TODO: wrap in try catch?
            return await manager.ResolveQuery(query, settings.Value);
        }
    }
}
