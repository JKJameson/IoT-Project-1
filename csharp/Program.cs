using static Ffi;

class Program {
    static void Main() {
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => {
            e.Cancel = true;
            cts.Cancel();
        };

        Console.WriteLine("Initialising display...");
        using var display = new Epd();

        display.Fill(WHITE);
        display.DrawIcon(4, 4, Icons.Bell, Icons.BellW, Icons.BellH);
        display.DrawText(24, 6, "Loading...", Font.F16, BLACK, WHITE);
        display.DrawIcon(4, 30, Icons.Check, Icons.CheckW, Icons.CheckH);
        display.DrawText(24, 34, "System OK", Font.F12, BLACK, WHITE);
        display.DrawLine(0, 52, 249, 52, BLACK);
        display.DrawRect(2, 2, 248, 120, BLACK, filled: false);

        Console.WriteLine("Setting base frame...");
        display.DisplayBase();

        const ushort SensorX = 4, SensorY = 58, SensorW = 244, SensorH = 14;

        while (!cts.Token.IsCancellationRequested)
        {
            string line;
            try
            {
                var r = Dht11.Read();
                line = $"{r.TemperatureC:F1}C  {r.Humidity:F0}% RH";
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"DHT11: {e.Message}");
                line = "-- sensor error --";
            }

            display.ClearWindow(24, 6, 24 + SensorW, 6 + 16, WHITE);
            display.DrawText(24, 6, string.Format("{0:HH:mm tt}", DateTime.Now), Font.F16, BLACK, WHITE);

            display.ClearWindow(SensorX, SensorY, SensorX + SensorW, SensorY + SensorH, WHITE);
            display.DrawText(SensorX, SensorY, line, Font.F12, BLACK, WHITE);

            display.DisplayPartial();

            Console.WriteLine(line);
            cts.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
        }
    }
}
