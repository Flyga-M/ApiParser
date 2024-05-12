using ApiParser.V2.Settings;
using Gw2Sharp.WebApi;

namespace ApiParser
{
    public class ApiManager
    {
        public readonly V2.ApiManager V2;
        
        // TODO: i guess there needs to be a settings interface or something?
        // or settings contain .V2?
        public ApiManager(IGw2WebApiClient client, ApiManagerSettings settings)
        {
            V2 = new V2.ApiManager(client.V2, settings);
        }
    }
}
