using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ApiParser
{
    public static class ReflectionUtil
    {
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static bool IsGenericType(object @object, Type genericType)
        {
            if (@object == null)
            {
                throw new ArgumentNullException(nameof(@object));
            }

            if (genericType == null)
            {
                throw new ArgumentNullException(nameof(@genericType));
            }

            Type actualType;

            try
            {
                actualType = @object.GetType().GetGenericTypeDefinition();
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException($"Given object of type {@object.GetType()} is not generic.", ex);
            }
            catch (NotSupportedException ex)
            {
                throw new InvalidOperationException($"Unable to determine whether the object of the given type " +
                    $"{@object.GetType()} is of generic type {genericType}.", ex);
            }

            return actualType == genericType;
        }
        
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static Type[] GetGenericArguments(object @object)
        {
            if (@object == null)
            {
                throw new ArgumentNullException(nameof(@object));
            }

            Type[] result;

            try
            {
                result = @object.GetType().GetGenericArguments();
            }
            catch (NotSupportedException ex)
            {
                throw new InvalidOperationException($"Unable to retrieve generic arguments for object of " +
                    $"type {@object.GetType()}.", ex);
            }
            
            return result;
        }

        /// <summary>
        /// Does not catch potential exceptions from the async method.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ApiParserInternalException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static async Task<TResult> InvokeAsyncMethodAsync<TResult>(object @object, string methodName, object[] parameters)
        {
            if (@object == null)
            {
                throw new ArgumentNullException(nameof(@object));
            }

            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            MethodInfo method;

            try
            {
                method = @object.GetType().GetMethod(methodName, parameters.Select(parameter => parameter.GetType()).ToArray());
            }
            catch (AmbiguousMatchException ex)
            {
                throw new InvalidOperationException($"Method name {methodName} is ambigiuous.", ex);
            }
            catch (ArgumentNullException ex)
            {
                throw new ApiParserInternalException("An internal exception occured while attempting to get async " +
                    $"method {methodName}.", ex);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"{nameof(parameters)} can't be multi-dimensional.", nameof(parameters), ex);
            }
            
            if (method == null)
            {
                throw new InvalidOperationException($"Can't invoke {methodName} on object with type {@object.GetType()}. " +
                    $"No method titled \"{methodName}\" could be found.");
            }

            Task task;

            try
            {
                task = (Task)method.Invoke(@object, parameters);
            }
            catch (TargetException ex)
            {
                throw new InvalidOperationException($"Unable to invoke {methodName} on object with type {@object.GetType()}.", ex);
            }
            catch (ArgumentException ex)
            {
                throw new ApiParserInternalException($"An internal exception occured while attempting to invoke async method " +
                    $"{methodName}.", ex);
            }
            catch (TargetInvocationException ex)
            {
                throw new InvalidOperationException($"Unable to invoke {methodName} on object with type {@object.GetType()}.", ex);
            }
            catch (TargetParameterCountException ex)
            {
                throw new ApiParserInternalException($"An internal exception occured while attempting to invoke async method " +
                    $"{methodName}.", ex);
            }
            catch (MethodAccessException ex)
            {
                throw new InvalidOperationException($"Unable to invoke {methodName} on object with type {@object.GetType()}.", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException($"Unable to invoke {methodName} on object with type {@object.GetType()}.", ex);
            }
            catch (NotSupportedException ex)
            {
                throw new InvalidOperationException($"Unable to invoke {methodName} on object with type {@object.GetType()}.", ex);
            }

            // purposefully no try/catch here, so the individual exceptions can be caught more easily 
            await task;

            PropertyInfo resultProperty;

            try
            {
                resultProperty = task.GetType().GetProperty("Result");
            }
            catch (AmbiguousMatchException ex)
            {
                throw new ApiParserInternalException($"An internal exception occured while attempting to get " +
                    $"result from executed task.", ex);
            }
            catch (ArgumentNullException ex)
            {
                throw new ApiParserInternalException($"An internal exception occured while attempting to get " +
                    $"result from executed task.", ex);
            }

            object result = resultProperty.GetValue(task);

            if (!(result is TResult data))
            {
                throw new InvalidOperationException($"Invoking of async method failed. Result is not " +
                    $"{typeof(TResult)}. Given type: {result.GetType()}.");
            }

            return data;
        }
    }
}
