using ApiParser.V2.Endpoint;
using Gw2Sharp.WebApi.V2;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiParser.V2.Settings;
using System;
using Gw2Sharp.WebApi.V2.Clients;
using System.Reflection;
using Gw2Sharp.WebApi.V2.Models;

namespace ApiParser.V2
{
    /// <summary>
    /// Manages the <see cref="EndpointManager"/>s. May be used to resolve an <see cref="EndpointQuery"/> and retrieve 
    /// data from the gw2 api.
    /// </summary>
    public class ApiManager : IDisposable
    {
        private bool _disposed;

        private readonly IGw2WebApiV2Client _client;

        private IssueTracker _issueTracker;

        private readonly Dictionary<string, EndpointManager> _endpointsByPath = new Dictionary<string, EndpointManager>();

        /// <summary>
        /// Determines how the <see cref="ApiManager"/> manages the <see cref="EndpointManager"/>s.
        /// </summary>
        public ApiManagerSettings Settings { get; }

        /// <inheritdoc cref="IssueTracker.State"/>
        public ApiState State => _issueTracker.State;

        /// <inheritdoc cref="IssueTracker.StateChanged"/>
        public EventHandler<ApiState> StateChanged;

        private void OnStateChange(object _, ApiState state)
        {
            StateChanged?.Invoke(this, state);
        }

        /// <exception cref="ArgumentNullException">If <paramref name="client"/> is null.</exception>
        public ApiManager(IGw2WebApiV2Client client, ApiManagerSettings settings)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }
            _client = client;
            Settings = settings;

            _issueTracker = new IssueTracker(settings);

            _issueTracker.StateChanged += OnStateChange;
        }

        /// <summary>
        /// Returns the permissions, that are required to resolve the <paramref name="query"/>.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="settings"></param>
        /// <returns>The permissions, that are required to resolve the <paramref name="query"/>. Might return 
        /// <see langword="null"/>, if the endpoint is authorized and not properly implemented by this library.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="SettingsException">If the <paramref name="query"/> contains at least one variable, but the 
        /// VariableResolver of the <paramref name="settings"/> is null, or if the converted value of 
        /// the resolved variable is not of the type that the <paramref name="settings"/> IndexConverter 
        /// promised.</exception>
        /// <exception cref="QueryResolveException">If the <paramref name="query"/> can't be resolved correctly.</exception>
        /// <exception cref="QueryParsingException">If the <paramref name="query"/> contains a variable and the 
        /// resolved variable can't be parsed correctly.</exception>
        public async Task<TokenPermission[]> RequiredPermissions(EndpointQuery query, QuerySettings? settings = null)
        {
            // TODO: lots of duplicated code from ResolveQuery. Should be moved to it's own method
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (settings == null)
            {
                settings = QuerySettings.Default;
            }

            if (query.ContainsVariable && settings.Value.VariableResolver == null)
            {
                throw new SettingsException($"Unable to retrieve required permissions for query {query}, because query " +
                    $"contains at least one variable, but the variable resolver in the {nameof(settings)} is null.");
            }

            ProcessedQueryData[] queryData = await ProcessQuery(_client, query, settings.Value);

            ProcessedQueryData validCandidate = queryData.LastOrDefault();

            if (validCandidate == null)
            {
                throw new QueryResolveException($"Unable to retrieve required permissions for query {query}, because query " +
                    $"can't be successfully processed.");
            }

            return PermissionUtil.GetPermissions(validCandidate.Client);
        }

        /// <summary>
        /// Resolves the provided <paramref name="query"/> and retrieves the data from the gw2 api.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="settings">If settings is null, the default settings will be used.</param>
        /// <returns>The retrieved data from the gw2 api.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="SettingsException">If the <paramref name="query"/> contains at least one variable, but the 
        /// VariableResolver of the <paramref name="settings"/> is null, or if the converted value of 
        /// the resolved variable is not of the type that the <paramref name="settings"/> IndexConverter 
        /// promised.</exception>
        /// <exception cref="QueryResolveException">If the <paramref name="query"/> can't be resolved correctly.</exception>
        /// <exception cref="QueryParsingException">If the <paramref name="query"/> contains a variable and the 
        /// resolved variable can't be parsed correctly.</exception>
        /// <exception cref="QueryNotSupportedException">If the endpoint that the <paramref name="query"/> targets is neither 
        /// all expandable nor has blob data.</exception>
        /// <exception cref="ApiParserInternalException">If there is an error with the internal logic of the library.</exception>
        /// <exception cref="EndpointRequestException">If the api responds with an error and the error is not recoverable 
        /// via the <see cref="ResolveMode"/> of the <paramref name="settings"/>.</exception>
        /// <exception cref="ObjectDisposedException">If the <see cref="ApiManager"/> was disposed.</exception>
        public async Task<object> ResolveQuery(EndpointQuery query, QuerySettings? settings = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (settings == null)
            {
                settings = QuerySettings.Default;
            }

            if (query.ContainsVariable && settings.Value.VariableResolver == null)
            {
                throw new SettingsException($"Unable to resolve query {query}, because it contains at least one " +
                    $"variable, but the variable resolver in the {nameof(settings)} is null.");
            }

            ProcessedQueryData[] queryData = await ProcessQuery(_client, query, settings.Value);

            ProcessedQueryData validCandidate = queryData.LastOrDefault();

            if (validCandidate == null)
            {
                throw new QueryResolveException($"Unable to resolve query {query}, because it can't be successfully " +
                    $"processed.");
            }

            if (!_endpointsByPath.ContainsKey(validCandidate.Path.ToString()))
            {
                try
                {
                    _endpointsByPath[validCandidate.Path.ToString()] = new EndpointManager(_client, validCandidate.Client, validCandidate.Path, Settings.Cooldown, _issueTracker);
                }
                catch (EndpointException ex)
                {
                    throw new QueryNotSupportedException($"The query {query} is not supported.", ex);
                }
            }

            object result;

            try
            {
                result = await _endpointsByPath[validCandidate.Path.ToString()].ResolveQuery(validCandidate, settings.Value);
            }
            catch (QueryNotSupportedException ex)
            {
                throw new ApiParserInternalException(ex);
            }

            return result;
        }

        // this is currently built on the assumption, that no attribute from an api response has the same name as a
        // sub endpoint (e.g. there's Achievements/Daily as an endpoint, but the response from the Achievements endpoint
        // has no attribute "daily").
        // This may change in the future (or may already be the case and i overlooked it), hence why we're keeping a list 
        // of all valid endpoint from the query, so it's easier to adapt to that case in the future

        // This is not ideal, because in the case of a faulty query, it would have to update all the constructed 
        // endpoints, to see if there is a match. Hence why this is not implemented preemptively.

        /// <exception cref="QueryResolveException">If the <paramref name="query"/> can't be resolved correctly.</exception>
        /// <exception cref="QueryParsingException">If the <paramref name="query"/> contains a variable and the 
        /// resolved variable can't be parsed correctly.</exception>
        /// <exception cref="SettingsException">If the <paramref name="query"/> contains a variable and the converted value of 
        /// the resolved variable is not of the type that the <see cref="ParseSettings"/> IndexConverter of 
        /// the <paramref name="query"/> promised.</exception>
        private async Task<ProcessedQueryData[]> ProcessQuery(IGw2WebApiV2Client apiClient, EndpointQuery query, QuerySettings settings)
        {
            List<ProcessedQueryData> result = new List<ProcessedQueryData>();

            object resolved = apiClient;

            List<EndpointQueryPart> traversedParts = new List<EndpointQueryPart>();
            List<EndpointQueryPart> remainingParts = new List<EndpointQueryPart>(query.QueryParts);

            EndpointQueryIndex[] remainingIndices = Array.Empty<EndpointQueryIndex>();

            foreach (EndpointQueryPart part in query.QueryParts)
            {
                string property = part.EndpointName;

                if (string.IsNullOrWhiteSpace(property))
                {
                    throw new QueryResolveException($"Unable to resolve query {query}, because a query part name" +
                        $"is null, empty or whitespace.");
                }

                PropertyInfo propertyInfo;

                try
                {
                    propertyInfo = resolved.GetType().GetProperty(property);
                }
                catch (AmbiguousMatchException ex)
                {
                    throw new QueryResolveException($"Unable to resolve query {query}, because the query part {part} " +
                        $"is ambigiuous.", ex);
                }

                if (propertyInfo == null) // property does not exist
                {
                    if (!result.Any())
                    {
                        throw new QueryResolveException($"Unable to resolve query {query}, because no endpoint " +
                            $"with that path exists.");
                    }
                    break;
                }

                traversedParts.Add(part);
                remainingParts.RemoveAt(0);

                // TODO: evaluate if this needs to be in a try catch
                resolved = propertyInfo.GetValue(resolved);

                if (resolved == null)
                {
                    throw new QueryResolveException($"Unable to resolve query {query}. The given part {part} could be " +
                        $"found, but is null.");
                }

                if (part.Enumerate)
                {
                    // don't catch anything, so it can bubble up
                    ProcessedIndexData indexData = await ResolveIndices(resolved, part.Indices, settings);

                    resolved = indexData.Resolved;
                    remainingIndices = indexData.RemainingIndices;

                    EndpointQueryPart currentPart = traversedParts.Last();
                    
                    if (remainingIndices.Any()) // if not all indices are traversed, remove the remaining from the current part
                    {
                        currentPart = new EndpointQueryPart(currentPart.EndpointName, indexData.TraversedIndices, currentPart.Settings);
                    }

                    traversedParts.RemoveAt(traversedParts.Count - 1);
                    traversedParts.Add(currentPart);
                }

                if (resolved == null)
                {
                    throw new QueryResolveException($"Unable to resolve query {query}. The given part {part} could be " +
                        $"enumerated, but is null.");
                }

                if (resolved is IEndpointClient endpointClient)
                {
                    EndpointQuery path = new EndpointQuery(traversedParts, query.Settings);
                    EndpointQuery subQuery = null;
                    if (remainingParts.Any())
                    {
                        subQuery = new EndpointQuery(remainingParts, query.Settings);
                    }
                    result.Add(new ProcessedQueryData(endpointClient, path, subQuery, remainingIndices));
                }

                // if there are remaining indices, this path cannot continue to be traversed
                // else this might lead to issues where X[1][2]["abc"].Y.Z will be resolved as
                // X[1].Y.Z with remainingIndices.Count = 0 if X[1][2] can not be resolved
                if (remainingIndices.Any())
                {
                    break;
                }
            }

            return result.ToArray();
        }

        /// <exception cref="QueryResolveException">If any of the <paramref name="indices"/> can't be resolved correctly.</exception>
        /// <exception cref="QueryParsingException">If any of the <paramref name="indices"/> contain a variable and the 
        /// resolved variable can't be parsed correctly.</exception>
        /// <exception cref="SettingsException">If any of the <paramref name="indices"/> contain a variable and the 
        /// resolved variable is not of the type that the <see cref="ParseSettings"/> IndexConverter of the 
        /// <paramref name="indices"/> promised.</exception>
        private async Task<ProcessedIndexData> ResolveIndices(object @object, EndpointQueryIndex[] indices, QuerySettings settings)
        {
            object resolved = @object;
            List<EndpointQueryIndex> traversedIndices = new List<EndpointQueryIndex>();
            List<EndpointQueryIndex> remainingIndices = new List<EndpointQueryIndex>(indices);

            foreach (EndpointQueryIndex index in indices)
            {
                object indexValue;

                if (index.IsVariable)
                {
                    // don't catch anything, so it can bubble up
                    indexValue = await index.ResolveVariable(settings.VariableResolver);
                }
                else
                {
                    indexValue = index.Value;
                }

                PropertyInfo indexer;

                indexer = resolved.GetType().GetIndexer(new Type[] { index.IndexType });

                if (indexer == null) // not directly enumerable with the given type
                {
                    return new ProcessedIndexData(resolved, traversedIndices, remainingIndices);
                }

                traversedIndices.Add(index);
                remainingIndices.RemoveAt(0);

                object value;

                try
                {
                    value = indexer.GetValue(resolved, new object[] { indexValue });
                }
                catch (TargetInvocationException ex)
                {
                    throw new QueryResolveException($"Indexer of object of type {resolved.GetType()} threw an exception " +
                        $"for index value {indexValue}.", ex);
                }

                if (value == null)
                {
                    throw new QueryResolveException($"Unable to resolve indices {string.Join<EndpointQueryIndex>(", ", indices)} " +
                        $"in query. The given index {index} could be used, but it's value returns null.");
                }

                resolved = value;
            }

            return new ProcessedIndexData(resolved, traversedIndices, remainingIndices);
        }

        /// <summary>
        /// Clears the data of the last response from the gw2 api for every endpoint.
        /// </summary>
        public void ClearCache()
        {
            foreach (EndpointManager endpointManager in _endpointsByPath.Values)
            {
                endpointManager.ClearCache();
            }

            _endpointsByPath.Clear();
        }

        /// <summary>
        /// Clears the data of the last response from the gw2 api for the given <paramref name="endpointPath"/>.
        /// </summary>
        public void ClearCache(string endpointPath)
        {
            if (!_endpointsByPath.ContainsKey(endpointPath))
            {
                return;
            }

            _endpointsByPath[endpointPath].ClearCache();
            _endpointsByPath.Remove(endpointPath);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (_issueTracker != null)
                {
                    _issueTracker.StateChanged -= OnStateChange;

                    _issueTracker.Dispose();
                    _issueTracker = null;
                }
            }

            StateChanged = null;
            _endpointsByPath.Clear();

            _disposed = true;
        }

        /// <inheritdoc/>
        ~ApiManager()
        {
            Dispose(false);
        }
    }
}
