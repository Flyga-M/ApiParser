using System.Threading.Tasks;

namespace ApiParser.V2.Settings.Default
{
    public class QueryVariableResolver : IQueryVariableResolver
    {
        /// <inheritdoc/>
        public Task<string> Resolve(string variable)
        {
            return null;
        }
    }
}
