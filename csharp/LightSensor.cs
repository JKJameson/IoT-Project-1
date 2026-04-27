public sealed class LightSensor
{
    private readonly string? _sensorPath;
    private readonly int _darkThreshold;

    public LightSensor()
    {
        _darkThreshold = ParseIntOrDefault(Environment.GetEnvironmentVariable("LIGHT_DARK_THRESHOLD"), 400);

        var configured = Environment.GetEnvironmentVariable("LIGHT_SENSOR_PATH");
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
        {
            _sensorPath = configured;
            return;
        }

        string[] candidates =
        [
            "/sys/bus/iio/devices/iio:device1/in_voltage0_raw",
            "/sys/bus/iio/devices/iio:device0/in_voltage0_raw",
            "/sys/bus/iio/devices/iio:device1/in_illuminance_raw",
            "/sys/bus/iio/devices/iio:device0/in_illuminance_raw",
        ];

        _sensorPath = candidates.FirstOrDefault(File.Exists);
    }

    public bool IsAvailable => _sensorPath != null;

    public Reading Read()
    {
        if (_sensorPath == null)
            throw new InvalidOperationException("Light sensor path not found");

        var rawText = File.ReadAllText(_sensorPath).Trim();
        if (!int.TryParse(rawText, out var raw))
            throw new Exception($"Invalid light sensor reading: '{rawText}'");

        var condition = raw >= _darkThreshold ? "Light" : "Dark";
        return new Reading(raw, condition);
    }

    private static int ParseIntOrDefault(string? value, int fallback)
        => int.TryParse(value, out var parsed) ? parsed : fallback;

    public record Reading(int RawValue, string Condition);
}
