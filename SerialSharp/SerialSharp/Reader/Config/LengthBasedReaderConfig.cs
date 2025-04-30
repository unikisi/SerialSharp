namespace SerialSharp.Reader.Config
{
    public class LengthBasedReaderConfig : BaseReaderConfig
    {
        /// <summary>
        /// 数据部分长度所在字节索引
        /// </summary>
        public int DataSegmentsByteIndex { get; set; }

        /// <summary>
        /// 除去数据部分之外的总字节数
        /// </summary>
        public int TotalExceptDataSegLength { get; set; }
    }
}
