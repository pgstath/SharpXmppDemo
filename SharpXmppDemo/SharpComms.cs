using Sharp.Xmpp;
using Sharp.Xmpp.Client;
using Sharp.Xmpp.Extensions;
using Sharp.Xmpp.Im;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Im = Sharp.Xmpp.Im;

namespace SharpXmppDemo
{
    /// <summary>
    ///  A wrapper class for the management of the XMPP connection
    /// </summary>
    public class SharpComms
    {
        private int i = 0;

        /// <summary>
        /// The Sharp Xmpp Client
        /// </summary>
        private XmppClient xmppClient;

        /// <summary>
        /// RunOnUI thread dispatcher
        /// </summary>
        private IUIThreadDispatcher uiDispatcher;

        /// <summary>
        /// The S22 Roster List
        /// </summary>
        private List<RosterItem> remoteRosterList;

        /// <summary>
        /// Xmpp Connection Resource
        /// </summary>
        private string _resource = "SharpXmppDemo";

        /// <summary>
        /// Communication Timeout In milliseconds; Indicates a disconnection if timeout is reached
        /// Default value 30000 ms (30s)
        /// -1 Indicates no timeout
        /// </summary>
        public static int DefaultTimeOut = 20000;//20s

        /// <summary>
        /// The period with which ping messages are sent in msec, 10minX30sec
        /// </summary>
        private int _pingPeriod = 10 * DefaultTimeOut;//200s

        private Timer _pingTimer;
        private string _defaultNet;
        private string _defaultUser;
        /// <summary>
        /// The count of _pingTimers expired, and still client is disconnected
        /// </summary>

        /// Constructor of S22Comms. Setups communications and file transfer entities.
        /// Important notice: no actul connection yet takes place
        public SharpComms(String jid, String passphrase, string resource, IUIThreadDispatcher uidis)
        {
            if (passphrase == null || jid == null)
            {
                throw new ArgumentNullException("Provided null arguments to S22Comms constructor");
            }
            uiDispatcher = uidis;

            string[] temp = jid.Split(new Char[] { '@' });
            _defaultNet = temp[1]; //After the first @
            _defaultUser = temp[0]; //Before the first @
            _resource = resource;   //Resource provided in Constructor

            xmppClient = new XmppClient(_defaultNet, _defaultUser, passphrase);
            xmppClient.DefaultTimeOut = DefaultTimeOut;
            //Define the ping period multiplierf and the internal ping period

            remoteRosterList = new List<RosterItem>();
            //Method fired when a subscription request arrives
            xmppClient.SubscriptionRequest = SubScribeRequest;
            //Event fired when Subscription request has been approved.
            xmppClient.SubscriptionApproved += presenceMgt_OnSubscribed;
            //Event fired when Remote user or resource unsubscribed from receiving presence notifications
            xmppClient.Unsubscribed += presenceMgt_OnUnsubscribed;
            //Event fired when presence is detected
            xmppClient.StatusChanged += xmppClient_OnPresence;
            //Event fired when protocol error detected
            xmppClient.Error += xmppClient_OnError;
            //Event fired when Roster Updated
            xmppClient.RosterUpdated += xmppClient_RosterUpdated;
            //Delegate for incoming file transfer events
            xmppClient.FileTransferRequest = fileTransferMgt_OnFile_Delegate;
            //Delegate for incoming file transfer events progress
            xmppClient.FileTransferProgress += _xmppClient_FileTransferProgress;
            // Receive Messagea
            xmppClient.Message += xmppClient_Message;
        }

        private void xmppClient_Message(object sender, MessageEventArgs e)
        {
            uiDispatcher.multiDebug("Message Received from " + e.Jid + " with text " + e.Message);
        }

        private void xmppClient_OnError(object sender, Im.ErrorEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Connect using parameters provided in the constructor
        /// </summary>
        public void Connect()
        {
            //Connects using the Resource name. The catch block will raise appropriate high level error codes
            try
            {
                xmppClient.Connect(_resource); //Its not async, so we are waiting to return
                //Set Status with Presence Online and Priority -1
                xmppClient.SetStatus(Availability.Online, null, 1);
                if (isConnected())
                {
                    //Starts the ping timer and the exceptions bind to the timer that notify the higher level
                    _pingTimer = new Timer((o) =>
                    {
                        PingServer();
                    }, null, _pingPeriod, _pingPeriod);
                    //Starts a timer for pinging the server over time
                }
            } //Catch block of the connection command
            catch (System.Security.Authentication.AuthenticationException e)
            {
                uiDispatcher.multiDebug("Auth Exception, Authenication failed" + e.ToString() + e.StackTrace);
            }
            catch (System.IO.IOException e)
            {
                uiDispatcher.multiDebug("Net Exception" + e.ToString() + e.StackTrace);
            }
            catch (XmppException e)
            {
                uiDispatcher.multiDebug("XML Exception" + e.ToString() + e.StackTrace);
            }
            catch (Exception e)
            {
                uiDispatcher.multiDebug("XML Exception" + e.ToString() + e.StackTrace);
            }
        }

        /// <summary>
        /// Pings one time to the XMPP server and
        /// an appropriate error event is raised if disconnected
        /// </summary>
        public void PingServer()
        {
            try
            {
                var t = xmppClient.Ping(new Jid(_defaultNet, "")); //Pings the server
                uiDispatcher.multiDebug("Time to ping was " + t.Seconds.ToString());
            }
            catch (XmppDisconnectionException ex)
            {
                uiDispatcher.multiDebug("Error Pinging, disconnected from server " + ex + ex.StackTrace);
            }
            catch (InvalidOperationException ex)
            {
                uiDispatcher.multiDebug("Error Pinging, not connected to server " + ex + ex.StackTrace);
            }
            catch (NotSupportedException ex)
            {
                uiDispatcher.multiDebug("Error Pinging, not supported by server " + ex + ex.StackTrace);
            }
            catch (IOException ex)
            {
                uiDispatcher.multiDebug("Error Pinging, IO exception " + ex + ex.StackTrace);
            }
            catch (XmppException ex)
            {
                uiDispatcher.multiDebug("Error Pinging, Generic Exception" + ex + ex.StackTrace);
            }
        }

        public void SendMessage(Jid jid, string message)
        {
            xmppClient.SendMessage(jid, message);
        }

        /// <summary>
        /// Closing the connection.
        /// Also tries to dispose relevant objects
        /// </summary>
        public void Close()
        {
            uiDispatcher.multiDebug("Closing XMPP connection");
            uiDispatcher.multiDebug("S22 Close function called from:" + System.Environment.StackTrace);

            Dispose();
        }

        private void Dispose()
        {
            uiDispatcher.multiDebug("Disposing XMPP connection");
            if (_pingTimer != null) _pingTimer.Dispose();
            _pingTimer = null;
            try
            {
                //Closes actually also disposes
                xmppClient.Close();
            }
            catch (Exception e)
            {
                uiDispatcher.multiDebug("Exception on Disposing S22Comms" + e);
                //throw new NotImplementedException("Error on Disposing Comms" + e.ToString());
            }
        }

        /// <summary>
        /// Return a boolean value indicating connection state.
        /// If no client exists false is returned
        /// </summary>
        /// <returns>Connection State</returns>
        public Boolean isConnected()
        {
            if (xmppClient != null)
            {
                return xmppClient.Connected;
            }
            else { return false; }
        }

        private void xmppClient_RosterUpdated(object sender, RosterUpdatedEventArgs e)
        {
            List<RosterItem> rosterItems = new List<RosterItem>();

            RosterItem oldRosterItem = remoteRosterList.Where(w => w.Jid.GetBareJid() == e.Item.Jid.GetBareJid()).FirstOrDefault();
            remoteRosterList.Remove(oldRosterItem);
            remoteRosterList.Add(e.Item);
        }

        /// <summary>
        /// Method to handle the reception of a subscription request
        /// By Default returns false
        /// </summary>
        /// <param name="from">Jid from which the subscription request has arrived</param>
        /// <returns></returns>
        private bool SubScribeRequest(Jid from)
        {
            uiDispatcher.multiDebug("\n" + DateTime.Now + " | Subscription received by | " + from.ToString());

            return false;
        }

        /// <summary>
        /// Method to handle the receiption of a subscription event
        /// Since it is confirmation to a local subscription request proper changes
        /// are done on the group level
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void presenceMgt_OnSubscribed(object sender, SubscriptionApprovedEventArgs e)
        {
            uiDispatcher.multiDebug("\n" + DateTime.Now + " | Subscription received OnSubscribed | " + e.Jid);
        }

        /// <summary>
        /// On the Event an unsubscription message has arrived
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void presenceMgt_OnUnsubscribed(object sender, UnsubscribedEventArgs e)
        {
            uiDispatcher.multiDebug("\n" + DateTime.Now + " | UnSubscription request received OnSubscribe | from {1}" + e.Jid);
        }

        /// <summary>
        /// On the Event an unsubscription message has arrived
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        /// <summary>
        /// Method to handle on Presence events from Jid/Resource
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xmppClient_OnPresence(object sender, StatusEventArgs e)
        {
            uiDispatcher.multiDebug(string.Format("OnPresence from {0} (1)", e.Jid) + e.ToString());
        }

        /// <summary>
        /// On the event a peer has made a request to trust us
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// /// &lt;summary&gt;
        /// A callback method that is invoked whenever a subscription request from
        /// another XMPP user is received.
        /// &lt;/summary&gt;
        /// &lt;param name="from"&gt;The JID of the XMPP user who wishes to subscribe to our
        /// presence.&lt;/param&gt;
        /// &lt;returns>true to approve the request; Otherwise false.&lt;/returns&gt;

        private void presenceMgt_OnSubscribe(object sender, SubscriptionRequestEventArgs e) //Done
        {
            uiDispatcher.multiDebug("\n" + DateTime.Now + " | Subscription received OnSubscribe | from  " + e.Jid);
        }

        /// <summary>
        /// Incoming File delegate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">File transfer object</param>
        private string fileTransferMgt_OnFile_Delegate(FileTransfer e)
        {
            //Put a first wake lock since we are having an incoming file tranfered
            uiDispatcher.WakeLock((long)DefaultTimeOut);

            uiDispatcher.multiDebug("On File: " + e.ToString() + " " + e.Name + " " + e.SessionId + " " + e.From);

            return "filepath";
        }

        /// <summary>
        /// Sends a File Object to the Person
        /// </summary>
        /// <param name="peer">Person to send the file object</param>
        /// <param name="moment2distr">File object to send</param>
        /// <returns>The Sid of the transfer, null if error</returns>
        public string SendFile(Jid peer, string moment2distr)
        {
            if ((moment2distr == null) || !isConnected()) return null;

            String fileName = System.IO.Path.GetFileName(moment2distr);
            String tempFilePath = Path.Combine(System.IO.Path.GetTempPath(), fileName);
            string sid;

            ///COPY MOMENT TO A TEMP DIR
            ///
            try
            {
                File.Copy(moment2distr, tempFilePath, true);
            }
            catch (DirectoryNotFoundException copyError)
            {
                uiDispatcher.multiDebug(copyError.Message);
            }
            catch (PathTooLongException copyError)
            {
                uiDispatcher.multiDebug(copyError.Message);
            }
            catch (UnauthorizedAccessException copyError)
            {
                uiDispatcher.multiDebug(copyError.Message);
            }
            catch (ArgumentException copyError)
            {
                uiDispatcher.multiDebug(copyError.Message);
            }
            catch (FileNotFoundException copyError)
            {
                uiDispatcher.multiDebug(copyError.Message);
            }
            catch (IOException copyError)
            {
                uiDispatcher.multiDebug(copyError.Message);
            }

            //The sid of the file transfer
            //Serialize with escaped characters the description of the moment
            string xdesc = new System.Xml.Linq.XText(moment2distr.ToString()).ToString();

            try
            {
                sid = xmppClient.InitiateFileTransfer(peer, tempFilePath, xdesc);
                return sid;
            }
            catch (Exception e)
            {
                uiDispatcher.multiDebug("Error in transfering moment" + e.StackTrace + e.ToString());
            }
            return null;
        }

        private void FileTransferCallback(bool accepted, FileTransfer transfer)
        {
            uiDispatcher.multiDebug(transfer.To + " has " + (accepted == true ? "accepted " : "rejected ") +
                "the transfer of " + transfer.Name + ".");
        }

        private void _xmppClient_FileTransferProgress(object sender, FileTransferProgressEventArgs e)
        {
            i++;

            if (e.Transfer.From == xmppClient.Jid)
            {
                //direct = TVFileTransferEventArgs.DirectionEnum.Outgoing;
            }
            else
            {
                // direct = TVFileTransferEventArgs.DirectionEnum.Incoming;
            }

            int progress = (int)((100.0 * e.Transfer.Transferred) / e.Transfer.Size);

            //Check if file has finished transfer
            if (e.Transfer.Transferred == e.Transfer.Size)
            {
                //if (FileEnded != null)
                //{
                //}
            }
            else //else file has not finished transfer
            {
                //Displays information only every 5% of progress
                //if (fileTransferProgress / e.Transfer.Size >= 0.05)
                if (i > 9)
                {
                    i = 0;
                    uiDispatcher.multiDebug("File progress continued from" + e.ToString() + " " + e.Transfer.Transferred + " bytes out of" + "transferred" + e.Transfer.Size);
                    // = e.Transfer.Size;
                }
                else
                {
                    //fileTransferProgress = e.Transfer.Transferred - fileTransferProgress;
                }
            }
        }
    }
}