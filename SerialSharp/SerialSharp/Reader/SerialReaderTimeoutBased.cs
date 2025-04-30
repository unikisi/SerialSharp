using RJCP.IO.Ports;
using SerialSharp.Core;
using SerialSharp.Reader.Config;
using System.Buffers;

namespace SerialSharp.Reader
{
    /// <summary>
    /// 基于串口空闲超时判断数据接收完成的串口读取器。
    /// 当串口在设定时间内未收到新字节，且已有数据存在，则返回完整数据。
    /// </summary>
    /// <param name="port">SerialPortStream</param>
    public class SerialReaderTimeoutBased(
        SerialPortStream port,
        TimeoutBasedReaderConfig config)
        : ISerialReader
    {
        /// <summary>
        /// 异步读取串口数据，直到接收超时或被取消。
        /// </summary>
        /// <param name="cancellationToken">用于取消操作的令牌。</param>
        /// <returns>返回接收到的完整字节数组；如果被取消或异常，则抛出相应异常。</returns>
        public async Task<byte[]?> ReadAsync(CancellationToken cancellationToken = default)
        {
            var buffer = new MemoryStream(); // 用于累积接收的数据
            var temp = ArrayPool<byte>.Shared.Rent(config.ChunkSize); // 临时缓冲区，避免频繁分配
            var lastReadTime = DateTime.UtcNow; // 上次成功读取数据的时间

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var available = port.BytesToRead;
                    if (available > 0)
                    {
                        // 读取串口数据
                        var read = await port.ReadAsync(temp, 0, Math.Min(temp.Length, available), cancellationToken);
                        buffer.Write(temp, 0, read); // 写入缓冲区
                        lastReadTime = DateTime.UtcNow; // 更新时间戳

                        // 防止内存爆炸：如果超过最大接收限制，抛出异常
                        if (buffer.Length > config.ReadMaxSize)
                            throw new InvalidOperationException($"接收数据超出最大限制 {config.ReadMaxSize} 字节");
                    }
                    else
                    {
                        // 当前无数据可读，检测是否超时
                        var idle = DateTime.UtcNow - lastReadTime;

                        // 如果空闲时间超过阈值，并且之前读过数据，则视为帧结束
                        if (idle.TotalMilliseconds >= config.InactivityTimeoutMs && buffer.Length > 0)
                        {
                            return buffer.ToArray();
                        }

                        // 短暂延迟，避免死循环占满 CPU
                        await Task.Delay(1, cancellationToken);
                    }
                }
            }
            finally
            {
                // 始终归还租用的临时缓冲区，避免内存泄露
                ArrayPool<byte>.Shared.Return(temp);
            }
        }
    }
}
