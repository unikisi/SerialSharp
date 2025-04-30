using RJCP.IO.Ports;
using SerialSharp.Config;
using SerialSharp.Core;
using SerialSharp.Reader;
using SerialSharp.Reader.Config;
using System.Threading.Channels;

namespace SerialSharp
{
    public class SerialComChannel
    {
        private readonly SerialPortStream _port;
        private readonly Channel<IExecutableCommand> _commandQueue;
        private readonly ISerialReader _reader;
        private SerialPortConfig _portConfig;
        private CancellationTokenSource _cts = new();
        private readonly Action<string>? _logger;

        public string Name { get; }

        public SerialComChannel(string name, SerialPortConfig portConfig, ProtocolConfig protocolConfig, Action<string>? logger = null)
        {
            Name = name;
            _logger = logger;
            _portConfig = portConfig;

            _port = new SerialPortStream(portConfig.Port, portConfig.BaudRate)
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
                    _port,
                    config: (LengthBasedReaderConfig)protocolConfig.ReaderConfig
                ),

                SerialReaderType.TimeoutBased => new SerialReaderTimeoutBased(
                    _port,
                    config: (TimeoutBasedReaderConfig)protocolConfig.ReaderConfig
                ),

                _ => throw new NotSupportedException($"未知读取类型：{protocolConfig.ReaderType}")
            };

            _commandQueue = Channel.CreateUnbounded<IExecutableCommand>();
            _ = Task.Run(ProcessLoopAsync);
        }

        public void UpdateConfig(SerialPortConfig newConfig)
        {
            if (_port.IsOpen)
                throw new InvalidOperationException($"[{Name}] 更新配置前必须先断开串口连接。");

            _portConfig = newConfig;
            _logger?.Invoke($"[{Name}] 已更新串口配置。");
        }


        public void Connect()
        {
            if (_port.IsOpen)
                return;

            try
            {
                _port.PortName = _portConfig.Port;
                _port.BaudRate = _portConfig.BaudRate;
                _port.DataBits = _portConfig.DataBits;
                _port.Parity = (Parity)_portConfig.Parity;
                _port.StopBits = (StopBits)_portConfig.StopBits;

                _port.Open();
                _logger?.Invoke($"[{Name}] 串口已连接，波特率={_port.BaudRate}");
            }
            catch (UnauthorizedAccessException ex)
            {
                var msg = $"[{Name}] 串口被占用或无权限：{ex.Message}";
                _logger?.Invoke(msg);
                throw new IOException(msg, ex);
            }
            catch (IOException ex)
            {
                var msg = $"[{Name}] 串口不存在或硬件故障：{ex.Message}";
                _logger?.Invoke(msg);
                throw new IOException(msg, ex);
            }
            catch (Exception ex)
            {
                var msg = $"[{Name}] 串口连接失败：{ex.Message}";
                _logger?.Invoke(msg);
                throw new IOException(msg, ex);
            }
        }

        public void Disconnect()
        {
            try
            {
                _port.Close();
                _logger?.Invoke($"[{Name}] 串口已断开");
            }
            catch (Exception ex)
            {
                _logger?.Invoke($"[{Name}] 串口断开失败：{ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 当前串口是否已连接
        /// </summary>
        public bool IsConnected()
        {
            return _port.IsOpen;
        }

        public async Task WriteAsync<T>(SerialCommand<T> command)
        {
            await _commandQueue.Writer.WriteAsync(command);
        }

        private async Task ProcessLoopAsync()
        {
            try
            {
                while (await _commandQueue.Reader.WaitToReadAsync(_cts.Token))
                {
                    while (_commandQueue.Reader.TryRead(out var command))
                    {
                        await command.ExecuteAsync(this);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Invoke($"[{Name}] 通道异常：{ex.Message}");
            }
        }

        private void CancelPendingQueue()
        {
            _logger?.Invoke($"[{Name}] 清空队列中剩余命令");
            while (_commandQueue.Reader.TryRead(out _)) { }
        }

        public async Task ExecuteCommandAsync<T>(SerialCommand<T> cmd)
        {
            var attempt = 0;

            while (true)
            {
                CancellationTokenSource? timeoutCts = null;
                CancellationTokenSource? linkedCts = null;

                try
                {

                    _logger?.Invoke($"[{Name}] → 发送：{BitConverter.ToString(cmd.RequestBytes)} (第{attempt + 1}次尝试)");
                    await _port.WriteAsync(cmd.RequestBytes, 0, cmd.RequestBytes.Length, _cts.Token);

                    if (cmd.SkipRead)
                    {
                        cmd.CompletionSource.TrySetResult(true);
                        return;
                    }

                    if (cmd.DelayBeforeReadMs > 0)
                    {
                        _logger?.Invoke($"[{Name}] 等待 {cmd.DelayBeforeReadMs}ms 再开始读取响应...");
                        await Task.Delay(cmd.DelayBeforeReadMs, _cts.Token);
                    }

                    timeoutCts = new CancellationTokenSource(cmd.TimeoutMs);
                    linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, timeoutCts.Token);

                    var response = await _reader.ReadAsync(linkedCts.Token);
                    if (response != null)
                    {
                        _logger?.Invoke($"[{Name}] ← 接收：{BitConverter.ToString(response)}");

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
                    if (_cts.IsCancellationRequested)
                    {
                        // 手动取消
                        _logger?.Invoke($"[{Name}] 手动取消命令");
                        cmd.OnCanceled?.Invoke();
                    }
                    else if (timeoutCts!.IsCancellationRequested)
                    {
                        // 读取超时
                        _logger?.Invoke($"[{Name}] 读取超时取消");
                        cmd.OnError?.Invoke(new TimeoutException("设备响应超时"));
                    }

                    if (cmd.ClearQueueOnFailure) CancelPendingQueue();
                    cmd.CompletionSource.TrySetCanceled();
                    return;
                }
                catch (Exception ex)
                {
                    _logger?.Invoke($"[{Name}] 异常：{ex.Message} (第{attempt + 1}次)");
                    if (++attempt > cmd.RetryCount)
                    {
                        cmd.OnError?.Invoke(ex);
                        if (cmd.ClearQueueOnFailure) CancelPendingQueue();

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

        public void Stop()
        {
            _logger?.Invoke($"[{Name}] 通道已停止");
            _cts.Cancel();
        }

        public void CancelCurrentAndClearPending()
        {
            _logger?.Invoke($"[{Name}] 中断当前命令并清空队列");
            _cts.Cancel();
            while (_commandQueue.Reader.TryRead(out _)) { }
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }
    }
}
