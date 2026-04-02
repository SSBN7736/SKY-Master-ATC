using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using SkyMasterATC.SimConnect.Client;
using SkyMasterATC.SimConnect.Interop;

namespace SkyMasterATC.App
{
    /// <summary>
    /// Main application window.  Owns the Win32 WndProc hook that pumps SimConnect
    /// messages and exposes connection status in the UI.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ISimConnectClient _simConnect;
        private HwndSource? _hwndSource;

        public MainWindow()
        {
            InitializeComponent();

            // Resolve the shared SimConnect client from the application's composition root.
            _simConnect = ((App)Application.Current).SimConnectClient;

            _simConnect.Connected    += OnSimConnected;
            _simConnect.Disconnected += OnSimDisconnected;
            _simConnect.Error        += OnSimError;
        }

        // ── Window lifecycle ──────────────────────────────────────────────────────

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var hwnd = new WindowInteropHelper(this).Handle;
            _hwndSource = HwndSource.FromHwnd(hwnd);
            _hwndSource?.AddHook(WndProc);

            // Attempt to connect on startup; gracefully handles sim not running.
            TryConnect(hwnd);
        }

        protected override void OnClosed(EventArgs e)
        {
            _hwndSource?.RemoveHook(WndProc);
            _simConnect.Connected    -= OnSimConnected;
            _simConnect.Disconnected -= OnSimDisconnected;
            _simConnect.Error        -= OnSimError;

            base.OnClosed(e);
        }

        // ── Win32 WndProc hook ────────────────────────────────────────────────────

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == SimConnectConstants.WM_USER_SIMCONNECT)
            {
                _simConnect.ReceiveMessage();
                handled = true;
            }
            return IntPtr.Zero;
        }

        // ── UI event handlers ─────────────────────────────────────────────────────

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            TryConnect(hwnd);
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            _simConnect.Disconnect();
        }

        // ── SimConnect event handlers (always marshalled to UI thread) ────────────

        private void OnSimConnected(object? sender, EventArgs e) =>
            Dispatcher.Invoke(SetConnectedState);

        private void OnSimDisconnected(object? sender, EventArgs e) =>
            Dispatcher.Invoke(SetDisconnectedState);

        private void OnSimError(object? sender, Exception ex) =>
            Dispatcher.Invoke(() =>
            {
                SetDisconnectedState();
                MessageBox.Show(
                    this,
                    ex.Message,
                    "SimConnect Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            });

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void TryConnect(IntPtr hwnd)
        {
            if (_simConnect.IsConnected)
                return;

            try
            {
                _simConnect.Connect(hwnd);
            }
            catch
            {
                // Error event already fired; UI will update via OnSimError.
            }
        }

        private void SetConnectedState()
        {
            StatusLabel.Text          = "Connected";
            StatusIndicator.Fill      = new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1)); // green
            ConnectButton.IsEnabled    = false;
            DisconnectButton.IsEnabled = true;
        }

        private void SetDisconnectedState()
        {
            StatusLabel.Text          = "Not Connected";
            StatusIndicator.Fill      = new SolidColorBrush(Color.FromRgb(0xF3, 0x8B, 0xA8)); // red
            ConnectButton.IsEnabled    = true;
            DisconnectButton.IsEnabled = false;
        }
    }
}
