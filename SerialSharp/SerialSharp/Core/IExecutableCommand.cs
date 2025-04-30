namespace SerialSharp.Core
{
    /// <summary>
    /// 表示一个可执行的串口命令。
    /// 所有需要通过 <see cref="SerialComChannel" /> 调度执行的命令类型都应实现此接口。
    /// </summary>
    public interface IExecutableCommand
    {
        /// <summary>
        /// 在指定通道上异步执行命令逻辑。
        /// </summary>
        /// <param name="channel">当前命令所依附的串口通道。</param>
        /// <returns>表示命令执行过程的异步任务。</returns>
        Task ExecuteAsync(SerialComChannel channel);
    }
}
