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
    /// Scans the simulator's <c>SimObjects\Airplanes</c> (and optionally
    /// <c>Rotorcraft</c>) directory and builds an index of available aircraft models.
    /// </summary>
    public sealed class SimObjectScanner
    {
        /// <summary>
        /// Scans <paramref name="simObjectsRootPath"/> and returns all discovered
        /// aircraft entries parsed from <c>aircraft.cfg</c> files.
        /// </summary>
        public IReadOnlyList<SimObjectEntry> Scan(string simObjectsRootPath)
        {
            if (string.IsNullOrWhiteSpace(simObjectsRootPath) ||
                !Directory.Exists(simObjectsRootPath))
                return [];

            var results = new List<SimObjectEntry>();

            foreach (var category in new[] { "Airplanes", "Rotorcraft" })
            {
                var categoryPath = Path.Combine(simObjectsRootPath, category);
                if (!Directory.Exists(categoryPath))
                    continue;

                foreach (var subDir in Directory.EnumerateDirectories(categoryPath))
                {
                    var cfgPath = Path.Combine(subDir, "aircraft.cfg");
                    if (!File.Exists(cfgPath))
                        continue;

                    var entry = ParseAircraftCfg(cfgPath, subDir);
                    if (entry is not null)
                        results.Add(entry);
                }
            }

            return results;
        }

        // ── INI parser ────────────────────────────────────────────────────────────

        private static SimObjectEntry? ParseAircraftCfg(string cfgPath, string folderPath)
        {
            string? title = null;
            string? icaoType = null;
            string? currentSection = null;

            try
            {
                foreach (var rawLine in File.ReadLines(cfgPath))
                {
                    var line = rawLine.Trim();

                    if (line.StartsWith('[') && line.EndsWith(']'))
                    {
                        currentSection = line[1..^1].ToLowerInvariant();
                        continue;
                    }

                    if (line.StartsWith(';') || !line.Contains('='))
                        continue;

                    var eqIdx = line.IndexOf('=');
                    var key   = line[..eqIdx].Trim().ToLowerInvariant();
                    var value = StripComment(line[(eqIdx + 1)..]).Trim();

                    switch (currentSection)
                    {
                        case "fltsim.0":
                            if (key == "title" && title is null)
                                title = value;
                            break;

                        case "general":
                            if (key == "icao_type_designator" && icaoType is null)
                                icaoType = value;
                            else if (key == "atc_type" && icaoType is null)
                                icaoType = value;
                            break;
                    }
                }
            }
            catch (IOException)
            {
                return null;
            }

            if (string.IsNullOrEmpty(title))
                return null;

            return new SimObjectEntry
            {
                Title      = title,
                IcaoType   = string.IsNullOrEmpty(icaoType) ? "ZZZZ" : icaoType.ToUpper(),
                FolderPath = folderPath,
            };
        }

        private static string StripComment(string value)
        {
            var commentIdx = value.IndexOf(';');
            return commentIdx >= 0 ? value[..commentIdx] : value;
        }
    }
}
