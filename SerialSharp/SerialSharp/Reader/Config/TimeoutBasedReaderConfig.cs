namespace SerialSharp.Reader.Config
{
    public class TimeoutBasedReaderConfig : BaseReaderConfig
    {
        /// <summary>
        /// 此为空闲时间，如果超过阈值，并且之前读过数据，则视为帧结束
        /// </summary>
        public int InactivityTimeoutMs { get; set; } = 500;
    }
}
