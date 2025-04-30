namespace SerialSharp.Config
{
    /// <summary>
    /// Represents the configuration settings required to initialize and open a serial port.
    /// </summary>
    public class SerialPortConfig
    {
        /// <summary>
        /// The name of the serial port to use (e.g., "COM1", "COM3").
        /// </summary>
        public string Port { get; set; } = "COM1";

        /// <summary>
        /// The baud rate for the serial communication (e.g., 9600, 115200).
        /// </summary>
        public int BaudRate { get; set; } = 9600;

        /// <summary>
        /// The number of data bits per byte. Common values are 7 or 8.
        /// </summary>
        public int DataBits { get; set; } = 8;

        /// <summary>
        /// The parity setting for the serial port:
        /// 0 = None, 1 = Odd, 2 = Even, 3 = Mark, 4 = Space.
        /// </summary>
        public int Parity { get; set; } = 0;

        /// <summary>
        /// The number of stop bits used:
        /// 0 = None, 1 = One, 2 = Two, 3 = OnePointFive.
        /// </summary>
        public int StopBits { get; set; } = 0;
    }
}
