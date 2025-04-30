using SerialSharp.Config;

namespace SerialSharp
{
    /// <summary>
    /// Manages multiple serial communication channels.
    /// Provides functionality to register, connect, send commands, and manage the lifecycle of serial channels.
    /// </summary>
    public class SerialComManager(Action<string>? logger = null)
    {
        private readonly Dictionary<string, SerialComChannel> _channels = new();
        private readonly Dictionary<string, (SerialPortConfig port, ProtocolConfig proto)> _channelConfigs = new();

        /// <summary>
        /// Registers a new channel with the specified name and configuration.
        /// </summary>
        public void RegisterChannel(string name, SerialPortConfig portConfig, ProtocolConfig protocolConfig)
        {
            if (!_channels.ContainsKey(name))
            {
                _channels[name] = new SerialComChannel(name, portConfig, protocolConfig, logger);
                _channelConfigs[name] = (portConfig, protocolConfig);
            }
        }

        /// <summary>
        /// Connects the specified channel.
        /// </summary>
        public void Connect(string channelName)
        {
            if (_channels.TryGetValue(channelName, out var channel))
                channel.Connect();
            else
                throw new InvalidOperationException($"Channel {channelName} is not registered.");
        }

        /// <summary>
        /// Disconnects the specified channel.
        /// </summary>
        public void Disconnect(string channelName)
        {
            if (_channels.TryGetValue(channelName, out var channel))
                channel.Disconnect();
            else
                throw new InvalidOperationException($"Channel {channelName} is not registered.");
        }

        /// <summary>
        /// Checks if the specified channel is currently connected.
        /// </summary>
        public bool IsConnected(string channelName)
        {
            if (_channels.TryGetValue(channelName, out var channel))
                return channel.IsConnected();

            return false;
        }

        /// <summary>
        /// Updates the configuration of an existing channel.
        /// </summary>
        public void UpdateConfig(string channelName, SerialPortConfig config)
        {
            if (_channels.TryGetValue(channelName, out var channel))
                channel.UpdateConfig(config);
            else
                throw new InvalidOperationException($"Channel {channelName} is not registered.");
        }

        /// <summary>
        /// Sends a command asynchronously to the specified channel.
        /// </summary>
        public async Task<bool> SendAsync<T>(string channelName, SerialCommand<T> command)
        {
            if (_channels.TryGetValue(channelName, out var channel))
            {
                await channel.WriteAsync(command);
                return await command.CompletionSource.Task;
            }

            else
                throw new InvalidOperationException($"Channel {channelName} is not registered.");
        }

        /// <summary>
        /// Stops and removes the specified channel.
        /// </summary>
        public void StopChannel(string channelName)
        {
            if (_channels.TryGetValue(channelName, out var channel))
            {
                channel.Stop();
                _channels.Remove(channelName);
            }
        }

        /// <summary>
        /// Gets the channel instance by name.
        /// </summary>
        public SerialComChannel GetChannel(string name) =>
            _channels.TryGetValue(name, out var channel)
                ? channel
                : throw new InvalidOperationException($"Channel {name} is not registered.");

        /// <summary>
        /// Restarts the specified channel using its previously registered configuration.
        /// </summary>
        public void RestartChannel(string channelName)
        {
            StopChannel(channelName);
            if (_channelConfigs.TryGetValue(channelName, out var config))
            {
                _channels[channelName] = new SerialComChannel(channelName, config.port, config.proto, logger);
                logger?.Invoke($"[{channelName}] Channel restarted.");
            }
        }

        /// <summary>
        /// Stops and clears all registered channels.
        /// </summary>
        public void StopAll()
        {
            foreach (var channel in _channels.Values)
                channel.Stop();
            _channels.Clear();
        }
    }
}
