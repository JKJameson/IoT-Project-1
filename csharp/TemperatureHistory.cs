using System.Globalization;

public sealed class TemperatureHistory : IDisposable
{
    private readonly List<(DateTime Time, float TempC)> _readings = new();
    private readonly object _lock = new();
    private readonly string _filePath;
    private const int MaxAgeHours = 168; // 7 days
    private const int SaveIntervalMinutes = 5;
    private DateTime _lastSave = DateTime.MinValue;
    private readonly Timer _saveTimer;

    public TemperatureHistory(string dataDir)
    {
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "temp_history.csv");
        Load();
        _saveTimer = new Timer(_ => SaveIfNeeded(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(SaveIntervalMinutes));
    }

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
                    Time = r.Time.AddHours(1),
                    TempC = MathF.Round(r.TempC, 1)
                })
                .ToList();
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return;

            var lines = File.ReadAllLines(_filePath);
            var cutoff = DateTime.UtcNow.AddHours(-MaxAgeHours);
            _readings.Clear();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(',');
                if (parts.Length != 2) continue;
                if (!DateTime.TryParse(parts[0], out var time)) continue;
                if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var temp)) continue;
                if (time >= cutoff)
                    _readings.Add((time, temp));
            }
            Console.WriteLine($"Loaded {_readings.Count} temperature readings");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load temp history: {ex.Message}");
        }
    }

    private void SaveIfNeeded()
    {
        lock (_lock)
        {
            if ((DateTime.UtcNow - _lastSave).TotalMinutes < SaveIntervalMinutes)
                return;
            SaveInternal();
            _lastSave = DateTime.UtcNow;
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            SaveInternal();
            _lastSave = DateTime.UtcNow;
        }
    }

    private void SaveInternal()
    {
        try
        {
            var lines = _readings.Select(r => $"{r.Time:O},{r.TempC.ToString(CultureInfo.InvariantCulture)}");
            var csv = string.Join(Environment.NewLine, lines);

            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, csv);
            File.Move(tempPath, _filePath, overwrite: true);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to save temp history: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Save();
        _saveTimer.Dispose();
    }
}

public class TemperatureReading
{
    public DateTime Time { get; set; }
    public float TempC { get; set; }
}