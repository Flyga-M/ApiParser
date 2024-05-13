using Gw2Sharp.WebApi.V2;
using Gw2Sharp.WebApi.V2.Clients;
using System;
using System.Reflection;

namespace ApiParser.V2.Endpoint
{
    public class GuildEndpointManager : EndpointManager
    {
        public object Index { get; }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="EndpointException"></exception>
        public GuildEndpointManager(IGw2WebApiV2Client apiClient, Endpoint endpoint, double cooldown, IGuildClient guildClient, object index) : base(apiClient, endpoint, cooldown)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            Index = index;
            _guildClient = guildClient;

            if (guildClient == null)
            {
                throw new ArgumentNullException(nameof(guildClient));
            }

            if (index == null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            try
            {
                _client = ResolveClient();
            }
            catch (ApiParserInternalException ex)
            {
                throw new ApiParserInternalException("An internal exception occured while attempting to instantiate a " +
                    $"{typeof(GuildEndpointManager)}.", ex);
            }
            catch (InvalidOperationException ex)
            {
                // TODO: maybe there is a more specific exception we could throw here?
                throw new EndpointException($"Unable to instantiate guild endpoint for guild with id " +
                    $"{index}.", ex);
            }
            
            _guildClient = null;
        }

        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private IEndpointClient ResolveClient()
        {
            PropertyInfo indexer;

            try
            {
                indexer = _guildClient.GetType().GetIndexer(new Type[] { Index.GetType() });
            }
            catch (ArgumentException ex)
            {
                throw new ApiParserInternalException($"An internal exception occured while attemting to resolve " +
                    $"client for guild endpoint.", ex);
            }

            if (indexer == null)
            {
                throw new InvalidOperationException($"Can't update endpoint {Endpoint.Name} with indexer that " +
                    $"takes {Index.GetType()} as index, because no such indexer was found.");
            }

            object clientId;

            try
            {
                clientId = indexer.GetValue(_guildClient, new object[] { Index });
            }
            catch (ArgumentException ex)
            {
                throw new ApiParserInternalException($"An internal exception occured while attempting to resolve the " +
                    $"index {Index} on the guild endpoint.", ex);
            }
            catch (TargetException ex)
            {
                throw new ApiParserInternalException($"An internal exception occured while attempting to resolve the " +
                    $"index {Index} on the guild endpoint.", ex);
            }
            catch (TargetParameterCountException ex)
            {
                throw new ApiParserInternalException($"An internal exception occured while attempting to resolve the " +
                    $"index {Index} on the guild endpoint.", ex);
            }
            catch (MethodAccessException ex)
            {
                throw new ApiParserInternalException($"An internal exception occured while attempting to resolve the " +
                    $"index {Index} on the guild endpoint.", ex);
            }
            catch (TargetInvocationException ex)
            {
                throw new InvalidOperationException($"Indexer of object of guild endpoint threw an exception " +
                    $"for index value {Index}.", ex);
            }
            catch (Exception ex)
            {
                throw new ApiParserInternalException($"An unhandled exception occured while attempting to resolve the " +
                    $"index {Index} on the guild endpoint.", ex);
            }

            if (clientId == null)
            {
                throw new InvalidOperationException($"Can't update endpoint {Endpoint.Name} for guild with index " +
                    $"{Index}, because result is null.");
            }

            if (!(clientId is IEndpointClient cliendIdEndpoint))
            {
                throw new ApiParserInternalException($"Can't update endpoint {Endpoint.Name} for guild with index " +
                    $"{Index}, because result is not {typeof(IEndpointClient)}. " +
                    $"Given type: {clientId.GetType()}.");
            }

            return cliendIdEndpoint;
        }
    }
}
