using static Ffi;

class Program {
    static void Main() {
        Console.WriteLine("Initialising display...");
        using var display = new Epd();

        display.Fill(WHITE);
        display.DrawIcon(4, 4, Icons.Bell, Icons.BellW, Icons.BellH);
        display.DrawText(24, 6, "Notifications", Font.F16, BLACK, WHITE);
        display.DrawIcon(4, 30, Icons.Check, Icons.CheckW, Icons.CheckH);
        display.DrawText(24, 34, "System OK", Font.F12, BLACK, WHITE);
        display.DrawLine(0, 52, 249, 52, BLACK);
        display.DrawRect(2, 2, 248, 120, BLACK, filled: false);

        Console.WriteLine("Setting base frame...");
        display.DisplayBase();

        const ushort SensorX = 4, SensorY = 58, SensorW = 244, SensorH = 14;

        while (true)
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

            display.ClearWindow(SensorX, SensorY, SensorX + SensorW, SensorY + SensorH, WHITE);
            display.DrawText(SensorX, SensorY, line, Font.F12, BLACK, WHITE);
            display.DisplayPartial();

            Console.WriteLine(line);
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }
}
