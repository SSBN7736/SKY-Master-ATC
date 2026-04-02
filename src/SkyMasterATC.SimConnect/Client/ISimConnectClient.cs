namespace SkyMasterATC.SimConnect.Client
{
    /// <summary>
    /// Abstraction over the SimConnect session.  Implementations handle opening
    /// a connection to FSX/P3D using a Win32 window handle and pumping SimConnect
    /// messages via a WndProc hook.
    /// </summary>
    public interface ISimConnectClient : IDisposable
    {
        /// <summary>Returns <see langword="true"/> when the session is open.</summary>
        bool IsConnected { get; }

        /// <summary>Raised on the UI thread when the SimConnect session opens successfully.</summary>
        event EventHandler? Connected;

        /// <summary>Raised on the UI thread when the SimConnect session closes or the sim exits.</summary>
        event EventHandler? Disconnected;

        /// <summary>Raised when a recoverable SimConnect error or exception occurs.</summary>
        event EventHandler<Exception>? Error;

        /// <summary>
        /// Opens a SimConnect session.  Must be called after the WPF window handle
        /// is available (i.e., from <c>OnSourceInitialized</c> or later).
        /// </summary>
        /// <param name="hwnd">Win32 window handle of the host WPF window.</param>
        void Connect(IntPtr hwnd);

        /// <summary>Closes the active SimConnect session cleanly.</summary>
        void Disconnect();

        /// <summary>
        /// Must be called each time the application receives the
        /// <see cref="SkyMasterATC.SimConnect.Interop.SimConnectConstants.WM_USER_SIMCONNECT"/>
        /// Windows message (from the WndProc hook).
        /// SimConnect delivers all callbacks synchronously during this call.
        /// </summary>
        void ReceiveMessage();
    }
}
