using System;

namespace ApiParser.V2.Settings
{
    // TODO: should probably be a class for proper value checking
    
    /// <summary>
    /// Determines how an <see cref="ApiManager"/> manages it's endpoints.
    /// </summary>
    public struct ApiManagerSettings
    {
        /// <summary>
        /// The default <see cref="ApiManagerSettings"/>.
        /// </summary>
        public static readonly ApiManagerSettings Default = new ApiManagerSettings(null, null, null, null, null);
        
        /// <summary>
        /// The milliseconds that need to elapse, before an endpoint is updated again.
        /// </summary>
        public int Cooldown;

        /// <summary>
        /// The maximum amount of api requests the <see cref="IssueTracker"/> keeps track of.
        /// </summary>
        public int IssueTrackerSize;

        /// <summary>
        /// The relative amount of how many issues are considered okay for the <see cref="IssueTracker"/> to consider 
        /// the <see cref="ApiState"/> as reliable.
        /// </summary>
        public float ReliableApiCutoff;

        /// <summary>
        /// The amount of time a tracked api request stays relevant to the <see cref="IssueTracker"/>.
        /// </summary>
        public TimeSpan IssueDecay;

        /// <summary>
        /// The relative amount of how many api requests the <see cref="IssueTracker"/> must have, before the <see cref="ApiState"/> 
        /// is considered anything other than <see cref="ApiState.Unknown"/>.
        /// </summary>
        public float MeaningfullRequestCountCutoff;

        /// <inheritdoc/>
        public ApiManagerSettings(int? cooldown = null, int? issueTrackerSize = null, float? reliableApiCutoff = null, TimeSpan? issueDecay = null, float? meaningfullRequestCountCutoff = null)
        {
            Cooldown = cooldown ?? 120_000;
            IssueTrackerSize = issueTrackerSize ?? 6;
            ReliableApiCutoff = reliableApiCutoff ?? (1.0f/6.0f);
            IssueDecay = issueDecay ?? TimeSpan.FromSeconds(60);
            MeaningfullRequestCountCutoff = meaningfullRequestCountCutoff ?? (3.0f/6.0f);
        }
    }
}
