using ApiParser.V2.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiParser.V2.Endpoint
{
    public struct EndpointQuery
    {
        public ParseSettings Settings;
        
        public string Query;

        public EndpointQueryPart[] QueryParts;

        public bool ContainsVariable => QueryParts?.Any(part => part.ContainsVariable) ?? false;

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="QueryParsingException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="QueryNotSupportedException"></exception>
        /// <exception cref="SettingsException"></exception>
        public EndpointQuery(string query, ParseSettings? settings = null)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentNullException("query");
            }
            
            Query = query;
            Settings = settings ?? ParseSettings.Default;

            QueryParts = Array.Empty<EndpointQueryPart>();

            try
            {
                ResolveQuery();
            }
            catch (InvalidOperationException ex)
            {
                throw new QueryParsingException($"Query {query} could not be parsed correctly.", query, ex);
            }
            catch (ApiParserInternalException ex)
            {
                throw new ApiParserInternalException($"An internal exception occured during initialization of a " +
                    $"{typeof(EndpointQuery)} with query: \"{query}\".", ex);
            }
            catch (QueryParsingException ex)
            {
                throw new QueryParsingException($"Query {query} could not be parsed correctly.", ex);
            }
            catch (QueryNotSupportedException ex)
            {
                throw new QueryNotSupportedException($"Query {query} is not supported.", ex);
            }
            catch (SettingsException ex)
            {
                throw new SettingsException($"Query {query} could not be parsed.", ex);
            }

        }

        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="QueryParsingException"></exception>
        /// <exception cref="QueryNotSupportedException"></exception>
        /// <exception cref="SettingsException"></exception>
        private void ResolveQuery()
        {
            List<EndpointQueryPart> queryParts = new List<EndpointQueryPart>();

            string[] parts = Query.Split(Settings.EndpointSeparator);

            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part))
                {
                    throw new InvalidOperationException($"Query part can't be null. Maybe you accidentally put two " +
                        $"{Settings.EndpointSeparator} together.");
                }
                
                EndpointQueryPart queryPart;

                try
                {
                    queryPart = new EndpointQueryPart(part, Settings);
                }
                catch (ArgumentNullException ex)
                {
                    throw new ApiParserInternalException(ex);
                }
                catch (ApiParserInternalException ex)
                {
                    throw new ApiParserInternalException(ex);
                }
                catch (QueryParsingException ex)
                {
                    throw new QueryParsingException($"The part {part} could not be parsed correctly.", Query, ex);
                }
                catch (QueryNotSupportedException ex)
                {
                    throw new QueryNotSupportedException($"The part {part} is not supported.", ex);
                }
                catch (SettingsException ex)
                {
                    throw new SettingsException($"The part {part} could not be parsed.", ex);
                }

                queryParts.Add(queryPart);
            }

            QueryParts = queryParts.ToArray();
        }

        /// <exception cref="QueryNotSupportedException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        internal EndpointQuery GetSubQuery(Endpoint endpoint)
        {

            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }


            if (!endpoint.SupportsQuery(this))
            {
                throw new QueryNotSupportedException($"The given endpoint {endpoint.Name} does not support this query " +
                    $"{Query}.");
            }

            string[] parts = Query.Split(Settings.EndpointSeparator);

            string newQuery = string.Join(Settings.EndpointSeparator.ToString(), parts.Skip(endpoint.Parts.Length - 1).ToArray());

            EndpointQuery result;

            // all caught exceptions are internal, because they should've been caught before when creating the original query
            try
            {
                result = new EndpointQuery(newQuery, Settings);
            }
            catch(ArgumentNullException ex)
            {
                throw new ApiParserInternalException(ex);
            }
            catch (ApiParserInternalException ex)
            {
                throw new ApiParserInternalException($"An internal exception occured, while attempting to get the sub query " +
                    $"for endpoint {endpoint.Name} and query {Query}.", ex);
            }
            catch (QueryParsingException ex)
            {
                throw new ApiParserInternalException($"An internal exception occured, while attempting to get the sub query " +
                    $"for endpoint {endpoint.Name} and query {Query}.", ex);
            }
            catch (QueryNotSupportedException ex)
            {
                throw new ApiParserInternalException($"An internal exception occured, while attempting to get the sub query " +
                    $"for endpoint {endpoint.Name} and query {Query}.", ex);
            }
            catch (SettingsException ex)
            {
                throw new ApiParserInternalException($"An internal exception occured, while attempting to get the sub query " +
                    $"for endpoint {endpoint.Name} and query {Query}.", ex);
            }

            return result;
        }
    }
}
