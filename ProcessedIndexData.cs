using ApiParser.Endpoint;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiParser
{
    /// <summary>
    /// Contains data of processed indices.
    /// </summary>
    public class ProcessedIndexData
    {
        /// <summary>
        /// The object, that was resolved via the <see cref="TraversedIndices"/>.
        /// </summary>
        public readonly object Resolved;
        
        /// <summary>
        /// The indices that were traversed, to get the <see cref="Resolved"/> object.
        /// </summary>
        public readonly EndpointQueryIndex[] TraversedIndices;

        /// <summary>
        /// The indices that can't be resolved directly on the <see cref="Resolved"/> object.
        /// </summary>
        public readonly EndpointQueryIndex[] RemainingIndices;

        /// <summary>
        /// Determines whether there are no <see cref="RemainingIndices"/> left, after the index data was processed.
        /// </summary>
        public bool Completed => RemainingIndices == null || RemainingIndices.Length == 0;

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
