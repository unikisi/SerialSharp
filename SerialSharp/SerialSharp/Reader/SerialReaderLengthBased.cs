using SerialSharp.Core;
using SerialSharp.Reader.Config;
using System.Buffers;
using RJCP.IO.Ports;

namespace SerialSharp.Reader
{
    /// <summary>
    /// 基于长度字段解析串口返回数据的读取器。
    /// 会持续读取串口，直到读到的字节数满足数据段长度字段 + 固定长度，即认为数据包完整。
    /// </summary>
    /// <param name="serialPort"></param>
    public class SerialReaderLengthBased(
        SerialPortStream serialPort,
        LengthBasedReaderConfig config)
        : ISerialReader
    {
        /// <summary>
        /// 异步读取串口数据，直到根据长度字段判断数据包接收完成。
        /// </summary>
        /// <param name="cancellationToken">用于取消接收操作的令牌。</param>
        /// <returns>接收到的完整数据包，或在超限/取消时抛出异常。</returns>
        public async Task<byte[]?> ReadAsync(CancellationToken cancellationToken = default)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(config.ChunkSize); // 临时缓冲区，避免频繁分配
            var totalLength = 0; // 预期完整包总长度
            var count = 0;       // 当前已读取字节数

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var bytesToRead = serialPort.BytesToRead;
                    if (bytesToRead > 0)
                    {
                        // 计算最多可读多少字节（避免溢出）
                        var maxRead = Math.Min(buffer.Length - count, bytesToRead);

                        // 读取串口数据
                        var bytesRead = await serialPort.ReadAsync(buffer, count, maxRead, cancellationToken);
                        count += bytesRead;

                        // 一旦数据超过长度字段位置，且尚未确定总长度
                        if (count > config.DataSegmentsByteIndex && totalLength == 0)
                        {
                            // 提取数据段长度字段的值
                            var dataSegLen = buffer[config.DataSegmentsByteIndex];

                            // 计算完整数据包长度（固定部分 + 数据段）
                            totalLength = config.TotalExceptDataSegLength + dataSegLen;

                            // 安全保护：如果超出最大读取范围则视为异常
                            if (totalLength > config.ReadMaxSize)
                                throw new InvalidOperationException($"接收数据超出最大限制 {config.ReadMaxSize} 字节");
                        }

                        // 如果读满了完整包长度，返回结果
                        if (totalLength > 0 && count >= totalLength)
                        {
                            var result = new byte[totalLength];
                            Array.Copy(buffer, result, totalLength);
                            return result;
                        }
                    }

                    // 没有数据，稍作等待避免 CPU 空转
                    await Task.Delay(1, cancellationToken);
                }
            }
            finally
            {
                // 归还临时缓冲区，防止内存泄漏
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
