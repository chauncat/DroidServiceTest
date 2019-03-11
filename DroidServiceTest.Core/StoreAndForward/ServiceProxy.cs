using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DroidServiceTest.Core.Logging;
using DroidServiceTest.Core.Logging.Logger;
using DroidServiceTest.Core.StoreAndForward.Model;
using Newtonsoft.Json;

namespace DroidServiceTest.Core.StoreAndForward
{
    public class ServiceProxy<T> : IDisposable
    {
        #region Delegates

        public delegate void CallComplete(Object src, ServiceProxyEventArgs args);

        public event CallComplete CallCompleteEventHandler;

        public delegate T ProxiedServiceDelegate();

        public ProxiedServiceDelegate CreateService;

        #endregion

        #region Private Member Variables

        private readonly ILogger _logger = LogFactory.Instance.GetLogger<ServiceProxy<T>>();
        private readonly TimeSpan _retryTimeSpan;
        private CancellationTokenSource _workerToken;
        private bool _disposed;
        private Task _workerProcTask;
        private IPlatformService _platformService;
        private int _servcieCount;
        private static int _typeCount;

        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor.  
        /// </summary>
        /// <param name="theDelegate">theDelegate will throw a ArgumentNullException if null.</param>
        /// <param name="ts">Retry timer interval.  If null timespan will be 30 seconds.</param>
        public ServiceProxy(ProxiedServiceDelegate theDelegate, TimeSpan ts)
        {
            if (theDelegate == null)
            {
                throw new ArgumentNullException("theDelegate", "ProxiedServiceDelegate cannot be null.");
            }
            CreateService = theDelegate;
            _retryTimeSpan = ts;
            _platformService = Ioc.Container.Instance.Resolve<IPlatformService>();
            _typeCount++;
            _servcieCount = _typeCount;
        }

        public bool WorkerIsRunning => (_workerProcTask != null && !_workerProcTask.IsCompleted);

        /// <summary>
        /// Starts the task that will makes sure pending messages 
        /// are sent. 
        /// </summary>
        /// <returns>Returns true if task is running</returns>
        public bool StartWorker()
        {
            var svc = ProxiedService.IsNull() ? "Null" : ProxiedService.GetType().FullName;
            _logger.Debug("Starting worker thread: " + svc);
            try
            {
                if (!WorkerIsRunning)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            _logger.Debug($"Starting task. Id={ Task.CurrentId.GetValueOrDefault() }, Managed Thread Id {Environment.CurrentManagedThreadId}");

                            _workerToken = new CancellationTokenSource();
                            _workerProcTask = WorkerProcAsync(_workerToken.Token);
                            await _workerProcTask.ConfigureAwait(false);

                            _logger.Debug($"Task Status: {_workerProcTask.Status}");
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.Debug("WorkProc has been Canceled");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to start worker: " + ex.Message, ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Stop Worker running
        /// </summary>
        /// <returns></returns>
        public void StopWorker()
        {
            _logger.Debug("Started");
            if (_workerToken == null)
            {
                _logger.Debug("No Token Finished");
                return;
            }

            _workerToken.Cancel();
            _workerToken = null;
            _logger.Debug("Finished");
        }

        /// <summary>
        /// Proxied service property
        /// </summary>
        public T ProxiedService
        {
            get
            {
                if (CreateService != null)
                {
                    T ret = CreateService();

                    // Testing if T is null-able and is set to null
                    if (ret.IsNull()) return ret;

                    var value = ret.GetType().GetTypeInfo().Assembly.GetName().Name;
                    Assembly.Load(new AssemblyName(value));
                    _logger.Debug($"Assembly, {value}, has been loaded.");
                    return ret;
                }

                _logger.Debug("CreateServcie is null.");
                return default(T);
            }
        }

        /// <summary>
        /// Returns the number of pending messages (unsent)
        /// </summary>        
        public int PendingMessageCount
        {
            get
            {
                var count = 0;

                try
                {
                    count = Messages.Instance.GetPendingMessages().Count;
                    _logger.Debug("Pending message count " + count);
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to retrieve pending message count " + ex.Message, ex);
                }

                return count;
            }
        }

        /// <summary>
        /// Will return a identifier fro a recurring message.
        /// </summary>
        public Guid NewRecurringId => Guid.NewGuid();

        /// <summary>
        /// Purges pending (unsent) messages
        /// </summary>
        /// <returns>Number of messages purged</returns>
        public int PurgePendingMessages()
        {
            var ret = 0;

            try
            {
                ret = Messages.Instance.GetPendingMessages().Count;
                _logger.Debug("Purging pending messages " + ret);
                Messages.Instance.DeletePendingMessages();
            }
            catch (Exception e)
            {
                _logger.Debug("Failed to purge pending messages " + e.Message, e);
            }

            return ret;
        }

        /// <summary>
        /// Invokes method in proxied service passing parameters specified.
        /// Optionally, persists and retries the method until successful.
        /// </summary>
        /// <param name="methodName">Name of method in proxied service to invoke</param>
        /// <param name="parameters">Optional list of parameters to include in method invocation</param>
        /// <returns>If call is recurring will return an id that can be used to stop to recurrence; Otherwise will return null.</returns>
        public Guid CallService(String methodName, params Object[] parameters)
        {
            _logger.Debug("CallService invoked, methodName is: " + methodName);
            return CallService(ProxiedService.GetType().GetRuntimeMethod(methodName, Util.GetTypes(parameters)), false, Guid.Empty, parameters);
        }

        /// <summary>
        /// Invokes method in proxied service passing parameters specified.
        /// Optionally, persists and retries the method until successful.
        /// </summary>
        /// <param name="methodName">Name of method in proxied service to invoke</param>
        /// <param name="recurringId">Will mark a recurring message with and Id that can be used to stop the call from recurring.
        /// Method must have RecurringAttribute or id will not be record.  Pass Guid.Empty if you don't wish to set this value.</param>
        /// <param name="parameters">Optional list of parameters to include in method invocation</param>
        /// <returns>If call is recurring will return an id that can be used to stop to recurrence; Otherwise will return null.</returns>
        public Guid CallService(String methodName, Guid recurringId, params Object[] parameters)
        {
            _logger.Debug("CallService invoked, methodName is: " + methodName + ", Guid: " + recurringId);
            return CallService(ProxiedService.GetType().GetRuntimeMethod(methodName, Util.GetTypes(parameters)), false, recurringId, parameters);
        }

        /// <summary>
        /// Invokes method in proxied service passing parameters specified.
        /// Optionally, persists and retries the method until successful.
        /// </summary>
        /// <param name="methodName">Name of method in proxied service to invoke</param>
        /// <param name="wifiOnly">Indicates if message is to be sent via WiFi only</param>
        /// <param name="parameters">Optional list of parameters to include in method invocation</param>
        /// <returns>If call is recurring will return an id that can be used to stop to recurrence; Otherwise will return null.</returns>
        public Guid CallService(String methodName, bool wifiOnly, params Object[] parameters)
        {
            _logger.Debug("CallService invoked, methodName is: " + methodName + ", wifiOnly: " + wifiOnly);
            return CallService(ProxiedService.GetType().GetRuntimeMethod(methodName, Util.GetTypes(parameters)), wifiOnly, Guid.Empty, parameters);
        }

        /// <summary>
        /// Invokes method in proxied service passing parameters specified.
        /// Optionally, persists and retries the method until successful.
        /// </summary>
        /// <param name="methodName">Name of method in proxied service to invoke</param>
        /// <param name="wifiOnly">Indicates if message is to be sent via WiFi only</param>
        /// <param name="recurringId">Will mark a recurring message with and Id that can be used to stop the call from recurring.
        /// Method must have RecurringAttribute or id will not be record.  Pass Guid.Empty if you don't wish to set this value.</param>
        /// <param name="parameters">Optional list of parameters to include in method invocation</param>
        /// <returns>If call is recurring will return an id that can be used to stop to recurrence; Otherwise will return null.</returns>
        public Guid CallService(String methodName, bool wifiOnly, Guid recurringId, params Object[] parameters)
        {
            _logger.Debug("CallService invoked, methodName is: " + methodName + ", wifiOnly: " + wifiOnly + ", Guid: " + recurringId);
            var proxyType = ProxiedService.GetType();
            _logger.Debug("proxyType: " + proxyType);

            return CallService(ProxiedService.GetType().GetRuntimeMethod(methodName, Util.GetTypes(parameters)), wifiOnly, recurringId, parameters);
        }

        /// <summary>
        /// Invokes method in proxied service passing parameters specified.
        /// Optionally, persists and retries the method until successful.
        /// </summary>
        /// <param name="method">MethodInfo containing method to invoke</param>
        /// <param name="wifiOnly">Indicates if message is to be sent via WiFi only</param>
        /// <param name="recurringId">Will mark a recurring message with and Id that can be used to stop the call from recurring.
        /// Method must have RecurringAttribute or id will not be record.  Pass Guid.Empty if you don't wish to set this value.</param>
        /// <param name="parameters">Optional list of parameters to include in method invocation</param>
        /// <returns>If call is recurring will return an id that can be used to stop to recurrence; Otherwise will return null.</returns>
        public Guid CallService(MethodInfo method, bool wifiOnly, Guid recurringId, params Object[] parameters)
        {
            _logger.Debug("Entered MethodInfo ServiceProxy.CallService");
            if (method == null)
            {
                _logger.Warn("Null method provided.  Call skipped.");
                throw new ArgumentException("Method value null");
            }

            var sc = new ServiceCall
            {
                Target = ProxiedService,
                Method = method,
                MethodName = method.Name,
                Parameters = parameters,
                UseGetResult = method.GetCustomAttributes(typeof(UseIResultAttribute), false).Any(),
                RecurringMessageId = (method.GetCustomAttributes(typeof(RecurringAttribute), false).Any()) ? (recurringId == Guid.Empty ? NewRecurringId : recurringId) : Guid.Empty,
                WifiOnly = wifiOnly
            };

            _logger.Debug("In ServiceProxy.CallService, about to call Create");

            Create(sc);
            return sc.RecurringMessageId;
        }

        /// <summary>
        /// Will stop the recurring service call for the identifier supplied.
        /// </summary>
        /// <param name="servcieCallId">Recurring identifier</param>
        /// <returns>True if successful; Otherwise false</returns>
        public static bool StopRecurringServiceCall(Guid servcieCallId)
        {
            return Messages.Instance.RemoveRecurringMessage(servcieCallId);
        }

        /// <summary>
        /// Send all pending messages.
        /// </summary>
        /// <param name="forceSend">If true WiFi only will not be observed.</param>
        /// <param name="token"></param>
        public async Task SendPendingMessages(bool forceSend = false, CancellationToken token = default(CancellationToken))
        {
            var pendingMessages = RetrievePendingMessages();
            if (pendingMessages != null && pendingMessages.Count > 0)
            {
                var svc = ProxiedService.IsNull() ? "Null" : ProxiedService.GetType().FullName;
                _logger.Debug($"Pending message count: {pendingMessages.Count} / ProxiedService Type == {svc} / WiFiConnected == {WifiConnected} / ForceSend == {forceSend}");

                foreach (var call in pendingMessages.Where(call => !call.WifiOnly || WifiConnected || forceSend))
                {
                    if (token.IsCancellationRequested)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    try
                    {
                        _logger.Debug($"Replaying message {call.MessageId} {call.MethodName}");
                        call.Target = ProxiedService;
                        call.Method = ProxiedService.GetType()
                            .GetRuntimeMethod(call.MethodName, Util.GetTypes(call.Parameters));
                        CallService(call);

                        await Task.Delay(TimeSpan.FromSeconds(5), token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception e)
                    {
                        _logger.Warn(e.Message, e);
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Checks if WiFi is connected using Platform Services.
        /// </summary>
        private bool WifiConnected
        {
            get
            {
                bool connected;

                try
                {
                    connected = true; //Container.Instance.Resolve<IPlatformService>().IsWifiConnected;
                    _logger.Debug($"WiFi Connected = {connected}");
                }
                catch (Exception ex)
                {
                    connected = false;
                    _logger.Error("Failed to get network state : " + ex.Message, ex);
                }

                return connected;
            }
        }

        /// <summary>
        /// Marks messages as transmitting and increments the retry counter.
        /// Invokes the service call and connects the completion event. 
        /// </summary>
        /// <param name="call"></param>
        private void CallService(ServiceCall call)
        {
            var targetMethod = call.Method;
            _logger.Debug("Started");
            try
            {
                _logger.Debug($"Operating on ServiceCall: {call}");
                _logger.Debug($"Call target: {call.Target}");
                if (targetMethod == null)
                {
                    _logger.Debug("targetMethod was null, attempting to get RuntimeMethod");
                    var types = Util.GetTypes(call.Parameters);

                    _logger.Debug($"Looking for method: {call.MethodName}");
                    _logger.Debug($"Target: {call.Target.GetType().FullName}");
                    targetMethod = call.Target.GetType().GetRuntimeMethod(call.MethodName, types);
                }

                if (targetMethod != null)
                {
                    _logger.Debug("Entering lock");
                    _logger.Info($"Target Method: {targetMethod.Name}");
                    var message = Messages.Instance.GetMessage(call.MessageId);
                    Messages.Instance.IncrementRetry(message);
                    Messages.Instance.MarkMessage(message, MessageStatus.Transmitting);

                    call.CallCompleteEventHandler += sc_CallCompleteEventHandler;
                    _logger.Debug($"CallService: Id: {call.MessageId}, Method {call.MethodName}");

                    _logger.Debug("in ServiceProxy.Create, invoking call.InvokeServiceMethodAsync");
                    Task.Run(async () => await call.InvokeServiceMethodAsync()).ConfigureAwait(false);

                    _logger.Debug("Finished");

                }
                else
                {
                    var methods = call.Target.GetType().GetRuntimeMethods();
                    if (methods != null)
                    {
                        foreach (var method in methods)
                        {
                            _logger.Debug($"Found method: {method.Name}");
                        }
                    }
                    else
                    {
                        _logger.Debug("No runtime methods found!");
                    }

                    _logger.Info($"Ignoring call to method {call.MethodName}, not contained in target {call.Target.GetType()}");
                }
            }
            catch (Exception ex)
            {

                _logger.Error("Failed to mark message for transmit : " + ex.Message, ex);
            }
            finally
            {
                if (targetMethod != null)
                {
                    _logger.Debug("Exited lock");
                }
            }
        }

        /// <summary>
        /// Completion event for service calls
        /// </summary>
        /// <param name="src"></param>
        /// <param name="args"></param>
        private void sc_CallCompleteEventHandler(object src, ServiceProxyEventArgs args)
        {
            if (src == null)
            {
                _logger.Warn("Source object is null skipping processing");
                return;
            }

            var serviceCall = (ServiceCall)src;
            serviceCall.CallCompleteEventHandler -= sc_CallCompleteEventHandler;
            args.CallParameters = serviceCall.Parameters;

            if (args.Status != ServiceProxyCallStatus.FailedToSend)
            {
                _logger.Debug($"CallService completed {serviceCall.MessageId} {serviceCall.MethodName}");
                if (CallCompleteEventHandler != null)
                {
                    try
                    {
                        CallCompleteEventHandler.BeginInvoke(this, args, null, this);
                    }
                    catch (Exception e)
                    {
                        _logger.Warn(e.Message, e);
                    }
                }
            }
            else
            {
                _logger.Debug($"CallService Failed to Send. {serviceCall.MessageId} {serviceCall.MethodName}");
            }

            try
            {
                var message = Messages.Instance.GetMessage(serviceCall.MessageId);
                if (serviceCall.RecurringMessageId != Guid.Empty)
                {
                    Messages.Instance.ResetMessage(message.Id);
                }
                else
                {
                    switch (args.Status)
                    {
                        case ServiceProxyCallStatus.CompletedSuccessfully:
                            Messages.Instance.MarkMessage(message, MessageStatus.Sent);
                            break;
                        case ServiceProxyCallStatus.CompletedWithError:
                            Messages.Instance.MarkMessage(message, MessageStatus.Failed);
                            break;
                        case ServiceProxyCallStatus.FailedToSend:
                        case ServiceProxyCallStatus.NotDestinedForTarget:
                            Messages.Instance.MarkMessage(message, MessageStatus.Incomplete);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to mark message status {serviceCall.MessageId} {serviceCall.MethodName} {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Saves service call into the database..
        /// </summary>
        /// <param name="serviceCall">Service call that you would like to save to the database</param>
        private void Create(ServiceCall serviceCall)
        {
            var message = new Message
            {
                Text = JsonConvert.SerializeObject(serviceCall),
                Type = serviceCall.GetType().FullName,
                Status = MessageStatus.Incomplete,
                WifiOnly = serviceCall.WifiOnly,
                RecurringId = serviceCall.RecurringMessageId

            };
            if (serviceCall.Parameters != null)
            {
                for (var index = 0; index < serviceCall.Parameters.Length; index++)
                {
                    MessageParameter parameter = null;
                    if (serviceCall.Parameters[index] != null)
                    {
                        parameter = new MessageParameter
                        {
                            Sequence = index + 1,
                            Type = serviceCall.Parameters[index].GetType().FullName,
                            Text = JsonConvert.SerializeObject(serviceCall.Parameters[index])
                        };
                        _logger.Debug("in ServiceProxy.Create, paramType is: " + parameter.Type);
                        _logger.Debug("in ServiceProxy.Create, paramText is: " + parameter.Text);
                    }
                    message.Parameters.Add(parameter);
                }
            }

            try
            {
                _logger.Debug("in ServiceProxy.Create, about to invoke lock");
                Messages.Instance.AddNewMessage(message);
                serviceCall.MessageId = message.Id;
                _logger.Debug("in ServiceProxy.Create, AddNewMessage completed");
            }
            catch (Exception e)
            {
                _logger.Error("Error Creating Message", e);
            }
        }

        /// <summary>
        /// Retrieve Pending messages from database.
        /// </summary>
        /// <returns>List of Pending Service Calls.</returns>
        private List<ServiceCall> RetrievePendingMessages()
        {
            List<ServiceCall> serviceCalls = new List<ServiceCall>();

            try
            {
                _logger.Debug("Retrieving pending messages");

                List<Message> pendingMessages = Messages.Instance.GetPendingMessages();
                if (pendingMessages != null)
                {
                    foreach (Message pendingMessage in pendingMessages)
                    {
                        _logger.Debug($"Deserializing MsgId {pendingMessage.Id}");
                        _logger.Debug($"Message: {pendingMessage.Text}");
                        var sc = (ServiceCall)JsonConvert.DeserializeObject(pendingMessage.Text, typeof(ServiceCall));
                        _logger.Debug($"Service Call Target: {sc.Target}");
                        sc.MessageId = pendingMessage.Id;
                        sc.Parameters = null;

                        // todo:  Children should be there
                        var msgParms = Messages.Instance.GetMessageParameters(sc.MessageId);

                        if (msgParms != null && msgParms.Count > 0)
                        {
                            sc.Parameters = new object[msgParms.Count];

                            for (var i = 0; i < msgParms.Count; i++)
                            {
                                _logger.Debug($"Deserializing MsgId {pendingMessage.Id}, Parm[{i + 1}]");

                                Type type = Type.GetType(msgParms[i].Type);

                                _logger.Debug(($"in RetrievePendingMessages, msgParms[i].MsgType is: {msgParms[i].Type}"
                                    ));
                                _logger.Debug(($"in RetrievePendingMessages, type is: {type.FullName}"));

                                sc.Parameters[i] = JsonConvert.DeserializeObject(msgParms[i].Text, type);
                            }
                        }

                        _logger.Debug($"Adding MsgId {pendingMessage.Id} to call list / WiFiOnly {pendingMessage.WifiOnly}");
                        serviceCalls.Add(sc);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to retrieve pending messages " + ex.Message, ex);
            }

            return serviceCalls;
        }

        private bool _error = false;
        public void CauseError()
        {
            _error = true;
        }

        /// <summary>
        /// Worker task that will send pending messages.
        /// </summary>
        /// <param name="token"></param>
        private async Task WorkerProcAsync(CancellationToken token = default(CancellationToken))
        {
            var serviceType = $"{_servcieCount}. {typeof(T).FullName}";
            _logger.Debug($"Begin WorkerProc - Service Type: {serviceType}");
            while (true)
            {

                if (token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                }

                try
                {
                    _logger.Debug($"Start Sending Pending Messages - Service Type: {serviceType}");
                    await SendPendingMessages(token: token).ConfigureAwait(false);
                    _logger.Debug($"Finished Sending Pending Messages - Service Type: {serviceType}");
                }
                catch (OperationCanceledException)
                {
                    _logger.Debug("OperationCanceledException #2");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Retry failed - Service Type: {serviceType}", ex);
                }
                finally
                {
                    _logger.Debug($"End check: WorkerProc - Service Type: {serviceType}");
                }

                var timeToWait = Convert.ToInt32(_retryTimeSpan.TotalMilliseconds);
                _logger.Debug($"^^^^ Starting Wait Loop: timeToWait: {timeToWait} - Service Type: {serviceType}");

                _platformService.GetAvailableThreads(out var worker, out var completionPort);
                _logger.Debug($"1.............  Managed Thread Id {Environment.CurrentManagedThreadId}, WorkThreads {worker}, CompletionPortThreads {completionPort},  - Service Type: {serviceType}");

                while (timeToWait > 0)
                {
                    if (token.IsCancellationRequested)
                    {
                        _logger.Info($"Breaking loops because CanRun is false. Service Type: {serviceType}");
                        break;
                    }

                    if (_error)
                    {
                        _logger.Debug("----------- Throwing Error --------------------");
                        throw new Exception("Test Error");
                    }

                    try
                    {
                        _logger.Debug($"Starting Wait: timeToWait: {timeToWait} Thread ID: {Task.CurrentId.GetValueOrDefault()}, Managed Thread Id {Environment.CurrentManagedThreadId} - Service Type: {serviceType}");
                        await Task.Delay(5000, token).ConfigureAwait(false);
                        _platformService.GetAvailableThreads(out worker, out completionPort);
                        _logger.Debug($"2.............  Managed Thread Id {Environment.CurrentManagedThreadId}, WorkThreads {worker}, CompletionPortThreads {completionPort},  - Service Type: {serviceType}");
                        _logger.Debug($"Finished Waiting: timeToWait: {timeToWait} Thread ID: {Task.CurrentId.GetValueOrDefault()}, Managed Thread Id {Environment.CurrentManagedThreadId} - Service Type: {serviceType}");
                        timeToWait -= 5000;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.Debug("OperationCanceledException #2");
                        break;
                    }
                }

                _platformService.GetAvailableThreads(out worker, out completionPort);
                _logger.Debug($"3.............  Managed Thread Id {Environment.CurrentManagedThreadId}, WorkThreads {worker}, CompletionPortThreads {completionPort},  - Service Type: {serviceType}");
                _logger.Debug($"Finished wait loop. Service Type: {serviceType}");
            }

            _logger.Debug($"End WorkerProc - Service Type: {serviceType}");
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            _logger.Debug("Started");
            Dispose(true);
            GC.SuppressFinalize(this);
            _logger.Debug("Finished");
        }

        private void Dispose(bool disposing)
        {
            _logger.Debug("Started");
            if (!_disposed)
            {
                _logger.Debug("Not Disposed");
                if (disposing)
                {
                    _logger.Debug("Disposing");
                    StopWorker();
                    CreateService = null;
                    try
                    {
                        // not going to do this want this instance to remain open.
                        //Messages.Instance.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Failed to dispose data access : " + ex.Message, ex);
                    }
                }
                _disposed = true;
            }

            _logger.Debug("Finished");
        }
        #endregion
    }

    internal class Util
    {
        private static readonly ILogger Logger;

        static Util()
        {
            Logger = LogFactory.Instance.GetLogger<Util>();
        }
        internal static Type[] GetTypes(Object[] items)
        {
            if (items == null)
            {
                return new Type[0];
            }

            Type[] types = new Type[items.Length];

            for (int index = 0; index < items.Length; index++)
            {
                types[index] = (items[index] == null) ? typeof(Object) : items[index].GetType();
                Logger.Debug($"Adding type to return: {types[index]}, value {items[index] ?? "null"}");
            }

            return types;
        }
    }
}