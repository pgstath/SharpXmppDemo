using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Util;
using System;

namespace SharpXmppDemo
{
    /// <summary>
    /// Broadcast receiver, that receives the periodic alarm message
    /// It starts in a wakefull fashion the background GUIStateservice
    /// </summary>
    [BroadcastReceiver]
    public class WakefulXmppReceiver : Android.Support.V4.Content.WakefulBroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Android.Util.Log.Info("SharpXmppDemo", this.Class.ToString() + "Starting wakefull receiver Alarm");

            // Start the service, keeping the device awake while the service is
            // launching. This is the Intent to deliver to the service.
            if (Utils.IsOnline(context))
            {
                Intent mServiceIntent = new Intent(context, typeof(BackgroundService));
                mServiceIntent.SetAction(ServiceAction.CONNECT.ToString());
                StartWakefulService(context, mServiceIntent);
            }
            else
            {
                Android.Util.Log.Info("SharpXmppDemo", this.Class.ToString() + "Not waked up since we are offline anyway!");
            }
        }
    }

    /// <summary>
    /// Receiver class that is activated during boot (Intent.ActionBootCompleted)
    /// It sets the connection alarm
    /// </summary>
    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new String[] { Intent.ActionBootCompleted }, Priority = (int)IntentFilterPriority.LowPriority)]
    public class InitBootReceiver : BroadcastReceiver
    {
        /// <summary>
        /// When booting up init
        /// </summary>
        /// <param name="context"></param>
        /// <param name="intent"></param>
        public override void OnReceive(Context context, Intent intent)
        {
            Log.Info("SharpXmppDemo", this.Class.ToString() + " InitBootReceiver Activated on Boot");
            Utils.SetConnectionAlarm(context);
        }
    }

    /// <summary>
    /// Receiver that is fired when the connectivity status changes
    /// If connectivity ON check for connecting
    /// </summary>
    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new string[] { "android.net.conn.CONNECTIVITY_CHANGE" })]
    public class ConnectivityChangeReceiver : Android.Support.V4.Content.WakefulBroadcastReceiver
    {
        public ConnectivityChangeReceiver()
        {
        }

        public override void OnReceive(Context context, Intent intent)
        {
            bool noConnection = !Utils.IsOnline(context);

            if (noConnection)
                Log.Debug("SharpXmppDemo", this.Class.ToString() + " No Connection detected event");
            else
            {
                Log.Debug("SharpXmppDemo", this.Class.ToString() + "Has Connection detected event");

                IntentFilter connectInitIntentFilter = new IntentFilter(ServiceAction.CONNECT.ToString());
                // Starts the Connection service
                Intent mServiceIntent = new Intent(context, typeof(BackgroundService));
                mServiceIntent.SetAction(ServiceAction.CONNECT.ToString());
                // The service is started as Wakeful, in order to prevent Android from killing prematurely the service
                StartWakefulService(context, mServiceIntent);
            }
        }
    }

    /// <summary>
    /// Class for managing Android Wake locks
    /// </summary>
    public class WakeHolder : IWakeAble, IDisposable
    {
        private Android.OS.PowerManager.WakeLock wakeLock;

        /// <summary>
        /// Creates a WakeHolder object
        /// </summary>
        public WakeHolder()
        {
            Context context = Android.App.Application.Context;
            PowerManager pm = (PowerManager)context.GetSystemService(Context.PowerService);
            wakeLock = pm.NewWakeLock(WakeLockFlags.Partial, "Background Service Provided WakeLock");
            wakeLock.Acquire();
        }

        /// <summary>
        ///  Sets the time to lock for a wake lock
        /// </summary>
        /// <param name="timeout">Time to lock in ms</param>
        public WakeHolder(long timeout)
        {
            Context context = Android.App.Application.Context;
            PowerManager pm = (PowerManager)context.GetSystemService(Context.PowerService);
            wakeLock = pm.NewWakeLock(WakeLockFlags.Partial, "Background Service Provided WakeLock");
            wakeLock.Acquire(timeout);
        }

        /// <summary>
        /// Removes the wake lock
        /// </summary>
        public void Remove()
        {
            try
            {
                wakeLock.Release();
            }
            catch (Exception e)
            {
                Log.Error("SharpXmppDemo", ".MultiDebug: " + e.StackTrace + e.ToString());
            }
        }

        /// <summary>
        ///  Dispose the wakelock
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (wakeLock != null)
                {
                    wakeLock.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Placeholder class for various utility methods
    /// </summary>
    internal class Utils
    {
        /// <summary>
        /// Set up a connection alarm, every 15mins
        /// Check the connection status
        /// </summary>
        /// <param name="act"></param>
        public static void SetConnectionAlarm(Context act)
        {
            if (!Utils.IsAlarmSet(act))
            {
                var alarm = (AlarmManager)act.GetSystemService(Context.AlarmService);
                // The Alarm is setup as a class derived from a wake ful receivre
                Intent mServiceIntent = new Intent(act, typeof(WakefulXmppReceiver));
                // We cancel an pending alarm
                var pendingServiceIntent = PendingIntent.GetBroadcast(act, 0, mServiceIntent, PendingIntentFlags.CancelCurrent);
                // Alarm is inexact, in order to preserve battery time, set every fifteen minutes.
                alarm.SetInexactRepeating(AlarmType.ElapsedRealtimeWakeup, AlarmManager.IntervalFifteenMinutes, AlarmManager.IntervalFifteenMinutes, pendingServiceIntent);
                Android.Util.Log.Info("SharpXmppDemo", act.Class.ToString() + " Setting Wakefull Alarm");
            }
            else
            {
                Log.Info("SharpXmppDemo", act.Class.ToString() + " Wakefull Alarm already set");
            }
        }

        /// <summary>
        /// Returns true if the alarm is set, false if it is not set
        /// </summary>
        /// <param name="act"></param>
        /// <returns>True if alarm is set, false otherwise</returns>
        private static bool IsAlarmSet(Context act)
        {
            // Checks if an alarm of the same type, for the same context already exists
            Intent mServiceIntent = new Intent(act, typeof(WakefulXmppReceiver));
            bool alarmUp = (PendingIntent.GetBroadcast(act, 0, mServiceIntent, PendingIntentFlags.NoCreate) != null);

            return alarmUp;
        }

        /// <summary>
        /// Returns true if we have network connectivity
        /// </summary>
        /// <param name="context">Context</param>
        /// <returns>True if there is network connectivity, false if there is no network connectivity</returns>
        public static bool IsOnline(Context context)
        {
            Log.Info("SharpXmppDemo", "IsOnline - Checking if Online");

            ConnectivityManager cm =
                (ConnectivityManager)context.GetSystemService(Android.Content.Context.ConnectivityService);
            NetworkInfo netInfoMobile = cm.GetNetworkInfo(ConnectivityType.Mobile);
            NetworkInfo netInfoWifi = cm.GetNetworkInfo(ConnectivityType.Wifi);
            return (netInfoMobile != null && netInfoMobile.IsConnectedOrConnecting)
                || (netInfoWifi != null && netInfoWifi.IsConnectedOrConnecting);
        }

        /// <summary>
        /// Checks if the static singleton variable exists
        /// </summary>
        /// <param name="act">Activity Called. If Activity!=null, also if singleton not exists go to Signup else nothing</param>
        /// <returns>True if Singleton exists, false if not singleton object is stored</returns>
        public static bool IsServiceActive(Activity act = null)
        {
            //return true;

            try
            {
                if (BackgroundService.XmppConnection == null)
                {
                    Log.Error("SharpXmppDemo", "IsServiceActive Stateholder is Instance Null");

                    Log.Error("SharpXmppDemo", "IsServiceActive Stateholder is Instance Null");

                    ////////////////////////
                    return false;
                }
            }
            catch (NullReferenceException e)
            {
                Log.Error("SharpXmppDemo", "IsServiceActive: Null argument exception" + e.StackTrace + e.ToString());

                return false;
            }
            catch (ArgumentException e)
            {
                Log.Error("SharpXmppDemo", "IsServiceActive: Unknown argument exception" + e.StackTrace + e.ToString());

                return false;
            }

            Log.Debug("SharpXmppDemo", "IsServiceActive Stateholder is Instance Exists, everything OK");

            return true;
        }
    }
}