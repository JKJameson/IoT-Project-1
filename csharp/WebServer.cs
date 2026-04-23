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
