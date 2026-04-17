using System.Device.I2c;

public sealed class PressureSensor : IDisposable
{
    private readonly I2cDevice _i2c;
    private ushort _t1, _p1;
    private short _t2, _t3, _p2, _p3, _p4, _p5, _p6, _p7, _p8, _p9;

    public record Reading(float TemperatureC, float PressureHpa);

    public PressureSensor()
    {
        _i2c = I2cDevice.Create(new I2cConnectionSettings(1, 0x76));
        var id = ReadByte(0xD0);
        if (id != 0x58)
            throw new Exception($"BMP280 not found, got chip ID 0x{id:X2}");
        ReadCalibrationData();
    }

    public Reading Read()
    {
        WriteByte(0xF4, 0x27);
        WriteByte(0xF5, 0xA0);
        Thread.Sleep(40);

        Span<byte> raw = stackalloc byte[6];
        _i2c.WriteRead(new byte[] { 0xF7 }, raw);

        int tRaw = (raw[3] << 12) | (raw[4] << 4) | (raw[5] >> 4);
        int pRaw = (raw[0] << 12) | (raw[1] << 4) | (raw[2] >> 4);

        double tFine = CompensateT(tRaw);
        double pressure = CompensateP(pRaw, tFine);
        double temp = tFine / 5120.0;

        return new Reading((float)temp, (float)(pressure / 100.0));
    }

    private void ReadCalibrationData()
    {
        Span<byte> buf = stackalloc byte[24];
        _i2c.WriteRead(new byte[] { 0x88 }, buf);

        _t1 = (ushort)(buf[0] | (buf[1] << 8));
        _t2 = (short)(buf[2] | (buf[3] << 8));
        _t3 = (short)(buf[4] | (buf[5] << 8));
        _p1 = (ushort)(buf[6] | (buf[7] << 8));
        _p2 = (short)(buf[8] | (buf[9] << 8));
        _p3 = (short)(buf[10] | (buf[11] << 8));
        _p4 = (short)(buf[12] | (buf[13] << 8));
        _p5 = (short)(buf[14] | (buf[15] << 8));
        _p6 = (short)(buf[16] | (buf[17] << 8));
        _p7 = (short)(buf[18] | (buf[19] << 8));
        _p8 = (short)(buf[20] | (buf[21] << 8));
        _p9 = (short)(buf[22] | (buf[23] << 8));
    }

    private double CompensateT(int adcT)
    {
        double v1 = (adcT / 16384.0 - _t1 / 1024.0) * _t2;
        double v2 = (adcT / 131072.0 - _t1 / 8192.0) * (adcT / 131072.0 - _t1 / 8192.0) * _t3;
        return v1 + v2;
    }

    private double CompensateP(int adcP, double tFine)
    {
        double v1 = tFine / 2.0 - 64000.0;
        double v2 = v1 * v1 * _p6 / 32768.0;
        v2 += _p5 * v1 * 2.0;
        v2 = v2 / 4.0 + _p4 * 65536.0;
        v1 = (_p3 * v1 * v1 / 524288.0 + _p2 * v1) / 524288.0;
        v1 = (1.0 + v1 / 32768.0) * _p1;
        if (v1 == 0) return 0;
        double p = 1048576.0 - adcP;
        p = (p - v2 / 4096.0) * 6250.0 / v1;
        v1 = _p9 * p * p / 2147483648.0;
        v2 = p * _p8 / 32768.0;
        p = p + (v1 + v2 + _p7) / 16.0;
        return p;
    }

    private byte ReadByte(byte reg)
    {
        _i2c.WriteByte(reg);
        return _i2c.ReadByte();
    }

    private void WriteByte(byte reg, byte val)
    {
        _i2c.Write(new byte[] { reg, val });
    }

    public void Dispose()
    {
        _i2c.Dispose();
    }
}
