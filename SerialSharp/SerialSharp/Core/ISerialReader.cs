namespace SerialSharp.Core
{
    /// <summary>
    /// 表示串口读取器接口，定义了串口数据的异步读取行为。
    /// 该接口允许通过不同策略（如长度字段、结束符、超时等）实现自定义的串口读取逻辑。
    /// </summary>
    public interface ISerialReader
    {
        /// <summary>
        /// 异步读取串口返回的数据，直到满足接收完成的条件。
        /// </summary>
        /// <param name="cancellationToken">用于取消读取操作的可选令牌。</param>
        /// <returns>
        /// 表示异步读取操作的任务。返回的数据为接收到的字节数组；
        /// 如果在读取过程中被取消，则抛出 <see cref="OperationCanceledException"/>。
        /// </returns>
        Task<byte[]?> ReadAsync(CancellationToken cancellationToken = default);
    }
}
