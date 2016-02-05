# SharpXmppDemo
### Maintaining a background XMPP connection in Android with Xamarin
A full blown demo project for connecting and maintaining a long running background Xmpp connection, on Android, with Xamarin and [Sharp.Xmpp](https://github.com/pgstath/Sharp.Xmpp). Code sample includes:
- A ["Sticky" Intent Service](https://github.com/pgstath/SharpXmppDemo/blob/master/SharpXmppDemo/StickyIntentService.cs), for long running tasks. See full explanation at this dedicated [codeproject article](http://www.codeproject.com/Articles/1068249/A-Sticky-Intent-Service-for-long-running-tasks-wit)
- Network reconnection [events monitoring](http://developer.android.com/training/monitoring-device-state/connectivity-monitoring.html), in [C#](https://github.com/pgstath/SharpXmppDemo/blob/master/SharpXmppDemo/Utils.cs), for checking connection connectivity, and reconnecting.
- [Scheduling repeating Android alarms](http://developer.android.com/training/scheduling/alarms.html), in [C#](https://github.com/pgstath/SharpXmppDemo/blob/master/SharpXmppDemo/Utils.cs), in order to schedule XMPP background pings, making sure the connection is live. 
- [Wakelocks](http://developer.android.com/training/scheduling/wakelock.html), in [C#](https://github.com/pgstath/SharpXmppDemo/blob/master/SharpXmppDemo/Utils.cs), in order to make sure that long running tasks such as file transfers, or initial reconnections, are able to complete before the Android device sleeps.
- Improved detection of broken TCP connections by Sharp.Xmpp

Download and enjoy. You can contact me at pgstath@gmail.com or through Github. 

### Copyright
2015-2016 Panagiotis Stathopoulos
