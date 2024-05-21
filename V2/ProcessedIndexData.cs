﻿using ApiParser.V2.Endpoint;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiParser.V2
{
    public class ProcessedIndexData
    {
        public readonly object Resolved;
        
        public readonly EndpointQueryIndex[] TraversedIndices;

        public readonly EndpointQueryIndex[] RemainingIndices;

        /// <exception cref="ArgumentNullException">If <paramref name="resolved"/> is null.</exception>
        /// <exception cref="ArgumentException">If <paramref name="remainingIndices"/> and <paramref name="traversedIndices"/> 
        /// are both null or empty.</exception>
        public ProcessedIndexData(object resolved, IEnumerable<EndpointQueryIndex> traversedIndices, IEnumerable<EndpointQueryIndex> remainingIndices)
        {
            if (resolved == null)
            {
                throw new ArgumentNullException(nameof(resolved));
            }

            if (traversedIndices == null)
            {
                traversedIndices = Array.Empty<EndpointQueryIndex>();
            }

            if (remainingIndices == null)
            {
                remainingIndices = Array.Empty<EndpointQueryIndex>();
            }

            if (!traversedIndices.Any() && !remainingIndices.Any())
            {
                throw new ArgumentException($"Either {nameof(traversedIndices)} or {nameof(remainingIndices)} must at " +
                    $"least have one element.", nameof(traversedIndices));
            }

            Resolved = resolved;
            TraversedIndices = traversedIndices.ToArray();
            RemainingIndices = remainingIndices.ToArray();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            char indexOpen = '[';
            char indexClose = ']';

            if (TraversedIndices.Any())
            {
                indexOpen = TraversedIndices.First().Settings.IndexOpen;
                indexClose = TraversedIndices.First().Settings.IndexClose;
            }
            else if (RemainingIndices.Any())
            {
                indexOpen = RemainingIndices.First().Settings.IndexOpen;
                indexClose = RemainingIndices.First().Settings.IndexClose;
            }

            string traversedIndices = "";
            string remainingIndices = "";

            if (TraversedIndices.Any())
            {
                traversedIndices = $"{indexOpen}{string.Join<EndpointQueryIndex>($"{indexClose}{indexOpen}", TraversedIndices)}{indexClose}";
            }

            if (RemainingIndices.Any())
            {
                remainingIndices = $"{indexOpen}{string.Join<EndpointQueryIndex>($"{indexClose}{indexOpen}", RemainingIndices)}{indexClose}";
            }

            return $"{traversedIndices}///{remainingIndices}";
        }
    }
}
