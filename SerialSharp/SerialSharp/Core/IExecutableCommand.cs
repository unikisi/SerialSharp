namespace SerialSharp.Core
{
    /// <summary>
    /// Represents a serial command that can be executed by a communication channel.
    /// Implementations define how the command is sent and how the response is processed.
    /// </summary>
    public interface IExecutableCommand
    {
        /// <summary>
        /// Executes the command asynchronously on the specified serial communication channel.
        /// </summary>
        /// <param name="channel">The serial communication channel to execute the command on.</param>
        Task ExecuteAsync(SerialComChannel channel);
    }
}
