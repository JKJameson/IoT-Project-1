using System.Net;
using System.Net.Mail;
using System.Net.Http;
using System.Text.Json;

public sealed class AlertService
{
    private readonly string _emailTo;
    private readonly TimeSpan _cooldown;
    private readonly object _lock = new();
    private readonly HttpClient _http = new();

    private const string? TelegramBotToken = "8429669155:AAHfLkioktJVb0tNvA3-fqONybYaq8xp9mY";
    private const string? TelegramChatId = "-5228658452";

    private bool _isRaining;
    private DateTime _lastNotificationTime = DateTime.MinValue;
    private string _lastNotificationType = "";

    private readonly Queue<(DateTime Time, bool SensorRain, float Humidity, float? PressureHpa, int LightRaw)> _history = new();
    private const int HistorySizeMinutes = 10;

    public bool IsRaining => _isRaining;

    public AlertService(string emailTo, TimeSpan? cooldown = null)
    {
        _emailTo = emailTo;
        _cooldown = cooldown ?? TimeSpan.FromMinutes(5);
    }

    public (string Message, bool IsRaining) CheckWeather(
        RainData? rainData,
        float tempC,
        float humidity,
        float? pressureHpa,
        int? lightRaw)
    {
        lock (_lock)
        {
            bool wasRaining = _isRaining;

            AddToHistory(rainData, humidity, pressureHpa, lightRaw);

            var confidence = CalculateRainConfidence(rainData, humidity, pressureHpa, lightRaw);
            bool isNowRaining = confidence >= 60;

            string message;
            if (isNowRaining && !wasRaining)
            {
                _isRaining = true;
                message = $"🌧️⚠️ RAIN CONFIRMED ({confidence}% confidence) - {DateTime.Now:HH:mm:ss}";

                if (ShouldNotify("start"))
                {
                    _lastNotificationTime = DateTime.Now;
                    _lastNotificationType = "start";
                    var details = BuildAlertDetails(rainData, tempC, humidity, pressureHpa, lightRaw, confidence);
                    SendEmail("🌧️ RAIN CONFIRMED at your weather station", details);
                    SendTelegramAsync($"🌧️ RAIN CONFIRMED\n\n{confidence}% confidence\n{details}").Wait();
                }
            }
            else if (!isNowRaining && wasRaining)
            {
                _isRaining = false;
                message = $"☀️ Rain cleared ({confidence}% confidence) - {DateTime.Now:HH:mm:ss}";

                if (ShouldNotify("stop"))
                {
                    _lastNotificationTime = DateTime.Now;
                    _lastNotificationType = "stop";
                    var details = BuildAlertDetails(rainData, tempC, humidity, pressureHpa, lightRaw, confidence);
                    SendEmail("☀️ RAIN STOPPED at your weather station", details);
                    SendTelegramAsync($"☀️ RAIN STOPPED\n\nCleared at {DateTime.Now:HH:mm}").Wait();
                }
            }
            else
            {
                message = _isRaining
                    ? $"🌧️ Rain active ({confidence}%)"
                    : $"☁️ No rain ({confidence}% confident)";
            }

            return (message, _isRaining);
        }
    }

    private void AddToHistory(RainData? rainData, float humidity, float? pressureHpa, int? lightRaw)
    {
        var now = DateTime.UtcNow;
        bool sensorRain = rainData != null && rainData.Level != RainLevel.NoRain && rainData.Level != RainLevel.Unknown;
        _history.Enqueue((now, sensorRain, humidity, pressureHpa, lightRaw ?? -1));

        while (_history.Count > 0 && (now - _history.Peek().Time).TotalMinutes > HistorySizeMinutes)
            _history.Dequeue();
    }

    private int CalculateRainConfidence(RainData? rainData, float humidity, float? pressureHpa, int? lightRaw)
    {
        int score = 0;
        int factors = 0;

        if (rainData != null)
        {
            factors++;
            int sensorScore = rainData.Level switch
            {
                RainLevel.Heavy => 50,
                RainLevel.Moderate => 40,
                RainLevel.Light => 25,
                _ => 0
            };

            int lightPenalty = rainData.LightLux < 100 ? 10 : (rainData.LightLux < 300 ? 5 : 0);
            sensorScore -= lightPenalty;

            score += sensorScore;
        }

        if (humidity >= 85f)
        {
            factors++;
            score += humidity >= 95 ? 25 : (humidity >= 90 ? 18 : (humidity >= 85 ? 12 : 0));
        }

        if (pressureHpa.HasValue)
        {
            factors++;
            if (pressureHpa < 1000f) score += 15;
            else if (pressureHpa < 1009f) score += 8;
            else if (pressureHpa > 1020f) score -= 5;

            float? trend = CalculatePressureTrend();
            if (trend.HasValue)
            {
                factors++;
                if (trend < -2f) score += 15;
                else if (trend < -1f) score += 10;
                else if (trend < -0.5f) score += 5;
                else if (trend > 1f) score -= 8;
            }
        }

        int? lightDip = DetectLightDip();
        if (lightDip.HasValue && rainData != null && rainData.Level != RainLevel.NoRain)
        {
            factors++;
            score += lightDip.Value switch
            {
                < -100 => 15,
                < -50 => 10,
                < -30 => 5,
                _ => 0
            };
        }

        if (humidity >= 85 && DetectPressureDrop())
        {
            score += 8;
            factors++;
        }

        if (factors == 0) return score;
        return Math.Clamp(score, 0, 100);
    }

    private float? CalculatePressureTrend()
    {
        if (_history.Count < 5) return null;

        var recent = _history.Where(h => h.PressureHpa.HasValue).TakeLast(10).ToList();
        if (recent.Count < 5) return null;

        int n = recent.Count;
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
        for (int i = 0; i < n; i++)
        {
            sumX += i;
            sumY += recent[i].PressureHpa!.Value;
            sumXY += i * recent[i].PressureHpa!.Value;
            sumX2 += i * i;
        }

        double denom = n * sumX2 - sumX * sumX;
        if (Math.Abs(denom) < 0.0001) return null;

        double slope = (n * sumXY - sumX * sumY) / denom;
        double totalMinutes = (recent.Last().Time - recent.First().Time).TotalMinutes;
        if (totalMinutes < 1) return null;

        return (float)(slope * 60.0 / totalMinutes * n);
    }

    private int? DetectLightDip()
    {
        if (_history.Count < 3) return null;

        var recent = _history.TakeLast(5).Select(h => h.LightRaw).ToList();
        if (recent.Any(l => l < 0)) return null;

        int avg = recent.Skip(1).Take(3).Sum() / 3;
        int latest = recent.Last();
        return latest - avg;
    }

    private bool DetectPressureDrop()
    {
        if (_history.Count < 5) return false;

        var recent = _history.Where(h => h.PressureHpa.HasValue).TakeLast(5).ToList();
        if (recent.Count < 3) return false;

        float oldest = recent.First().PressureHpa!.Value;
        float newest = recent.Last().PressureHpa!.Value;
        return (oldest - newest) > 2f;
    }

    private bool ShouldNotify(string type)
    {
        return type != _lastNotificationType || (DateTime.Now - _lastNotificationTime) > _cooldown;
    }

    private string BuildAlertDetails(RainData? rainData, float tempC, float humidity, float? pressureHpa, int? lightRaw, int confidence)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Confidence: {confidence}%");
        sb.AppendLine($"Time: {DateTime.Now:g}");

        if (rainData != null)
        {
            sb.AppendLine($"SEN0545 Rain: {rainData.Level}");
            sb.AppendLine($"SEN0545 Temp: {rainData.TemperatureC}°C");
            sb.AppendLine($"SEN0545 Light: {rainData.LightLux} lux");
        }
        else
        {
            sb.AppendLine("SEN0545: Not available");
        }

        sb.AppendLine($"DHT11 Temp: {tempC}°C");
        sb.AppendLine($"DHT11 Humidity: {humidity}% RH");

        if (pressureHpa.HasValue)
        {
            sb.AppendLine($"BMP280 Pressure: {pressureHpa:F1} hPa");
            float? trend = CalculatePressureTrend();
            if (trend.HasValue)
                sb.AppendLine($"Pressure Trend: {trend:F2} hPa/hr");
        }

        if (lightRaw.HasValue)
            sb.AppendLine($"Light Sensor: {lightRaw}");

        return sb.ToString();
    }

    private void SendEmail(string subject, string body)
    {
        try
        {
            string? smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");
            string? smtpUser = Environment.GetEnvironmentVariable("SMTP_USER");
            string? smtpPass = Environment.GetEnvironmentVariable("SMTP_PASS");
            string? smtpPortStr = Environment.GetEnvironmentVariable("SMTP_PORT");

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
            {
                Console.WriteLine($"📧 EMAIL WOULD BE SENT TO: {_emailTo}");
                Console.WriteLine($"Subject: {subject}");
                Console.WriteLine($"Body: {body}");
                return;
            }

            int smtpPort = int.TryParse(smtpPortStr, out var p) ? p : 587;

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUser, smtpPass)
            };

            using var msg = new MailMessage();
            msg.From = new MailAddress(smtpUser);
            msg.To.Add(_emailTo);
            msg.Subject = subject;
            msg.Body = body;

            client.Send(msg);
            Console.WriteLine($"✅ Email sent to {_emailTo}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Email error: {ex.Message}");
        }
    }

    private async Task<bool> SendTelegramAsync(string message)
    {
        try
        {
            string? botToken = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN") ?? TelegramBotToken;
            string? chatId = Environment.GetEnvironmentVariable("TELEGRAM_CHAT_ID") ?? TelegramChatId;

            if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(chatId) ||
                botToken == "YOUR_TELEGRAM_BOT_TOKEN" || chatId == "YOUR_TELEGRAM_CHAT_ID")
            {
                Console.WriteLine($"📱 TELEGRAM NOT CONFIGURED: {message}");
                return false;
            }

            var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
            var payload = new { chat_id = chatId, text = message };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Telegram message sent");
                return true;
            }
            else
            {
                Console.WriteLine($"⚠️ Telegram error: {response.StatusCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Telegram error: {ex.Message}");
            return false;
        }
    }

    public bool SendTestTelegram()
    {
        return SendTelegramAsync("🧪 Weather Nest: Test message from your weather station!\nTime: " + DateTime.Now.ToString("g")).Result;
    }

    public bool SendRainEventTelegram(string status, int confidence, float? temperature, float? humidity)
    {
        string message = status == "start"
            ? $"🌧️ RAIN EVENT STARTED\nConfidence: {confidence}%\nTime: {DateTime.Now:HH:mm}"
            : $"☀️ RAIN EVENT ENDED\nLast confidence: {confidence}%\nTime: {DateTime.Now:HH:mm}";

        return SendTelegramAsync(message).Result;
    }
}