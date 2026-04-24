using System.Text.Json.Serialization;

public sealed class SensorData
{
    private readonly object _lock = new();
    private readonly TemperatureHistory _tempHistory = new();
    private float _tempC;
    private float _humidity;
    private float? _pressureHpa;
    private float? _pressureTempC;
    private string _rainLabel = "";
    private int _rainConfidence;
    private string _rainLikelihood = "";
    private float? _dewPointC;
    private float? _pressureTrend;
    private string _rainAlertMessage = "🌧️ Rain monitor active: Will notify when rain starts or stops";
    private bool _isRaining;
    private DateTime _updatedAt;

    public void Update(float tempC, float humidity, float? pressureHpa, float? pressureTempC,
                       RainPrediction prediction, string rainAlertMessage, bool isRaining)
    {
        lock (_lock)
        {
            _tempC = tempC;
            _humidity = humidity;
            _pressureHpa = pressureHpa;
            _pressureTempC = pressureTempC;
            _rainLabel = prediction.Likelihood.Label();
            _rainConfidence = prediction.ConfidencePct;
            _rainLikelihood = prediction.Likelihood.ToString();
            _dewPointC = prediction.DewPointC;
            _pressureTrend = prediction.PressureTrendHpaPerHour;
            _rainAlertMessage = rainAlertMessage;
            _isRaining = isRaining;
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
                RainConfidence = _rainConfidence,
                RainLikelihood = _rainLikelihood,
                DewPointC = _dewPointC.HasValue ? MathF.Round(_dewPointC.Value, 1) : null,
                PressureTrendHpaPerHour = _pressureTrend.HasValue ? MathF.Round(_pressureTrend.Value, 2) : null,
                RainAlertMessage = _rainAlertMessage,
                IsRaining = _isRaining,
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
    public int RainConfidence { get; set; }
    public string RainLikelihood { get; set; } = "";
    public float? DewPointC { get; set; }
    public float? PressureTrendHpaPerHour { get; set; }
    public string RainAlertMessage { get; set; } = "";
    public bool IsRaining { get; set; }
    public DateTime UpdatedAt { get; set; }
}
