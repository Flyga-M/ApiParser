using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ApiParser
{
    /// <summary>
    /// Provides utility functions for reflection.
    /// </summary>
    public static class ReflectionUtil
    {
        /// <summary>
        /// Invokes the async method with the <paramref name="methodName"/> on the <paramref name="object"/> 
        /// with the given <paramref name="parameters"/> and returns it's result. Does not catch any potential 
        /// <see cref="Exception"/>s from the async method.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="object"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">If either <paramref name="object"/>, <paramref name="methodName"/> or 
        /// <paramref name="parameters"/> is null.</exception>
        /// <exception cref="InvalidOperationException">If the <paramref name="methodName"/> is ambigious, or the method does 
        /// not exist. Also if the invoking of the method throws a <see cref="TargetException"/>, 
        /// <see cref="TargetInvocationException"/>, <see cref="MethodAccessException"/>, <see cref="InvalidOperationException"/> 
        /// or a <see cref="NotSupportedException"/>. Also, if the result of the method is not of the same <see cref="Type"/> 
        /// as <typeparamref name="TResult"/>.</exception>
        /// <exception cref="ApiParserInternalException">If there is an error with the internal logic of the library.</exception>
        /// <exception cref="ArgumentException">If <paramref name="methodName"/> is empty or whitespace. Also if 
        /// <paramref name="parameters"/> is multi-dimensional.</exception>
        public static async Task<TResult> InvokeAsyncMethodAsync<TResult>(object @object, string methodName, object[] parameters)
        {
            if (@object == null)
            {
                throw new ArgumentNullException(nameof(@object));
            }

            if (methodName == null)
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentException($"{nameof(methodName)} can't be empty or whitespace.", nameof(methodName));
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
