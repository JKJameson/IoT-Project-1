using System;
using System.IO.Ports;

public sealed class Sen0545 : IDisposable
{
    private const byte FrameHeader = 0x3A;
    private const byte ReadFlag = 0x00;
    private const byte WriteFlag = 0x80;
    private const byte CmdRainfallStatus = 0x01;

    private const byte Polynomial = 0x31;
    private const byte CrcInitial = 0xFF;
    private const byte CrcXor = 0x00;

    private readonly SerialPort _serial;
    private readonly object _lock = new();
    private RainLevel _heldLevel = RainLevel.Clear;
    private DateTime _lastRainUtc = DateTime.MinValue;
    private static readonly TimeSpan RainHoldDuration = TimeSpan.FromSeconds(30);
    private bool _disposed;

    public Sen0545(string? port = null, int baudRate = 115200)
    {
        if (string.IsNullOrEmpty(port))
        {
            port = FindPort();
            if (string.IsNullOrEmpty(port))
                throw new InvalidOperationException("SEN0545 not found on any serial port");
        }

        _serial = new SerialPort(port, baudRate, Parity.None, 8, StopBits.One)
        {
            ReadTimeout = 1000,
            WriteTimeout = 1000
        };
        _serial.Open();
        Console.WriteLine($"SEN0545: Opened on {port}");

        try
        {
            FlushBuffer();
            var exitCmd = BuildFrame(WriteFlag, 0x04, 0, 0);
            _serial.Write(exitCmd, 0, exitCmd.Length);
            Thread.Sleep(200);
            ReadFrame();
            FlushBuffer();
        }
        catch { }
    }

    public static string? GetDefaultPort()
    {
        string? envPort = Environment.GetEnvironmentVariable("SEN0545_PORT");
        if (!string.IsNullOrEmpty(envPort))
            return envPort;
        string? autoPort = FindPort();
        if (autoPort != null)
            return autoPort;
        if (File.Exists("/dev/serial0")) return "/dev/serial0";
        if (File.Exists("/dev/ttyS0")) return "/dev/ttyS0";
        return null;
    }

    public bool IsOpen => _serial.IsOpen;

    private static string? FindPort()
    {
        var ports = SerialPort.GetPortNames();
        Console.WriteLine($"SEN0545: Available ports: {string.Join(", ", ports)}");
        foreach (var p in ports)
        {
            try
            {
                using var sp = new SerialPort(p, 115200, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 500,
                    WriteTimeout = 500
                };
                sp.Open();
                sp.DiscardInBuffer();

                var cmd = BuildFrame(ReadFlag, CmdRainfallStatus, 0, 0);
                sp.Write(cmd, 0, cmd.Length);

                var response = new byte[5];
                var bytesRead = sp.Read(response, 0, 5);
                if (bytesRead == 5 && response[0] == FrameHeader && (response[1] & 0x7F) == CmdRainfallStatus)
                {
                    sp.Close();
                    return p;
                }
                sp.Close();
            }
            catch { }
        }
        return null;
    }

    public RainData? TryRead()
    {
        lock (_lock)
        {
            FlushBuffer();
            var cmd = BuildFrame(ReadFlag, CmdRainfallStatus, 0, 0);
            _serial.Write(cmd, 0, cmd.Length);
            Thread.Sleep(100);
            var response = ReadFrame();

            if (response == null || response.Flag != (WriteFlag | CmdRainfallStatus))
                return null;

            var rawLevel = response.DataLo switch
            {
                0x00 => RainLevel.Clear,
                0x01 => RainLevel.Light,
                0x02 => RainLevel.Moderate,
                0x03 => RainLevel.Heavy,
                _ => RainLevel.Unknown
            };

            var now = DateTime.UtcNow;
            var level = rawLevel;

            if (rawLevel != RainLevel.Clear && rawLevel != RainLevel.Unknown)
            {
                _heldLevel = rawLevel;
                _lastRainUtc = now;
            }
            else if (rawLevel == RainLevel.Clear && now - _lastRainUtc < RainHoldDuration)
            {
                level = _heldLevel;
            }

            Console.WriteLine($"SEN0545 level={level}");
            return new RainData(level, response.DataLo, DateTime.Now);
        }
    }

    private void FlushBuffer()
    {
        _serial.DiscardInBuffer();
        _serial.DiscardOutBuffer();
    }

    private static byte[] BuildFrame(byte flag, byte dataNumber, byte dataLo, byte dataHi)
    {
        var frame = new byte[5];
        frame[0] = FrameHeader;
        frame[1] = (byte)(flag | dataNumber);
        frame[2] = dataLo;
        frame[3] = dataHi;
        frame[4] = Crc8(frame.AsSpan(1, 3));
        return frame;
    }

    private FrameResponse? ReadFrame()
    {
        var headerBuf = new byte[1];
        var deadline = DateTime.Now.AddMilliseconds(_serial.ReadTimeout);

        while (DateTime.Now < deadline)
        {
            var bytesRead = _serial.Read(headerBuf, 0, 1);
            if (bytesRead == 1 && headerBuf[0] == FrameHeader)
            {
                var data = new byte[5];
                data[0] = FrameHeader;
                var frameRead = 1;
                var frameDeadline = DateTime.Now.AddMilliseconds(500);
                while (frameRead < 5 && DateTime.Now < frameDeadline)
                {
                    var n = _serial.Read(data, frameRead, 5 - frameRead);
                    if (n > 0) frameRead += n;
                }
                if (frameRead == 5)
                {
                    var crc = Crc8(data.AsSpan(1, 3));
                    if (crc == data[4])
                    {
                        return new FrameResponse(data[1], data[2], data[3]);
                    }
                    Console.WriteLine($"SEN0545 CRC mismatch: got 0x{data[4]:X2}, expected 0x{crc:X2}");
                }
                else
                {
                    Console.WriteLine($"SEN0545 short read: {frameRead}/5");
                }
            }
        }
        return null;
    }

    private record FrameResponse(byte Flag, byte DataLo, byte DataHi);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        lock (_lock)
        {
            if (_serial.IsOpen)
                _serial.Close();
            _serial.Dispose();
        }
    }

    private static byte Crc8(ReadOnlySpan<byte> data)
    {
        byte crc = CrcInitial;
        foreach (var b in data)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
            {
                if ((crc & 0x80) != 0)
                    crc = (byte)((crc << 1) ^ Polynomial);
                else
                    crc <<= 1;
            }
        }
        return (byte)(crc ^ CrcXor);
    }
}

public enum RainLevel
{
    Clear = 0,
    Light = 1,
    Moderate = 2,
    Heavy = 3,
    Unknown = -1
}

public record RainData(
    RainLevel Level,
    int RawValue,
    DateTime Timestamp
);