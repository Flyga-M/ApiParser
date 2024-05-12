using Gw2Sharp.WebApi.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Gw2Sharp.WebApi;

namespace ApiParser
{
    /// <summary>
    /// Provides utility functions for <see cref="Gw2Sharp.WebApi.Exceptions"/>.
    /// </summary>
    public static class RequestExceptionUtil
    {
        /// <summary>
        /// Determines whether an <see cref="Exception"/> thrown by the <see cref="IGw2WebApiClient"/> 
        /// is retryable.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns>True, if the <paramref name="exception"/> is either a 
        /// <see cref="TooManyRequestsException"/> or a
        /// <see cref="ServerErrorException"/> or a 
        /// <see cref="ServiceUnavailableException"/>.</returns>
        public static bool IsRetryable(Exception exception)
        {
            return exception is TooManyRequestsException
                || exception is ServerErrorException
                || exception is ServiceUnavailableException;
        }

        /// <summary>
        /// Executes the <paramref name="retryFunction"/>. Will retry an amount of <paramref name="tries"/>, 
        /// if it fails and the exception is retryable. Will wait for the <paramref name="delay"/> amount of milliseconds.
        /// </summary>
        /// <param name="retryFunction"></param>
        /// <param name="delay"></param>
        /// <param name="tries"></param>
        /// <param name="exception"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        public async static Task RetryIfRetryable(Func<CancellationToken, Task> retryFunction, int delay = 15000, int tries = 3, CancellationToken cancellationToken = default)
        {
            bool success = true;

            for (int i = 0; i < tries; i++)
            {
                try
                {
                    await retryFunction(cancellationToken);
                }
                catch (Exception ex)
                {
                    if (!IsRetryable(ex))
                    {
                        return;
                    }
                    success = false;
                }

                if (success)
                {
                    break;
                }

                await Task.Delay(delay);
            }
        }

        /// <summary>
        /// Executes the <paramref name="retryFunction"/>. Will retry an amount of <paramref name="tries"/>, 
        /// if it fails and the exception is retryable. Will wait for the <paramref name="delay"/> amount of milliseconds.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="retryFunction"></param>
        /// <param name="delay"></param>
        /// <param name="tries"></param>
        /// <param name="exception"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TData"></typeparam>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        public async static Task RetryIfRetryable<TData>(TData data, Func<TData, CancellationToken, Task> retryFunction, int delay = 15000, int tries = 3, CancellationToken cancellationToken = default)
        {
            bool success = true;

            for (int i = 0; i < tries; i++)
            {
                try
                {
                    await retryFunction(data, cancellationToken);
                }
                catch (Exception ex)
                {
                    if (!IsRetryable(ex))
                    {
                        return;
                    }
                    success = false;
                }

                if (success)
                {
                    break;
                }

                await Task.Delay(delay);
            }
        }
    }
}
