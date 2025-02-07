﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Artemis.Plugins.Devices.iDotMatrix.Extensions
{
    public static class TaskExtensions
    {
        public static async Task<TResult?> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            using var timeoutCancellationTokenSource = new CancellationTokenSource();

            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
            if (completedTask == task)
            {
                timeoutCancellationTokenSource.Cancel();
                return await task;
            }
            else
            {
                return default;
            }
        }
    }
}
