using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Sharp.Xmpp;
using System;
using System.Threading;

namespace SharpXmppDemo
{
    /// <summary>
    /// The Background Service hosting the XMPP objet
    /// </summary>
    [Service]
    [IntentFilter(new String[] { "com.xamarin.GUIStateService" })]
    public class BackgroundService : StickyIntentService
    {
        private IBinder binder;

        private static SharpComms xmppConnection;
        private static AndroidDispatcher osDispatcher;

        public static SharpComms XmppConnection
        {
            get
            {
                return xmppConnection;
            }

            set { BackgroundService.xmppConnection = value; }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            Log.Info("SharpXmppDemo", this.Class.ToString() + "I'm being destroyed :(");
        }

        /// <summary>
        /// Create the service, setting the osDispatcher object
        /// </summary>
        public BackgroundService()
            : base("BackgroundService")
        {
            if (xmppConnection == null)
            {
                if (osDispatcher == null) osDispatcher = new AndroidDispatcher();
                osDispatcher.ServiceCallback = this;
            }
        }

        public override void OnCreate()
        {
            base.OnCreate();

            if (osDispatcher == null) osDispatcher = new AndroidDispatcher();
            osDispatcher.ServiceCallback = this;

            Log.Info("SharpXmppDemo", this.Class.ToString() + " I'm being created :)");
        }

        protected override void OnHandleIntent(Intent intent)
        {
            if (XmppConnection != null)
            {
                if (osDispatcher == null) osDispatcher = new AndroidDispatcher();
                osDispatcher.ServiceCallback = this;
            }

            // If service is restarted and no intent provided, the default action is to make a new connection to the server
            if (intent == null)
            {
                Log.Info("SharpXmppDemo", this.Class.ToString() + " StartSticky reinitiated Intent ");

                //Connect by default
                var newIntent = new Intent(ServiceAction.CONNECT.ToString());
                connect(newIntent);
                return;
            }

            String action = intent.Action;
            Intent response = null;

            Log.Info("SharpXmppDemo", this.Class.ToString() + " Processing action " + intent.Action.ToString());

            if (action.Equals(ServiceAction.CONNECT.ToString()))
            {
                response = connect(intent);
            }

            if (action.Equals(ServiceAction.INIT.ToString()))
            {
                response = init(intent);
            }

            if (action.Equals(ServiceAction.SENDMESSAGE.ToString()))
            {
                response = sendMessage(intent);
            }
            // Due to activation via an alarm
            WakefulXmppReceiver.CompleteWakefulIntent(intent);
        }

        private Intent sendMessage(Intent intent)
        {
            Log.Info("SharpXmppDemo", this.Class.ToString() + " Accepted intend to send message");
            String message = intent.GetStringExtra(ServiceMessage.MESSAGE);
            String recipient = intent.GetStringExtra(ServiceMessage.JID);

            var responseIntent = new Intent(intent.Action);

            Log.Info("SharpXmppDemo", this.Class.ToString() + " Moment: " + message + " is send");

            if (XmppConnection != null)
            {
                try
                {
                    XmppConnection.SendMessage(new Jid(recipient), message);
                    responseIntent.PutExtra(ServiceMessage.OK, true);
                    return responseIntent;
                }
                catch (Exception e)
                {
                    Log.Error("SharpXmppDemo", this.Class.ToString() + e.StackTrace + "\n" + e.ToString());

                    responseIntent.PutExtra(ServiceMessage.OK, false);
                    return responseIntent;
                }
            }
            else
            {
                responseIntent.PutExtra(ServiceMessage.OK, false);
                return responseIntent;
            }
        }

        /// <summary>
        /// Connect to TVNet
        /// </summary>
        /// <param name="intent"></param>
        /// <returns></returns>
        private Intent connect(Intent intent)
        {
            var initIntent = new Intent(intent.Action);

            //Attempt to connect. If tv exists successfull

            if ((XmppConnection != null))
            {
                if (!XmppConnection.isConnected())
                {
                    try
                    {
                        if (Utils.IsOnline(Android.App.Application.Context))
                        {
                            Log.Info("SharpXmppDemo", this.Class.ToString() + "Attempting to Connect from Background Service");

                            XmppConnection.Connect();
                        }
                        else
                        {
                            Log.Info("SharpXmppDemo", this.Class.ToString() + "No network, will not proceed to Connect from Background Service");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error("SharpXmppDemo", this.Class.ToString() + "Error at Connection, Exception Raised during reconnection" + e.ToString() + e.StackTrace);
                    }

                    initIntent.PutExtra(ServiceMessage.OK, true);
                } //is already connected
                else
                {
                    try
                    {
                        // If online make a ping instead of a connect
                        if (Utils.IsOnline(Android.App.Application.Context))
                        {
                            Log.Info("SharpXmppDemo", this.Class.ToString() + "Attempting to Ping from Background Service");

                            XmppConnection.PingServer();
                        }
                        else
                        {
                            Log.Info("SharpXmppDemo", this.Class.ToString() + "No network, will not proceed to Ping from Background Service");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error("SharpXmppDemo", this.Class.ToString() + "Error at Pinging, Exception Raised during reconnection" + e.ToString() + e.StackTrace);
                    }

                    initIntent.PutExtra(ServiceMessage.OK, true); //Fail is only if TVnet does not exists
                }
            }
            else
            {
                // A fail is returned only if an exception occurs
                // Even with no connect an OK message is send
                // Connection or not is detected subsequently with another event
                initIntent.PutExtra(ServiceMessage.OK, false);
            }
            return initIntent;
        }

        private Intent init(Intent intent)
        {
            var loginIntent = new Intent(ServiceAction.INIT.ToString());

            Log.Info("SharpXmppDemo", this.Class.ToString() + "Accepted intend to init from form-provided connection strings");

            String jid = intent.GetStringExtra(ServiceMessage.JID);
            String password = intent.GetStringExtra(ServiceMessage.PASSWORD);
            String name = intent.GetStringExtra(ServiceMessage.TAG);

            try
            {
                xmppConnection = new SharpComms(jid, password, name, (IUIThreadDispatcher)osDispatcher);

                loginIntent.PutExtra(ServiceMessage.OK, true);
            }
            catch (Exception e)
            {
                Log.Error("SharpXmppDemo", this.Class.ToString() + "Error at updating TrustVillage" + e.ToString() + e.StackTrace);
                loginIntent.PutExtra(ServiceMessage.OK, false);
            }

            return loginIntent;
        }

        public override IBinder OnBind(Intent intent)
        {
            binder = new GUIStateServiceBinder(this);
            return binder;
        }

        public class GUIStateServiceBinder : Binder
        {
            private BackgroundService service;

            public GUIStateServiceBinder(BackgroundService service)
            {
                this.service = service;
            }

            public BackgroundService GetGUIStateServiceBinderService()
            {
                return service;
            }
        }
    }

    /// <summary>
    /// Helper class for DIspatching and Invoking, except from Notifications Raising
    /// It implemented an interface in order to facilitate multiplatform operation
    /// </summary>
    public class AndroidDispatcher : IUIThreadDispatcher
    {
        public BackgroundService ServiceCallback = null;

        public AndroidDispatcher()
        {
        }

        //The Invoke action for the interface
        //Provides Run to Gui mechanism
        public void Invoke(Action action)
        {
            var handler = new Handler(Looper.MainLooper);
            Log.Info("SharpXmppDemo", ".MultiDebug: " + "Invoke was called for action: " + action.ToString());

            if (handler != null)
            {
                handler.Post(action);
            }
            else
            {
                throw new InvalidOperationException("No handler for the main thread was created");
            }
        }

        private void setWaitHandle(ManualResetEvent m)
        {
            m.Set();
        }

        /// <summary>
        /// Acquires a wake lock for the timeout period
        /// </summary>
        /// <param name="timeout">The time which the wakelock will be released</param>
        public IWakeAble WakeLock(long timeout)
        {
            multiDebug("Setting up a wakelock for " + timeout + "seconds");
            return (IWakeAble)new WakeHolder(timeout);
        }

        /// <summary>
        /// Returns a new Wakelock object
        /// </summary>
        /// <returns></returns>
        public IWakeAble WakeLock()
        {
            multiDebug("Setting up a wakelock");
            return (IWakeAble)new WakeHolder();
        }

        /// <summary>
        ///  Generic debug message print. Can be used in
        ///  multiplatform environments
        /// </summary>
        /// <param name="message">The message to print</param>
        public void multiDebug(string message)
        {
            Log.Info("SharpXmppDemo", ".MultiDebug: " + message);
        }
    }
}