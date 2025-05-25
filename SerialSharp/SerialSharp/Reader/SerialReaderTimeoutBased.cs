using RJCP.IO.Ports;
using SerialSharp.Core;
using SerialSharp.Reader.Config;
using System.Buffers;

namespace SerialSharp.Reader
{
    /// <summary>
    /// Serial reader that determines the end of a message based on a period of inactivity.
    /// When no new bytes are received within the configured timeout and data has been read, the buffer is returned.
    /// </summary>
    /// <param name="port">The SerialPortStream instance for reading data.</param>
    public class SerialReaderTimeoutBased(
        SerialPortStream port,
        TimeoutBasedReaderConfig config)
        : ISerialReader
    {
        /// <summary>
        /// Asynchronously reads data from the serial port until an inactivity timeout occurs or the operation is canceled.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>
        /// A tuple containing:
        /// - The total number of bytes read.
        /// - The complete received packet as a byte array, or null if the read was canceled or no valid packet was received.
        /// </returns>
        public async Task<(int, byte[]?)> ReadAsync(CancellationToken cancellationToken = default)
        {
            var receiveBuffer = new MemoryStream(); // Accumulates received bytes
            var tempBuffer = ArrayPool<byte>.Shared.Rent(config.ChunkSize); // Temporary buffer from the shared pool
            var lastReceivedTime = DateTime.UtcNow; // Last time data was read

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var bytesAvailable = port.BytesToRead;
                    if (bytesAvailable > 0)
                    {
                        // Read available bytes from the port
                        var bytesRead = await port.ReadAsync(tempBuffer, 0, Math.Min(tempBuffer.Length, bytesAvailable), cancellationToken);
                        receiveBuffer.Write(tempBuffer, 0, bytesRead);
                        lastReceivedTime = DateTime.UtcNow;

                        // Fail-safe: if buffer exceeds max allowed size, throw exception
                        if (receiveBuffer.Length > config.ReadMaxSize)
                            throw new InvalidOperationException($"Received data exceeds the maximum allowed size of {config.ReadMaxSize} bytes");
                    }
                    else
                    {
                        // No data currently available — check for timeout
                        var idleDuration = DateTime.UtcNow - lastReceivedTime;

                        // If idle for longer than the configured threshold and some data has been read, return it
                        if (idleDuration.TotalMilliseconds >= config.InactivityTimeoutMs && receiveBuffer.Length > 0)
                        {
                            return (bytesAvailable, receiveBuffer.ToArray());
                        }

                        // Wait briefly to reduce CPU usage when no data is available
                        await Task.Delay(1, cancellationToken);
                    }
                }
            }
            finally
            {
                // Return the rented buffer to the pool to avoid memory leaks
                ArrayPool<byte>.Shared.Return(tempBuffer);
            }
        }
    }
}
