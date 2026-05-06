using System;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;

namespace RagebateMobs.Managers
{
    public class TaskManager
    {
        private readonly ManualLogSource _logger;

        public TaskManager(ManualLogSource logger)
        {
            _logger = logger;
        }

        public void SafeFireAndForgetAsync(Func<Task> asyncFunc)
        {
            if (asyncFunc == null)
                return;

            _ = asyncFunc().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    _logger.LogWarning($"[Ragebait] Async task failed: {task.Exception?.InnerException?.Message}");
                }
                else if (task.IsCanceled)
                {
                    _logger.LogDebug("[Ragebait] Async task was cancelled");
                }
            });
        }

        public void SafeFireAndForgetAsync(Func<Task> asyncFunc, SemaphoreSlim sem)
        {
            if (asyncFunc == null)
                return;

            SafeFireAndForgetAsync(async () =>
            {
                if (sem == null)
                {
                    await asyncFunc();
                    return;
                }
                if (!sem.Wait(0))
                {
                    _logger.LogDebug("[Ragebait] LLM semaphore full, skipping request");
                    return;
                }
                try
                {
                    await asyncFunc();
                }
                finally
                {
                    sem.Release();
                }
            });
        }
    }
}
