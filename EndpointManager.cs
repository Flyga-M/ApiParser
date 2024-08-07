﻿using ApiParser.Endpoint;
using ApiParser.Settings;
using Gw2Sharp.WebApi.Exceptions;
using Gw2Sharp.WebApi.V2.Clients;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using static ApiParser.QueryUtil;

namespace ApiParser
{
    /// <summary>
    /// Manages the cache, updates and query for an endpoint.
    /// </summary>
    public class EndpointManager
    {
        private readonly IEndpointClient _client;
        private readonly EndpointQuery _path;

        private object _endpointData;

        private readonly IssueTracker _issueTracker;

        /// <summary>
        /// How much milliseconds have to pass, until the data of the <see cref="EndpointManager"/> can be refreshed.
        /// </summary>
        public double Cooldown { get; }

        /// <summary>
        /// The milliseconds since the <see cref="EndpointManager"/> was last accessed.
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
        /// <see cref="Cooldown"/>. Also <see langword="true"/>, if the current data is <see langword="null"/>.
        /// </summary>
        public bool CanRefresh
        {
            get
            {
                return _endpointData == null || MillisecondsSinceLastAccess > Cooldown;
            }
        }

        /// <summary>
        /// The time, the <see cref="EndpointManager"/> was last accessed.
        /// </summary>
        public DateTime LastAccess { get; private set; } = DateTime.MinValue;

        /// <summary>
        /// The <see cref="TokenPermission"/>s that are required to access this endpoint. Might be <see langword="null"/>, if 
        /// the endpoint is authorized and not properly implemented by this library.
        /// </summary>
        public readonly TokenPermission[] RequiredPermissions;

        /// <exception cref="ArgumentNullException">If either <paramref name="endpointClient"/> 
        /// or <paramref name="path"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="cooldown"/> is less than zero.</exception>
        /// <exception cref="EndpointException">If the <paramref name="endpointClient"/> is neither all expandable nor 
        /// has blob data.</exception>
        public EndpointManager(IEndpointClient endpointClient, EndpointQuery path, double cooldown, IssueTracker issueTracker = null)
        {
            if (endpointClient == null)
            {
                throw new ArgumentNullException(nameof(endpointClient));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (cooldown < 0)
            {
                throw new ArgumentOutOfRangeException("Cooldown can't be negative.", nameof(cooldown));
            }

            if (!endpointClient.IsAllExpandable && !endpointClient.HasBlobData)
            {
                throw new EndpointException($"Endpoint is neither all expandable nor has blob data. Those endpoints " +
                    $"are currently not supported.");
            }

            _client = endpointClient;
            _path = path;
            Cooldown = cooldown;
            _issueTracker = issueTracker;

            RequiredPermissions = PermissionUtil.GetPermissions(_client);
        }

        /// <exception cref="QueryNotSupportedException">If the <paramref name="queryData"/> path is not the same as 
        /// <see cref="_path"/>.</exception>
        /// <exception cref="EndpointRequestException">If the api responds with an error and the error is not recoverable 
        /// via the <see cref="ResolveMode"/> of the <paramref name="settings"/>.</exception>
        /// <exception cref="QueryResolveException">If the <paramref name="queryData"/> can't be resolved correctly.</exception>
        /// <exception cref="QueryParsingException">If the <paramref name="queryData"/> contains a variable and the 
        /// resolved variable can't be parsed correctly.</exception>
        /// <exception cref="ApiParserInternalException">If there is an error with the internal logic of the library.</exception>
        /// <exception cref="SettingsException">If the <paramref name="queryData"/> contains at least one variable, but the 
        /// VariableResolver of the <paramref name="settings"/> is null, or if the converted value of 
        /// the resolved variable is not of the type that the <paramref name="settings"/> IndexConverter 
        /// promised.</exception>
        public async Task<object> ResolveQueryAsync(ProcessedQueryData queryData, QuerySettings settings)
        {
            if (queryData.Path.ToString() != _path.ToString())
            {
                throw new QueryNotSupportedException($"{nameof(queryData)}.{nameof(queryData.Path)} ({queryData.Path}) is not " +
                    $"the same as the path of this {this.GetType()} ({_path}).");
            }

            if (queryData.ContainsVariable && settings.VariableResolver == null)
            {
                throw new SettingsException($"Unable to resolve query {queryData}, because it contains at least one " +
                    $"variable, but the variable resolver in the {nameof(settings)} is null.");
            }

            try
            {
                await UpdateEndpointAsync(settings);
            }
            catch (NotImplementedException ex)
            {
                throw new ApiParserInternalException(ex);
            }

            object endpointData = _endpointData;

            if (endpointData == null)
            {
                throw new ApiParserInternalException($"endpoint data is null, even after the update and no exception " +
                    $"was thrown. QueryData: {queryData}");
            }

            if (queryData.RemainingIndices != null && queryData.RemainingIndices.Any())
            {
                ProcessedIndexData resolvedIndices = await ResolveIndicesAsync(endpointData, queryData.RemainingIndices, settings);

                if (!resolvedIndices.Completed)
                {
                    throw new QueryResolveException($"Object of type {endpointData.GetType()} has no indexer for " +
                    $"{resolvedIndices.RemainingIndices.First().IndexType}.");
                }

                endpointData = resolvedIndices.Resolved;
            }

            if (endpointData == null)
            {
                throw new QueryResolveException($"Unable to resolve query {queryData}. Endpoint data is null, " +
                    $"after remaining indices were applied.");
            }

            if (queryData.SubQuery == null)
            {
                return endpointData;
            }
            endpointData = await ResolveSubQueryAsync(endpointData, queryData.SubQuery, settings);

            return endpointData;
        }

        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="SettingsException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="EndpointRequestException"></exception>
        private async Task UpdateEndpointAsync(QuerySettings settings)
        {
            if (!CanRefresh)
            {
                return;
            }

            LastAccess = DateTime.Now;

            object retrievedData = null;

            if (_client.IsAllExpandable)
            {
                retrievedData = await UpdateEndpointAllAsync(settings);

                if (retrievedData != null) // if data is null, keep previous data
                {
                    _endpointData = retrievedData;
                }

                return;
            }

            if (_client.HasBlobData)
            {
                retrievedData = await UpdateEndpointGetAsync(settings);

                if (retrievedData != null) // if data is null, keep previous data
                {
                    _endpointData = retrievedData;
                }

                return;
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Might return null, if <see cref="ResolveMode.RetryOrUsePrevious"/> or <see cref="ResolveMode.UsePrevious"/> is 
        /// selected.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="SettingsException"></exception>
        /// <exception cref="EndpointRequestException"></exception>
        private async Task<object> UpdateEndpointGetAsync(QuerySettings settings)
        {
            string methodName = "GetAsync";

            if (!_client.HasBlobData)
            {
                throw new InvalidOperationException($"Can't invoke {methodName} on endpoint {_client.EndpointPath}. Endpoint " +
                    $"has not blob data.");
            }

            return await InvokeUpdate(methodName, new object[] { new CancellationToken() }, settings);
        }

        /// <summary>
        /// Might return null, if <see cref="ResolveMode.RetryOrUsePrevious"/> or <see cref="ResolveMode.UsePrevious"/> is 
        /// selected.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="SettingsException"></exception>
        /// <exception cref="EndpointRequestException"></exception>
        private async Task<object> UpdateEndpointAllAsync(QuerySettings settings)
        {   
            string methodName = "AllAsync";

            if (!_client.IsAllExpandable)
            {
                throw new InvalidOperationException($"Can't invoke {methodName} on endpoint {_client.EndpointPath}. Endpoint " +
                    $"is not directly accessible or client is not all expandable.");
            }

            return await InvokeUpdate(methodName, new object[] { new CancellationToken() }, settings);
        }

        /// <summary>
        /// Might return null, if <see cref="ResolveMode.RetryOrUsePrevious"/> or <see cref="ResolveMode.UsePrevious"/> is 
        /// selected.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="SettingsException"></exception>
        /// <exception cref="EndpointRequestException"> All kinds of sub types.</exception>
        private async Task<object> InvokeUpdate(string methodName, object[] parameters, QuerySettings settings)
        {
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
                result = await HandleResolveMode(methodName, parameters, settings);
            }
            catch (ArgumentNullException ex)
            {
                throw new ApiParserInternalException(ex);
            }

            return result;
        }

        // TODO: use a more descriptive name for the function
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="SettingsException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="EndpointRequestException"></exception>
        private async Task<object> HandleResolveMode(string methodName, object[] parameters, QuerySettings settings)
        {
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
                        try
                        {
                            result = await RetryRecoverable(methodName, parameters, 1, 0);
                        }
                        catch (ArgumentNullException ex)
                        {
                            throw new ApiParserInternalException(ex);
                        }
                        catch (ArgumentException ex)
                        {
                            throw new ApiParserInternalException(ex);
                        }
                        // let EndpointRequestException bubble up.

                        break;
                    }
                case ResolveMode.Retry:
                    {
                        if (settings.RetryAmount < 1 || settings.RetryDelay < 0)
                        {
                            throw new SettingsException($"Unable to update endpoint {_client?.EndpointPath}, because " +
                                $"{nameof(settings)} did not specify a valid {nameof(settings.RetryAmount)} or " +
                                $"{nameof(settings.RetryDelay)}.");
                        }

                        try
                        {
                            result = await RetryRecoverable(methodName, parameters, settings.RetryAmount, settings.RetryDelay);
                        }
                        catch (ArgumentNullException ex)
                        {
                            throw new ApiParserInternalException(ex);
                        }
                        catch (ArgumentException ex)
                        {
                            throw new ApiParserInternalException(ex);
                        }
                        // let EndpointRequestException bubble up.

                        break;
                    }
                case ResolveMode.RetryOrUsePrevious:
                    {
                        if (settings.RetryAmount < 1 || settings.RetryDelay < 0)
                        {
                            throw new SettingsException($"Unable to update endpoint {_client?.EndpointPath}, because " +
                                $"{nameof(settings)} did not specify a valid {nameof(settings.RetryAmount)} or " +
                                $"{nameof(settings.RetryDelay)}.");
                        }

                        try
                        {
                            result = await RetryRecoverable(methodName, parameters, settings.RetryAmount, settings.RetryDelay);
                        }
                        catch (ArgumentNullException ex)
                        {
                            throw new ApiParserInternalException(ex);
                        }
                        catch (ArgumentException ex)
                        {
                            throw new ApiParserInternalException(ex);
                        }
                        catch (EndpointRequestException ex)
                        {
                            if (!ex.Recoverable) // bad request
                            {
                                ExceptionDispatchInfo.Capture(ex).Throw();
                            }

                            if (_endpointData == null) // not possible to use previous data
                            {
                                ExceptionDispatchInfo.Capture(ex).Throw();
                            }

                            // if the retry was recoverable, but still unsuccessfull, just use the old data
                        }

                        break;
                    }
                case ResolveMode.UsePrevious:
                    {
                        try
                        {
                            result = await RetryRecoverable(methodName, parameters, 1, 0);
                        }
                        catch (ArgumentNullException ex)
                        {
                            throw new ApiParserInternalException(ex);
                        }
                        catch (ArgumentException ex)
                        {
                            throw new ApiParserInternalException(ex);
                        }
                        catch (EndpointRequestException ex)
                        {
                            if (!ex.Recoverable) // bad request
                            {
                                ExceptionDispatchInfo.Capture(ex).Throw();
                            }

                            if (_endpointData == null) // not possible to use previous data
                            {
                                ExceptionDispatchInfo.Capture(ex).Throw();
                            }

                            // if the retry was recoverable, but still unsuccessfull, just use the old data
                        }

                        break;
                    }
            }

            return result;
        }

        /// <summary>
        /// Will throw all flavours of <see cref="EndpointRequestException"/>, even the recoverable ones, if the last 
        /// retry still fails.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="EndpointRequestException"></exception>
        private async Task<object> RetryRecoverable(string methodName, object[] parameters, int amount, int delay)
        {   
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
                    result = await ReflectionUtil.InvokeAsyncMethodAsync<object>(_client, methodName, parameters);
                }
                catch (ArgumentNullException ex)
                {
                    throw new ApiParserInternalException(ex);
                }
                catch (InvalidOperationException ex)
                {
                    throw new ApiParserInternalException(ex);
                }
                catch (ApiParserInternalException ex) // need to catch this explicitly, because we catch a general Exception further down
                {
                    throw new ApiParserInternalException(ex);
                }
                catch (ArgumentException ex) // parameters is multi dimensional
                {
                    throw new ApiParserInternalException(ex);
                }
                catch (TooManyRequestsException ex)
                {
                    if (_issueTracker?.Disposed == false)
                    {
                        _issueTracker?.AddIssue(ex);
                    }
                    success = false;

                    if (i == amount - 1)
                    {
                        throw new EndpointRequestException($"Api request via {methodName} on endpoint {_client.EndpointPath} " +
                        $"failed after {amount} retries.", ex);
                    }
                }
                catch (ServerErrorException ex)
                {
                    if (_issueTracker?.Disposed == false)
                    {
                        _issueTracker?.AddIssue(ex);
                    }
                    success = false;
                    if (i == amount - 1)
                    {
                        throw new EndpointRequestException($"Api request via {methodName} on endpoint {_client.EndpointPath} " +
                        $"failed after {amount} retries.", ex);
                    }
                }
                catch (ServiceUnavailableException ex)
                {
                    if (_issueTracker?.Disposed == false)
                    {
                        _issueTracker?.AddIssue(ex);
                    }
                    success = false;
                    if (i == amount - 1)
                    {
                        throw new EndpointRequestException($"Api request via {methodName} on endpoint {_client.EndpointPath} " +
                        $"failed after {amount} retries.", ex);
                    }
                }
                catch (RequestException ex)
                {
                    throw new EndpointRequestException($"Api request via {methodName} on endpoint {_client.EndpointPath} " +
                        $"failed.", ex);
                }
                catch (RequestException<ErrorObject> ex)
                {
                    throw new EndpointRequestException($"Api request via {methodName} on endpoint {_client.EndpointPath} " +
                        $"failed.", ex);
                }
                catch (RequestException<string> ex)
                {
                    throw new EndpointRequestException($"Api request via {methodName} on endpoint {_client.EndpointPath} " +
                        $"failed.", ex);
                }
                // catch all other RequestException<T> for unknown T. Pray that this does not catch anything else.
                // In v1.7.4 RequestException<T> does not inherit from RequestException. If that changes in the future, this
                // "solution" can be avoided.
                catch (Exception ex)
                {
                    throw new EndpointRequestException($"Api request via {methodName} on endpoint {_client.EndpointPath} " +
                        $"failed.", ex);
                }

                if (success)
                {
                    break;
                }

                await Task.Delay(delay);
            }

            if (_issueTracker?.Disposed == false)
            {
                _issueTracker?.AddSuccess();
            }
            return result;
        }

        /// <summary>
        /// Clears the data of the last response from the gw2 api.
        /// </summary>
        public void ClearCache()
        {
            _endpointData = null;
        }
    }
}
