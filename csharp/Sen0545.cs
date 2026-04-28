using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

public sealed class Sen0545 : IDisposable
{
    private const byte FrameHeader = 0x3A;
    private const byte ReadFlag = 0x00;
    private const byte WriteFlag = 0x80;
    private const byte CmdReadFirmware = 0x00;
    private const byte CmdRainfallStatus = 0x01;
    private const byte CmdSystemStatus = 0x02;
    private const byte CmdOpticalCalibration = 0x03;
    private const byte CmdRealtimeMode = 0x04;
    private const byte CmdOutputFrequency = 0x05;
    private const byte CmdThresholdV1 = 0x06;
    private const byte CmdThresholdV2 = 0x07;
    private const byte CmdThresholdV3 = 0x08;
    private const byte CmdThresholdS1 = 0x09;
    private const byte CmdThresholdS2 = 0x0A;
    private const byte CmdThresholdS3 = 0x0B;
    private const byte CmdThresholdN1 = 0x0C;
    private const byte CmdThresholdN2 = 0x0D;
    private const byte CmdThresholdN3 = 0x0E;
    private const byte CmdAmbientLight = 0x0F;
    private const byte CmdChipTemperature = 0x10;
    private const byte CmdSleepMode = 0x11;

    private const byte Polynomial = 0x31;
    private const byte CrcInitial = 0xFF;
    private const byte CrcXor = 0x00;

    private readonly SerialPort _serial;
    private readonly object _lock = new();
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
    }

    public bool IsOpen => _serial.IsOpen;

    private static string? FindPort()
    {
        var ports = SerialPort.GetPortNames();
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

    public RainData Read()
    {
        lock (_lock)
        {
            FlushBuffer();
            var cmd = BuildFrame(ReadFlag, CmdRainfallStatus, 0, 0);
            _serial.Write(cmd, 0, cmd.Length);
            var response = ReadFrame();
            if (response == null || response.Flag != (WriteFlag | CmdRainfallStatus))
                throw new InvalidOperationException("Invalid rain status response");

            var level = response.DataLo switch
            {
                0 => RainLevel.NoRain,
                1 => RainLevel.Light,
                2 => RainLevel.Moderate,
                3 => RainLevel.Heavy,
                _ => RainLevel.Unknown
            };

            FlushBuffer();

            var tempCmd = BuildFrame(ReadFlag, CmdChipTemperature, 0, 0);
            _serial.Write(tempCmd, 0, tempCmd.Length);
            var tempResponse = ReadFrame();
            var tempC = -40f;
            if (tempResponse != null && tempResponse.Flag == (WriteFlag | CmdChipTemperature))
            {
                tempC = TempFromRaw(tempResponse.DataLo);
            }

            FlushBuffer();

            var lightCmd = BuildFrame(ReadFlag, CmdAmbientLight, 0, 0);
            _serial.Write(lightCmd, 0, lightCmd.Length);
            var lightResponse = ReadFrame();
            var lightLux = 0;
            if (lightResponse != null && lightResponse.Flag == (WriteFlag | CmdAmbientLight))
            {
                lightLux = lightResponse.DataLo | (lightResponse.DataHi << 8);
            }

            return new RainData(level, tempC, lightLux, DateTime.Now);
        }
    }

    private static float TempFromRaw(byte raw)
    {
        return raw * 5f - 40f;
    }

    public void SetSensitivity(byte v1, byte s1)
    {
        lock (_lock)
        {
            FlushBuffer();
            var cmd = BuildFrame(WriteFlag, CmdThresholdV1, v1, 0);
            _serial.Write(cmd, 0, cmd.Length);
            ReadFrame();

            cmd = BuildFrame(WriteFlag, CmdThresholdS1, s1, 0);
            _serial.Write(cmd, 0, cmd.Length);
            ReadFrame();
        }
    }

    public void SetLowPowerMode(bool enable)
    {
        lock (_lock)
        {
            FlushBuffer();
            var cmd = BuildFrame(WriteFlag, CmdSleepMode, (byte)(enable ? 1 : 0), 0);
            _serial.Write(cmd, 0, cmd.Length);
            ReadFrame();
        }
    }

    public void EnterRealtimeMode()
    {
        lock (_lock)
        {
            FlushBuffer();
            var cmd = BuildFrame(WriteFlag, CmdRealtimeMode, 1, 0);
            _serial.Write(cmd, 0, cmd.Length);
            ReadFrame();
        }
    }

    public void ExitRealtimeMode()
    {
        lock (_lock)
        {
            FlushBuffer();
            var cmd = BuildFrame(WriteFlag, CmdRealtimeMode, 0, 0);
            _serial.Write(cmd, 0, cmd.Length);
            ReadFrame();
        }
    }

    public SystemStatus ReadSystemStatus()
    {
        lock (_lock)
        {
            FlushBuffer();
            var cmd = BuildFrame(ReadFlag, CmdSystemStatus, 0, 0);
            _serial.Write(cmd, 0, cmd.Length);
            var response = ReadFrame();
            if (response == null || response.Flag != (WriteFlag | CmdSystemStatus))
                return SystemStatus.Unknown;

            return response.DataLo switch
            {
                0 => SystemStatus.Normal,
                1 => SystemStatus.InternalCommError,
                2 => SystemStatus.LedaDamaged,
                3 => SystemStatus.LedbDamaged,
                4 => SystemStatus.CalibrationNotGood,
                5 => SystemStatus.ParamWriteFailure,
                6 => SystemStatus.SerialCheckError,
                7 => SystemStatus.LowVoltage,
                _ => SystemStatus.Unknown
            };
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
        frame[4] = Crc8(frame.AsSpan(0, 4));
        return frame;
    }

    private FrameResponse? ReadFrame()
    {
        var headerIndex = 0;
        var headerBuf = new byte[1];
        var deadline = DateTime.Now.AddMilliseconds(_serial.ReadTimeout);

        while (DateTime.Now < deadline)
        {
            var bytesRead = _serial.Read(headerBuf, 0, 1);
            if (bytesRead == 1)
            {
                if (headerBuf[0] == FrameHeader)
                {
                    var frame = new byte[4];
                    var frameRead = 0;
                    var frameDeadline = DateTime.Now.AddMilliseconds(500);
                    while (frameRead < 4 && DateTime.Now < frameDeadline)
                    {
                        var n = _serial.Read(frame, frameRead, 4 - frameRead);
                        if (n > 0) frameRead += n;
                    }
                    if (frameRead == 4)
                    {
                        var fcs = Crc8(frame.AsSpan(0, 4));
                        if (fcs == frame[4])
                        {
                            return new FrameResponse(
                                frame[0],
                                frame[1],
                                frame[2],
                                frame[3]
                            );
                        }
                    }
                    return null;
                }
            }
        }
        return null;
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

    private record FrameResponse(byte Flag, byte DataLo, byte DataHi, byte Fcs);

    public static byte[] ComputeCrc(byte[] data) => new[] { Crc8(data.AsSpan(0, data.Length)) };
}

public enum RainLevel
{
    NoRain = 0,
    Light = 1,
    Moderate = 2,
    Heavy = 3,
    Unknown = -1
}

public enum SystemStatus
{
    Normal = 0,
    InternalCommError = 1,
    LedaDamaged = 2,
    LedbDamaged = 3,
    CalibrationNotGood = 4,
    ParamWriteFailure = 5,
    SerialCheckError = 6,
    LowVoltage = 7,
    Unknown = -1
}

public record RainData(
    RainLevel Level,
    float TemperatureC,
    int LightLux,
    DateTime Timestamp
);