using SerialSharp.Core;

namespace SerialSharp
{
    /// <summary>
    /// 表示一个串口命令及其响应解析逻辑。
    /// 支持泛型响应、自定义重试机制、回调通知等功能。
    /// </summary>
    /// <typeparam name="T">响应数据的解析结果类型。</typeparam>
    public class SerialCommand<T> : IExecutableCommand
    {
        /// <summary>
        /// 要发送给设备的原始请求字节数组。
        /// </summary>
        public byte[] RequestBytes { get; set; } = [];

        /// <summary>
        /// 响应解析函数，将设备返回的原始字节解析为 <typeparamref name="T"/> 类型的数据。
        /// </summary>
        public Func<byte[], T>? ParseFunc { get; set; }

        /// <summary>
        /// 响应成功且解析成功时的回调处理。
        /// </summary>
        public Action<T>? OnParsedResponse { get; set; }

        /// <summary>
        /// 命令执行失败（超时、异常、重试失败等）时的回调处理。
        /// </summary>
        public Action<Exception>? OnError { get; set; }

        /// <summary>
        /// 命令被取消时的回调处理。
        /// </summary>
        public Action? OnCanceled { get; set; }

        /// <summary>
        /// 是否跳过读取响应，仅发送命令（适用于无返回型写指令）。
        /// </summary>
        public bool SkipRead { get; set; } = false;

        /// <summary>
        /// 当前命令的超时时间（毫秒）。
        /// 超时后将触发取消逻辑，并调用 <see cref="OnCanceled"/>。
        /// </summary>
        public int TimeoutMs { get; set; } = 3000;

        /// <summary>
        /// 命令失败后重试的次数。
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// 每次重试之间的延迟时间（毫秒）。
        /// </summary>
        public int RetryDelayMs { get; set; } = 200;

        /// <summary>
        /// 发送命令后，在读取响应前的延迟时间（毫秒）。
        /// 用于等待设备准备好响应（如某些设备先处理后回复）。
        /// </summary>
        public int DelayBeforeReadMs { get; set; } = 0;

        /// <summary>
        /// 当前命令执行失败后，是否主动清空命令队列中尚未执行的其他命令。
        /// </summary>
        public bool ClearQueueOnFailure { get; set; } = false;

        public TaskCompletionSource<bool> CompletionSource { get; } = new();

        /// <summary>
        /// 在指定通道上异步执行命令。
        /// </summary>
        /// <param name="channel">命令所属的串口通道。</param>
        public async Task ExecuteAsync(SerialComChannel channel)
        {
            await channel.ExecuteCommandAsync<T>(this);
        }
    }
}
