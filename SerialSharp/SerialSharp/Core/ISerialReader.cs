namespace SerialSharp.Core
{
    /// <summary>
    /// Represents a serial reader that reads and returns a complete data packet
    /// from the serial port using a specific strategy (e.g., length-based or timeout-based).
    /// </summary>
    public interface ISerialReader
    {
        /// <summary>
        /// Asynchronously reads a complete response packet from the serial port.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>
        /// A tuple containing:
        /// - The total number of bytes read.
        /// - The complete received packet as a byte array, or null if the read was canceled or no valid packet was received.
        /// </returns>
        Task<(int, byte[]?)> ReadAsync(CancellationToken cancellationToken = default);
    }
}
