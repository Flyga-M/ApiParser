using Gw2Sharp.WebApi.V2.Clients;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiParser
{
    /// <summary>
    /// Provides utility functions for <see cref="Attribute"/>s.
    /// </summary>
    public static class AttributeUtil
    {
        /// <summary>
        /// Retrieves the <see cref="EndpointPathAttribute.EndpointPath"/> from an <see cref="IEndpointClient"/> subclass.
        /// </summary>
        /// <param name="endpointClient"></param>
        /// <returns>The EndpointPath for the given <paramref name="endpointClient"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="endpointClient"/> is null.</exception>
        /// <exception cref="ArgumentException">If <paramref name="endpointClient"/> is an interface, abstract or not a 
        /// subclass of <see cref="IEndpointClient"/>.</exception>
        /// <exception cref="InvalidOperationException">If the attribute does not exist, or an exception occured while trying to 
        /// retrieve the attribute.</exception>
        public static string GetEndpointPathAttribute(Type endpointClient)
        {
            if (endpointClient == null)
            {
                throw new ArgumentNullException(nameof(endpointClient));
            }
            
            if (endpointClient.IsInterface)
            {
                throw new ArgumentException($"{nameof(endpointClient)} can't be an interface. Given type: {endpointClient}.", nameof(endpointClient));
            }

            if (endpointClient.IsAbstract)
            {
                throw new ArgumentException($"{nameof(endpointClient)} can't be abstract. Given type: {endpointClient}.", nameof(endpointClient));
            }

            if (!typeof(IEndpointClient).IsAssignableFrom(endpointClient))
            {
                throw new ArgumentException($"{nameof(endpointClient)} must be derived from {typeof(IEndpointClient)}. Given type: {endpointClient}.", nameof(endpointClient));
            }

            EndpointPathAttribute endpointPath = GetAttribute<EndpointPathAttribute>(endpointClient);

            return endpointPath.EndpointPath;
        }

        /// <summary>
        /// Wrapper for <see cref="Attribute.GetCustomAttribute(System.Reflection.MemberInfo, Type)"/>. Will throw, if the 
        /// attribute does not exist. 
        /// <inheritdoc cref="Attribute.GetCustomAttribute(System.Reflection.MemberInfo, Type)"/>
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="element"></param>
        /// <returns>The attribute of the <typeparamref name="TAttribute"/> type for the <paramref name="element"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="element"/> is null.</exception>
        /// <exception cref="InvalidOperationException">If no attribute of type <typeparamref name="TAttribute"/> exists on the 
        /// given <paramref name="element"/>, or if <see cref="Attribute.GetCustomAttribute(System.Reflection.MemberInfo, Type)"/> 
        /// throws any exception.</exception>
        public static TAttribute GetAttribute<TAttribute>(Type element) where TAttribute : Attribute
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            TAttribute attribute;

            try
            {
                attribute = (TAttribute)Attribute.GetCustomAttribute(element, typeof(TAttribute));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unable to get attribute of type {typeof(TAttribute)} for " +
                    $"{nameof(element)} of type {element}.", ex);
            }

            if (attribute == null)
            {
                throw new InvalidOperationException($"{nameof(element)} has no attribute of type {typeof(TAttribute)}. Given type: {element}.");
            }

            return attribute;
        }
    }
}
