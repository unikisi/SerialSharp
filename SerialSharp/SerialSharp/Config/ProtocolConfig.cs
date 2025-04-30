using SerialSharp.Core;

namespace SerialSharp.Config
{
    /// <summary>
    /// Represents the configuration for the serial communication protocol,
    /// including how the response packets should be read and parsed.
    /// </summary>
    public class ProtocolConfig
    {
        /// <summary>
        /// Specifies the type of serial reader to use for parsing incoming data,
        /// such as length-based or timeout-based reading strategies.
        /// </summary>
        public SerialReaderType ReaderType { get; set; }

        /// <summary>
        /// Contains the configuration parameters specific to the selected reader type.
        /// Must match the structure required by the selected SerialReaderType.
        /// </summary>
        public IReaderConfig ReaderConfig { get; set; } = null!;
    }
}
