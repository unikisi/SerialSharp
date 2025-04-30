namespace SerialSharp.Core
{
    /// <summary>
    /// Represents a serial reader that reads and returns a complete data packet
    /// from the serial port using a specific strategy (e.g., length-based or timeout-based).
    /// </summary>
    public interface ISerialReader
    {
        /// <summary>
        /// Reads and returns a complete response packet from the serial port asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the read operation.</param>
        /// <returns>The received packet as a byte array, or null if the read failed or was canceled.</returns>
        Task<byte[]?> ReadAsync(CancellationToken cancellationToken = default);
    }
}
