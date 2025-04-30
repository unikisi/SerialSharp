using SerialSharp.Core;

namespace SerialSharp.Reader.Config
{
    /// <summary>
    /// Base class for serial reader configuration settings, shared by all reader types.
    /// Provides common parameters such as buffer size and read limits.
    /// </summary>
    public abstract class BaseReaderConfig : IReaderConfig
    {
        /// <summary>
        /// The size (in bytes) of the temporary buffer used during each read operation.
        /// Determines how much data is read from the serial port in one chunk.
        /// </summary>
        public int ChunkSize { get; set; } = 256;

        /// <summary>
        /// The maximum total number of bytes allowed to be read for a single packet.
        /// Acts as a safeguard to prevent runaway memory usage due to malformed or oversized data.
        /// </summary>
        public int ReadMaxSize { get; set; } = 4096;
    }
}
