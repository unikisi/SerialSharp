namespace SerialSharp.Reader.Config
{
    /// <summary>
    /// Configuration for length-based serial reading, where the complete packet length
    /// is calculated based on a specific byte in the data stream.
    /// </summary>
    public class LengthBasedReaderConfig : BaseReaderConfig
    {
        /// <summary>
        /// The index (zero-based) in the incoming byte array that contains the length of the data segment.
        /// </summary>
        public int DataSegmentsByteIndex { get; set; }

        /// <summary>
        /// The total number of bytes to add to the data segment length to calculate the full message length.
        /// This typically includes header, footer, or checksum bytes.
        /// </summary>
        public int TotalExceptDataSegLength { get; set; }
    }
}
