using System;

namespace SharpXmppDemo
{
    /// <summary>
    /// The Multiplatform Interface that the Library is using in order to communicate
    /// with the calling Application. It comprises calls for Running Commands on each
    /// platforms GUI thread, Debug messages, and Notification Raising to the Application
    /// </summary>
    public interface IUIThreadDispatcher
    {
        /// <summary>
        /// A method which runs the provided Action on the platform's
        /// GUI thread. The implementation should provide a way to run
        /// the action on each platforms GUI thread
        /// </summary>
        /// <param name="action">The Action to run on th GUI thread</param>
        void Invoke(Action action);

        /// <summary>
        /// A method that logs debug messages.
        /// The implementation should provided a way
        /// to log debug messages on the local platform
        /// </summary>
        /// <param name="message">The message to log</param>
        void multiDebug(string message);

        /// <summary>
        /// Provides a CPU wakelock, automatically released after the Timeout
        /// </summary>
        /// <param name="timeout">The wakelock time out time</param>
        IWakeAble WakeLock(long timeout);

        IWakeAble WakeLock();
    }

    public interface IWakeAble : IDisposable
    {
        void Remove();
    }
}