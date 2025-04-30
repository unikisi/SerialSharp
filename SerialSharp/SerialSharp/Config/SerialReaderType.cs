namespace SerialSharp.Config
{
    /// <summary>
    /// Defines the strategy used to determine when a full packet of data
    /// has been received over a serial connection.
    /// </summary>
    public enum SerialReaderType
    {
        /// <summary>
        /// Reads data based on a known length field in the packet structure.
        /// Suitable for protocols where the total message length can be calculated.
        /// </summary>
        LengthBased,

        /// <summary>
        /// Reads data until no new bytes are received for a configured timeout duration.
        /// Suitable for protocols with no explicit length indicator.
        /// </summary>
        TimeoutBased
    }
}
