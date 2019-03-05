using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using DroidServiceTest.Core;

namespace DroidStarter.Droid
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private readonly ILogger Logger = new Logger();
        private const string ServiceIntent = "DriodServiceTest.Droid.DroidMessageService";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            var button = FindViewById<Button>(Resource.Id.start_service);

            button.Click += delegate
            {
                if (IsMyServiceRunning(ServiceIntent)) return;

                var intent = new Intent(ServiceIntent);

                intent.PutExtra("action", "START");

                var name = StartService(intent);

                Logger.Debug("Starting up DroidMessageService...");

            };
        }

        private bool IsMyServiceRunning(string className)
        {

            var manager = (ActivityManager)Application.Context.GetSystemService(ActivityService);

            foreach (var service in manager.GetRunningServices(100))
            {
                Logger.Debug("serviceClassType is:" + className);
                if (service.Service.ClassName.ToLower().Equals(className.ToLower()))
                {
                    Logger.Debug("Service already running" + className);
                    return true;
                }
            }
            return false;
        }
    }
}