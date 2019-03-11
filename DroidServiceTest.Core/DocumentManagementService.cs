using System;
using DroidServiceTest.Core.Ioc;
using DroidServiceTest.Core.Logging;
using DroidServiceTest.Core.Logging.Logger;
using DroidServiceTest.Core.StoreAndForward;

namespace DroidServiceTest.Core
{
    public sealed class DocumentManagementService
    {
        private static readonly ILogger Logger = LogFactory.Instance.GetLogger<DocumentManagementService>();
        private static readonly TimeSpan SnFRetryInterval = new TimeSpan(0, 0, 15);
        public event AsyncWebServiceOperationCompleted ArchiveDocumentCompleted;
        private readonly ServiceProxy<DocumentManagementServiceProxy> _proxy;
        private DocumentManagementServiceProxy _dms;
        private bool _disposed;

        public DocumentManagementService()
        {
            // Wire up an SnF proxy and delegate to map the SnF CallComplete event to a Dms ArchiveDocumentComplete event
            _proxy = new ServiceProxy<DocumentManagementServiceProxy>(ResolveDmsProxy, SnFRetryInterval);
            _proxy.CallCompleteEventHandler += HandleDmsArchiveDocumentServiceProxyCallComplete;
        }

        ~DocumentManagementService()
        {
            Dispose(false);
        }

        public void CauseError()
        {
            _proxy.CauseError();
        }

        #region DMS API implementation

        public void ArchiveDocument(string corpCode)
        {
            _proxy.CallService("ArchiveDocument", true, corpCode);
        }

        #endregion

        #region helper logic for wiring up the SnF service proxy

        private DocumentManagementServiceProxy ResolveDmsProxy()
        {
            return _dms ?? (_dms = new DocumentManagementServiceProxy());
        }

        private void HandleDmsArchiveDocumentServiceProxyCallComplete(object src, ServiceProxyEventArgs args)
        {
            var results = new AsyncWebServiceResults
            {
                Exception = args.ServiceException,
                Results = args.ReturnValue,
                Success = (args.Status == ServiceProxyCallStatus.CompletedSuccessfully),
                CallParameters = args.CallParameters
            };
            FireArchiveDocumentCompleted(results);
        }

        private void FireArchiveDocumentCompleted(AsyncWebServiceResults results)
        {
            Logger.Debug("Started");

            if (ArchiveDocumentCompleted != null)
            {
                Logger.Debug("ArchiveDocumentCompleted is not null, calling delegate.");
                ArchiveDocumentCompleted(this, results);
            }
            Logger.Debug("Finished");
        }

        #endregion

        #region SnF API implementation

        public void StartWorker()
        {
            if (_proxy != null)
            {
                _proxy.StartWorker();
            }
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
                        //_proxy = null;
                    }
                }
                _disposed = true;
            }
        }

        #endregion
    }

    public delegate void AsyncWebServiceOperationCompleted(object source, AsyncWebServiceResults args);

    public class AsyncWebServiceResults : EventArgs
    {
        public Exception Exception { get; set; }
        public Object Results { get; set; }
        public bool Success { get; set; }
        public Object[] CallParameters { get; set; }
    }
}