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

    private bool _isRaining;
    private DateTime _lastNotificationTime = DateTime.MinValue;
    private string _lastNotificationType = "";

    public bool IsRaining => _isRaining;

    public AlertService(string emailTo, TimeSpan? cooldown = null)
    {
        _emailTo = emailTo;
        _cooldown = cooldown ?? TimeSpan.FromMinutes(5);
    }

    public (string Message, bool IsRaining) CheckRainChange(RainPrediction prediction, float tempC, float humidity)
    {
        lock (_lock)
        {
            bool wasRaining = _isRaining;
            bool isNowRaining = prediction.ConfidencePct > 30 || prediction.Likelihood == RainLikelihood.VeryLikely;

            string message;
            if (isNowRaining && !wasRaining)
            {
                _isRaining = true;
                message = $"🌧️⚠️ RAIN STARTED! ({prediction.ConfidencePct}% confidence) - {DateTime.Now:HH:mm:ss}";

                if (ShouldNotify("start"))
                {
                    _lastNotificationTime = DateTime.Now;
                    _lastNotificationType = "start";
                    SendEmail("🌧️ RAIN STARTED at your weather station",
                        $"Rain has started!\n\nConfidence: {prediction.ConfidencePct}%\nTemperature: {tempC}°C\nHumidity: {humidity}%\nTime: {DateTime.Now:g}\n\nPlease take necessary precautions.");
                    SendTelegram($"🌧️ RAIN STARTED!\n\nConfidence: {prediction.ConfidencePct}%\nTemp: {tempC}°C | Humidity: {humidity}%\nTime: {DateTime.Now:HH:mm}");
                }
            }
            else if (!isNowRaining && wasRaining)
            {
                _isRaining = false;
                message = $"☀️ Rain stopped. Weather clearing. Last confidence: {prediction.ConfidencePct}% - {DateTime.Now:HH:mm:ss}";

                if (ShouldNotify("stop"))
                {
                    _lastNotificationTime = DateTime.Now;
                    _lastNotificationType = "stop";
                    SendEmail("☀️ RAIN STOPPED at your weather station",
                        $"Rain has stopped!\n\nLast confidence: {prediction.ConfidencePct}%\nTemperature: {tempC}°C\nHumidity: {humidity}%\nTime: {DateTime.Now:g}\n\nWeather is clearing up.");
                    SendTelegram($"☀️ RAIN STOPPED\n\nLast confidence: {prediction.ConfidencePct}%\nTemp: {tempC}°C | Humidity: {humidity}%\nTime: {DateTime.Now:HH:mm}");
                }
            }
            else
            {
                message = "🌧️ Rain monitor active: Will notify when rain starts or stops";
            }

            return (message, _isRaining);
        }
    }

    private bool ShouldNotify(string type)
    {
        return type != _lastNotificationType || (DateTime.Now - _lastNotificationTime) > _cooldown;
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

    private async void SendTelegram(string message)
    {
        try
        {
            string? botToken = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
            string? chatId = Environment.GetEnvironmentVariable("TELEGRAM_CHAT_ID");

            if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(chatId))
            {
                Console.WriteLine($"📱 TELEGRAM WOULD BE SENT: {message}");
                return;
            }

            var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
            var payload = new { chat_id = chatId, text = message };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
                Console.WriteLine("✅ Telegram message sent");
            else
                Console.WriteLine($"⚠️ Telegram error: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Telegram error: {ex.Message}");
        }
    }
}