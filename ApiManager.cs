using ApiParser.Endpoint;
using Gw2Sharp.WebApi.V2;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiParser.Settings;
using System;
using Gw2Sharp.WebApi.V2.Clients;
using System.Reflection;
using Gw2Sharp.WebApi.V2.Models;
using Gw2Sharp.WebApi;
using static ApiParser.QueryUtil;

namespace ApiParser
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
        public event EventHandler<ApiState> StateChanged;

        private void OnStateChange(object _, ApiState state)
        {
            StateChanged?.Invoke(this, state);
        }

        /// <exception cref="ArgumentNullException">If <paramref name="client"/> is null.</exception>
        public ApiManager(IGw2WebApiClient client, ApiManagerSettings settings)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }
            _client = client.V2;
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
            ProcessedQueryData validCandidate = await GetValidCandidateAsync(query, settings);

            return PermissionUtil.GetPermissions(validCandidate.Client);
        }

        // TODO: SettingsException should not occur, when VariableResolver is null. It's not a settings problem, when
        // TODO: a user inserts variables, that can't be resolved.

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
        public async Task<object> ResolveQueryAsync(EndpointQuery query, QuerySettings? settings = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (settings == null)
            {
                settings = QuerySettings.Default;
            }

            ProcessedQueryData validCandidate = await GetValidCandidateAsync(query, settings);

            if (!_endpointsByPath.ContainsKey(validCandidate.Path.ToString()))
            {
                try
                {
                    _endpointsByPath[validCandidate.Path.ToString()] = new EndpointManager(validCandidate.Client, validCandidate.Path, Settings.Cooldown, _issueTracker);
                }
                catch (EndpointException ex)
                {
                    throw new QueryNotSupportedException($"The query {query} is not supported.", ex);
                }
            }

            object result;

            try
            {
                result = await _endpointsByPath[validCandidate.Path.ToString()].ResolveQueryAsync(validCandidate, settings.Value);
            }
            catch (QueryNotSupportedException ex)
            {
                throw new ApiParserInternalException(ex);
            }

            return result;
        }

        /// <summary>
        /// Returns the valid candidate to resolve the <paramref name="query"/>.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="settings"></param>
        /// <returns>The valid candidate to resolve the <paramref name="query"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="SettingsException">If the <paramref name="query"/> contains at least one variable, but the 
        /// VariableResolver of the <paramref name="settings"/> is null, or if the converted value of 
        /// the resolved variable is not of the type that the <paramref name="settings"/> IndexConverter 
        /// promised.</exception>
        /// <exception cref="QueryResolveException">If <see cref="EndpointQueryPart.EndpointName"/> is <see langword="null"/> 
        /// for any <see cref="EndpointQueryPart"/> in <paramref name="query"/>, or if any <see cref="EndpointQueryPart"/> 
        /// is ambigiuous, or if the property targeted by any <see cref="EndpointQueryPart"/> does not exist, or if 
        /// the resulting object is <see langword="null"/>, or if the indexer for any of the indices throws an 
        /// exception, or if no valid candidate can be found.</exception>
        /// <exception cref="QueryParsingException">If the <paramref name="query"/> contains a variable and the 
        /// resolved variable can't be parsed correctly.</exception>
        private async Task<ProcessedQueryData> GetValidCandidateAsync(EndpointQuery query, QuerySettings? settings = null)
        {
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

            ProcessedQueryData[] queryData = await ProcessQueryAsync(_client, query, settings.Value);

            ProcessedQueryData validCandidate = queryData.LastOrDefault();

            if (validCandidate == null)
            {
                throw new QueryResolveException($"Unable to retrieve required permissions for query {query}, because query " +
                    $"can't be successfully processed.");
            }

            return validCandidate;
        }

        // this is currently built on the assumption, that no attribute from an api response has the same name as a
        // sub endpoint (e.g. there's Achievements/Daily as an endpoint, but the response from the Achievements endpoint
        // has no attribute "daily").
        // This may change in the future (or may already be the case and i overlooked it), hence why we're keeping a list 
        // of all valid endpoint from the query, so it's easier to adapt to that case in the future

        // This is not ideal, because in the case of a faulty query, it would have to update all the constructed 
        // endpoints, to see if there is a match. Hence why this is not implemented preemptively.

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
