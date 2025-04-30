using SerialSharp.Config;

namespace SerialSharp
{
    public class SerialComManager(Action<string>? logger = null)
    {
        private readonly Dictionary<string, SerialComChannel> _channels = new();
        private readonly Dictionary<string, (SerialPortConfig port, ProtocolConfig proto)> _channelConfigs = new();

        public void RegisterChannel(string name, SerialPortConfig portConfig, ProtocolConfig protocolConfig)
        {
            if (!_channels.ContainsKey(name))
            {
                _channels[name] = new SerialComChannel(name, portConfig, protocolConfig, logger);
                _channelConfigs[name] = (portConfig, protocolConfig);
            }
        }

        public void Connect(string channelName)
        {
            if (_channels.TryGetValue(channelName, out var channel))
                channel.Connect();
            else
                throw new InvalidOperationException($"通道 {channelName} 未注册");
        }

        public void Disconnect(string channelName)
        {
            if (_channels.TryGetValue(channelName, out var channel))
                channel.Disconnect();
            else
                throw new InvalidOperationException($"通道 {channelName} 未注册");
        }

        public bool IsConnected(string channelName)
        {
            if (_channels.TryGetValue(channelName, out var channel))
                return channel.IsConnected();

            return false;
        }

        public void UpdateConfig(string channelName, SerialPortConfig config)
        {
            if (_channels.TryGetValue(channelName, out var channel))
                channel.UpdateConfig(config);
            else
                throw new InvalidOperationException($"通道 {channelName} 未注册");
        }

        public async Task<bool> SendAsync<T>(string channelName, SerialCommand<T> command)
        {
            if (_channels.TryGetValue(channelName, out var channel))
            {
                await channel.WriteAsync(command);
                return await command.CompletionSource.Task;
            }

            else
                throw new InvalidOperationException($"通道 {channelName} 未注册");
        }

        public void StopChannel(string channelName)
        {
            if (_channels.TryGetValue(channelName, out var channel))
            {
                channel.Stop();
                _channels.Remove(channelName);
            }
        }

        public SerialComChannel GetChannel(string name) =>
            _channels.TryGetValue(name, out var channel)
                ? channel
                : throw new InvalidOperationException($"通道 {name} 未注册");

        public void RestartChannel(string channelName)
        {
            StopChannel(channelName);
            if (_channelConfigs.TryGetValue(channelName, out var config))
            {
                _channels[channelName] = new SerialComChannel(channelName, config.port, config.proto, logger);
                logger?.Invoke($"[{channelName}] 通道已重启");
            }
        }

        public void StopAll()
        {
            foreach (var channel in _channels.Values)
                channel.Stop();
            _channels.Clear();
        }
    }
}
