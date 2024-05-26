using System;

namespace ApiParser.V2.Settings
{
    // TODO: should probably be a class for proper value checking
    // TODO: add default values
    
    /// <summary>
    /// Determines how an <see cref="ApiManager"/> manages it's endpoints.
    /// </summary>
    public struct ApiManagerSettings
    {
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
        public float RealiableApiCutoff;

        /// <summary>
        /// The amount of time a tracked issue stays relevant to the <see cref="IssueTracker"/>.
        /// </summary>
        public TimeSpan IssueDecay;

        /// <summary>
        /// The relative amount of how many api requests the <see cref="IssueTracker"/> must have, before the <see cref="ApiState"/> 
        /// is considered anything other than <see cref="ApiState.Unknown"/>.
        /// </summary>
        public float MeaningfullRequestCountCutoff;
    }
}
