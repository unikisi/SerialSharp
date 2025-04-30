namespace SerialSharp.Reader.Config
{
    /// <summary>
    /// Configuration for timeout-based serial reading, where a message is considered complete
    /// when no additional data is received within a defined time window.
    /// </summary>
    public class TimeoutBasedReaderConfig : BaseReaderConfig
    {
        /// <summary>
        /// The number of milliseconds to wait after the last byte is received before
        /// considering the message complete.
        /// </summary>
        public int InactivityTimeoutMs { get; set; } = 500;
    }
}
