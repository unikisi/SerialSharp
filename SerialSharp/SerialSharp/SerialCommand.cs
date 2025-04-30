using SerialSharp.Core;

namespace SerialSharp
{
    /// <summary>
    /// Represents a serial command with support for generic response parsing, retry logic, and callbacks.
    /// </summary>
    /// <typeparam name="T">The type of the parsed response data.</typeparam>
    public class SerialCommand<T> : IExecutableCommand
    {
        /// <summary>
        /// The raw byte array to send to the device.
        /// </summary>
        public byte[] RequestBytes { get; set; } = [];

        /// <summary>
        /// A function to parse the raw response bytes into a typed result of <typeparamref name="T"/>.
        /// </summary>
        public Func<byte[], T>? ParseFunc { get; set; }

        /// <summary>
        /// Callback invoked when the command is successfully executed and parsed.
        /// </summary>
        public Action<T>? OnParsedResponse { get; set; }

        /// <summary>
        /// Callback invoked when the command fails due to timeout, exception, or retries exceeded.
        /// </summary>
        public Action<Exception>? OnError { get; set; }

        /// <summary>
        /// Callback invoked when the command is explicitly canceled.
        /// </summary>
        public Action? OnCanceled { get; set; }

        /// <summary>
        /// Indicates whether to skip reading the response (e.g., for write-only commands).
        /// </summary>
        public bool SkipRead { get; set; } = false;

        /// <summary>
        /// Timeout for the command in milliseconds.
        /// </summary>
        public int TimeoutMs { get; set; } = 3000;

        /// <summary>
        /// The number of times to retry the command upon failure.
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// Delay in milliseconds between retry attempts.
        /// </summary>
        public int RetryDelayMs { get; set; } = 200;

        /// <summary>
        /// Delay in milliseconds after sending the request before reading the response.
        /// Useful when devices require processing time before responding.
        /// </summary>
        public int DelayBeforeReadMs { get; set; } = 0;

        /// <summary>
        /// If true, clears the remaining commands in the queue if this command fails.
        /// </summary>
        public bool ClearQueueOnFailure { get; set; } = false;

        /// <summary>
        /// Task completion source to track the execution status of this command.
        /// </summary>
        public TaskCompletionSource<bool> CompletionSource { get; } = new();

        /// <summary>
        /// Executes the command asynchronously on the specified communication channel.
        /// </summary>
        /// <param name="channel">The serial communication channel used to execute this command.</param>
        public async Task ExecuteAsync(SerialComChannel channel)
        {
            await channel.ExecuteCommandAsync<T>(this);
        }
    }
}
