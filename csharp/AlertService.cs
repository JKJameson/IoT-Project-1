using System.Net.Http;
using System.Text.Json;

public sealed class AlertService
{
    private readonly TimeSpan _cooldown;
    private readonly object _lock = new();
    private readonly HttpClient _http = new();

    private const string? TelegramBotToken = "8429669155:AAHfLkioktJVb0tNvA3-fqONybYaq8xp9mY";
    private const string? TelegramChatId = "-5228658452";

    private bool _isRaining;
    private bool _alertsEnabled = true;
    private DateTime _lastNotificationTime = DateTime.MinValue;
    private string _lastNotificationType = "";

    public bool IsRaining => _isRaining;
    public bool AlertsEnabled
    {
        get => _alertsEnabled;
        set
        {
            _alertsEnabled = value;
            SaveAlertsState();
        }
    }

    private string AlertsStatePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".iot-alerts-enabled");

    public void LoadAlertsState()
    {
        try
        {
            if (File.Exists(AlertsStatePath))
                _alertsEnabled = File.ReadAllText(AlertsStatePath).Trim() == "1";
        }
        catch { }
    }

    private void SaveAlertsState()
    {
        try
        {
            File.WriteAllText(AlertsStatePath, _alertsEnabled ? "1" : "0");
        }
        catch { }
    }

    public AlertService(TimeSpan? cooldown = null)
    {
        _cooldown = cooldown ?? TimeSpan.FromMinutes(5);
    }

    public (string Message, bool IsRaining) CheckWeather(RainData? rainData)
    {
        lock (_lock)
        {
            bool wasRaining = _isRaining;

            bool isNowRaining = rainData != null
                && rainData.Level != RainLevel.Clear
                && rainData.Level != RainLevel.Unknown;

            string message;
            if (isNowRaining && !wasRaining)
            {
                _isRaining = true;
                message = $"🌧️ Rain started ({rainData?.Level}) - {DateTime.Now:HH:mm:ss}";
            }
            else if (!isNowRaining && wasRaining)
            {
                _isRaining = false;
                message = $"☀️ Rain cleared - {DateTime.Now:HH:mm:ss}";

                if (_alertsEnabled && ShouldNotify("stop"))
                {
                    _lastNotificationTime = DateTime.Now;
                    _lastNotificationType = "stop";
                    SendTelegramAsync($"☀️ RAIN STOPPED").Wait();
                }
            }
            else
            {
                message = _isRaining
                    ? $"🌧️ Rain active"
                    : $"☀️ Clear";
            }

            return (message, _isRaining);
        }
    }

    private bool ShouldNotify(string type)
    {
        return type != _lastNotificationType || (DateTime.Now - _lastNotificationTime) > _cooldown;
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
        return SendTelegramAsync("🧪 Weather Nest: Test message from your weather station!").Result;
    }
}
