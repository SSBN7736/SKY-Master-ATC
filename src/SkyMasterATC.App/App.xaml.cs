using System.Windows;
using SkyMasterATC.Core.Services;
using SkyMasterATC.SimConnect.Client;
using SkyMasterATC.Traffic.Control;
using SkyMasterATC.Traffic.Spawning;

namespace SkyMasterATC.App
{
    /// <summary>
    /// Application entry point.  Constructs the service composition root and
    /// launches the main window.
    /// </summary>
    public partial class App : Application
    {
        // ── Service composition root ──────────────────────────────────────────────
        // Replace with a proper DI container (e.g., Microsoft.Extensions.DependencyInjection)
        // as the application grows.

        internal ISimConnectClient SimConnectClient { get; }
        internal RadarRendererService RadarRenderer { get; }
        internal AiSpawnManager SpawnManager { get; }
        internal GroundTowerController GroundTower { get; }
        internal ApproachCenterController ApproachCenter { get; }
        internal QraController Qra { get; }
        internal TtsService Tts { get; }

        public App()
        {
            SimConnectClient  = new SimConnectClient();
            RadarRenderer     = new RadarRendererService();
            SpawnManager      = new AiSpawnManager(SimConnectClient);
            GroundTower       = new GroundTowerController(SimConnectClient);
            ApproachCenter    = new ApproachCenterController(SimConnectClient);
            Qra               = new QraController(SimConnectClient);
            Tts               = new TtsService();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SimConnectClient.Dispose();
            base.OnExit(e);
        }
    }
}
