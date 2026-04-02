using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using SkyMasterATC.Core.Models;
using SkyMasterATC.Core.Services;
using SkyMasterATC.SimConnect.Client;
using SkyMasterATC.SimConnect.Interop;
using SkyMasterATC.Traffic.Control;
using SkyMasterATC.Traffic.SimObjectLibrary;
using SkyMasterATC.Traffic.Spawning;

namespace SkyMasterATC.App
{
    public partial class MainWindow : Window
    {
        // ── Services ──────────────────────────────────────────────────────────────
        private readonly ISimConnectClient   _simConnect;
        private readonly RadarRendererService _radarRenderer;
        private readonly AiSpawnManager      _spawnManager;
        private readonly GroundTowerController  _groundTower;
        private readonly ApproachCenterController _approachCenter;
        private readonly QraController       _qra;
        private readonly TtsService          _tts;
        private readonly SimObjectScanner    _scanner = new();

        // ── State ─────────────────────────────────────────────────────────────────
        private uint _selectedObjectId;
        private HwndSource? _hwndSource;

        public MainWindow()
        {
            var app = (App)Application.Current;
            _simConnect    = app.SimConnectClient;
            _radarRenderer = app.RadarRenderer;
            _spawnManager  = app.SpawnManager;
            _groundTower   = app.GroundTower;
            _approachCenter = app.ApproachCenter;
            _qra           = app.Qra;
            _tts           = app.Tts;

            InitializeComponent();

            WireEvents();
            LoadVoices();
        }

        // ── Window lifecycle ──────────────────────────────────────────────────────

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            _hwndSource = HwndSource.FromHwnd(hwnd);
            _hwndSource?.AddHook(WndProc);
            TryConnect(hwnd);
        }

        protected override void OnClosed(EventArgs e)
        {
            _hwndSource?.RemoveHook(WndProc);
            _simConnect.Connected    -= OnSimConnected;
            _simConnect.Disconnected -= OnSimDisconnected;
            _simConnect.Error        -= OnSimError;
            _radarRenderer.TracksUpdated -= OnTracksUpdated;
            _spawnManager.AircraftUpdated -= OnAircraftUpdated;
            _groundTower.CommandIssued  -= OnCommandIssued;
            _approachCenter.CommandIssued -= OnCommandIssued;
            _qra.ScrambleInitiated -= OnScrambleInitiated;
            base.OnClosed(e);
        }

        // ── Win32 WndProc ─────────────────────────────────────────────────────────

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == SimConnectConstants.WM_USER_SIMCONNECT)
            {
                _simConnect.ReceiveMessage();
                handled = true;
            }
            return IntPtr.Zero;
        }

        // ── Event wiring ──────────────────────────────────────────────────────────

        private void WireEvents()
        {
            _simConnect.Connected    += OnSimConnected;
            _simConnect.Disconnected += OnSimDisconnected;
            _simConnect.Error        += OnSimError;
            _radarRenderer.TracksUpdated += OnTracksUpdated;
            _spawnManager.AircraftUpdated += OnAircraftUpdated;
            _groundTower.CommandIssued  += OnCommandIssued;
            _approachCenter.CommandIssued += OnCommandIssued;
            _qra.ScrambleInitiated += OnScrambleInitiated;
        }

        // ── Toolbar buttons ───────────────────────────────────────────────────────

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            TryConnect(hwnd);
        }

        private void BtnDisconnect_Click(object sender, RoutedEventArgs e) =>
            _simConnect.Disconnect();

        private void BtnSubscribe_Click(object sender, RoutedEventArgs e)
        {
            _simConnect.SubscribeAircraftPositionUpdates();
            BtnSubscribe.IsEnabled = false;
            BtnSubscribe.Content   = "Position Updates Active";
        }

        // ── SimConnect event handlers ─────────────────────────────────────────────

        private void OnSimConnected(object? sender, EventArgs e) =>
            Dispatcher.Invoke(SetConnectedState);

        private void OnSimDisconnected(object? sender, EventArgs e) =>
            Dispatcher.Invoke(SetDisconnectedState);

        private void OnSimError(object? sender, Exception ex) =>
            Dispatcher.Invoke(() =>
            {
                SetDisconnectedState();
                MessageBox.Show(this, ex.Message, "SimConnect Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            });

        private void OnTracksUpdated(object? sender, EventArgs e) =>
            Dispatcher.Invoke(() => RadarView.UpdateTracks(_radarRenderer.Tracks));

        private void OnAircraftUpdated(object? sender, SimAircraft aircraft) =>
            Dispatcher.Invoke(RefreshAircraftList);

        private void OnCommandIssued(object? sender, AtcCommand cmd) =>
            Dispatcher.Invoke(() =>
            {
                TxtLastCommand.Text = $"Last command: {cmd.CommandType} → {cmd.TargetObjectId}  @ {cmd.IssuedUtc:HH:mm:ss}";
            });

        private void OnScrambleInitiated(object? sender, ScrambleEventArgs e) =>
            Dispatcher.Invoke(() =>
            {
                TxtLastCommand.Text = $"SCRAMBLE: interceptor {e.InterceptorObjectId} → target {e.TargetObjectId}";
                RefreshAircraftList();
            });

        // ── Radar ─────────────────────────────────────────────────────────────────

        private void RadarView_TrackSelected(object sender, SimAircraft aircraft)
        {
            _selectedObjectId = aircraft.ObjectId;
            _radarRenderer.SetSelected(_selectedObjectId);

            TxtSelectedAc.Text = $"Selected: {aircraft.Callsign}  ({aircraft.TypeDesignator})";
            SetAtcButtonsEnabled(true);

            // Sync list selection
            foreach (var item in AircraftList.Items)
            {
                if (item is AircraftListItem li && li.ObjectId == aircraft.ObjectId)
                {
                    AircraftList.SelectedItem = item;
                    break;
                }
            }
        }

        // ── Aircraft list ─────────────────────────────────────────────────────────

        private void AircraftList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (AircraftList.SelectedItem is AircraftListItem li)
            {
                _selectedObjectId = li.ObjectId;
                _radarRenderer.SetSelected(_selectedObjectId);
                RadarView.SelectTrack(_selectedObjectId);

                if (_spawnManager.SpawnedAircraft.TryGetValue(_selectedObjectId, out var ac))
                    TxtSelectedAc.Text = $"Selected: {ac.Callsign}  ({ac.TypeDesignator})";

                SetAtcButtonsEnabled(true);
            }
        }

        private void RefreshAircraftList()
        {
            AircraftList.Items.Clear();
            foreach (var kv in _spawnManager.SpawnedAircraft)
            {
                var ac = kv.Value;
                AircraftList.Items.Add(new AircraftListItem(
                    ac.ObjectId,
                    $"{ac.Callsign,-8} {ac.TypeDesignator,-4}  FL{(int)(ac.AltitudeFt / 100):D3}  {(int)ac.SpeedKnots:D3}kt  {ac.Squawk}"));
            }
        }

        private async void BtnDespawn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedObjectId == 0) return;
            await _spawnManager.DespawnAsync(_selectedObjectId);
            _selectedObjectId = 0;
            SetAtcButtonsEnabled(false);
            TxtSelectedAc.Text = "No aircraft selected";
            RefreshAircraftList();
        }

        // ── Spawn tab ─────────────────────────────────────────────────────────────

        private void BtnBrowseSimObjects_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog
            {
                Title = "Select SimObjects root folder",
            };
            if (dlg.ShowDialog(this) == true)
                TxtSimObjectsPath.Text = dlg.FolderName;
        }

        private void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            var path = TxtSimObjectsPath.Text.Trim();
            var models = _scanner.Scan(path);
            CmbAircraftType.ItemsSource   = models;
            CmbAircraftType.DisplayMemberPath = "Title";
            if (models.Count > 0)
                CmbAircraftType.SelectedIndex = 0;
        }

        private async void BtnSpawn_Click(object sender, RoutedEventArgs e)
        {
            if (CmbAircraftType.SelectedItem is not SimObjectEntry model)
            {
                // Use a placeholder model when scanner hasn't been run.
                model = new SimObjectEntry { Title = "Generic Aircraft", IcaoType = "ZZZZ" };
            }

            if (!double.TryParse(TxtLat.Text, out double lat)) lat = 51.4775;
            if (!double.TryParse(TxtLon.Text, out double lon)) lon = -0.4614;
            if (!double.TryParse(TxtAlt.Text, out double alt)) alt = 0;
            if (!double.TryParse(TxtHeading.Text, out double hdg)) hdg = 0;

            uint id = await _spawnManager.SpawnAsync(model, lat, lon, alt, hdg);
            RefreshAircraftList();

            // Centre radar on spawned aircraft if first one.
            if (_spawnManager.SpawnedAircraft.Count == 1)
            {
                RadarView.CenterLatitude  = lat;
                RadarView.CenterLongitude = lon;
            }
        }

        // ── ATC tab ───────────────────────────────────────────────────────────────

        private void BtnPushback_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedObjectId != 0) _groundTower.IssuePushback(_selectedObjectId);
        }

        private void BtnTaxi_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedObjectId != 0) _groundTower.IssueTaxi(_selectedObjectId);
        }

        private void BtnTakeoff_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedObjectId != 0) _groundTower.ClearForTakeoff(_selectedObjectId);
        }

        private void BtnAssignAlt_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedObjectId == 0) return;
            if (double.TryParse(TxtAssignAlt.Text, out double alt))
                _approachCenter.AssignAltitude(_selectedObjectId, alt);
        }

        private void BtnAssignHdg_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedObjectId == 0) return;
            if (double.TryParse(TxtAssignHdg.Text, out double hdg))
                _approachCenter.AssignHeading(_selectedObjectId, hdg);
        }

        private void BtnAssignSpd_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedObjectId == 0) return;
            if (double.TryParse(TxtAssignSpd.Text, out double spd))
                _approachCenter.AssignSpeed(_selectedObjectId, spd);
        }

        private async void BtnScramble_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedObjectId != 0)
                await _qra.ScrambleAsync(_selectedObjectId);
        }

        // ── TTS tab ───────────────────────────────────────────────────────────────

        private void LoadVoices()
        {
            var voices = _tts.GetAvailableVoices();
            CmbVoice.ItemsSource = voices;
            if (voices.Count > 0) CmbVoice.SelectedIndex = 0;
        }

        private void CmbVoice_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CmbVoice.SelectedItem is string name)
                _tts.SetVoice(name);
        }

        private async void BtnSpeak_Click(object sender, RoutedEventArgs e)
        {
            var text = TxtTtsPhrase.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            TxtLastTts.Text = $"Last TTS: {text}";
            await _tts.SpeakAsync(text);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void TryConnect(IntPtr hwnd)
        {
            if (_simConnect.IsConnected) return;
            try { _simConnect.Connect(hwnd); }
            catch { /* Error event fires; UI updates via OnSimError */ }
        }

        private void SetConnectedState()
        {
            StatusLabel.Text         = "Connected";
            StatusIndicator.Fill     = new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1));
            BtnConnect.IsEnabled     = false;
            BtnDisconnect.IsEnabled  = true;
            BtnSubscribe.IsEnabled   = true;
        }

        private void SetDisconnectedState()
        {
            StatusLabel.Text         = "Not Connected";
            StatusIndicator.Fill     = new SolidColorBrush(Color.FromRgb(0xF3, 0x8B, 0xA8));
            BtnConnect.IsEnabled     = true;
            BtnDisconnect.IsEnabled  = false;
            BtnSubscribe.IsEnabled   = false;
        }

        private void SetAtcButtonsEnabled(bool enabled)
        {
            BtnPushback.IsEnabled   = enabled;
            BtnTaxi.IsEnabled       = enabled;
            BtnTakeoff.IsEnabled    = enabled;
            BtnAssignAlt.IsEnabled  = enabled;
            BtnAssignHdg.IsEnabled  = enabled;
            BtnAssignSpd.IsEnabled  = enabled;
            BtnScramble.IsEnabled   = enabled;
        }
    }

    /// <summary>Simple display wrapper for the aircraft list box.</summary>
    internal sealed record AircraftListItem(uint ObjectId, string DisplayText)
    {
        public override string ToString() => DisplayText;
    }
}
