using System.Threading.Tasks;

namespace ApiParser.V2.Settings
{
    /// <summary>
    /// Represents a class, that is able to resolve a variable inside an <see cref="EndpointQuery"/>.
    /// </summary>
    public interface IQueryVariableResolver
    {
        /// <summary>
        /// Resolves the <paramref name="variable"/> into a string representation of the value.
        /// </summary>
        /// <param name="variable"></param>
        /// <returns>A <see cref="Task"/> representing the string representation of the value of the <paramref name="variable"/>. 
        /// Or <see langword="null"/>, if the <paramref name="variable"/> could not be resolved.</returns>
        Task<string> Resolve(string variable);
    }
}
