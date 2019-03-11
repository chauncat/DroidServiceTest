using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.App;
using DroidServiceTest.Core;
using DroidServiceTest.Core.Logging;
using DroidServiceTest.Core.Logging.Logger;

namespace ServiceTest.Droid
{
    [IntentFilter(new[] { "ServiceTest.Droid.DroidMessageBroadcastReceiver" })]
    class DroidMessageReceiver : BroadcastReceiver
    {
        private readonly ILogger _logger = LogFactory.Instance.GetLogger<DroidMessageReceiver>();
        private readonly ConcurrentQueue<Intent> _jobs = new ConcurrentQueue<Intent>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Awaiter _awaiter = new Awaiter();
        public event IntentHandlerDelegate IntentHandler;

        public DroidMessageReceiver()
        {
            _cts.Token.Register(() =>
            {
                _logger.Debug("Canceling");
                _awaiter.Cancel();
            });
            StartWorker();
        }

        private void StartWorker()
        {
            _logger.Debug("Started");
            Task.Run(async () =>
            {
                _logger.Debug("Start Loop");
                try
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        _logger.Debug("Running");
                        ProcessNextIntent();

                        await _awaiter.Wait(() => _jobs.Count != 0).ConfigureAwait(false);

                        _logger.Debug("Done Waiting");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message, ex);
                    // TODO: Keep this? Remove it? Love it? Hate it?
                }

                _logger.Debug("Not Running");
            }, _cts.Token).ConfigureAwait(false);
            _logger.Debug("Finished");
        }

        private void ProcessNextIntent()
        {
            _logger.Debug("Started");
            var succeed = _jobs.TryDequeue(out var intent);
            if (!succeed)
            {
                _logger.Debug("Dequeue returned False");
                return;
            }

            var id = Guid.NewGuid();
            _logger.Debug("Have Job start Processing: " + id);
            if (intent?.Action != null)
            {
                var bundle = GetResultExtras(true);
                bundle.PutInt("resultCode", (int)Result.Ok);
                var clientGuid = "error";
                var action = "error";

                try
                {
                    clientGuid = intent.GetStringExtra(Constants.ClientSessionGuid) ?? "no guid";
                    action = intent.GetStringExtra(Constants.Action) ?? "no action";
                }
                catch (Exception e)
                {
                    _logger.Error("Unable to get id and action", e);
                }

                _logger.Debug($"action received is: {action}, id: {id}, : client session: {clientGuid}");

                if (IntentHandler == null) return;
                try
                {
                    IntentHandler(intent);
                }
                catch (Exception ex)
                {
                    _logger.Error($"IntentHandler Unknown Exception. Id = {id}", ex);
                }
                _logger.Debug($"IntentHandler called: {id}");
            }
            else
            {
                _logger.Warn("Intent.action is null, exiting onReceive: " + id);
            }
        }

        public override void OnReceive(Context context, Intent intent)
        {
            var id = "error";
            var action = "error";

            try
            {
                id = intent?.GetStringExtra(Constants.ClientSessionGuid) ?? "no guid";
                action = intent?.GetStringExtra(Constants.Action) ?? "no action";
            }
            catch (Exception e)
            {
                _logger.Error("Unable to get id and action", e);
            }

            _logger.Debug($"Started: Client Session: {id} / Action: {action} / Jobs: {_jobs.Count}");
            _jobs.Enqueue(intent);
            _awaiter.Pulse();

            if (action == "cleanup")
            {
                _logger.Debug($"Calling GarabeCollector: {id} / Action: {action} / Jobs: {_jobs.Count}");
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            _logger.Debug($"Finished: Client Session: {id} / Action: {action} / Jobs: {_jobs.Count}");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _logger.Debug("Disposing");
                _cts.Cancel(true);
            }
            base.Dispose(disposing);
        }
    }
}