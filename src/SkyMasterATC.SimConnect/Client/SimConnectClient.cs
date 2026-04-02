using SkyMasterATC.SimConnect.Exceptions;
using SkyMasterATC.SimConnect.Interop;

// When the managed SimConnect assembly is available (copied from the FSX/P3D
// SDK into lib/SimConnect/), uncomment the line below and remove the
// SIMCONNECT_UNAVAILABLE conditional compilation symbol from the project.
//
// #define SIMCONNECT_AVAILABLE
//
// With SIMCONNECT_AVAILABLE defined the code uses the real managed wrapper;
// without it a thin no-op adapter is compiled so the project still builds on
// machines that do not have the simulator SDK installed.

namespace SkyMasterATC.SimConnect.Client
{
    /// <summary>
    /// Concrete SimConnect client.  Opens a session using a Win32 window handle
    /// and delegates message pumping to the WndProc hook installed by the WPF
    /// host window.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Real SimConnect assembly:</b> copy
    /// <c>Microsoft.FlightSimulator.SimConnect.dll</c> (and its native companion
    /// <c>SimConnect.dll</c>) from the FSX/P3D SDK into
    /// <c>lib/SimConnect/</c> and add a project reference.  Then define
    /// <c>SIMCONNECT_AVAILABLE</c> (or set it in the .csproj) and replace the
    /// placeholder body of <see cref="Connect"/> / <see cref="ReceiveMessage"/>
    /// with the real managed-wrapper calls shown in the comments.
    /// </para>
    /// <para>
    /// <b>Message-pump flow:</b>
    /// <list type="number">
    ///   <item>WPF <c>MainWindow</c> installs an <c>HwndSourceHook</c> (WndProc).</item>
    ///   <item>SimConnect posts <see cref="SimConnectConstants.WM_USER_SIMCONNECT"/> to the window.</item>
    ///   <item>WndProc calls <see cref="ReceiveMessage"/>.</item>
    ///   <item>SimConnect dispatches all pending callbacks synchronously on the UI thread.</item>
    /// </list>
    /// This is entirely event-driven – no busy loops or background threads are needed.
    /// </para>
    /// </remarks>
    public sealed class SimConnectClient : ISimConnectClient
    {
        // ── Real SimConnect field (uncomment when assembly is referenced) ──────────
        // private Microsoft.FlightSimulator.SimConnect.SimConnect? _sim;

        // Placeholder keeps the project compiling without the SDK assembly.
        private object? _sim;

        private IntPtr _hwnd;
        private bool _disposed;

        /// <inheritdoc/>
        public bool IsConnected { get; private set; }

        /// <inheritdoc/>
        public event EventHandler? Connected;

        /// <inheritdoc/>
        public event EventHandler? Disconnected;

        /// <inheritdoc/>
        public event EventHandler<Exception>? Error;

        /// <inheritdoc/>
        public void Connect(IntPtr hwnd)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (IsConnected)
                return;

            _hwnd = hwnd;

            try
            {
#if SIMCONNECT_AVAILABLE
                // ── Real SimConnect open ──────────────────────────────────────────
                _sim = new Microsoft.FlightSimulator.SimConnect.SimConnect(
                    SimConnectConstants.APP_NAME,
                    hwnd,
                    SimConnectConstants.WM_USER_SIMCONNECT,
                    null,   // event handle – null = use window messages
                    0);

                // Wire standard callbacks
                _sim.OnRecvOpen      += OnRecvOpen;
                _sim.OnRecvQuit      += OnRecvQuit;
                _sim.OnRecvException += OnRecvException;

                // Register data definitions and system events here (Phase 2+)
                // RegisterDefinitionsAndEvents();
#else
                // ── Placeholder: no SDK assembly present ──────────────────────────
                // Simulate a successful open so the UI shows "Connected" and
                // the WndProc plumbing can be tested without the simulator.
                _sim = new object();
                IsConnected = true;
                Connected?.Invoke(this, EventArgs.Empty);
#endif
            }
            catch (Exception ex)
            {
                IsConnected = false;
                Error?.Invoke(this, new SimConnectException("Failed to open SimConnect session.", ex));
            }
        }

        /// <inheritdoc/>
        public void ReceiveMessage()
        {
            if (!IsConnected || _sim is null)
                return;

            try
            {
#if SIMCONNECT_AVAILABLE
                // Delivers all pending SimConnect callbacks synchronously.
                ((Microsoft.FlightSimulator.SimConnect.SimConnect)_sim).ReceiveMessage();
#endif
                // Placeholder: nothing to pump without the real assembly.
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex);
            }
        }

        /// <inheritdoc/>
        public void Disconnect()
        {
            if (!IsConnected)
                return;

            try
            {
#if SIMCONNECT_AVAILABLE
                var sim = (Microsoft.FlightSimulator.SimConnect.SimConnect?)_sim;
                if (sim is not null)
                {
                    sim.OnRecvOpen      -= OnRecvOpen;
                    sim.OnRecvQuit      -= OnRecvQuit;
                    sim.OnRecvException -= OnRecvException;
                    sim.Dispose();
                }
#endif
                _sim = null;
            }
            finally
            {
                IsConnected = false;
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
                return;

            Disconnect();
            _disposed = true;
        }

#if SIMCONNECT_AVAILABLE
        // ── SimConnect callbacks (real assembly) ─────────────────────────────────

        private void OnRecvOpen(
            Microsoft.FlightSimulator.SimConnect.SimConnect sender,
            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_RECV_OPEN data)
        {
            IsConnected = true;
            Connected?.Invoke(this, EventArgs.Empty);
        }

        private void OnRecvQuit(
            Microsoft.FlightSimulator.SimConnect.SimConnect sender,
            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_RECV data)
        {
            Disconnect();
        }

        private void OnRecvException(
            Microsoft.FlightSimulator.SimConnect.SimConnect sender,
            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_RECV_EXCEPTION data)
        {
            var ex = new SimConnectException(
                $"SimConnect exception: {data.dwException} (sendId={data.dwSendID}, index={data.dwIndex})");
            Error?.Invoke(this, ex);
        }
#endif
    }
}
