using ApiParser.V2.Endpoint;
using ApiParser.V2.Settings;
using Gw2Sharp.WebApi.Exceptions;
using Gw2Sharp.WebApi.V2;
using Gw2Sharp.WebApi.V2.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace ApiParser.V2
{
    // TODO: implement proper support for sub endpoints (e.g. Guild[GUID].Logs, Characters[INT:STRING].BuildTabs.Active)
    public class EndpointManager
    {
        private IGw2WebApiV2Client _apiClient;
        protected IEndpointClient _client;

        // I know this is messy, but at this point I'm done with the alternative solutions.
        protected IGuildClient _guildClient;

        private readonly Type[] _clientGenericArguments;

        protected object _directlyAccessibleData;
        private Dictionary<object, GuildEndpointManager> _indirectlyAccessibleData = new Dictionary<object, GuildEndpointManager>();
        
        /// <summary>
        /// The <see cref="Endpoint"/> that the <see cref="EndpointManager"/> manages.
        /// </summary>
        public Endpoint.Endpoint Endpoint { get; }
        
        /// <summary>
        /// How much milliseconds must pass, until the data of the <see cref="EndpointManager"/> can be refreshed.
        /// </summary>
        public double Cooldown { get; } 

        /// <summary>
        /// The milliseconds since the <see cref="Endpoint"/> was last accessed.
        /// </summary>
        public double MillisecondsSinceLastAccess
        {
            get
            {
                TimeSpan elapsed = DateTime.Now - LastAccess;
                return elapsed.TotalMilliseconds;
            }
        }

        /// <summary>
        /// Determines whether more time has elapsed since the <see cref="LastAccess"/>, than the 
        /// <see cref="Cooldown"/>. Does not reflect if a specific indirect endpoint can be refreshed as well.
        /// </summary>
        public bool CanRefresh
        {
            get
            {
                return _directlyAccessibleData == null || MillisecondsSinceLastAccess > Cooldown;
            }
        }

        /// <summary>
        /// The time, the <see cref="Endpoint"/> was last accessed.
        /// </summary>
        public DateTime LastAccess { get; private set; } = DateTime.MinValue;

        public bool IsGuildClient => _guildClient != null;

        public EndpointManager(IGw2WebApiV2Client apiClient, Endpoint.Endpoint endpoint, double cooldown)
        {
            if (apiClient == null)
            {
                throw new ArgumentNullException(nameof(apiClient));
            }

            if (cooldown < 0)
            {
                throw new ArgumentException("Cooldown can't be negative.", nameof(cooldown));
            }

            _apiClient = apiClient;
            Endpoint = endpoint;
            Cooldown = cooldown;

            if (!TryResolveClient(apiClient))
            {
                throw new ArgumentException($"Endpoint could not be resolved as a {typeof(IEndpointClient)}.", nameof(endpoint));
            }

            _clientGenericArguments = ReflectionUtil.GetGenericArguments(apiClient);
        }

        // TODO: maybe return proper exception
        private bool TryResolveClient(IGw2WebApiV2Client apiClient)
        {
            if (apiClient == null)
            {
                return false;
            }

            object resolved = apiClient;

            try
            {
                foreach (EndpointPart part in Endpoint.Parts)
                {
                    string property = part.EndpointName;

                    resolved = resolved.GetType().GetProperty(property).GetValue(resolved);
                }
            }
            catch
            {
                return false;
            }

            _client = resolved as IEndpointClient;
            _guildClient = resolved as IGuildClient;

            return _client != null || _guildClient != null;
        }

        /// <exception cref="QueryNotSupportedException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="SettingsException"></exception>
        /// <exception cref="QueryResolveException"></exception>
        /// <exception cref="EndpointException"></exception>
        public async Task<object> ResolveQuery(EndpointQuery query, QuerySettings settings)
        {
            if (!Endpoint.SupportsQuery(query))
            {
                throw new QueryNotSupportedException($"Unable to resolve query {query.Query}. Query is not supported by the endpoint " +
                    $"{Endpoint.Name}.", nameof(query));
            }

            if (query.ContainsVariable && settings.VariableResolver == null)
            {
                throw new QueryNotSupportedException($"Unable to resolve query {query.Query}. Query is not supported by the endpoint " +
                    $"{Endpoint.Name}.", nameof(query), new SettingsException($"Given settings do not support variables."));
            }

            if (query.QueryParts == null || query.QueryParts.Length == 0)
            {
                throw new QueryNotSupportedException($"Query {query.Query} must contain at least one query part.", nameof(query));
            }

            EndpointQuery subQuery;

            try
            {
                subQuery = query.GetSubQuery(Endpoint);
            }
            catch (QueryNotSupportedException ex)
            {
                throw new ApiParserInternalException($"An internal exception occured while attempting to create " +
                    $"sub query for query {query.Query} for the endpoint {Endpoint.Name}.", ex);
            }
            catch (ApiParserInternalException ex)
            {
                throw new ApiParserInternalException($"An internal exception occured while attempting to create " +
                    $"sub query for query {query.Query} for the endpoint {Endpoint.Name}.", ex);
            }

            object index = null;

            if (IsGuildClient)
            {
                EndpointQueryIndex? queryIndex = subQuery.QueryParts?.FirstOrDefault().Indices?.FirstOrDefault();

                if (!queryIndex.HasValue)
                {
                    throw new QueryNotSupportedException($"Query {query.Query} does not contain an index for the " +
                        $"guild endpoint {Endpoint.Name}.");
                }

                if (queryIndex.Value.IsVariable)
                {
                    try
                    {
                        index = await queryIndex.Value.ResolveVariable(settings.VariableResolver);
                    }
                    catch (ArgumentNullException ex)
                    {
                        throw new ApiParserInternalException($"Unable to resolve queryIndex variable {queryIndex.Value.Variable}.", ex);
                    }
                    catch (InvalidOperationException ex)
                    {
                        throw new ApiParserInternalException($"Unable to resolve queryIndex variable {queryIndex.Value.Variable}.", ex);
                    }
                    catch (ArgumentException ex)
                    {
                        throw new SettingsException($"Unable to resolve queryIndex variable {queryIndex.Value.Variable} with " +
                            $"given {nameof(settings.VariableResolver)}.", ex);
                    }
                    catch (ApiParserInternalException ex)
                    {
                        throw new ApiParserInternalException($"An internal exception occurred while attempting to " +
                            $"resolve queryIndex variable {queryIndex.Value.Variable}.", ex);
                    }
                    catch (SettingsException ex)
                    {
                        throw new SettingsException($"Unable to resolve queryIndex variable {queryIndex.Value.Variable} with " +
                            $"given {nameof(settings.VariableResolver)}. Endpoint settings are faulty.", ex);
                    }
                }
                else
                {
                    index = queryIndex.Value.Value;
                }
            }

            try
            {
                await UpdateEndpoint(settings, index);
            }
            catch (ArgumentNullException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new EndpointException($"Endpoint {Endpoint.Name} currently not supported by this library.", ex);
            }
            catch (ApiParserInternalException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (SettingsException ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
            catch (RequestException ex)
            {
                throw new QueryResolveException($"Unable to resolve query {query.Query} for Endpoint {Endpoint.Name}, " +
                    $"because the API response was an exception.", ex);
            }


            object endpointData = _directlyAccessibleData;

            if (IsGuildClient)
            {
                endpointData = _indirectlyAccessibleData[index]._directlyAccessibleData;
            }
            else if (subQuery.QueryParts.First().Enumerate)
            {
                if (endpointData == null)
                {
                    throw new ApiParserInternalException("Endpoint data is null, after endpoint was updated.");
                }

                if (subQuery.QueryParts.First().Indices == null || subQuery.QueryParts.First().Indices.Length == 0)
                {
                    throw new ApiParserInternalException("QueryPart must contain at least one index if enumerate is true.");
                }

                try
                {
                    endpointData = await ResolveIndices(endpointData, subQuery.QueryParts.First().Indices, settings);
                }
                catch (ArgumentNullException ex)
                {
                    throw new ApiParserInternalException($"An internal exception occured while attempting to " +
                        $"resolve indices for query at part {subQuery.QueryParts.First().EndpointName} for " +
                        $"endpoint {Endpoint.Name}.", ex);
                }
                catch (ArgumentException ex)
                {
                    throw new ApiParserInternalException($"An internal exception occured while attempting to " +
                        $"resolve indices for sub query at part {subQuery.QueryParts.First().EndpointName} for " +
                        $"endpoint {Endpoint.Name}.", ex);
                }
                catch (ApiParserInternalException ex)
                {
                    throw new ApiParserInternalException($"An internal exception occured while attempting to " +
                        $"resolve indices for sub query at part {subQuery.QueryParts.First().EndpointName} for " +
                        $"endpoint {Endpoint.Name}.", ex);
                }
                catch (InvalidOperationException ex)
                {
                    throw new QueryResolveException($"Unable to resolve sub query at part " +
                        $"{subQuery.QueryParts.First().EndpointName} for endpoint {Endpoint.Name}.", subQuery.Query, ex);
                }
                catch (SettingsException ex)
                {
                    throw new SettingsException($"Unable to resolve sub query at part " +
                        $"{subQuery.QueryParts.First().EndpointName} for endpoint {Endpoint.Name}.", ex);
                }
            }

            if (endpointData == null)
            {
                throw new ApiParserInternalException("Endpoint data is null, after endpoint was updated.");
            }

            object result;

            try
            {
                result = await ResolveSubQuery(endpointData, subQuery, settings);
            }
            catch (ArgumentNullException ex)
            {
                throw new ApiParserInternalException($"An internal exception occured while attempting to " +
                        $"resolve query {query.Query} for " +
                        $"endpoint {Endpoint.Name}.", ex);
            }
            catch (ApiParserInternalException ex)
            {
                throw new ApiParserInternalException($"An internal exception occured while attempting to " +
                        $"resolve query {query.Query} for " +
                        $"endpoint {Endpoint.Name}.", ex);
            }
            catch (QueryResolveException ex)
            {
                throw new QueryResolveException($"Unable to resolve query {query.Query}.", ex);
            }
            catch (SettingsException ex)
            {
                throw new SettingsException($"Unable to resolve query {query.Query} " +
                    $"for endpoint {Endpoint.Name}.", ex);
            }

            return result;
        }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="QueryResolveException"></exception>
        /// <exception cref="SettingsException"></exception>
        private async Task<object> ResolveSubQuery(object endpointData, EndpointQuery subQuery, QuerySettings settings)
        {
            object result = endpointData;
            
            if (result == null)
            {
                throw new ArgumentNullException(nameof(endpointData), "EndpointData can't be null.");
            }

            foreach(EndpointQueryPart queryPart in subQuery.QueryParts.Skip(1))
            {
                string property = queryPart.EndpointName;

                PropertyInfo propertyInfo;

                try
                {
                    propertyInfo = result.GetType().GetProperty(property);
                }
                catch (ArgumentNullException ex)
                {
                    throw new ApiParserInternalException($"An internal exception occured while attempting to " +
                        $"resolve sub query {subQuery.Query} at part {queryPart.EndpointName} on endpoint {Endpoint.Name}.", ex);
                }
                catch (AmbiguousMatchException ex)
                {
                    throw new ApiParserInternalException($"An internal exception occured while attempting to " +
                        $"resolve sub query {subQuery.Query} at part {queryPart.EndpointName} on endpoint {Endpoint.Name}.", ex);
                }

                if (propertyInfo == null)
                {
                    throw new QueryResolveException($"Unable to resolve sub query at part {queryPart.EndpointName} " +
                        $"for endpoint {Endpoint.Name}. The given property {property} could not be found.");
                }

                result = propertyInfo.GetValue(result);

                if (result == null)
                {
                    throw new QueryResolveException($"Unable to resolve sub query at part {queryPart.EndpointName} " +
                        $"for endpoint {Endpoint.Name}. The given property {property} could be found, but is null.");
                }

                if (queryPart.Enumerate)
                {
                    if (queryPart.Indices == null || queryPart.Indices.Length == 0)
                    {
                        throw new ApiParserInternalException("QueryPart must contain at least one index if enumerate is true.");
                    }

                    try
                    {
                        result = await ResolveIndices(result, queryPart.Indices, settings);
                    }
                    catch (ArgumentNullException ex)
                    {
                        throw new ApiParserInternalException($"An internal exception occured while attempting to " +
                            $"resolve indices for sub query at part {queryPart.EndpointName} for " +
                            $"endpoint {Endpoint.Name}.", ex);
                    }
                    catch (ArgumentException ex)
                    {
                        throw new ApiParserInternalException($"An internal exception occured while attempting to " +
                            $"resolve indices for sub query at part {queryPart.EndpointName} for " +
                            $"endpoint {Endpoint.Name}.", ex);
                    }
                    catch (ApiParserInternalException ex)
                    {
                        throw new ApiParserInternalException($"An internal exception occured while attempting to " +
                            $"resolve indices for sub query at part {queryPart.EndpointName} for " +
                            $"endpoint {Endpoint.Name}.", ex);
                    }
                    catch (InvalidOperationException ex)
                    {
                        throw new QueryResolveException($"Unable to resolve sub query at part " +
                            $"{queryPart.EndpointName} for endpoint {Endpoint.Name}.", subQuery.Query, ex);
                    }
                    catch (SettingsException ex)
                    {
                        throw new SettingsException($"Unable to resolve sub query at part " +
                            $"{queryPart.EndpointName} for endpoint {Endpoint.Name}.", ex);
                    }
                }
            }

            return result;
        }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="SettingsException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task<object> ResolveIndices(object @object, EndpointQueryIndex[] indices, QuerySettings settings)
        {
            object result = @object;

            if (@object == null)
            {
                throw new ArgumentNullException(nameof(@object));
            }

            if (indices == null)
            {
                throw new ArgumentNullException(nameof(indices));
            }
            
            if (indices.Length == 0)
            {
                throw new ArgumentException("Indices must at least have one entry.", nameof(indices));
            }

            foreach (EndpointQueryIndex index in indices)
            {
                object indexValue;

                if (index.IsVariable)
                {
                    try
                    {
                        indexValue = await index.ResolveVariable(settings.VariableResolver);
                    }
                    catch (ArgumentNullException ex)
                    {
                        throw new ApiParserInternalException($"Unable to resolve index variable {index.Variable}.", ex);
                    }
                    catch (InvalidOperationException ex)
                    {
                        throw new ApiParserInternalException($"Unable to resolve index variable {index.Variable}.", ex);
                    }
                    catch (ArgumentException ex)
                    {
                        throw new SettingsException($"Unable to resolve index variable {index.Variable} with " +
                            $"given {nameof(settings.VariableResolver)}.", ex);
                    }
                    catch (ApiParserInternalException ex)
                    {
                        throw new ApiParserInternalException($"An internal exception occurred while attempting to " +
                            $"resolve index variable {index.Variable}.", ex);
                    }
                    catch (SettingsException ex)
                    {
                        throw new SettingsException($"Unable to resolve index variable {index.Variable} with " +
                            $"given {nameof(settings.VariableResolver)}. Endpoint settings are faulty.", ex);
                    }
                }
                else
                {
                    indexValue = index.Value;
                }

                PropertyInfo indexer;

                try
                {
                    indexer = result.GetType().GetIndexer(new Type[] { index.IndexType });
                }
                catch (ArgumentException ex) // exception does not occur, when the indexer can't be found
                {
                    throw new ApiParserInternalException(ex);
                }

                if (indexer == null) // not directly enumerable with the given type
                {
                    throw new InvalidOperationException($"Object of type {result.GetType()} has no indexer for " +
                        $"{index.IndexType}.");
                }

                // TODO: revisit exceptions. result info is not usefull, since result is newly assigned in the try block
                try
                {
                    result = indexer.GetValue(result, new object[] { indexValue });
                }
                catch (ArgumentException ex)
                {
                    throw new ApiParserInternalException($"An internal exception occured while attempting to resolve the " +
                        $"index {indexValue} on object of type {result.GetType()}.", ex);
                }
                catch (TargetException ex)
                {
                    throw new ApiParserInternalException($"An internal exception occured while attempting to resolve the " +
                        $"index {indexValue} on object of type {result.GetType()}.", ex);
                }
                catch (TargetParameterCountException ex)
                {
                    throw new ApiParserInternalException($"An internal exception occured while attempting to resolve the " +
                        $"index {indexValue} on object of type {result.GetType()}.", ex);
                }
                catch (MethodAccessException ex)
                {
                    throw new ApiParserInternalException($"An internal exception occured while attempting to resolve the " +
                        $"index {indexValue} on object of type {result.GetType()}.", ex);
                }
                catch (TargetInvocationException ex)
                {
                    throw new InvalidOperationException($"Indexer of object of type {result.GetType()} threw an exception " +
                        $"for index value {indexValue}.", ex);
                }
                catch (Exception ex)
                {
                    throw new ApiParserInternalException($"An unhandled exception occured while attempting to resolve the " +
                        $"index {indexValue} on object of type {result.GetType()}.", ex);
                }
            }

            return result;
        }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="SettingsException"></exception>
        /// <exception cref="RequestException"> All kinds of sub types.</exception>
        private async Task UpdateEndpoint(QuerySettings settings, object index = null)
        {
            if (IsGuildClient)
            {
                if (index == null)
                {
                    throw new ArgumentNullException(nameof(index), "Index can't be null, if the endpoint is the guild endpoint.");
                }
                
                try
                {
                    await UpdateIndirectEndpoint(settings, index);
                }
                catch (ArgumentNullException ex)
                {
                    throw new ApiParserInternalException(ex);
                }
                catch (ApiParserInternalException ex)
                {
                    throw new ApiParserInternalException(ex);
                }
                catch (EndpointRequestException ex)
                {
                    throw new EndpointRequestException($"Failed to update endpoint {Endpoint.Name}.", ex);
                }
                catch (InvalidOperationException ex)
                {
                    ExceptionDispatchInfo.Capture(ex).Throw();
                }
                catch (SettingsException ex)
                {
                    ExceptionDispatchInfo.Capture(ex).Throw();
                }
                catch (RequestException ex)
                {
                    ExceptionDispatchInfo.Capture(ex).Throw();
                }

                return;
            } 
            
            // TODO: separate guild endpoint more clearly from this mess
            if (!Endpoint.IsDirectlyAccessible && Endpoint.Parts.Last().EndpointName != "Guild")
            {
                throw new InvalidOperationException("The only not directly accessible endpoint currently supported is " +
                    $"the Guild endpoint. Given endpoint: {Endpoint.Name}.");
            }

            if (!CanRefresh) // after indirect endpoint, because indirect endpoints handle this themselves.
            {
                return;
            }

            LastAccess = DateTime.Now;

            object retrievedData = null;

            if (_client.IsAllExpandable)
            {
                try
                {
                    retrievedData = await UpdateEndpointAllAsync(_client, settings);
                }
                catch (ArgumentNullException ex)
                {
                    throw new ApiParserInternalException(ex);
                }
                catch (InvalidOperationException ex)
                {
                    throw new ApiParserInternalException(ex);
                }
                catch (ApiParserInternalException ex)
                {
                    throw new ApiParserInternalException(ex);
                }
                catch (SettingsException ex)
                {
                    ExceptionDispatchInfo.Capture(ex).Throw();
                }
                catch (RequestException ex) // rethrow request exceptions
                {
                    ExceptionDispatchInfo.Capture(ex).Throw();
                }

                if (retrievedData != null) // if data is null, keep previous data
                {
                    _directlyAccessibleData = retrievedData;
                }

                return;
            }

            try
            {
                retrievedData = await UpdateEndpointGetAsync(_client, settings);
            }
            catch (ArgumentNullException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (ApiParserInternalException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (SettingsException ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
            catch (RequestException ex) // rethrow request exceptions
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }

            if (retrievedData != null) // if data is null, keep previous data
            {
                _directlyAccessibleData = retrievedData;
            }
        }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="EndpointRequestException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="SettingsException"></exception>
        /// <exception cref="RequestException"> All kinds of sub types.</exception>
        private async Task UpdateIndirectEndpoint(QuerySettings settings, object id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }
            
            if (!_indirectlyAccessibleData.ContainsKey(id))
            {
                try
                {
                    _indirectlyAccessibleData[id] = new GuildEndpointManager(_apiClient, Endpoint, Cooldown, _guildClient, id);
                }
                catch (ArgumentNullException ex)
                {
                    throw new ApiParserInternalException($"An internal exception occured while attempting to update " +
                        $"indirect endpoint with {nameof(id)} {id}.", ex);
                }
                catch (ApiParserInternalException ex)
                {
                    throw new ApiParserInternalException($"An internal exception occured while attempting to update " +
                        $"indirect endpoint with {nameof(id)} {id}.", ex);
                }
                catch (EndpointException ex)
                {
                    throw new EndpointRequestException($"Unable to update indirect endpoint with {nameof(id)} {id}.", ex);
                }
            }

            try
            {
                await _indirectlyAccessibleData[id].UpdateEndpoint(settings, id);
            }
            catch (ArgumentNullException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (InvalidOperationException ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
            catch (ApiParserInternalException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (SettingsException ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
            catch (RequestException ex) // rethrow request exceptions
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }

        /// <summary>
        /// Might return null, if <see cref="ResolveMode.RetryOrUsePrevious"/> or <see cref="ResolveMode.UsePrevious"/> is 
        /// selected.
        /// </summary>
        /// <param name="endpointClient"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="SettingsException"></exception>
        /// <exception cref="RequestException"> All kinds of sub types.</exception>
        private async Task<object> UpdateEndpointGetAsync(IEndpointClient endpointClient, QuerySettings settings)
        {
            if (endpointClient == null)
            {
                throw new ArgumentNullException(nameof(endpointClient));
            }

            string methodName = "GetAsync";

            // TODO: seperate guild endpoint more clearly from this mess.
            if (!Endpoint.IsDirectlyAccessible && !endpointClient.IsBulkExpandable && !(Endpoint.Parts.Last().EndpointName == "Guild"))
            {
                throw new InvalidOperationException($"Can't invoke {methodName} on endpoint {endpointClient.EndpointPath}. Endpoint " +
                    $"is not directly accessible or client is not bulk expandable.");
            }

            try
            {
                return await InvokeUpdate(endpointClient, methodName, new object[] { new CancellationToken() }, settings);
            }
            catch (ArgumentNullException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (ApiParserInternalException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (SettingsException ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw; // just to get rid of warnings
            }
            catch (RequestException ex) // rethrow request exceptions
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw; // just to get rid of warnings
            }
        }

        /// <summary>
        /// Might return null, if <see cref="ResolveMode.RetryOrUsePrevious"/> or <see cref="ResolveMode.UsePrevious"/> is 
        /// selected.
        /// </summary>
        /// <param name="endpointClient"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="SettingsException"></exception>
        /// <exception cref="RequestException"> All kinds of sub types.</exception>
        private async Task<object> UpdateEndpointAllAsync(IEndpointClient endpointClient, QuerySettings settings)
        {
            if (endpointClient == null)
            {
                throw new ArgumentNullException(nameof(endpointClient));
            }
            
            string methodName = "AllAsync";

            if (!Endpoint.IsDirectlyAccessible && !_client.IsAllExpandable)
            {
                throw new InvalidOperationException($"Can't invoke {methodName} on endpoint {endpointClient.EndpointPath}. Endpoint " +
                    $"is not directly accessible or client is not all expandable.");
            }

            try
            {
                return await InvokeUpdate(endpointClient, methodName, new object[] { new CancellationToken() }, settings);
            }
            catch (ArgumentNullException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (ApiParserInternalException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (SettingsException ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw; // just to get rid of warnings
            }
            catch (RequestException ex) // rethrow request exceptions
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw; // just to get rid of warnings
            }
        }

        /// <summary>
        /// Might return null, if <see cref="ResolveMode.RetryOrUsePrevious"/> or <see cref="ResolveMode.UsePrevious"/> is 
        /// selected.
        /// </summary>
        /// <param name="endpointClient"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="SettingsException"></exception>
        /// <exception cref="RequestException"> All kinds of sub types.</exception>
        private async Task<object> InvokeUpdate(IEndpointClient endpointClient, string methodName, object[] parameters, QuerySettings settings)
        {   
            if (endpointClient == null)
            {
                throw new ArgumentNullException(nameof(endpointClient));
            }
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentNullException(nameof(methodName));
            }
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }
            
            object result = null;

            try
            {
                result = await HandleResolveMode(endpointClient, methodName, parameters, settings);
            }
            catch (ArgumentNullException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (ApiParserInternalException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (SettingsException ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
            catch (RequestException ex) // rethrow request exceptions
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }

            return result;
        }

        // TODO: use a more descriptive name for the function
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="SettingsException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="RequestException"> All kinds of sub types.</exception>
        private async Task<object> HandleResolveMode(IEndpointClient endpointClient, string methodName, object[] parameters, QuerySettings settings)
        {
            if (endpointClient == null)
            {
                throw new ArgumentNullException(nameof(endpointClient));
            }
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentNullException(nameof(methodName));
            }
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            object result = null;
            
            switch(settings.ResolveMode)
            {
                case ResolveMode.None:
                    {
                        throw new SettingsException($"Unable to update endpoint {endpointClient?.EndpointPath}, because " +
                            $"{nameof(settings)} did not specify a {typeof(ResolveMode)}.");
                    }
                case ResolveMode.Retry:
                    {
                        if (settings.RetryAmount < 1 || settings.RetryDelay < 0)
                        {
                            throw new SettingsException($"Unable to update endpoint {endpointClient?.EndpointPath}, because " +
                                $"{nameof(settings)} did not specify a valid {nameof(settings.RetryAmount)} or " +
                                $"{nameof(settings.RetryDelay)}.");
                        }

                        try
                        {
                            result = await RetryRetryable(endpointClient, methodName, parameters, settings.RetryAmount, settings.RetryDelay);
                        }
                        catch (ArgumentNullException ex)
                        {
                            throw new ApiParserInternalException(ex);
                        }
                        catch (ArgumentException ex)
                        {
                            throw new ApiParserInternalException(ex);
                        }
                        catch (ApiParserInternalException ex)
                        {
                            throw new ApiParserInternalException(ex);
                        }
                        catch (RequestException ex) // rethrow request exceptions
                        {
                            ExceptionDispatchInfo.Capture(ex).Throw();
                        }

                        break;
                    }
                case ResolveMode.RetryOrUsePrevious:
                    {
                        if (settings.RetryAmount < 1 || settings.RetryDelay < 0)
                        {
                            throw new SettingsException($"Unable to update endpoint {endpointClient?.EndpointPath}, because " +
                                $"{nameof(settings)} did not specify a valid {nameof(settings.RetryAmount)} or " +
                                $"{nameof(settings.RetryDelay)}.");
                        }

                        try
                        {
                            result = await RetryRetryable(endpointClient, methodName, parameters, settings.RetryAmount, settings.RetryDelay);
                        }
                        catch (ArgumentNullException ex)
                        {
                            throw new ApiParserInternalException(ex);
                        }
                        catch (ArgumentException ex)
                        {
                            throw new ApiParserInternalException(ex);
                        }
                        catch (ApiParserInternalException ex)
                        {
                            throw new ApiParserInternalException(ex);
                        }
                        catch (RequestException) // rethrow request exceptions
                        {
                            // don't do anything, just use the previous value
                        }

                        break;
                    }
                case ResolveMode.UsePrevious:
                    {
                        if (settings.RetryAmount < 1 || settings.RetryDelay < 0)
                        {
                            throw new SettingsException($"Unable to update endpoint {endpointClient?.EndpointPath}, because " +
                                $"{nameof(settings)} did not specify a valid {nameof(settings.RetryAmount)} or " +
                                $"{nameof(settings.RetryDelay)}.");
                        }

                        try
                        {
                            result = await RetryRetryable(endpointClient, methodName, parameters, 1, 0);
                        }
                        catch (ArgumentNullException ex)
                        {
                            throw new ApiParserInternalException(ex);
                        }
                        catch (ArgumentException ex)
                        {
                            throw new ApiParserInternalException(ex);
                        }
                        catch (ApiParserInternalException ex)
                        {
                            throw new ApiParserInternalException(ex);
                        }
                        catch (RequestException) // rethrow request exceptions
                        {
                            // don't do anything, just use the previous value
                        }

                        break;
                    }
            }

            return result;
        }

        /// <summary>
        /// Will throw all flavours of <see cref="RequestException"/>, even the retryable ones, if the last 
        /// retry still fails.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="RequestException"> All kinds of sub types.</exception>
        private async Task<object> RetryRetryable(IEndpointClient endpoint, string methodName, object[] parameters, int amount, int delay)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }
            
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (amount < 1)
            {
                throw new ArgumentException("Amount must be >= 1.", nameof(amount));
            }

            if (delay < 0)
            {
                throw new ArgumentException("Delay must be >= 0.", nameof(delay));
            }


            object result = null;

            for (int i = 0; i < amount; i++)
            {
                bool success = true;

                try
                {
                    result = await ReflectionUtil.InvokeAsyncMethodAsync<object>(endpoint, methodName, parameters);
                }
                catch (ArgumentNullException ex)
                {
                    throw new ApiParserInternalException(ex);
                }
                catch (InvalidOperationException ex) // TODO: reevaluate if this can only happen when an internal exception occurs
                {
                    throw new ApiParserInternalException(ex);
                }
                catch (ApiParserInternalException ex)
                {
                    throw new ApiParserInternalException(ex);
                }
                catch (ArgumentException ex) // parameters is multi dimensional
                {
                    throw new ApiParserInternalException(ex);
                }
                catch (TooManyRequestsException ex)
                {
                    success = false;
                    if (i == amount - 1)
                    {
                        ExceptionDispatchInfo.Capture(ex).Throw();
                    }
                }
                catch (ServerErrorException ex)
                {
                    success = false;
                    if (i == amount - 1)
                    {
                        ExceptionDispatchInfo.Capture(ex).Throw();
                    }
                }
                catch (ServiceUnavailableException ex)
                {
                    success = false;
                    if (i == amount - 1)
                    {
                        ExceptionDispatchInfo.Capture(ex).Throw();
                    }
                }
                // don't handle other RequestExceptions here, so they can be caught one layer up

                if (success)
                {
                    break;
                }

                await Task.Delay(delay);
            }

            return result;
        }

    }
}
