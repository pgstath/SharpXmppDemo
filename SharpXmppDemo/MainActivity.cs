using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Widget;

namespace SharpXmppDemo
{
    [Activity(Label = "SharpXmppDemo", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private int messageCount = 1;
        private ConnectActivityReceiver connectActivityReceiver;
        private InitActivityReceiver initActivityReceiver;
        private MesssageActivityReceiver messageActivityReceiver;
        private Button button;
        private string message = " This is a test message no#";
        private string password = "test1";
        private string jid = "test1@xmpp.momentum.im";
        private string messageRecipient = "test1@xmpp.momentum.im"; // Send a message to myself

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            button = FindViewById<Button>(Resource.Id.MyButton);

            button.Click += button_Click;
        }

        /// <summary>
        ///  Click the button and send a message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_Click(object sender, System.EventArgs e)
        {
            // Starts the IntentService and connects
            Intent mServiceIntent = new Intent(this, typeof(BackgroundService));
            mServiceIntent.PutExtra(ServiceMessage.JID, messageRecipient);
            mServiceIntent.PutExtra(ServiceMessage.MESSAGE, message + messageCount);
            mServiceIntent.SetAction(ServiceAction.SENDMESSAGE.ToString());
            StartService(mServiceIntent);

            button.Text = string.Format("{0} Messages!", messageCount++);
        }

        protected override void OnPause()
        {
            base.OnPause();

            if (initActivityReceiver != null)
            {
                UnregisterReceiver(initActivityReceiver);
            }
            if (connectActivityReceiver != null)
            {
                UnregisterReceiver(connectActivityReceiver);
            }
            if (messageActivityReceiver != null)
            {
                UnregisterReceiver(messageActivityReceiver);
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            // Setup the receivers for the service's results
            IntentFilter initIntentFilter = new IntentFilter(ServiceAction.INIT.ToString());
            initActivityReceiver = new InitActivityReceiver();
            // Registers the receiver and its intent filters
            RegisterReceiver(initActivityReceiver, initIntentFilter);

            // Setup the connection receiver
            IntentFilter connectIntentFilter = new IntentFilter(ServiceAction.CONNECT.ToString());
            connectActivityReceiver = new ConnectActivityReceiver();
            // Registers the receiver and its intent filters
            RegisterReceiver(connectActivityReceiver, connectIntentFilter);

            // Setup the message sent receiver
            IntentFilter messageIntentFilter = new IntentFilter(ServiceAction.SENDMESSAGE.ToString());
            messageActivityReceiver = new MesssageActivityReceiver();
            // Registers the receiver and its intent filters
            RegisterReceiver(messageActivityReceiver, messageIntentFilter);

            // Starts the IntentService and connects
            Intent mServiceIntent = new Intent(this, typeof(BackgroundService));
            mServiceIntent.PutExtra(ServiceMessage.JID, jid);
            mServiceIntent.PutExtra(ServiceMessage.PASSWORD, password);
            mServiceIntent.SetAction(ServiceAction.INIT.ToString());
            StartService(mServiceIntent);

            Utils.SetConnectionAlarm(this);
        }
    }

    /// <summary>
    /// Broadcast receiver, for the initialisation of the background service
    /// If initialisation is correct, it then connects to the XMPP server
    /// </summary>
    [BroadcastReceiver]
    public class InitActivityReceiver : BroadcastReceiver
    {
        public Activity act;

        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.GetBooleanExtra(ServiceMessage.OK, true))
            {
                // If initialisation succesful then connect.
                Intent mServiceIntent = new Intent(context, typeof(BackgroundService));
                mServiceIntent.SetAction(ServiceAction.CONNECT.ToString());
                context.StartService(mServiceIntent);
            }
            else
            {
                // Error in Init has occured
            }
        }
    }

    /// <summary>
    /// Broadcast receiver, for receiving the result of the
    /// Connection to the XMPP server
    /// </summary>
    [BroadcastReceiver]
    public class ConnectActivityReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.GetBooleanExtra(ServiceMessage.OK, true))
            {
                Log.Info("SharpXmppDemo", this.Class.ToString() + "Service call has succeeded");
            }
            else
            {
                Log.Error("SharpXmppDemo", this.Class.ToString() + "Service call failed");
            }
        }
    }

    /// <summary>
    /// Broadcast Receiver to executed upon the receive of the Message
    /// Send command
    /// </summary>
    [BroadcastReceiver]
    public class MesssageActivityReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.GetBooleanExtra(ServiceMessage.OK, true))
            {
                Log.Info("SharpXmppDemo", this.Class.ToString() + "Message was sent");
            }
            else
            {
                Log.Error("SharpXmppDemo", this.Class.ToString() + "Message failed");
            }
        }
    }
}