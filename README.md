# SerialSharp

> High-performance, extensible serial communication framework for .NET  
> Designed for device integration, industrial control, and lab automation.

## Features

- Fully async command queue execution
- Generic response parsing with `Func<byte[], T>`
- Length-based and timeout-based reading strategies
- Built-in retry, timeout, and cancellation support
- Channel manager for multi-device orchestration
- Minimal dependencies (uses [RJCP.SerialPortStream](https://github.com/jcurl/serialportstream) internally)

## Installation

Install via NuGet:

```bash
dotnet add package SerialSharp
```

## Basic Usage

```csharp
var manager = new SerialComManager();

var portConfig = new SerialPortConfig
{
    Port = "COM3",
    BaudRate = 9600,
    DataBits = 8,
    Parity = 0,
    StopBits = 1
};

var protocolConfig = new ProtocolConfig
{
    ReaderType = SerialReaderType.LengthBased,
    ReaderConfig = new LengthBasedReaderConfig
    {
        ChunkSize = 256,
        ReadMaxSize = 1024,
        DataSegmentsByteIndex = 4,
        TotalExceptDataSegLength = 6
    }
};

manager.RegisterChannel("DeviceA", portConfig, protocolConfig);
manager.Connect("DeviceA");

var command = new SerialCommand<string>
{
    RequestBytes = new byte[] { 0xAA, 0x01, 0x05, 0xCC },
    TimeoutMs = 3000,
    ParseFunc = raw =>
    {
        var payload = raw[3..^2];
        return Encoding.ASCII.GetString(payload);
    },
    OnParsedResponse = result => Console.WriteLine($"Received: {result}"),
    OnError = ex => Console.WriteLine($"Error: {ex.Message}")
};

await manager.SendAsync("DeviceA", command);
```

## Advanced Features

| Feature               | Description                                                                 |
|----------------------|-----------------------------------------------------------------------------|
| `RetryCount`         | Set retry attempts for fragile connections                                  |
| `DelayBeforeReadMs`  | Add delay after sending but before reading (some devices need this)         |
| `ClearQueueOnFailure`| Automatically flush queue on failure                                        |
| `SkipRead`           | Send-only support for no-response commands                                  |
| `TimeoutBasedReader` | Read until no new bytes arrive for `InactivityTimeoutMs`                    |

## Test / Debugging

Enable logging by passing a logger to `SerialComManager`:

```csharp
var manager = new SerialComManager(msg => Console.WriteLine("[Log] " + msg));
```

## Project Structure

- `SerialComManager` – Manages all registered channels
- `SerialComChannel` – Handles port I/O and command queue
- `ISerialReader` – Strategy interface for different reading logic
- `SerialCommand<T>` – Encapsulates a serial transaction
- `ProtocolConfig` – Defines the reader behavior per device
