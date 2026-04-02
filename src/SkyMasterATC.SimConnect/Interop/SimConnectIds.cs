namespace SkyMasterATC.SimConnect.Interop
{
    // -----------------------------------------------------------------------
    // Enumeration identifiers used when registering data definitions and
    // submitting data requests to SimConnect.  Each value must be unique
    // within its enum.  Add new entries here as new phases are implemented.
    // -----------------------------------------------------------------------

    /// <summary>
    /// Identifies each data-definition group registered with SimConnect via
    /// <c>AddToDataDefinition</c>.
    /// </summary>
    public enum DataDefinitionId
    {
        /// <summary>Basic AI aircraft position/attitude data definition.</summary>
        AircraftPositionData = 0,
    }

    /// <summary>
    /// Identifies each outstanding data request sent to SimConnect via
    /// <c>RequestDataOnSimObject</c> or similar calls.
    /// </summary>
    public enum DataRequestId
    {
        /// <summary>One-shot position request for a single AI object.</summary>
        AircraftPosition = 0,
    }

    /// <summary>
    /// Client-side event identifiers mapped to simulator system events via
    /// <c>MapClientEventToSimEvent</c>.
    /// </summary>
    public enum SimEventId
    {
        /// <summary>Placeholder – maps to "ObjectAdded" when implemented.</summary>
        ObjectAdded = 0,

        /// <summary>Placeholder – maps to "ObjectRemoved" when implemented.</summary>
        ObjectRemoved = 1,

        /// <summary>Maps to "PUSHBACK_SET" simulator event.</summary>
        PushbackSet = 2,

        /// <summary>Maps to "SET_ATC_ACTIVE" simulator event.</summary>
        SetAtcActive = 3,

        /// <summary>Maps to "XPNDR_CODE_SET" for transponder code changes.</summary>
        XpndrCodeSet = 4,
    }
}
