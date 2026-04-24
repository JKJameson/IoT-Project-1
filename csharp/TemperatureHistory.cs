public sealed class TemperatureHistory
{
    private readonly List<(DateTime Time, float TempC)> _readings = new();
    private readonly object _lock = new();
    private const int MaxAgeHours = 168; // 7 days

    public void AddReading(float tempC)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            _readings.Add((now, tempC));
            _readings.RemoveAll(r => (now - r.Time).TotalHours > MaxAgeHours);
        }
    }

    public List<TemperatureReading> GetReadings(int hours = 168)
    {
        lock (_lock)
        {
            var cutoff = DateTime.UtcNow.AddHours(-hours);
            return _readings
                .Where(r => r.Time >= cutoff)
                .Select(r => new TemperatureReading
                {
                    Time = r.Time.AddHours(1), // Convert to local
                    TempC = MathF.Round(r.TempC, 1)
                })
                .ToList();
        }
    }
}

public class TemperatureReading
{
    public DateTime Time { get; set; }
    public float TempC { get; set; }
}