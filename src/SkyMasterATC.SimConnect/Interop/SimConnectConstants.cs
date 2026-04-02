namespace SkyMasterATC.SimConnect.Interop
{
    /// <summary>
    /// Constants used for the SimConnect Win32 message pump integration.
    /// </summary>
    public static class SimConnectConstants
    {
        /// <summary>Windows WM_USER base value.</summary>
        public const int WM_USER = 0x0400;

        /// <summary>
        /// Application-defined message ID used for SimConnect notification.
        /// Passed as the <c>UserEventWin32</c> parameter when opening a SimConnect session.
        /// </summary>
        public const int WM_USER_SIMCONNECT = WM_USER + 0x2D;

        /// <summary>Application name shown in SimConnect connection dialogs.</summary>
        public const string APP_NAME = "SkyMaster ATC";
    }
}
