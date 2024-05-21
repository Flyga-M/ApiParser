using ApiParser.V2.Settings;
using Gw2Sharp.WebApi;
using System;

namespace ApiParser
{
    /// <summary>
    /// Manages the api endpoints and their data.
    /// </summary>
    public class ApiManager
    {
        /// <summary>
        /// The <see cref="V2.ApiManager"/> to access the endpoints for v2 of the gw2 api.
        /// </summary>
        public readonly V2.ApiManager V2;
        
        // TODO: i guess there needs to be a settings interface or something?
        // or settings contain .V2?

        /// <exception cref="ArgumentNullException">If <paramref name="client"/> is null.</exception>
        public ApiManager(IGw2WebApiClient client, ApiManagerSettings settings)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }
            
            V2 = new V2.ApiManager(client.V2, settings);
        }
    }
}
