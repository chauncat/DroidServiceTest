using System;
using System.Threading;
using System.Threading.Tasks;

namespace DroidServiceTest.Core
{
    public delegate void TimerCallback(object state);

    public sealed class Timer : CancellationTokenSource, IDisposable
    {
        /// <inheritdoc />
        /// <summary>
        /// Starts a task that delays for 'dueTime' and then repeats every 'period'
        /// </summary>
        /// <param name="callback">The callback to execute</param>
        /// <param name="state">Data to be passed to the callback</param>
        /// <param name="dueTime">Initial delay in milliseconds before starting the task</param>
        /// <param name="period">How often in milliseconds to repeat the task</param>
        public Timer(TimerCallback callback, object state, int dueTime, int period)
        {
            Task.Delay(dueTime, Token).ContinueWith(async (t, s) =>
                {
                    var tuple = (Tuple<TimerCallback, object>)s;

                    while (!IsCancellationRequested)
                    {
                        // We don't want to await this
                        Task.Run(() => tuple.Item1(tuple.Item2)); // Item1 == callback, Item2 == state

                        try
                        {
                            await Task.Delay(period, Token).ConfigureAwait(false);
                        }
                        catch (TaskCanceledException)
                        {
                            break;
                        }
                    }

                },
                Tuple.Create(callback, state),  
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default).ConfigureAwait(false);
        }

        public new void Dispose()
        {
            Cancel();
            base.Dispose();
        }
    }
}