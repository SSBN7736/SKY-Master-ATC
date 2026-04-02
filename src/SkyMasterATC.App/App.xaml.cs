using System.Windows;
using SkyMasterATC.Core.Services;
using SkyMasterATC.SimConnect.Client;
using SkyMasterATC.Traffic.Control;
using SkyMasterATC.Traffic.Spawning;

namespace SkyMasterATC.App
{
    /// <summary>
    /// Application entry point and composition root.
    /// All services are constructed here and shared via properties.
    /// </summary>
    public partial class App : Application
    {
        internal ISimConnectClient      SimConnectClient { get; }
        internal RadarRendererService   RadarRenderer    { get; }
        internal TtsService             Tts              { get; }
        internal AiSpawnManager         SpawnManager     { get; }
        internal GroundTowerController  GroundTower      { get; }
        internal ApproachCenterController ApproachCenter { get; }
        internal QraController          Qra              { get; }

        public App()
        {
            SimConnectClient = new SimConnectClient();
            RadarRenderer    = new RadarRendererService();
            Tts              = new TtsService();
            SpawnManager     = new AiSpawnManager(SimConnectClient);
            GroundTower      = new GroundTowerController(SimConnectClient, Tts, SpawnManager);
            ApproachCenter   = new ApproachCenterController(SimConnectClient, Tts, SpawnManager);
            Qra              = new QraController(SimConnectClient, SpawnManager, ApproachCenter, Tts);

            // Wire aircraft position updates → radar renderer
            SpawnManager.AircraftUpdated     += (_, ac) => RadarRenderer.UpdateTrack(ac);
            SimConnectClient.AiObjectRemoved += (_, id) => RadarRenderer.RemoveTrack(id);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SimConnectClient.Dispose();
            SpawnManager.Dispose();
            Tts.Dispose();
            base.OnExit(e);
        }
    }
}
