using System;
using System.Collections.Generic;
using DroidServiceTest.Core.Logging;
using DroidServiceTest.Core.Logging.Logger;
using DroidServiceTest.Core.StoreAndForward;

namespace DroidServiceTest.Core
{
    public class TwmTestServiceProxy
    {
        public string SendMessage(string request)
        {
            return String.Empty;
        }
    }

    public sealed class TwmTestService
    {
        private static readonly ILogger Logger;
        private static readonly TimeSpan SnFRetryInterval = new TimeSpan(0, 0, 15);
        public delegate void SendComplete(Object src, AsyncServiceResults args);
        public static event SendComplete SendCompleteEventHandler;
        private readonly ServiceProxy<TwmTestServiceProxy> _proxy;
        private TwmTestServiceProxy _session;
        private bool _disposed;
        private Timer _timer;
        private List<string> _subscribers = new List<string>();
        private List<string> _publishers = new List<string>();

        static TwmTestService()
        {
            Logger = LogFactory.Instance.GetLogger<TwmTestService>();
        }

        public TwmTestService()
        {
            // Wire up an SnF proxy and delegate to map the SnF CallComplete event to a Dms ArchiveDocumentComplete event
            _proxy = new ServiceProxy<TwmTestServiceProxy>(ResolveProxy, SnFRetryInterval);
            _proxy.CallCompleteEventHandler += proxiedSerivce_CallCompleteEventHandler;

            _subscribers.Add("Sub Item One");
            _subscribers.Add("Sub Item Two");

            CreateTimer();
        }

        ~TwmTestService()
        {
            Dispose(false);
        }

        private void CreateTimer()
        {
            if (_timer != null) DestroyTimer();

            _timer = new Timer(TimerCallbackHandler, null, 10000, 10000);
        }

        private void DestroyTimer()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        private void TimerCallbackHandler(object state)
        {
            DestroyTimer();

            try
            {
                Logger.Debug("*** SUBSCRIBERS LIST ***");
                foreach (var subscriber in _subscribers)
                {
                    Logger.Debug($"sub={subscriber}");
                }

                Logger.Debug("*** PUBLISHERS LIST ***");
                foreach (var publisher in _publishers)
                {
                    Logger.Debug($"pub={publisher}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message, ex);
            }

            CreateTimer();
        }


        public void CauseError()
        {
            _proxy.CauseError();
        }

        #region DMS API implementation

        public void SendMessage(string request)
        {
            _proxy.CallService("SendMessage", true, request);
        }

        #endregion

        #region helper logic for wiring up the SnF service proxy

        private TwmTestServiceProxy ResolveProxy()
        {
            return _session ?? (_session = new TwmTestServiceProxy());
        }

        void proxiedSerivce_CallCompleteEventHandler(object src, ServiceProxyEventArgs args)
        {
            var result = new AsyncServiceResults
            {
                Exception = args.ServiceException,
                Results = args.ReturnValue,
                Success = (args.Status == ServiceProxyCallStatus.CompletedSuccessfully) ? true : false
            };
            switch (args.MethodName)
            {
                case "SendMessage":
                    FireSendCompleted(result);
                    break;
            }
        }

        private void FireSendCompleted(AsyncServiceResults results)
        {
            SendCompleteEventHandler?.Invoke(this, results);
        }

        #endregion

        #region SnF API implementation

        public void StartWorker()
        {
            _proxy?.StartWorker();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async void SendPendingMessages()
        {
            if (_proxy != null)
            {
                await _proxy.SendPendingMessages().ConfigureAwait(false);
            }
        }

        public void PurgePendingMessages()
        {
            if (_proxy != null)
            {
                _proxy.PurgePendingMessages();
            }
        }

        private void Dispose(bool disposing)
        {
            // check to see if Dispose has already been called.
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_proxy != null)
                    {
                        _proxy.Dispose();
                    }
                }
                _disposed = true;
            }
        }

        #endregion
    }

    public class AsyncServiceResults : EventArgs
    {
        public Exception Exception { get; set; }
        public Object Results { get; set; }

        public bool Success { get; set; }
    }
}