using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ApiParser.Endpoint;
using ApiParser.Settings;
using Gw2Sharp.WebApi.V2.Clients;
using Gw2Sharp.WebApi.V2;

namespace ApiParser
{
    /// <summary>
    /// Provides utility functions for <see cref="EndpointQuery"/>ies.
    /// </summary>
    public static class QueryUtil
    {
        /// <summary>
        /// Applies the given <paramref name="indices"/> to the <paramref name="object"/>.
        /// </summary>
        /// <param name="object"></param>
        /// <param name="indices"></param>
        /// <param name="settings"></param>
        /// <returns>The <see cref="ProcessedIndexData"/> containing the resulting object and information on which 
        /// <paramref name="indices"/> were successfully traversed.</returns>
        /// <exception cref="QueryResolveException">If the indexer for any of the <paramref name="indices"/> throws an 
        /// exception, or if the resulting object is <see langword="null"/>.</exception>
        /// <exception cref="QueryParsingException">If any of the <paramref name="indices"/> contain a variable and the 
        /// resolved variable can't be parsed correctly.</exception>
        /// <exception cref="SettingsException">If any of the <paramref name="indices"/> contain a variable and the 
        /// resolved variable is not of the type that the <see cref="ParseSettings"/> IndexConverter of the 
        /// <paramref name="indices"/> promised.</exception>
        public static async Task<ProcessedIndexData> ResolveIndicesAsync(object @object, EndpointQueryIndex[] indices, QuerySettings settings)
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
        /// Applies the given <paramref name="subQuery"/> to the <paramref name="object"/>.
        /// </summary>
        /// <param name="object"></param>
        /// <param name="subQuery"></param>
        /// <param name="settings"></param>
        /// <returns>The resulting object.</returns>
        /// <exception cref="QueryResolveException">If any <see cref="EndpointQueryPart"/> is ambigiuous, or if the property 
        /// targeted by any <see cref="EndpointQueryPart"/> does not exist, or if the resulting object is 
        /// <see langword="null"/>, or if the indexer for any of the indices throws an 
        /// exception, or if not all indices can be resolved.</exception>
        /// <exception cref="QueryParsingException">If any of the indices contain a variable and the 
        /// resolved variable can't be parsed correctly.</exception>
        /// <exception cref="SettingsException">If any of the indices contain a variable and the 
        /// resolved variable is not of the type that the <see cref="ParseSettings"/> IndexConverter of the 
        /// <paramref name="subQuery"/> promised.</exception>
        public static async Task<object> ResolveSubQueryAsync(object @object, EndpointQuery subQuery, QuerySettings settings)
        {
            object result = @object;

            foreach (EndpointQueryPart queryPart in subQuery.QueryParts)
            {
                string property = queryPart.EndpointName;

                PropertyInfo propertyInfo;

                try
                {
                    propertyInfo = result.GetType().GetProperty(property);
                }
                catch (AmbiguousMatchException ex)
                {
                    throw new QueryResolveException($"Unable to resolve sub query {subQuery}, because the query part {queryPart} " +
                        $"is ambigiuous.", ex);
                }

                if (propertyInfo == null)
                {
                    throw new QueryResolveException($"Unable to resolve sub query at part {queryPart.EndpointName}. The " +
                        $"given property {property} could not be found.");
                }

                result = propertyInfo.GetValue(result);

                if (result == null)
                {
                    throw new QueryResolveException($"Unable to resolve sub query at part {queryPart.EndpointName}. The " +
                        $"given property {property} could be found, but is null.");
                }

                if (queryPart.Enumerate)
                {
                    ProcessedIndexData resolvedIndices = await ResolveIndicesAsync(result, queryPart.Indices, settings);
                    
                    if (!resolvedIndices.Completed)
                    {
                        throw new QueryResolveException($"Object of type {result.GetType()} has no indexer for " +
                        $"{resolvedIndices.RemainingIndices.First().IndexType}.");
                    }

                    result = resolvedIndices.Resolved;
                }
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiClient"></param>
        /// <param name="query"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// <exception cref="QueryResolveException">If <see cref="EndpointQueryPart.EndpointName"/> is <see langword="null"/> 
        /// for any <see cref="EndpointQueryPart"/> in <paramref name="query"/>, or if any <see cref="EndpointQueryPart"/> 
        /// is ambigiuous, or if the property targeted by any <see cref="EndpointQueryPart"/> does not exist, or if 
        /// the resulting object is <see langword="null"/>, or if the indexer for any of the indices throws an 
        /// exception.</exception>
        /// <exception cref="QueryParsingException">If the <paramref name="query"/> contains a variable and the 
        /// resolved variable can't be parsed correctly.</exception>
        /// <exception cref="SettingsException">If the <paramref name="query"/> contains a variable and the converted value of 
        /// the resolved variable is not of the type that the <see cref="ParseSettings"/> IndexConverter of 
        /// the <paramref name="query"/> promised.</exception>
        public static async Task<ProcessedQueryData[]> ProcessQueryAsync(IGw2WebApiV2Client apiClient, EndpointQuery query, QuerySettings settings)
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
                    ProcessedIndexData indexData = await ResolveIndicesAsync(resolved, part.Indices, settings);

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
    }
}
