using System.Text.Json.Serialization;

public sealed class SensorData
{
    private readonly object _lock = new();
    private readonly TemperatureHistory _tempHistory;
    private float _tempC;
    private float _humidity;
    private float? _pressureHpa;
    private float? _pressureTempC;
    private string _rainLabel = "";
    private string _rainAlertMessage = "🌧️ Rain monitor active: Will notify when rain starts or stops";
    private bool _isRaining;
    private string _sunriseLocal = "";
    private string _sunsetLocal = "";
    private DateTime _updatedAt;

    public SensorData(string dataDir)
    {
        _tempHistory = new TemperatureHistory(dataDir);
    }

    public void Update(float tempC, float humidity, float? pressureHpa, float? pressureTempC,
                       RainData? rainData, string rainLabel, string rainAlertMessage, bool isRaining,
                       string? sunriseLocal = null, string? sunsetLocal = null)
    {
        lock (_lock)
        {
            _tempC = tempC;
            _humidity = humidity;
            _pressureHpa = pressureHpa;
            _pressureTempC = pressureTempC;
            _rainLabel = rainLabel;
            if (rainData != null)
            {
                _rainLabel = rainData.Level.ToString();
            }
            _rainAlertMessage = rainAlertMessage;
            _isRaining = isRaining;
            _sunriseLocal = sunriseLocal ?? "";
            _sunsetLocal = sunsetLocal ?? "";
            _updatedAt = DateTime.UtcNow;
            _tempHistory.AddReading(tempC);
        }
    }

    public Snapshot Take()
    {
        lock (_lock)
        {
            return new Snapshot
            {
                TemperatureC = MathF.Round(_tempC, 1),
                Humidity = MathF.Round(_humidity, 0),
                PressureHpa = _pressureHpa.HasValue ? MathF.Round(_pressureHpa.Value, 1) : null,
                PressureTempC = _pressureTempC.HasValue ? MathF.Round(_pressureTempC.Value, 1) : null,
                RainLabel = _rainLabel,
                RainAlertMessage = _rainAlertMessage,
                IsRaining = _isRaining,
                SunriseLocal = _sunriseLocal,
                SunsetLocal = _sunsetLocal,
                UpdatedAt = _updatedAt,
            };
        }
    }

    public List<TemperatureReading> GetTemperatureHistory(int hours = 168)
    {
        return _tempHistory.GetReadings(hours);
    }
}

public class Snapshot
{
    public float TemperatureC { get; set; }
    public float Humidity { get; set; }
    public float? PressureHpa { get; set; }
    public float? PressureTempC { get; set; }
    public string RainLabel { get; set; } = "";
    public string RainAlertMessage { get; set; } = "";
    public bool IsRaining { get; set; }
    public string SunriseLocal { get; set; } = "";
    public string SunsetLocal { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
}