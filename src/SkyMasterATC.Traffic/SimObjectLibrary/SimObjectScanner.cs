namespace SkyMasterATC.Traffic.SimObjectLibrary
{
    /// <summary>
    /// Describes an aircraft model found in the simulator's SimObjects folder.
    /// </summary>
    public sealed class SimObjectEntry
    {
        /// <summary>Display title of the aircraft (from <c>aircraft.cfg</c>).</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>ICAO type designator (e.g., "B738").</summary>
        public string IcaoType { get; set; } = string.Empty;

        /// <summary>Full path to the containing SimObjects sub-folder.</summary>
        public string FolderPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Scans the simulator's <c>SimObjects\Airplanes</c> directory and builds an
    /// index of available aircraft models.  Phase 2 will implement the full scan.
    /// </summary>
    public sealed class SimObjectScanner
    {
        /// <summary>
        /// Scans <paramref name="simObjectsRootPath"/> and returns all discovered
        /// aircraft entries.
        /// </summary>
        public IReadOnlyList<SimObjectEntry> Scan(string simObjectsRootPath)
        {
            // TODO (Phase 2): enumerate sub-folders, parse aircraft.cfg, build index.
            return Array.Empty<SimObjectEntry>();
        }
    }
}
