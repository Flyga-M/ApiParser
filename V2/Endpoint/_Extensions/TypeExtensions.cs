﻿using System;
using System.Linq;
using System.Reflection;

namespace ApiParser.V2.Endpoint
{
    public static class TypeExtensions
    {
        // https://stackoverflow.com/a/55457150
        /// <summary>
        /// Returns the indexer that takes the given <see cref="Type"/> of <paramref name="arguments"/>.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="arguments"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <returns>The indexer that takes the given <see cref="Type"/> of <paramref name="arguments"/>, or  
        /// <see langword="null"/> if none is found.</returns>
        public static PropertyInfo GetIndexer(this Type type, params Type[] arguments)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            if (arguments.Length == 0)
            {
                throw new ArgumentException("Arguments must have at least one element.", nameof(arguments));
            }
            
            return type.GetProperties().FirstOrDefault(property => property.GetIndexParameters().Select(indexParameter => indexParameter.ParameterType).SequenceEqual(arguments));
        }
    }
}
