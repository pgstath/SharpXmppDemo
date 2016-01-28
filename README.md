# SharpXmppDemo
### Maintaining a background XMPP connection in Android with Xamarin
A full blown demo project for connecting and maintaining a long running background Xmpp connection, on Android, with Xamarin and [Sharp.Xmpp](https://github.com/pgstath/Sharp.Xmpp). Code sample includes:
- A "Sticky" Intent Service, for long running tasks. See full explanation at this dedicated [codeproject article](http://www.codeproject.com/Articles/1068249/A-Sticky-Intent-Service-for-long-running-tasks-wit)
- Network reconnection alarms, for checking connection connectivity, and reconnecting.
- Scheduling Android alarms, in order to schedule XMPP background pings, making sure the connection is live. 
- Wakelocks in order to make sure that long running tasks such as file transfers, or initial reconnections, are able to complete before the Android device sleeps.
- Improved detection of broken TCP connections by Sharp.Xmpp

Download and enjoy. 
