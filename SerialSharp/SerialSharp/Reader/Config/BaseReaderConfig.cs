using SerialSharp.Core;

namespace SerialSharp.Reader.Config
{
    public abstract class BaseReaderConfig : IReaderConfig
    {
        /// <summary>
        /// 每次读取串口的最大块大小
        /// </summary>
        public int ChunkSize { get; set; } = 256;

        /// <summary>
        /// 数据包最大接收限制，防止内存炸裂
        /// </summary>
        public int ReadMaxSize { get; set; } = 4096;
    }
}
