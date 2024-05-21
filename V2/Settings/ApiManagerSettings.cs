namespace ApiParser.V2.Settings
{
    /// <summary>
    /// Determines how an <see cref="ApiManager"/> manages it's endpoints.
    /// </summary>
    public struct ApiManagerSettings
    {
        /// <summary>
        /// The milliseconds that need to elapse, before an endpoint is updated again.
        /// </summary>
        public int Cooldown;
    }
}
