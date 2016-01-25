using System;

namespace SharpXmppDemo
{
    /// <summary>
    ///  Helper Class for the definition of different Service Actions
    /// </summary>
    public sealed class ServiceAction
    {
        private readonly String name;
        private readonly int value;

        public static readonly ServiceAction INIT = new ServiceAction(1, "INIT");
        public static readonly ServiceAction CONNECT = new ServiceAction(2, "CONNECT");
        public static readonly ServiceAction SENDMESSAGE = new ServiceAction(3, "SENDMESSAGE");

        private ServiceAction(int value, String name)
        {
            this.name = name;
            this.value = value;
        }

        public override String ToString()
        {
            return name;
        }
    }

    /// <summary>
    /// Helper class for passing predefined Intent parameters
    /// </summary>
    public class ServiceMessage
    {
        public static String JID = "jid";
        public static String PASSWORD = "password";
        public static String TAG = "tag";
        public static String OK = "success";
        public static String MESSAGE = "message";
    }
}