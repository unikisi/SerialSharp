namespace SerialSharp.Config
{
    public class SerialPortConfig
    {
        public string Port { get; set; } = "COM1";
        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public int Parity { get; set; } = 0;
        public int StopBits { get; set; } = 0;
    }
}
