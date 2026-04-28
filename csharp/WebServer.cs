using System.Net;
using System.Text.Json;

public sealed class WebServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly SensorData _data;
    private readonly AlertService? _alertService;
    private readonly string _htmlPath;
    private readonly CancellationTokenSource _cts = new();
    private Task? _task;

    public WebServer(SensorData data, string htmlPath, string prefix = "http://+:80/")
    {
        _data = data;
        _htmlPath = Path.GetFullPath(htmlPath);
        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
    }

    public WebServer(SensorData data, AlertService alertService, string htmlPath, string prefix = "http://+:80/")
    {
        _data = data;
        _alertService = alertService;
        _htmlPath = Path.GetFullPath(htmlPath);
        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
    }

    public void Start()
    {
        _listener.Start();
        _task = Task.Run(() => Loop(_cts.Token));
        Console.WriteLine($"Web server listening on {string.Join(", ", _listener.Prefixes)}");
    }

    private async Task Loop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try { ctx = await _listener.GetContextAsync(); }
            catch (HttpListenerException) { return; }
            catch (ObjectDisposedException) { return; }

            if (ct.IsCancellationRequested) { ctx.Response.Abort(); return; }

            _ = Handle(ctx, ct);
        }
    }

    private async Task Handle(HttpListenerContext ctx, CancellationToken ct)
    {
        var path = ctx.Request.Url?.AbsolutePath ?? "/";
        try
        {
            switch (path)
            {
                case "/api/sensors":
                    await HandleApi(ctx);
                    break;
                case "/api/test-telegram":
                    await HandleTestTelegram(ctx);
                    break;
                case "/api/send-rain-alert":
                    await HandleSendRainAlert(ctx);
                    break;
                case "/api/temperature-history":
                    await HandleTempHistory(ctx);
                    break;
                case "/":
                    await HandleFile(ctx, _htmlPath, "text/html");
                    break;
                default:
                    ctx.Response.StatusCode = 404;
                    ctx.Response.Close();
                    break;
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"HTTP: {e.Message}");
            try { ctx.Response.Abort(); } catch { }
        }
    }

    private async Task HandleApi(HttpListenerContext ctx)
    {
        var snap = _data.Take();
        var json = JsonSerializer.Serialize(snap, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        });

        ctx.Response.ContentType = "application/json";
        ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        var buf = System.Text.Encoding.UTF8.GetBytes(json);
        await ctx.Response.OutputStream.WriteAsync(buf);
        ctx.Response.Close();
    }

    private async Task HandleTestTelegram(HttpListenerContext ctx)
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");

        string response;
        if (_alertService == null)
        {
            response = JsonSerializer.Serialize(new { ok = false, error = "Alert service not available" });
        }
        else
        {
            bool sent = _alertService.SendTestTelegram();
            response = JsonSerializer.Serialize(new { ok = sent });
        }

        var buf = System.Text.Encoding.UTF8.GetBytes(response);
        await ctx.Response.OutputStream.WriteAsync(buf);
        ctx.Response.Close();
    }

    private sealed class RainAlertRequest
    {
        public string Status { get; set; } = "";
        public int Confidence { get; set; }
        public float? Temperature { get; set; }
        public float? Humidity { get; set; }
    }

    private async Task HandleSendRainAlert(HttpListenerContext ctx)
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");

        if (_alertService == null)
        {
            ctx.Response.StatusCode = 503;
            var unavailable = JsonSerializer.Serialize(new { ok = false, error = "Alert service not available" });
            var unavailableBuf = System.Text.Encoding.UTF8.GetBytes(unavailable);
            await ctx.Response.OutputStream.WriteAsync(unavailableBuf);
            ctx.Response.Close();
            return;
        }

        if (ctx.Request.HttpMethod != "POST")
        {
            ctx.Response.StatusCode = 405;
            var methodErr = JsonSerializer.Serialize(new { ok = false, error = "Use POST" });
            var methodErrBuf = System.Text.Encoding.UTF8.GetBytes(methodErr);
            await ctx.Response.OutputStream.WriteAsync(methodErrBuf);
            ctx.Response.Close();
            return;
        }

        string body;
        using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding))
            body = await reader.ReadToEndAsync();

        RainAlertRequest? payload = null;
        try { payload = JsonSerializer.Deserialize<RainAlertRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
        catch { }

        if (payload == null || (payload.Status != "start" && payload.Status != "stop"))
        {
            ctx.Response.StatusCode = 400;
            var badReq = JsonSerializer.Serialize(new { ok = false, error = "Invalid payload. status must be 'start' or 'stop'." });
            var badReqBuf = System.Text.Encoding.UTF8.GetBytes(badReq);
            await ctx.Response.OutputStream.WriteAsync(badReqBuf);
            ctx.Response.Close();
            return;
        }

        bool sent = _alertService.SendRainEventTelegram(payload.Status, payload.Confidence, payload.Temperature, payload.Humidity);
        var response = JsonSerializer.Serialize(new { ok = sent });
        var buf = System.Text.Encoding.UTF8.GetBytes(response);
        await ctx.Response.OutputStream.WriteAsync(buf);
        ctx.Response.Close();
    }

    private async Task HandleTempHistory(HttpListenerContext ctx)
    {
        var history = _data.GetTemperatureHistory(168); // 7 days
        var json = JsonSerializer.Serialize(history, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        });

        ctx.Response.ContentType = "application/json";
        ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        var buf = System.Text.Encoding.UTF8.GetBytes(json);
        await ctx.Response.OutputStream.WriteAsync(buf);
        ctx.Response.Close();
    }

    private async Task HandleFile(HttpListenerContext ctx, string path, string contentType)
    {
        if (!File.Exists(path))
        {
            ctx.Response.StatusCode = 404;
            var msg = System.Text.Encoding.UTF8.GetBytes("File not found: " + path);
            await ctx.Response.OutputStream.WriteAsync(msg);
            ctx.Response.Close();
            return;
        }

        var bytes = await File.ReadAllBytesAsync(path);
        ctx.Response.ContentType = contentType;
        await ctx.Response.OutputStream.WriteAsync(bytes);
        ctx.Response.Close();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Close();
        try { _task?.Wait(TimeSpan.FromSeconds(2)); } catch { }
        _cts.Dispose();
    }
}
