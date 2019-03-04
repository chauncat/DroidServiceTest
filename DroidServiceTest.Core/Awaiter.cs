using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DroidServiceTest.Core
{
    public class Awaiter
    {
        private readonly ILogger _logger = new Logger();
        private readonly SemaphoreSlim _slim = new SemaphoreSlim(1);
        private SemaphoreSlim _pulseLock;
        private Task _wait;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private static readonly Task CompletedTask = Task.FromResult(false);


        public void Cancel()
        {
            _logger.Debug("Start");
            _cts.Cancel();
            _logger.Debug("End");
        }

        public Task Wait(Func<bool> skipWait = null)
        {
            Task wait = null;
            try
            {
                _logger.Debug("Started");
                _slim.Wait(_cts.Token);

                try
                {
                    _logger.Trace("Check Skip action");
                    if (skipWait != null && skipWait())
                    {
                        _logger.Debug("Skipping Send complete");
                        return CompletedTask;
                    }

                    if (_pulseLock == null)
                    {
                        _pulseLock = new SemaphoreSlim(0, 1);
                        _wait = wait = _pulseLock.WaitAsync(_cts.Token);
                        _logger.Debug("Acquire task for signal");
                    }
                    else
                    {
                        _logger.Debug("PulseLock is not null, returning the _wait");
                        wait = _wait;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Error handling waiter", ex);
                }
                finally
                {
                    _slim.Release();
                    _logger.Debug("Lock Released");
                }
            }
            catch (Exception e)
            {
                _logger.Error("Error with blocking", e);
            }

            _logger.Debug($"returning wait: {(wait == null ? "complete task" : "TCS")}");

            return wait ?? CompletedTask;
        }

        public void Pulse()
        {
            _logger.Debug($"Started. Thread ID: {Task.CurrentId.GetValueOrDefault()}, Managed Thread Id {Environment.CurrentManagedThreadId}");
            Task.Run(() =>
            {
                try
                {
                    _logger.Debug($"Starting Task Run. Thread ID: {Task.CurrentId.GetValueOrDefault()}, Managed Thread Id {Environment.CurrentManagedThreadId}");
                    _slim.Wait(_cts.Token);
                    try
                    {
                        _logger.Debug("Have Lock");
                        if (_pulseLock == null) return;

                        _pulseLock.Release();
                        _logger.Debug("Release signaled");
                        _pulseLock = null;
                        _wait = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Error handling waiter", ex);
                    }
                    finally
                    {
                        _slim.Release();
                        _logger.Debug("Have Released");
                    }
                }
                catch (Exception e)
                {
                    _logger.Error("Error with blocking", e);
                }
                finally
                {
                    _logger.Debug($"Finished Task Run. Thread ID: {Task.CurrentId.GetValueOrDefault()}, Managed Thread Id {Environment.CurrentManagedThreadId}");
                }
            }, _cts.Token);
            _logger.Debug($"Finished. Thread ID: {Task.CurrentId.GetValueOrDefault()}, Managed Thread Id {Environment.CurrentManagedThreadId}");
        }
    }
}
