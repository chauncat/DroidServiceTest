using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using DroidServiceTest.Core;
using DroidServiceTest.Core.Logging;
using DroidServiceTest.Core.Logging.Logger;

namespace ServiceTest.Droid
{
    [IntentFilter(new[] { "ServiceTest.Droid.DroidMessageService" })]
    [Service(Name = "servicetest.droid.droidmessageservice")]
    public class DroidMessageService : Service
    {
        public const int NotificationMaxMsgCount = 7;
        //private static readonly ILogger Logger = LogFactory.Instance.GetLogger<DroidMessageReceiver>();
        private static readonly IMyLogger Logger = new MyLogger();
        private const string NotificationDisplayStartingUpTitle = "Messaging Service Starting Up";
        private const string NotificationDisplayTitle = "Messaging Service Started";
        private Notification.Builder _msgNotificationBuilder;
        private DroidMessageReceiver _receiver;
        private static object _lock = new object();
        private static object _stoppingLock = new object();
        private bool _actionStopping;
        private int _publishCount = 1;
        private int DroidMessageAppId = 12345;
        private NotificationManager _notificationManager;
        private readonly List<string> _notificationMessages;
        private DocumentManagementService _dms1;
        private DocumentManagementService _dms2;
        private TwmTestService _twmTest1;

        private void HandleAndroidException(object sender, RaiseThrowableEventArgs e)
        {
            Logger.Error($"*** UNHANDLED ANDROID EXCEPTION OCCURRED ***{System.Environment.NewLine} {e?.Exception?.UnwindException() ?? "null message" }");
        }

        private void DomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            var ex = unhandledExceptionEventArgs?.ExceptionObject as Exception;
            Logger.Error($"*** UNHANDLED EXCEPTION OCCURRED *** {System.Environment.NewLine}, IsTerminating = {unhandledExceptionEventArgs != null && unhandledExceptionEventArgs.IsTerminating}, Error { ex?.UnwindException() ?? "null message" }");
        }

        private void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Logger.Error($"*** UNHANDLED TASK EXCEPTION OCCURRED ***{System.Environment.NewLine} {e?.Exception?.UnwindException() ?? "null message" }");
        }

        public DroidMessageService()
        {
            Logger.Debug("Started");

            DroidServiceTest.Core.Ioc.Container.Instance.Initialize(cc =>
            {
                cc.RegisterTypeAs<PlatformService, IPlatformService>(true);
            });

            AndroidEnvironment.UnhandledExceptionRaiser += HandleAndroidException;
            AppDomain.CurrentDomain.UnhandledException += DomainOnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;

            _notificationMessages = new List<string>();

            _dms1 = new DocumentManagementService();
            _dms2 = new DocumentManagementService();
            _twmTest1 = new TwmTestService();

            Logger.Debug("Finished");
        }

        public override IBinder OnBind(Intent intent)
        {
            Logger.Debug("Started");
            return null;
        }

        public override void OnCreate()
        {
            try
            {
                base.OnCreate();
                Logger.Debug("Started");

                _receiver = new DroidMessageReceiver();
                _receiver.IntentHandler += OnRequestReceived;
                Application.Context.RegisterReceiver(_receiver, new IntentFilter(Constants.DroidMessageBroadcastReceiver));

                var startedIntent = new Intent(Constants.DroidMessagingIntent);
                var pendingIntent = PendingIntent.GetActivity(this, 1, startedIntent, PendingIntentFlags.UpdateCurrent);

                //create the notification 
                _msgNotificationBuilder = new Notification.Builder(this)
                    .SetSmallIcon(Resource.Drawable.ic_messaging_service_wht)
                    .SetContentTitle(NotificationDisplayStartingUpTitle)
                    .SetContentIntent(pendingIntent);

                Logger.Debug("Starting up DroidMessageService...");
                StartForeground(DroidMessageAppId, _msgNotificationBuilder.Build());


                _dms1.StartWorker();
                _dms1.ArchiveDocumentCompleted += DMS_OnArchiveDocumentCompleted;

                // normally we have 2 or 3 of these service proxies classes running
                // not sure if we need them to reproduced the issues.
                _dms2.StartWorker();
                _dms2.ArchiveDocumentCompleted += DMS_OnArchiveDocumentCompleted;
                _twmTest1.StartWorker();

                Logger.Debug("Finished");
            }
            catch (Exception e)
            {
                Logger.Error(e.StackTrace);
                throw;
            }
        }



        private static void DMS_OnArchiveDocumentCompleted(object source, AsyncWebServiceResults results)
        {
            Logger.Debug("Started");

            try
            {
                if (results != null && results.Success)
                {
                    Logger.Debug("Received Results that were success");
                }
                else
                {
                    Logger.Debug("Received Results that were not success");
                }

            }
            catch (Exception e)
            {
                Logger.Error("Error processing Document Complete", e);
                throw;
            }

            Logger.Debug("Finished");
        }


        private bool ActionStopping
        {
            get => _actionStopping;
            set
            {
                lock (_stoppingLock)
                {
                    _actionStopping = value;
                }
            }
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            Logger.Debug("Started");

            var command = intent.GetStringExtra(Constants.MessagingServiceAction);
            if (command == Constants.StartService)
            {
                Logger.Debug("StartService");
                var startedIntent = new Intent(Constants.DroidMessagingIntent);
                startedIntent.PutExtra(Constants.MessagingServiceAction, Constants.StartService);
                SendBroadcast(startedIntent);
                ActionStopping = false;
            }

            Logger.Debug("Finished");
            return StartCommandResult.Sticky;
        }

        public void SetContentOfMessagingNotification(string text)
        {
            try
            {
                var notificationRedirectIntent = new Intent("ServiceTest.Droid.DroidMessageService");

                var pendingIntent = PendingIntent.GetActivity(this, 0, notificationRedirectIntent, PendingIntentFlags.OneShot);

                if (_notificationMessages.Count > NotificationMaxMsgCount)
                {
                    _notificationMessages.Clear();
                }

                var notificationBuilder = new NotificationCompat.Builder(Application.Context)
                    .SetAutoCancel(false)
                    .SetSmallIcon(Resource.Drawable.ic_messaging_service_wht)
                    .SetContentTitle(NotificationDisplayTitle)
                    .SetTicker(text)
                    .SetContentIntent(pendingIntent);

                var expandedStyle = new NotificationCompat.InboxStyle();

                _notificationMessages.Add($"{DateTime.Now:HH:mm:ss}: {text}");

                foreach (var msg in _notificationMessages)
                {
                    expandedStyle.AddLine(msg);

                }
                notificationBuilder.SetStyle(expandedStyle);

                _notificationManager = (NotificationManager)Application.Context.GetSystemService(NotificationService);
                _notificationManager.Notify(DroidMessageAppId, notificationBuilder.Build());
            }
            catch (Exception e)
            {
                Logger.Error(e.StackTrace);
                throw;
            }
        }


        private void OnRequestReceived(Intent intent)
        {
            Logger.Debug("Started");
            try
            {
                Monitor.Enter(_lock);
                var action = intent.GetStringExtra(Constants.Action);
                Logger.Debug($"{action} intent received");

                switch (action)
                {
                    case Constants.Publish:
                        Publish(intent);
                        break;

                        //case Constants.CreatePublisher:
                        //    CreatePublisher(intent);
                        //    break;
                        //case Constants.CreateSubscriber:
                        //    CreateSubscriber(intent);
                        //    break;
                        //case Constants.ConfirmMessage:
                        //    ConfirmMessage(intent);
                        //    break;
                        //case Constants.RemovePublisher:
                        //    RemovePublisher(intent);
                        //    break;
                        //case Constants.RemoveSubscriber:
                        //    RemoveSubscriber(intent);
                        //    break;
                        //case Constants.EndAllSession:
                        //    EndAllSession();
                        //    break;
                        //case Constants.EndSessionByAppName:
                        //    EndAllSessionByAppName(intent);
                        //    break;
                        //case Constants.CheckMessagingRunning:
                        //    SendMessagingRunning();
                        //    break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Unknown Error Handling Intent Message", ex);
            }
            finally
            {
                Logger.Debug("Exiting lock");
                Monitor.Exit(_lock);
                Logger.Debug("Finished exiting lock");
            }
            Logger.Debug("Finished");
        }

        private void Publish(Intent intent)
        {
            Logger.Debug("Started");
            try
            {
                SetContentOfMessagingNotification($"Received Publish Message #{_publishCount}");

                switch (_publishCount++)
                {
                    case 1:
                        _dms1.CauseError();
                        break;
                    case 2:
                        _dms2.CauseError();
                        break;
                    case 3:
                        _twmTest1.CauseError();
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error processing publish", e);
                throw;
            }

            Logger.Debug("Finished");
        }

        public override void OnTrimMemory(TrimMemory level)
        {
            base.OnTrimMemory(level);
            Logger.Warn("OnTrimMemory level " + level);
            if (level != TrimMemory.RunningModerate)
            {
                Logger.Warn("Calling.. GC.Collect()");
                GC.Collect();
                Logger.Warn("Waiting for finalizers");
                GC.WaitForPendingFinalizers();
                Logger.Warn("Second collect");
                GC.Collect();
            }
        }

        public override void OnDestroy()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Logger.Debug("In OnDestroy of DroidMessageService");
            _dms1.ArchiveDocumentCompleted -= DMS_OnArchiveDocumentCompleted;
            _dms1.Dispose();
            
            _dms2.ArchiveDocumentCompleted -= DMS_OnArchiveDocumentCompleted;
            _dms2.Dispose();

            _twmTest1.Dispose();

            base.OnDestroy();

            if (_receiver != null)
            {
                Logger.Debug("Unregister Receiver");
                Application.Context.UnregisterReceiver(_receiver);
                //UnregisterReceiver(_receiver);
            }

            Logger.Debug("Finished");
        }
    }
}