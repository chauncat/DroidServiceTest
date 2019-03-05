using System;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DroidServiceTest.Core.StoreAndForward
{
    public class ServiceCall 
    {
        public delegate void ServiceCallComplete(Object src, ServiceProxyEventArgs args);

        private readonly ILogger _logger = new Logger();

        /// <summary>
        /// Object that will be used when invoking the call.
        /// </summary>
        public Object Target { get; set; }
        public string MethodName { get; set; }
        /// <summary>
        /// Required if Method Name is not supplied
        /// </summary>
        [JsonIgnore]
        public MethodInfo Method { get; set; }
        /// <summary>
        /// Defines Service call as a recurring message.
        /// </summary>
        public Guid RecurringMessageId { get; set; }
        /// <summary>
        /// Set true if IResults interface is needed to retrieve final results.
        /// </summary>
        public bool UseGetResult { get; set; }
        /// <summary>
        /// Input parameters for the method call.
        /// </summary>
        public Object[] Parameters { get; set; }
        /// <summary>
        /// Unique identifier for the ServiceCall
        /// </summary>
        public int MessageId { get; set; }
        public bool WifiOnly { get; set; }
        public event ServiceCallComplete CallCompleteEventHandler;

        /// <summary>
        /// Used to get final results of this service called need to be retrieved 
        /// the IResults.GetResult implementation.
        /// </summary>
        /// <param name="requestor">requesting method call</param>
        /// <param name="parameter">method parameters</param>
        /// <returns>Returns Object from IResults.GetResults implementation.  If IResults has not been implemented MissingMemberException will be thrown.</returns>
        private Object CallGetResult(string requestor, Object parameter)
        {
            Object result;

            try
            {
                MethodInfo targetMethod;
                Object[] parameters = {requestor, parameter};
                // Make the service implement this we don't have to store information in the database. 
                targetMethod = Target.GetType().GetRuntimeMethod("GetResult", Util.GetTypes(parameters));

                if (targetMethod != null)
                {
                    result = targetMethod.Invoke(Target, parameters);
                }
                else
                {
                    throw new MissingMemberException("GetResult method has not been implemented.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to get results : " + ex.Message, ex);
                throw;
            }

            return result;
        }

        /// <summary>
        /// This methods invoke the Method or MethodName via reflection.
        /// MethodName is used if Method is not defined.
        /// </summary>
        /// <returns>Object returned by the Method call if successful; Otherwise will return null.</returns>
        public async Task<Object> InvokeServiceMethodAsync()
        {
            _logger.Debug("Started");
            Object ret = null;
            ServiceProxyEventArgs eventArgs = new ServiceProxyEventArgs
            {
                Status = ServiceProxyCallStatus.FailedToSend,
                MethodName = MethodName
            };

            var methodName = MethodName;

            try
            {
                MethodInfo targetMethod = Method;

                if (targetMethod == null)
                {
                    targetMethod = Target.GetType().GetRuntimeMethod(MethodName, Util.GetTypes(Parameters));
                }
                else
                {
                    methodName = Method.Name;
                }
                
                if (targetMethod != null && Target != null)
                {
                    _logger.Debug(String.Format("Begin method invoke {0} {1}", MessageId, methodName));
                    if (targetMethod.ReturnType == (typeof(Task)) || targetMethod.ReturnType.Name == "Task`1")
                    {
                        eventArgs.ReturnValue = await (dynamic)targetMethod.Invoke(Target, Parameters);
                        _logger.Debug("Finished");
                    }
                    else
                    {
                        //If the target does not return this could block the update and leave this message in transmitting status.
                        eventArgs.ReturnValue = targetMethod.Invoke(Target, Parameters);
                        _logger.Debug("Finished Invoke");
                    }

                    if (UseGetResult)
                    {
                        _logger.Debug("Getting return value");
                        eventArgs.ReturnValue = CallGetResult(methodName, eventArgs.ReturnValue);
                        _logger.Debug(string.Format("Return value: {0}", eventArgs.ReturnValue));
                    }
                    _logger.Debug("Marking status CompletedSuccessfully");
                    eventArgs.Status = ServiceProxyCallStatus.CompletedSuccessfully;
                    ret = eventArgs.ReturnValue;
                }
                else
                {
                    _logger.Error(string.Format("Method {0} not contained in target {1}", methodName, Target == null ? "Unknown Type" : Target.GetType().FullName));
                    eventArgs.Status = ServiceProxyCallStatus.NotDestinedForTarget;
                }

            }
            catch (Exception ex)
            {
                var errorMessage = string.Empty;
                var e = ex;

                while (e != null)
                {
                    errorMessage += string.Format("(Message = {1} -- Excpetion Type= '{0}'), ", e.GetType().Name, e.Message);
                    e = e.InnerException;
                }
                
                _logger.Error(String.Format("Method invoke messageid={0}, methodnamd={1}, exception {2}, innerExceptions: '{3}'", MessageId, methodName, ex.Message, errorMessage), ex);
                eventArgs.ReturnValue = null;
                if (ex is ErrorResponseException || (ex is TargetInvocationException && ex.InnerException is ErrorResponseException))
                {
                    _logger.Debug("Sending CompletedWithError status");
                    eventArgs.Status = ServiceProxyCallStatus.CompletedWithError;
                }
                else
                {
                    eventArgs.Status = ServiceProxyCallStatus.FailedToSend;
                }

                if (ex is TargetInvocationException)
                {
                    eventArgs.ServiceException = ex.InnerException;
                }
                else
                {
                    eventArgs.ServiceException = ex;
                }
            }
            finally
            {
                _logger.Debug(String.Format("End method invoke {0} {1}", MessageId, methodName));

                if (CallCompleteEventHandler != null)
                {
                    try
                    {
                        CallCompleteEventHandler(this, eventArgs);
                    }
                    catch (Exception ex)
                    {
                        // don't want listeners exception to affect us.
                        _logger.Error(string.Format("Call Complete Event Listener threw an exception after calling {0}.", methodName), ex);
                    }
                }
            }
            _logger.Debug("Finished");
            return ret;
        }
    }
}
