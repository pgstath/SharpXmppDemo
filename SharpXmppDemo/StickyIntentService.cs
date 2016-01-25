using Android.App;
using Android.Content;
using Android.OS;
using System;

namespace SharpXmppDemo
{
    /**
 * IntentService is a base class for {@link Service}s that handle asynchronous
 * requests (expressed as {@link Intent}s) on demand.  Clients send requests
 * through {@link android.content.Context#startService(Intent)} calls; the
 * service is started as needed, handles each Intent in turn using a worker
 * thread, and stops itself when it runs out of work.
 *
 * <p>This "work queue processor" pattern is commonly used to offload tasks
 * from an application's main thread.  The IntentService class exists to
 * simplify this pattern and take care of the mechanics.  To use it, extend
 * IntentService and implement {@link #onHandleIntent(Intent)}.  IntentService
 * will receive the Intents, launch a worker thread, and stop the service as
 * appropriate.
 *
 * <p>All requests are handled on a single worker thread -- they may take as
 * long as necessary (and will not block the application's main loop), but
 * only one request will be processed at a time.
 *
 * <div class="special reference">
 * <h3>Developer Guides</h3>
 * <p>For a detailed discussion about how to create services, read the
 * <a href="{@docRoot}guide/topics/fundamentals/services.html">Services</a> developer guide.</p>
 * </div>
 *
 * @see android.os.AsyncTask
 */

    public abstract class StickyIntentService : Service
    {
        private volatile Looper mServiceLooper;
        private volatile ServiceHandler mServiceHandler;
        private String mName;
        private bool mRedelivery;

        private sealed class ServiceHandler : Handler
        {
            private StickyIntentService sis;

            public ServiceHandler(Looper looper, StickyIntentService sis)
                : base(looper)
            {
                this.sis = sis;
            }

            public override void HandleMessage(Message msg)
            {
                sis.OnHandleIntent((Intent)msg.Obj);
                //sis.StopSelf(msg.Arg1);
            }
        }

        /**
         * Creates an IntentService.  Invoked by your subclass's constructor.
         *
         * @param name Used to name the worker thread, important only for debugging.
         */

        public StickyIntentService(String name)
            : base()
        {
            mName = name;
        }

        /**
         * Sets intent redelivery preferences.  Usually called from the constructor
         * with your preferred semantics.
         *
         * <p>If enabled is true,
         * {@link #onStartCommand(Intent, int, int)} will return
         * {@link Service#START_REDELIVER_INTENT}, so if this process dies before
         * {@link #onHandleIntent(Intent)} returns, the process will be restarted
         * and the intent redelivered.  If multiple Intents have been sent, only
         * the most recent one is guaranteed to be redelivered.
         *
         * <p>If enabled is false (the default),
         * {@link #onStartCommand(Intent, int, int)} will return
         * {@link Service#START_NOT_STICKY}, and if the process dies, the Intent
         * dies along with it.
         */

        public void setIntentRedelivery(bool enabled)
        {
            mRedelivery = enabled;
        }

        public override void OnCreate()
        {
            // TODO: It would be nice to have an option to hold a partial wakelock
            // during processing, and to have a static startService(Context, Intent)
            // method that would launch the service & hand off a wakelock.

            base.OnCreate();
            HandlerThread thread = new HandlerThread("IntentService[" + mName + "]");
            thread.Start();

            mServiceLooper = thread.Looper;
            mServiceHandler = new ServiceHandler(mServiceLooper, this);
        }

        public override void OnStart(Intent intent, int startId)
        {
            Message msg = mServiceHandler.ObtainMessage();
            msg.Arg1 = startId;
            msg.Obj = intent;
            mServiceHandler.SendMessage(msg);
        }

        /**
         * You should not override this method for your IntentService. Instead,
         * override {@link #onHandleIntent}, which the system calls when the IntentService
         * receives a start request.
         * @see android.app.Service#onStartCommand
         */

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            OnStart(intent, startId);
            //return mRedelivery ? StartCommandResult.RedeliverIntent : StartCommandResult.NotSticky;
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            mServiceLooper.Quit();
        }

        /**
         * Unless you provide binding for your service, you don't need to implement this
         * method, because the default implementation returns null.
         * @see android.app.Service#onBind
         */

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        /**
         * This method is invoked on the worker thread with a request to process.
         * Only one Intent is processed at a time, but the processing happens on a
         * worker thread that runs independently from other application logic.
         * So, if this code takes a long time, it will hold up other requests to
         * the same IntentService, but it will not hold up anything else.
         * When all requests have been handled, the IntentService stops itself,
         * so you should not call {@link #stopSelf}.
         *
         * @param intent The value passed to {@link
         *               android.content.Context#startService(Intent)}.
         */

        protected abstract void OnHandleIntent(Intent intent);
    }
}