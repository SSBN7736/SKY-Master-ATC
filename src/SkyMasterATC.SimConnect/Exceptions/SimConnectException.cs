namespace SkyMasterATC.SimConnect.Exceptions
{
    /// <summary>
    /// Represents errors that occur within the SkyMaster ATC SimConnect integration layer.
    /// </summary>
    public sealed class SimConnectException : Exception
    {
        public SimConnectException(string message) : base(message) { }

        public SimConnectException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
