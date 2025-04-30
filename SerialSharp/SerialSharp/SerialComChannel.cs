using RJCP.IO.Ports;
using SerialSharp.Config;
using SerialSharp.Core;
using SerialSharp.Reader;
using SerialSharp.Reader.Config;
using System.Threading.Channels;

namespace SerialSharp
{
    /// <summary>
    /// Represents a single serial communication channel.
    /// Manages serial port lifecycle, command queue execution, and response handling.
    /// </summary>
    public class SerialComChannel
    {
        private readonly SerialPortStream _serialPort;
        private readonly Channel<IExecutableCommand> _commandQueue;
        private readonly ISerialReader _reader;
        private SerialPortConfig _serialPortConfig;
        private CancellationTokenSource _channelCts = new();
        private readonly Action<string>? _logger;

        public string Name { get; }

        public SerialComChannel(string name, SerialPortConfig portConfig, ProtocolConfig protocolConfig, Action<string>? logger = null)
        {
            Name = name;
            _logger = logger;
            _serialPortConfig = portConfig;

            _serialPort = new SerialPortStream(portConfig.Port, portConfig.BaudRate)
            {
                DataBits = portConfig.DataBits,
                Parity = (Parity)portConfig.Parity,
                StopBits = (StopBits)portConfig.StopBits,
                ReadTimeout = 5000,
                WriteTimeout = 5000
            };

            _reader = protocolConfig.ReaderType switch
            {
                SerialReaderType.LengthBased => new SerialReaderLengthBased(
                    _serialPort,
                    config: (LengthBasedReaderConfig)protocolConfig.ReaderConfig
                ),

                SerialReaderType.TimeoutBased => new SerialReaderTimeoutBased(
                    _serialPort,
                    config: (TimeoutBasedReaderConfig)protocolConfig.ReaderConfig
                ),

                _ => throw new NotSupportedException($"Unsupported reader type: {protocolConfig.ReaderType}")
            };

            _commandQueue = Channel.CreateUnbounded<IExecutableCommand>();
            _ = Task.Run(ProcessCommandLoopAsync);
        }

        /// <summary>
        /// Updates the serial port configuration. Can only be called when disconnected.
        /// </summary>
        public void UpdateConfig(SerialPortConfig newConfig)
        {
            if (_serialPort.IsOpen)
                throw new InvalidOperationException($"[{Name}] Port must be disconnected before updating config.");

            _serialPortConfig = newConfig;
            _logger?.Invoke($"[{Name}] Serial port configuration updated.");
        }

        /// <summary>
        /// Opens the serial port connection.
        /// </summary>
        public void Connect()
        {
            if (_serialPort.IsOpen)
                return;

            try
            {
                _serialPort.PortName = _serialPortConfig.Port;
                _serialPort.BaudRate = _serialPortConfig.BaudRate;
                _serialPort.DataBits = _serialPortConfig.DataBits;
                _serialPort.Parity = (Parity)_serialPortConfig.Parity;
                _serialPort.StopBits = (StopBits)_serialPortConfig.StopBits;

                _serialPort.Open();
                _logger?.Invoke($"[{Name}] Port connected. BaudRate={_serialPort.BaudRate}");
            }
            catch (UnauthorizedAccessException ex)
            {
                var msg = $"[{Name}] Port is busy or access denied: {ex.Message}";
                _logger?.Invoke(msg);
                throw new IOException(msg, ex);
            }
            catch (IOException ex)
            {
                var msg = $"[{Name}] Port not found or hardware failure: {ex.Message}";
                _logger?.Invoke(msg);
                throw new IOException(msg, ex);
            }
            catch (Exception ex)
            {
                var msg = $"[{Name}] Failed to connect port: {ex.Message}";
                _logger?.Invoke(msg);
                throw new IOException(msg, ex);
            }
        }

        /// <summary>
        /// Closes the serial port connection.
        /// </summary>
        public void Disconnect()
        {
            try
            {
                _serialPort.Close();
                _logger?.Invoke($"[{Name}] Port disconnected.");
            }
            catch (Exception ex)
            {
                _logger?.Invoke($"[{Name}] Failed to disconnect port: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Indicates whether the serial port is currently open.
        /// </summary>
        public bool IsConnected() => _serialPort.IsOpen;

        /// <summary>
        /// Adds a serial command to the processing queue.
        /// </summary>
        public async Task WriteAsync<T>(SerialCommand<T> command)
        {
            await _commandQueue.Writer.WriteAsync(command);
        }

        private async Task ProcessCommandLoopAsync()
        {
            try
            {
                while (await _commandQueue.Reader.WaitToReadAsync(_channelCts.Token))
                {
                    while (_commandQueue.Reader.TryRead(out var command))
                    {
                        await command.ExecuteAsync(this);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Invoke($"[{Name}] Command loop error: {ex.Message}");
            }
        }

        private void ClearPendingQueue()
        {
            _logger?.Invoke($"[{Name}] Clearing pending commands in queue.");
            while (_commandQueue.Reader.TryRead(out _)) { }
        }

        /// <summary>
        /// Executes the specified serial command with retry and timeout support.
        /// </summary>
        public async Task ExecuteCommandAsync<T>(SerialCommand<T> cmd)
        {
            var attempt = 0;

            while (true)
            {
                CancellationTokenSource? timeoutCts = null;
                CancellationTokenSource? linkedCts = null;

                try
                {
                    _logger?.Invoke($"[{Name}] → Sending: {BitConverter.ToString(cmd.RequestBytes)} (Attempt {attempt + 1})");
                    await _serialPort.WriteAsync(cmd.RequestBytes, 0, cmd.RequestBytes.Length, _channelCts.Token);

                    if (cmd.SkipRead)
                    {
                        cmd.CompletionSource.TrySetResult(true);
                        return;
                    }

                    if (cmd.DelayBeforeReadMs > 0)
                    {
                        _logger?.Invoke($"[{Name}] Waiting {cmd.DelayBeforeReadMs}ms before reading response...");
                        await Task.Delay(cmd.DelayBeforeReadMs, _channelCts.Token);
                    }

                    timeoutCts = new CancellationTokenSource(cmd.TimeoutMs);
                    linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_channelCts.Token, timeoutCts.Token);

                    var response = await _reader.ReadAsync(linkedCts.Token);
                    if (response != null)
                    {
                        _logger?.Invoke($"[{Name}] ← Received: {BitConverter.ToString(response)}");

                        if (cmd.ParseFunc != null && cmd.OnParsedResponse != null)
                        {
                            var parsed = cmd.ParseFunc(response);
                            cmd.OnParsedResponse(parsed);
                        }

                        cmd.CompletionSource.TrySetResult(true);
                        return;
                    }
                }
                catch (OperationCanceledException ex)
                {
                    if (_channelCts.IsCancellationRequested)
                    {
                        _logger?.Invoke($"[{Name}] Command manually canceled.");
                        cmd.OnCanceled?.Invoke();
                    }
                    else if (timeoutCts!.IsCancellationRequested)
                    {
                        _logger?.Invoke($"[{Name}] Read timeout.");
                        cmd.OnError?.Invoke(new TimeoutException("Device response timeout."));
                    }

                    if (cmd.ClearQueueOnFailure) ClearPendingQueue();
                    cmd.CompletionSource.TrySetCanceled();
                    return;
                }
                catch (Exception ex)
                {
                    _logger?.Invoke($"[{Name}] Error: {ex.Message} (Attempt {attempt + 1})");
                    if (++attempt > cmd.RetryCount)
                    {
                        cmd.OnError?.Invoke(ex);
                        if (cmd.ClearQueueOnFailure) ClearPendingQueue();

                        cmd.CompletionSource.TrySetException(ex);
                        return;
                    }

                    await Task.Delay(cmd.RetryDelayMs, linkedCts!.Token);
                }
                finally
                {
                    timeoutCts?.Dispose();
                    linkedCts?.Dispose();
                }
            }
        }

        /// <summary>
        /// Stops the channel and cancels all ongoing operations.
        /// </summary>
        public void Stop()
        {
            _logger?.Invoke($"[{Name}] Channel stopped.");
            _channelCts.Cancel();
        }

        /// <summary>
        /// Cancels the currently running command and clears any pending commands in the queue.
        /// </summary>
        public void CancelCurrentAndClearPending()
        {
            _logger?.Invoke($"[{Name}] Canceling current command and clearing queue.");
            _channelCts.Cancel();
            while (_commandQueue.Reader.TryRead(out _)) { }
            _channelCts.Dispose();
            _channelCts = new CancellationTokenSource();
        }
    }
}
