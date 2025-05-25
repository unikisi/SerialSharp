using SerialSharp.Core;
using SerialSharp.Reader.Config;
using System.Buffers;
using RJCP.IO.Ports;
using SerialSharp.Utils;

namespace SerialSharp.Reader
{
    /// <summary>
    /// Serial data reader based on a length field strategy.
    /// Continuously reads from the serial port until the number of received bytes
    /// satisfies the expected total length, calculated from a fixed-length offset and data segment length.
    /// </summary>
    /// <param name="serialPort">The serial port stream used for reading.</param>
    /// <param name="config">Reader configuration containing offset and length parameters.</param>
    public class SerialReaderLengthBased(
        SerialPortStream serialPort,
        LengthBasedReaderConfig config)
        : ISerialReader
    {
        /// <summary>
        /// Asynchronously reads data from the serial port until a complete packet is received,
        /// determined by the length field and fixed-length header/footer.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to interrupt the read operation.</param>
        /// <returns>The received complete byte array, or throws an exception if cancelled or invalid.</returns>
        public async Task<(int, byte[]?)> ReadAsync(CancellationToken cancellationToken = default)
        {
            var tempBuffer = ArrayPool<byte>.Shared.Rent(config.ChunkSize); // Temporary buffer to avoid heap allocations
            var expectedPacketLength = 0; // Expected total length of the packet
            var totalBytesRead = 0;       // Current number of bytes read

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var availableBytes = serialPort.BytesToRead;
                    if (availableBytes > 0)
                    {
                        // Prevent overflow by calculating the safe read size
                        var readLength = Math.Min(tempBuffer.Length - totalBytesRead, availableBytes);

                        // Perform async read
                        var bytesRead = await serialPort.ReadAsync(tempBuffer, totalBytesRead, readLength, cancellationToken);
                        totalBytesRead += bytesRead;

                        // Determine expected total length once enough bytes are available to read the length field
                        if (totalBytesRead >= GetLengthFieldEndIndex(config.DataSegmentsByteIndex) + 1
                            && expectedPacketLength == 0)
                        {
                            var dataSegmentLength = ProtocolParsingUtils.ReadLengthFromIndices(tempBuffer, config.DataSegmentsByteIndex, config.IsLengthFieldBigEndian);
                            expectedPacketLength = config.TotalExceptDataSegLength + dataSegmentLength;

                            if (expectedPacketLength > config.ReadMaxSize)
                                throw new InvalidOperationException($"Received data exceeds the maximum allowed size of {config.ReadMaxSize} bytes");
                        }

                        // If the buffer has reached the expected total length, return the packet
                        if (expectedPacketLength > 0 && totalBytesRead >= expectedPacketLength)
                        {
                            var result = new byte[expectedPacketLength];
                            Array.Copy(tempBuffer, result, expectedPacketLength);
                            return (totalBytesRead, result);
                        }
                    }

                    // Wait briefly to reduce CPU usage when no data is available
                    await Task.Delay(1, cancellationToken);
                }
            }
            finally
            {
                // Return the rented buffer to the pool to avoid memory leaks
                ArrayPool<byte>.Shared.Return(tempBuffer);
            }
        }

        private static int GetLengthFieldEndIndex(int[] indices)
            => indices.Length == 0 ? 0 : indices.Max();
    }
}
