using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RegistryArtifactTransfer
{
    public static class TaskExtensions
    {
        public static async Task ThrottledWhenAll<T>(
            this IEnumerable<T> source,
            Func<T, Task> operation,
            int maxConcurrency)
        {
            var tasks = new List<Task>();
            using (var throttler = new SemaphoreSlim(maxConcurrency, maxConcurrency))
            {
                foreach (var element in source)
                {
                    await throttler.WaitAsync().ConfigureAwait(false);

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await operation(element).ConfigureAwait(false);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }
    }
}
