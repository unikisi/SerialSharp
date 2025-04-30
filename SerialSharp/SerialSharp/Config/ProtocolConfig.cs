using SerialSharp.Core;

namespace SerialSharp.Config
{
    public class ProtocolConfig
    {
        public SerialReaderType ReaderType { get; set; }
        public IReaderConfig ReaderConfig { get; set; } = null!;
    }
}
